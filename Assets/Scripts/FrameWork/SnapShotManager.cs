using System;
using UnityEngine;
using Lizi.FrameWork.Util;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class SnapShotManager : MonoBehaviour
{
    private bool snapshotActive = false;
    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;
    public int SnapWidth = 0;
    public int SnapHeight = 0;

    public float alpha = 0;

    public GameObject SnapGo;

    private GameObject SaveBtn;

    private GameObject Player;
    private Material _snapMat;

    public bool InSnaping = false;

    public Tilemap BgTileMap;

    public List<Tilemap> AllTileMaps = new List<Tilemap>();

    private Vector4 _curSnapCenter = Vector4.zero;

    private Vector4 _curSnapSize = Vector4.zero;

    Canvas _canvas;

    public RenderTexture LastSnapshotRT;

    public RawImage CopyImg;

    void Awake()
    {
        EventManager.Instance.AddListener(EventType.Snapshot, OnSnapshotEvent);
        EventManager.Instance.AddListener(EventType.SnapMove, OnSnapMove);
        EventManager.Instance.AddListener(EventType.SnapMoveBegin, OnSnapMoveBegin);
        EventManager.Instance.AddListener(EventType.SnapMoveEnd, OnSnapMoveEnd);
        EventManager.Instance.AddListener(EventType.GamePadSnapMove, OnGamePadSnapMove);
        _snapMat = SnapGo.transform.Find("Mask").GetComponent<Image>().material;
        SaveBtn = SnapGo.transform.Find("SaveBtn").gameObject;
        SaveBtn.GetComponent<Button>().onClick.AddListener(OnSaveBtnClick);
        Player = GameObject.Find("Player");
        _canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        DontDestroyOnLoad(gameObject);
    }

    void Destroy()
    {
        EventManager.Instance.RemoveListener(EventType.Snapshot, OnSnapshotEvent);
        EventManager.Instance.RemoveListener(EventType.SnapMove, OnSnapMove);
        EventManager.Instance.RemoveListener(EventType.SnapMoveBegin, OnSnapMoveBegin);
        EventManager.Instance.RemoveListener(EventType.SnapMoveEnd, OnSnapMoveEnd);
        EventManager.Instance.RemoveListener(EventType.GamePadSnapMove, OnGamePadSnapMove);
    }

    private void OnSnapshotEvent(object data)
    {
        bool isEnd = false;
        if (data is bool)
        {
            isEnd = (bool)data;
        }

        if (isEnd)
        {
            HandleSnapshotEnd();
        }
        else
        {
            HandleSnapshotStart();
        }
    }

    private void HandleSnapshotStart()
    {
        if (snapshotActive)
        {
            return;
        }
        snapshotActive = true;
        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        InSnaping = true;
        PauseGame();
        // Debug.Log("[SnapShotManager] Snapshot Start -> Pause Game");
        _snapMat.SetColor("_Color", new Color(0, 0, 0, alpha));
        float orthoHeight = Camera.main.orthographicSize * 2f;
        float orthoWidth = orthoHeight * Camera.main.aspect;
        float percentageX = Mathf.Clamp01(SnapWidth / orthoWidth);
        float percentageY = Mathf.Clamp01(SnapHeight / orthoHeight);
        _curSnapSize = new Vector4(percentageX, percentageY, 0, 0);
        _snapMat.SetVector("_HoleSize", _curSnapSize);
        if (Player != null && Camera.main != null)
        {
            RefreshCurSnapShotTiles(Player.transform.position + new Vector3(0, 1.05f, 0));
        }
        EnableGridShow(true);
        SnapGo.SetActive(true);
        RefreshSaveBtn(true);
    }

    public Vector3Int GetPlayerTopCurCell()
    {
        if (Player != null && Camera.main != null)
        {
            float offsetY = 2.1f + (int)SnapHeight / 2;
            return GetCellAtPosition(BgTileMap, Player.transform.position + new Vector3(0, offsetY, 0));
        }
        return Vector3Int.zero;
    }

    private void RefreshCurSnapShotTiles(Vector3 centerPos)
    {
        if (InSnaping)
        {
            // 获取Player当前世界坐标
            Vector3Int cell = GetCellAtPosition(BgTileMap, centerPos);
            Vector3 cellCenter = BgTileMap.GetCellCenterWorld(cell);
            // GetSnapshotTiles(cell);
            float xOffset = SnapWidth % 2 == 0 ? 0.5f : 0;
            float yOffset = SnapHeight % 2 == 0 ? 0.5f : 0;
            // 将世界坐标转为视口坐标（0~1的百分比）
            cellCenter = cellCenter + new Vector3(xOffset, yOffset, 0);
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(cellCenter);
            // 保证z在前面视口内
            float playerViewportX = Mathf.Clamp01(viewportPos.x);
            float playerViewportY = Mathf.Clamp01(viewportPos.y);
            _curSnapCenter = new Vector4(playerViewportX, playerViewportY, 0, 0);
            // 设置材质洞中心为玩家当前位置（百分比）
            _snapMat.SetVector("_HoleCenter", _curSnapCenter);
        }
    }

    private void HandleSnapshotEnd()
    {
        if (!snapshotActive)
        {
            return;
        }
        snapshotActive = false;
        ResumeGame();
        // Debug.Log("[SnapShotManager] Snapshot End -> Resume Game");
        SnapGo.SetActive(false);
        EnableGridShow(false);
        InSnaping = false;
    }

    public void EnableGridShow(bool show)
    {
        BgTileMap.GetComponent<TilemapGridLinesDrawer>().enabled = show;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime > 0f ? previousFixedDeltaTime : 0.02f;
    }

    /// 角色当前所处的“格坐标”（含z=tilemap的z）
    public static Vector3Int GetCellAtPosition(Tilemap tilemap, Vector3 worldPos)
    {
        var cell = tilemap.WorldToCell(worldPos);
        cell.z = tilemap.origin.z;
        return cell;
    }

    void OnSnapMove(object data)
    {
        MoveData moveData = (MoveData)data;
        if (InSnaping)
        {
            Vector2 viewportDelta = new Vector2(
                moveData.moveDir.x / Camera.main.pixelWidth,
                moveData.moveDir.y / Camera.main.pixelHeight
            );
            _curSnapCenter.x = _curSnapCenter.x + viewportDelta.x;
            _curSnapCenter.x = Mathf.Clamp01(_curSnapCenter.x);
            _curSnapCenter.y = _curSnapCenter.y + viewportDelta.y;
            _curSnapCenter.y = Mathf.Clamp01(_curSnapCenter.y);
            _snapMat.SetVector("_HoleCenter", _curSnapCenter);
            RefreshSaveBtn(false);
        }
    }

    void OnSnapMoveBegin(object data)
    {

    }

    void OnSnapMoveEnd(object data)
    {
        XFTile();
        RefreshSaveBtn(true);
    }

    void RefreshSaveBtn(bool show)
    {
        RectTransform btnRT = SaveBtn.GetComponent<RectTransform>();
        RectTransform canvasRT = _canvas.transform as RectTransform;

        // 计算洞中心在 Canvas 局部坐标
        Vector3 centerScreen = Camera.main.ViewportToScreenPoint(new Vector3(_curSnapCenter.x, _curSnapCenter.y, 0));
        Vector2 centerLocal;
        Camera uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, centerScreen, uiCam, out centerLocal);

        // 洞的尺寸（Canvas 局部单位）
        Rect canvasRect = canvasRT.rect;
        float holeW = _curSnapSize.x * canvasRect.width;
        float holeH = _curSnapSize.y * canvasRect.height;
        float holeHalfW = holeW * 0.5f;
        float holeHalfH = holeH * 0.5f;

        // 按钮尺寸/边距/额外偏移
        Vector2 btnSize = btnRT.rect.size;
        float halfW = btnSize.x * 0.5f;
        float halfH = btnSize.y * 0.5f;
        float margin = 16f;
        float scale = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
        float extra = 30f / scale;

        // 洞在 Canvas 下的外侧中点
        Vector2 rightPos = new Vector2(centerLocal.x + holeHalfW + (halfW + margin + extra), Mathf.Clamp(centerLocal.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH));
        Vector2 leftPos = new Vector2(centerLocal.x - holeHalfW - (halfW + margin + extra), Mathf.Clamp(centerLocal.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH));
        Vector2 topPos = new Vector2(Mathf.Clamp(centerLocal.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW), centerLocal.y + holeHalfH + (halfH + margin + extra));
        Vector2 bottomPos = new Vector2(Mathf.Clamp(centerLocal.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW), centerLocal.y - holeHalfH - (halfH + margin + extra));

        bool Fits(Vector2 p)
        {
            return p.x - halfW >= canvasRect.xMin && p.x + halfW <= canvasRect.xMax && p.y - halfH >= canvasRect.yMin && p.y + halfH <= canvasRect.yMax;
        }

        // 右->左->上->下 选择
        Vector2 chosen = rightPos;
        if (!Fits(chosen))
        {
            if (Fits(leftPos)) chosen = leftPos;
            else if (Fits(topPos)) chosen = topPos;
            else if (Fits(bottomPos)) chosen = bottomPos;
            else
            {
                chosen.x = Mathf.Clamp(chosen.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW);
                chosen.y = Mathf.Clamp(chosen.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH);
            }
        }

        // 设置位置与显示
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = chosen;

        SaveBtn.SetActive(show);
    }

    void XFTile()
    {
        // Debug.Log($"吸附截图 当前视口位置 {_curSnapCenter}");
        // 视口中心位置转世界坐标
        Vector3 viewportCenterWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(_curSnapCenter.x, _curSnapCenter.y, Camera.main.nearClipPlane));
        // Debug.Log($"视口中心世界坐标: {viewportCenterWorldPos}");
        RefreshCurSnapShotTiles(viewportCenterWorldPos);
    }

    void OnGamePadSnapMove(object data)
    {
        CardinalDir dir = (CardinalDir)data;
        if (InSnaping)
        {
            switch (dir)
            {
                case CardinalDir.Up:
                    _curSnapCenter.y = _curSnapCenter.y + OrthoViewportUtil.UnitToViewportHeight();
                    break;
                case CardinalDir.Down:
                    _curSnapCenter.y = _curSnapCenter.y - OrthoViewportUtil.UnitToViewportHeight();
                    break;
                case CardinalDir.Right:
                    _curSnapCenter.x = _curSnapCenter.x + OrthoViewportUtil.UnitToViewportWidth();
                    break;
                case CardinalDir.Left:
                    _curSnapCenter.x = _curSnapCenter.x - OrthoViewportUtil.UnitToViewportWidth();
                    break;
            }
            _curSnapCenter.x = Mathf.Clamp01(_curSnapCenter.x);
            _curSnapCenter.y = Mathf.Clamp01(_curSnapCenter.y);
            _snapMat.SetVector("_HoleCenter", _curSnapCenter);
            XFTile();
        }
    }

    void OnSaveBtnClick()
    {
        if (InSnaping)
        {
            Debug.Log("保存当前截取内容！");
            StartCoroutine(CaptureViewportAndEnd());
        }
    }

    public Dictionary<Tilemap, List<SaveTileData>> curSnapshotTiles = new Dictionary<Tilemap, List<SaveTileData>>();
    void SnapshotTiles()
    {
        curSnapshotTiles.Clear();
        Vector3 centerPos = Camera.main.ViewportToWorldPoint(new Vector3(_curSnapCenter.x, _curSnapCenter.y, Camera.main.nearClipPlane));
        Vector3Int bgCenterCell = GetCellAtPosition(BgTileMap, centerPos);

        List<SnapPos> cellList = GetSnapShotPos(bgCenterCell);

        for (int j = 0; j < AllTileMaps.Count; j++)
        {
            Tilemap tilemap = AllTileMaps[j];
            if (!curSnapshotTiles.ContainsKey(tilemap))
            {
                curSnapshotTiles[tilemap] = new List<SaveTileData>();
            }

            for (int i = 0; i < cellList.Count; i++)
            {
                SnapPos sp = cellList[i];
                TileBase baseTile = tilemap.GetTile(sp.cell);
                if (baseTile)
                {
                    SaveTileData std = new SaveTileData();
                    std.tile = baseTile;
                    std.pos = sp;
                    curSnapshotTiles[tilemap].Add(std);
                    Debug.Log($"选取保存 tilemap: {tilemap.name}, cell: {sp.cell}, offset: {sp.offset}, baseTile: {baseTile.name}");
                }
            }
            Debug.Log($"选取保存 tilemap: {tilemap.name} ------------------------");
        }
    }

    List<SnapPos> GetSnapShotPos(Vector3Int centerCell)
    {
        List<SnapPos> posList = new List<SnapPos>();
        Debug.Log($"center cell {centerCell}");

        int beginX = Mathf.FloorToInt(SnapWidth / 2f) * -1;
        int endX = Mathf.FloorToInt(SnapWidth / 2f);

        int beginY = Mathf.FloorToInt(SnapHeight / 2f) * -1;
        int endY = Mathf.FloorToInt(SnapHeight / 2f);

        for (int y = beginY; y <= endY; y++)
        {
            for (int x = beginX; x <= endX; x++)
            {
                Vector3Int cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                SnapPos sp = new SnapPos();
                sp.cell = cell;
                sp.offset = new Vector3(x, y, 0);
                // Debug.Log($"cell list {cell}");
                posList.Add(sp);
            }
        }

        // Debug.Log($"beginX: {beginX}, endX: {endX}, beginY: {beginY}, endY: {endY}");

        return posList;
    }

    IEnumerator CaptureViewportAndEnd()
    {
        SnapshotTiles();
        yield return StartCoroutine(CaptureViewportToRTCoroutine());
        HandleSnapshotEnd();
    }

    public void CaptureViewportToRenderTexture(Action<RenderTexture> onDone = null)
    {
        StartCoroutine(CaptureViewportToRTCoroutine(onDone));
    }

    IEnumerator CaptureViewportToRTCoroutine(Action<RenderTexture> onDone = null)
    {
        // 临时关闭网格线绘制，避免被截入
        TilemapGridLinesDrawer drawer = null;
        bool prevEnabled = false;
        if (BgTileMap != null)
        {
            drawer = BgTileMap.GetComponent<TilemapGridLinesDrawer>();
            if (drawer != null)
            {
                prevEnabled = drawer.enabled;
                drawer.enabled = false;
            }
        }

        yield return new WaitForEndOfFrame();

        Rect pixelRect = ComputeCapturePixelRect();
        int w = Mathf.Max(1, Mathf.RoundToInt(pixelRect.width));
        int h = Mathf.Max(1, Mathf.RoundToInt(pixelRect.height));

        Texture2D temp = new Texture2D(w, h, TextureFormat.RGBA32, false);
        temp.ReadPixels(new Rect(pixelRect.x, pixelRect.y, w, h), 0, 0, false);
        temp.Apply(false, false);

        if (LastSnapshotRT != null)
        {
            if (LastSnapshotRT.IsCreated()) LastSnapshotRT.Release();
            Destroy(LastSnapshotRT);
        }
        LastSnapshotRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        LastSnapshotRT.Create();

        Graphics.Blit(temp, LastSnapshotRT);
        Destroy(temp);

        UpdateCopyImgSizeAndTexture(w, h);

        // 恢复网格线绘制到之前状态
        if (drawer != null)
        {
            drawer.enabled = prevEnabled;
        }

        if (onDone != null)
        {
            onDone(LastSnapshotRT);
        }
    }

    Rect ComputeCapturePixelRect()
    {
        float halfW = _curSnapSize.x * 0.5f;
        float halfH = _curSnapSize.y * 0.5f;
        float startX01 = Mathf.Clamp01(_curSnapCenter.x - halfW);
        float startY01 = Mathf.Clamp01(_curSnapCenter.y - halfH);
        float endX01 = Mathf.Clamp01(_curSnapCenter.x + halfW);
        float endY01 = Mathf.Clamp01(_curSnapCenter.y + halfH);

        int x = Mathf.RoundToInt(startX01 * Screen.width);
        int y = Mathf.RoundToInt(startY01 * Screen.height);
        int w = Mathf.RoundToInt(Mathf.Max(1e-3f, (endX01 - startX01)) * Screen.width);
        int h = Mathf.RoundToInt(Mathf.Max(1e-3f, (endY01 - startY01)) * Screen.height);

        return new Rect(x, y, w, h);
    }

    void UpdateCopyImgSizeAndTexture(int pixelWidth, int pixelHeight)
    {
        if (CopyImg == null || LastSnapshotRT == null)
        {
            return;
        }
        CopyImg.texture = LastSnapshotRT;
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;
        RectTransform rt = CopyImg.rectTransform;
        rt.sizeDelta = new Vector2(pixelWidth / scale, pixelHeight / scale);
        // if (!CopyImg.gameObject.activeSelf)
        // {
        //     CopyImg.gameObject.SetActive(true);
        // }
    }

}

public static class OrthoViewportUtil
{
    public static float UnitToViewportHeight(float units = 1f)
        => units / (2f * Camera.main.orthographicSize);

    public static float UnitToViewportWidth(float units = 1f)
        => units / (2f * Camera.main.orthographicSize * Camera.main.aspect);
}

public class SaveTileData
{
    public TileBase tile;

    public SnapPos pos;

}

public class SnapPos
{
    public Vector3 offset;

    public Vector3Int cell;
}
