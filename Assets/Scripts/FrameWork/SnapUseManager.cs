using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class SnapUseManager : MonoBehaviour
{

    private SnapShotManager _shotManager;

    private bool _inSnapUse;

    public RawImage SnapCopy;

    private Canvas _canvas;
    private RectTransform _snapRT;
    private float _originalAlpha = 1f;

    public Button ConfirmBtn;

    void Awake()
    {
        _shotManager = GetComponent<SnapShotManager>();
        EventManager.Instance.AddListener(EventType.SnapUse, OnSnapUse);
        EventManager.Instance.AddListener(EventType.SnapMoveBegin, OnSnapMoveBegin);
        EventManager.Instance.AddListener(EventType.SnapMove, OnSnapMove);
        EventManager.Instance.AddListener(EventType.SnapMoveEnd, OnSnapMoveEnd);

        if (SnapCopy != null)
        {
            _snapRT = SnapCopy.rectTransform;
            _canvas = SnapCopy.canvas;
        }

        ConfirmBtn.onClick.AddListener(OnConfirmBtnClick);
    }

    private List<GameObject> _snapCloneElements = new List<GameObject>();
    private Dictionary<Tilemap, List<SaveTileData>> _lastTiles = new Dictionary<Tilemap, List<SaveTileData>>();

    void OnConfirmBtnClick()
    {

        // 清理之前得复制体
        for(int i = 0; i < _snapCloneElements.Count; i++)
        {
            GameObject obj = _snapCloneElements[i];
            if(obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        _snapCloneElements.Clear();

        //恢复 tile 
        foreach (var kvp in _lastTiles)
        {
            var tileMap = kvp.Key;
            foreach (var std in kvp.Value)
            {
                tileMap.SetTile(std.pos.cell, std.tile);
            }
            tileMap.RefreshAllTiles();
        }
        _lastTiles.Clear();

        Debug.Log($"复制截图元素 中心点位置 {_snapMoveEndCell}");
        // 先处理Tile
        foreach (var kvp in _shotManager.curSnapshotTiles)
        {
            var tileMap = kvp.Key;
            if(!_lastTiles.ContainsKey(tileMap))
            {
                _lastTiles[tileMap] = new List<SaveTileData>();
            }
            // 这里可以加入你处理key的逻辑
            foreach (var std in kvp.Value)
            {
                Vector3Int setCell = _snapMoveEndCell + new Vector3Int((int)std.pos.offset.x, (int)std.pos.offset.y, 0);

                TileBase curTile = tileMap.GetTile(setCell);
                SaveTileData curStd = new SaveTileData();
                curStd.tile = curTile;
                curStd.pos = new SnapPos();
                curStd.pos.cell = setCell;
                _lastTiles[tileMap].Add(curStd);

                tileMap.SetTile(setCell, std.tile);
            }
            tileMap.RefreshAllTiles();                  // 或针对范围 RefreshTile
        }
        _shotManager.curSnapshotTiles.Clear();

        // 在处理元素
        for(int i = 0; i < _shotManager.curSnapshotElements.Count; i++)
        {
            SaveElementData sed = _shotManager.curSnapshotElements[i];
            Vector3 centerPos = _shotManager.BgTileMap.GetCellCenterWorld(_snapMoveEndCell) + sed.offset;
            var clone = Instantiate(sed.obj);
            clone.name = sed.obj.name + "_snap_clone";
            clone.transform.position = centerPos;
            _snapCloneElements.Add(clone);
        }
        _shotManager.curSnapshotElements.Clear();
        OnSnapUse(true);
    }

    void Destroy()
    {
        EventManager.Instance.RemoveListener(EventType.SnapUse, OnSnapUse);
        EventManager.Instance.RemoveListener(EventType.SnapMoveBegin, OnSnapMoveBegin);
        EventManager.Instance.RemoveListener(EventType.SnapMove, OnSnapMove);
        EventManager.Instance.RemoveListener(EventType.SnapMoveEnd, OnSnapMoveEnd);
    }

    void OnSnapMove(object data)
    {
        MoveData moveData = (MoveData)data;
        if (_inSnapUse)
        {
            if (_snapRT == null && SnapCopy != null) _snapRT = SnapCopy.rectTransform;
            if (_canvas == null && SnapCopy != null) _canvas = SnapCopy.canvas;
            if (_snapRT == null) return;

            float scale = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
            Vector2 deltaUI = moveData.moveDir / scale;
            _snapRT.anchoredPosition += deltaUI;
        }
    }

    void OnSnapMoveBegin(object data)
    {
        if (_inSnapUse && SnapCopy != null)
        {
            _originalAlpha = SnapCopy.color.a;
            Color c = SnapCopy.color;
            c.a = 0.8f;
            SnapCopy.color = c;
        }
        // ConfirmBtn.gameObject.SetActive(false);
    }

    private Vector3Int _snapMoveEndCell;
    private Vector3 _snapMoveEndCellCenterWorld;
    void OnSnapMoveEnd(object data)
    {
        if (_inSnapUse)
        {
            if (_shotManager == null || _shotManager.BgTileMap == null || _snapRT == null)
            {
                return;
            }

            // 目标：将 SnapCopy 的中心对齐到 Tilemap 的最近格子中心
            // 计算 Tilemap 最近格子的世界坐标
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(
                _canvas != null ? _canvas.worldCamera : null,
                _snapRT.TransformPoint(_snapRT.rect.center)
            );
            Vector3 worldFromUI = Camera.main.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, Camera.main.nearClipPlane));

            Vector3Int cell = SnapShotManager.GetCellAtPosition(_shotManager.BgTileMap, worldFromUI);
            _snapMoveEndCell = cell;
            Vector3 cellCenterWorld = _shotManager.BgTileMap.GetCellCenterWorld(cell);
            _snapMoveEndCellCenterWorld = cellCenterWorld;

            // 世界 -> 屏幕 -> 父 RectTransform 本地坐标
            Vector2 screenTarget = Camera.main.WorldToScreenPoint(cellCenterWorld);
            RectTransform parentRT = _snapRT.parent as RectTransform;
            if (parentRT == null)
            {
                return;
            }
            Camera uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenTarget, uiCam, out localPoint))
            {
                _snapRT.anchoredPosition = localPoint;
            }

            // 恢复透明度
            if (SnapCopy != null)
            {
                Color c = SnapCopy.color;
                c.a = _originalAlpha;
                SnapCopy.color = c;
            }

            // 放置确认按钮于合适位置（不遮挡 RT）并保证在屏幕内
            PositionConfirmButton();
        }
        // ConfirmBtn.gameObject.SetActive(true);
    }

    void PositionConfirmButton()
    {
        if (ConfirmBtn == null || SnapCopy == null)
        {
            return;
        }

        RectTransform btnRT = ConfirmBtn.transform as RectTransform;
        RectTransform container = btnRT.parent as RectTransform;
        if (container == null)
        {
            return;
        }

        // 统一使用居中锚点/枢轴，便于计算与边界判断
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.pivot = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;

        // 基于 Canvas 空间计算，确保“在屏内”的判断正确
        if (_canvas == null)
        {
            _canvas = SnapCopy.canvas;
            if (_canvas == null) return;
        }
        RectTransform canvasRT = _canvas.transform as RectTransform;

        RectTransform snapCopyRT = SnapCopy.rectTransform;
        Bounds bCanvas = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRT, snapCopyRT);

        Vector2 btnSize = btnRT.rect.size;
        float halfW = btnSize.x * 0.5f;
        float halfH = btnSize.y * 0.5f;
        float margin = 16f; // 与图片保持的最小边距
        float scale = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
        // 仅当按钮当前位置尚未放置（或首次调用）时应用额外偏移
        bool firstPlacement = btnRT.anchoredPosition == Vector2.zero;
        float extra = firstPlacement ? (20f / scale) : 0f;
        Rect canvasRect = canvasRT.rect;

        // 备选位置（右、左、上、下），默认右侧中间（均在 Canvas 坐标系下）
        Vector2 rightCanvas = new Vector2(bCanvas.max.x + (halfW + margin + extra), Mathf.Clamp(bCanvas.center.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH));
        Vector2 leftCanvas = new Vector2(bCanvas.min.x - (halfW + margin + extra), Mathf.Clamp(bCanvas.center.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH));
        Vector2 topCanvas = new Vector2(Mathf.Clamp(bCanvas.center.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW), bCanvas.max.y + (halfH + margin + extra));
        Vector2 bottomCanvas = new Vector2(Mathf.Clamp(bCanvas.center.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW), bCanvas.min.y - (halfH + margin + extra));

        bool FitsInCanvas(Vector2 p)
        {
            return p.x - halfW >= canvasRect.xMin && p.x + halfW <= canvasRect.xMax && p.y - halfH >= canvasRect.yMin && p.y + halfH <= canvasRect.yMax;
        }

        Vector2 chosenCanvas = rightCanvas;
        if (!FitsInCanvas(chosenCanvas))
        {
            if (FitsInCanvas(leftCanvas)) chosenCanvas = leftCanvas;
            else if (FitsInCanvas(topCanvas)) chosenCanvas = topCanvas;
            else if (FitsInCanvas(bottomCanvas)) chosenCanvas = bottomCanvas;
            else
            {
                chosenCanvas.x = Mathf.Clamp(chosenCanvas.x, canvasRect.xMin + halfW, canvasRect.xMax - halfW);
                chosenCanvas.y = Mathf.Clamp(chosenCanvas.y, canvasRect.yMin + halfH, canvasRect.yMax - halfH);
            }
        }

        // 将 Canvas 局部坐标转换为容器局部坐标
        Vector3 world = canvasRT.TransformPoint(chosenCanvas);
        Vector3 localInContainer = container.InverseTransformPoint(world);
        btnRT.anchoredPosition = new Vector2(localInContainer.x, localInContainer.y);
    }

    void OnSnapUse(object data)
    {
        bool isEnd = false;
        if (data is bool)
        {
            isEnd = (bool)data;
        }

        if (isEnd)
        {
            Clear();
            _shotManager.ResumeGame();
            // 恢复透明度（防止未触发 MoveEnd 的情况）
            if (SnapCopy != null)
            {
                Color c = SnapCopy.color;
                c.a = _originalAlpha;
                SnapCopy.color = c;
            }
            _inSnapUse = false;
        }
        else
        {
            if (_shotManager.curSnapshotTiles.Count > 0)
            {
                _shotManager.PauseGame();
                ShowTileCopy();
                if (SnapCopy != null)
                {
                    SnapCopy.texture = _shotManager.LastSnapshotRT;
                    if (_snapRT == null) _snapRT = SnapCopy.rectTransform;
                    if (_canvas == null) _canvas = SnapCopy.canvas;
                    _originalAlpha = SnapCopy.color.a;
                }
                _inSnapUse = true;
                PositionConfirmButton();
                //  ConfirmBtn.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("当前没有可以使用的截图");
            }
        }
    }

    void ShowTileCopy()
    {
        _shotManager.EnableGridShow(true);
        SnapCopy.gameObject.SetActive(true);
    }

    void Clear()
    {
        _shotManager.EnableGridShow(false);
        SnapCopy.gameObject.SetActive(false);
    }
}
