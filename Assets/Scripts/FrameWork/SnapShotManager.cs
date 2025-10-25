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

    Canvas _canvas;

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
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        // Debug.Log("[SnapShotManager] Snapshot Start -> Pause Game");
        _snapMat.SetColor("_Color", new Color(0, 0, 0, alpha));
        float orthoHeight = Camera.main.orthographicSize * 2f;
        float orthoWidth = orthoHeight * Camera.main.aspect;
        float percentageX = Mathf.Clamp01(SnapWidth / orthoWidth);
        float percentageY = Mathf.Clamp01(SnapHeight / orthoHeight);
        _snapMat.SetVector("_HoleSize", new Vector4(percentageX, percentageY, 0, 0));
        if (Player != null && Camera.main != null)
        {
            RefreshCurSnapShotTiles(Player.transform.position + new Vector3(0, 1.05f, 0));
        }
        BgTileMap.GetComponent<TilemapGridLinesDrawer>().enabled = true;
        SnapGo.SetActive(true);
        RefreshSaveBtn(true);
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

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime > 0f ? previousFixedDeltaTime : 0.02f;
        // Debug.Log("[SnapShotManager] Snapshot End -> Resume Game");
        SnapGo.SetActive(false);
        BgTileMap.GetComponent<TilemapGridLinesDrawer>().enabled = false;
        InSnaping = false;
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
        Vector3 viewportCenterWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(_curSnapCenter.x, _curSnapCenter.y, Camera.main.nearClipPlane));
        Vector3 screen = Camera.main.WorldToScreenPoint(viewportCenterWorldPos);
        RectTransform rt = SaveBtn.GetComponent<RectTransform>();
        RectTransform canvasRT = _canvas.transform as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, null, out localPoint);

        float xOffsetUnit = SnapWidth / 2f;
        float viewPointwdith = OrthoViewportUtil.UnitToViewportWidth(xOffsetUnit);
        float viewPointHeightPixel = viewPointwdith * ((RectTransform)_canvas.transform).rect.width + 10;

        Vector2 pixelOffset = new Vector2(viewPointHeightPixel + 80, 0);
        rt.anchoredPosition = localPoint + pixelOffset;

        SaveBtn.SetActive(show);
        // SaveBtn.transform.position = new Vector3(_curSnapCenter.x, _curSnapCenter.y, 0);
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
            SnapshotTiles();
            HandleSnapshotEnd();
        }
    }

    Dictionary<Tilemap, List<TileBase>> curSnapshotTiles = new Dictionary<Tilemap, List<TileBase>>();
    void SnapshotTiles()
    {
        curSnapshotTiles.Clear();
        Vector3 centerPos = Camera.main.ViewportToWorldPoint(new Vector3(_curSnapCenter.x, _curSnapCenter.y, Camera.main.nearClipPlane));
        Vector3Int bgCenterCell = GetCellAtPosition(BgTileMap, centerPos);

        List<Vector3Int> cellList = GetSnapShotPos(bgCenterCell);

        for (int j = 0; j < AllTileMaps.Count; j++)
        {
            Tilemap tilemap = AllTileMaps[j];
            if (!curSnapshotTiles.ContainsKey(tilemap))
            {
                curSnapshotTiles[tilemap] = new List<TileBase>();
            }

            for (int i = 0; i < cellList.Count; i++)
            {
                Vector3Int cell = cellList[i];
                TileBase baseTile = tilemap.GetTile(cell);
                if (baseTile)
                {
                    curSnapshotTiles[tilemap].Add(baseTile);
                    Debug.Log($"选取保存 tilemap: {tilemap.name}, cell: {cell}, baseTile: {baseTile.name}");
                }
            }
            Debug.Log($"选取保存 tilemap: {tilemap.name} ------------------------");
        }
    }

    List<Vector3Int> GetSnapShotPos(Vector3Int centerCell)
    {
        List<Vector3Int> posList = new List<Vector3Int>();
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
                Debug.Log($"cell list {cell}");
                posList.Add(cell);
            }
        }

        Debug.Log($"beginX: {beginX}, endX: {endX}, beginY: {beginY}, endY: {endY}");

        return posList;
    }

}

public static class OrthoViewportUtil
{
    public static float UnitToViewportHeight(float units = 1f)
        => units / (2f * Camera.main.orthographicSize);

    public static float UnitToViewportWidth(float units = 1f)
        => units / (2f * Camera.main.orthographicSize * Camera.main.aspect);
}
