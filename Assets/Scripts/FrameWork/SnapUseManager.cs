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

    public Button RotateBtn;

    public GameObject SnapCopyRoot;

    void Awake()
    {
        _shotManager = GetComponent<SnapShotManager>();
        EventManager.Instance.AddListener(EventType.SnapUse, OnSnapUse);
        EventManager.Instance.AddListener(EventType.SnapMoveBegin, OnSnapMoveBegin);
        EventManager.Instance.AddListener(EventType.SnapMove, OnSnapMove);
        EventManager.Instance.AddListener(EventType.SnapMoveEnd, OnSnapMoveEnd);

        if (SnapCopy != null)
        {
            _snapRT = SnapCopyRoot.transform as RectTransform;
            _canvas = SnapCopy.canvas;
            Debug.Log("SnapCopyRoot: " + _canvas);
        }

        ConfirmBtn.onClick.AddListener(OnConfirmBtnClick);
        RotateBtn.onClick.AddListener(OnRotateBtnClick);
    }

    private List<GameObject> _snapCloneElements = new List<GameObject>();
    private Dictionary<Tilemap, List<SaveTileData>> _lastTiles = new Dictionary<Tilemap, List<SaveTileData>>();

    private int _curRotate = 0;

    void OnRotateBtnClick()
    {
        if (SnapCopy != null)
        {
            RectTransform rt = SnapCopy.transform as RectTransform;
            // 顺时针旋转90°
            rt.Rotate(0f, 0f, -90f);
            if (rt != null)
            {
                _curRotate = Mathf.RoundToInt(rt.eulerAngles.z);
                Debug.Log($"当前旋转角度 {_curRotate}");
            }
        }
    }

    void DealCopyTiles()
    {
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
            if (!_lastTiles.ContainsKey(tileMap))
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
    }

    void DealCopyElements()
    {
        // 在处理元素
        List<GameObject> newSnapCloneElements = new List<GameObject>();
        for (int i = 0; i < _shotManager.curSnapshotElements.Count; i++)
        {
            SaveElementData sed = _shotManager.curSnapshotElements[i];
            Vector3 centerPos = _shotManager.BgTileMap.GetCellCenterWorld(_snapMoveEndCell) + sed.offset;
            var clone = Instantiate(sed.obj);
            clone.name = sed.obj.name + "_snap_clone";
            clone.transform.position = centerPos;
            newSnapCloneElements.Add(clone);
        }
        _shotManager.curSnapshotElements.Clear();

        // 清理之前得复制体
        for (int i = 0; i < _snapCloneElements.Count; i++)
        {
            GameObject obj = _snapCloneElements[i];
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        _snapCloneElements.Clear();
        _snapCloneElements = newSnapCloneElements;
    }

    void OnConfirmBtnClick()
    {
        DealCopyTiles();
        DealCopyElements();
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
            if (_snapRT == null && SnapCopy != null) _snapRT = SnapCopyRoot.transform as RectTransform;
            if (_canvas == null && SnapCopy != null) _canvas = SnapCopy.canvas;
            if (_snapRT == null) return;

            float scale = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
            Vector2 deltaUI = moveData.moveDir / scale;
            _snapRT.anchoredPosition += deltaUI;

            ConfirmBtn.gameObject.SetActive(false);
            RotateBtn.gameObject.SetActive(false);
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
            ConfirmBtn.gameObject.SetActive(true);
            RotateBtn.gameObject.SetActive(true);
            PositionConfirmButton();
            PositionRotateButton();
        }
        // ConfirmBtn.gameObject.SetActive(true);
    }

    void PositionConfirmButton()
    {
        if (ConfirmBtn == null || SnapCopy == null)
        {
            return;
        }

        if (_snapRT == null) _snapRT = SnapCopyRoot.transform as RectTransform;
        if (_canvas == null) _canvas = SnapCopy.canvas;

        RectTransform btnRT = ConfirmBtn.GetComponent<RectTransform>();
        if (btnRT == null) return;

        Canvas btnCanvas = ConfirmBtn.GetComponentInParent<Canvas>();
        if (btnCanvas == null) btnCanvas = _canvas;
        RectTransform btnParentRT = btnRT.parent as RectTransform;
        if (btnParentRT == null) return;

        // 计算 SnapCopy 在屏幕空间的左右边与中心Y
        Vector3[] snapCorners = new Vector3[4];
        _snapRT.GetWorldCorners(snapCorners);
        Camera uiCamSnap = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        Vector3 tr = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[2]); // 右上
        Vector3 br = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[3]); // 右下
        Vector3 tl = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[1]); // 左上
        Vector3 bl = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[0]); // 左下

        float rightX = Mathf.Max(tr.x, br.x);
        float leftX = Mathf.Min(tl.x, bl.x);
        Vector2 snapCenterScreen = RectTransformUtility.WorldToScreenPoint(
            uiCamSnap,
            _snapRT.TransformPoint(_snapRT.rect.center)
        );

        // 按钮在屏幕空间的半宽/半高（像素）与边距
        float btnScale = (btnCanvas != null && btnCanvas.scaleFactor > 0f) ? btnCanvas.scaleFactor : 1f;
        float halfW = btnRT.rect.width * 0.5f * btnScale;
        float halfH = btnRT.rect.height * 0.5f * btnScale;
        const float margin = 30f;

        // 先默认放在右侧外 10px，垂直居中于 SnapCopy
        float targetX = rightX + margin + halfW;
        float targetY = snapCenterScreen.y;

        // 如果右边越界，改到左侧外 10px
        if (targetX + halfW > Screen.width)
        {
            targetX = leftX - margin - halfW;
        }

        // 垂直方向自适应：上越界向下、下越界向上
        if (targetY + halfH > Screen.height)
        {
            targetY = Screen.height - halfH;
        }
        else if (targetY - halfH < 0f)
        {
            targetY = halfH;
        }

        // 兜底：若左侧仍越界，夹紧到屏幕内
        if (targetX - halfW < 0f)
        {
            targetX = halfW;
        }

        // 将屏幕中心点转换为按钮父节点本地坐标
        Camera uiCamBtn = (btnCanvas != null && btnCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? btnCanvas.worldCamera : null;
        Vector2 localCenter;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(btnParentRT, new Vector2(targetX, targetY), uiCamBtn, out localCenter))
        {
            // 将中心点转换为以 pivot 为锚点的 anchoredPosition
            Vector2 size = btnRT.rect.size;
            Vector2 pivot = btnRT.pivot;
            Vector2 anchored = localCenter + new Vector2((pivot.x - 0.5f) * size.x, (pivot.y - 0.5f) * size.y);
            btnRT.anchoredPosition = anchored;
        }
    }

    void PositionRotateButton()
    {
        if (RotateBtn == null || SnapCopy == null)
        {
            return;
        }

        if (_snapRT == null) _snapRT = SnapCopyRoot.transform as RectTransform;
        if (_canvas == null) _canvas = SnapCopy.canvas;

        RectTransform btnRT = RotateBtn.GetComponent<RectTransform>();
        if (btnRT == null) return;

        Canvas btnCanvas = RotateBtn.GetComponentInParent<Canvas>();
        if (btnCanvas == null) btnCanvas = _canvas;
        RectTransform btnParentRT = btnRT.parent as RectTransform;
        if (btnParentRT == null) return;

        // 计算 SnapCopy 在屏幕空间的上下边与中心X
        Vector3[] snapCorners = new Vector3[4];
        _snapRT.GetWorldCorners(snapCorners);
        Camera uiCamSnap = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
        Vector3 tl = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[1]); // 左上
        Vector3 tr = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[2]); // 右上
        Vector3 bl = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[0]); // 左下
        Vector3 br = RectTransformUtility.WorldToScreenPoint(uiCamSnap, snapCorners[3]); // 右下

        float topY = Mathf.Max(tl.y, tr.y);
        float bottomY = Mathf.Min(bl.y, br.y);
        Vector2 snapCenterScreen = RectTransformUtility.WorldToScreenPoint(
            uiCamSnap,
            _snapRT.TransformPoint(_snapRT.rect.center)
        );

        // 按钮在屏幕空间的半宽/半高（像素）与边距
        float btnScale = (btnCanvas != null && btnCanvas.scaleFactor > 0f) ? btnCanvas.scaleFactor : 1f;
        float halfW = btnRT.rect.width * 0.5f * btnScale;
        float halfH = btnRT.rect.height * 0.5f * btnScale;
        const float margin = 0f;

        // 先默认放在上方外 30px，水平居中于 SnapCopy
        float targetY = topY + margin + halfH;
        float targetX = snapCenterScreen.x;

        // 如果顶部越界，改到下方外 30px；若仍然越界到底部，再回到上方
        if (targetY + halfH > Screen.height)
        {
            targetY = bottomY - margin - halfH;
        }
        else if (targetY - halfH < 0f)
        {
            targetY = topY + margin + halfH;
        }

        // 水平方向自适应：保证始终在屏幕内
        if (targetX + halfW > Screen.width)
        {
            targetX = Screen.width - halfW;
        }
        else if (targetX - halfW < 0f)
        {
            targetX = halfW;
        }

        // 将屏幕中心点转换为按钮父节点本地坐标
        Camera uiCamBtn = (btnCanvas != null && btnCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? btnCanvas.worldCamera : null;
        Vector2 localCenter;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(btnParentRT, new Vector2(targetX, targetY), uiCamBtn, out localCenter))
        {
            Vector2 size = btnRT.rect.size;
            Vector2 pivot = btnRT.pivot;
            Vector2 anchored = localCenter + new Vector2((pivot.x - 0.5f) * size.x, (pivot.y - 0.5f) * size.y);
            btnRT.anchoredPosition = anchored;
        }
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
                    if (_snapRT == null) _snapRT = SnapCopyRoot.transform as RectTransform;
                    if (_canvas == null) _canvas = SnapCopy.canvas;
                    _originalAlpha = SnapCopy.color.a;
                    UpdateSnapCopySizeToTexture();
                }
                _inSnapUse = true;
                ConfirmBtn.gameObject.SetActive(true);
                RotateBtn.gameObject.SetActive(true);
                PositionConfirmButton();
                PositionRotateButton();
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
        // 将当前截图中心（视口坐标）转换为 UGUI 坐标并设置到 SnapCopy
        if (_shotManager != null && SnapCopy != null && Camera.main != null)
        {
            if (_snapRT == null) _snapRT = SnapCopyRoot.transform as RectTransform;
            if (_canvas == null) _canvas = SnapCopy.canvas;
            RectTransform parentRT = _snapRT != null ? _snapRT.parent as RectTransform : null;
            if (_snapRT != null && parentRT != null)
            {
                Vector4 center01 = _shotManager._curSnapCenter;
                Vector3 screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(center01.x, center01.y, 0));
                Camera uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _canvas.worldCamera : null;
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenCenter, uiCam, out localPoint))
                {
                    _snapRT.anchoredPosition = localPoint;
                }
            }
        }
        SnapCopyRoot.SetActive(true);
    }

    void Clear()
    {
        _shotManager.EnableGridShow(false);
        SnapCopyRoot.SetActive(false);
    }

    void UpdateSnapCopySizeToTexture()
    {
        _curRotate = 0;

        if (SnapCopy == null || SnapCopy.texture == null)
        {
            return;
        }

        RectTransform imgRT = SnapCopy.transform as RectTransform;
        if (imgRT == null)
        {
            return;
        }

        // 让 RawImage 使用纹理的原生尺寸
        SnapCopy.SetNativeSize();

        // 同步容器（SnapCopyRoot）的尺寸，保证用于定位与边界计算的 _snapRT 与视觉一致
        if (_snapRT == null && SnapCopyRoot != null)
        {
            _snapRT = SnapCopyRoot.transform as RectTransform;
        }
        if (_snapRT != null)
        {
            Vector2 size = imgRT.sizeDelta;
            _snapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _snapRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        // 设置 SnapCopy 旋转为 0
        if (SnapCopy != null)
        {
            SnapCopy.transform.rotation = Quaternion.identity;
        }
    }
}
