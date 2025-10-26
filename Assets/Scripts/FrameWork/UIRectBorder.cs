using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIRectBorder : MonoBehaviour
{
    [Header("Border Settings")]
    public Color borderColor = Color.white;
    [Tooltip("线宽（像素），自动适配 Canvas 缩放")] public float borderThickness = 2f;

    [Tooltip("可选：为子边框命名的前缀")]
    public string childPrefix = "_Border_";

    private RectTransform _rt;
    private Canvas _canvas;
    private Image _top;
    private Image _bottom;
    private Image _left;
    private Image _right;

    void Awake()
    {
        _rt = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        EnsureBorders();
        SetBordersActive(true);
        UpdateBorders();
    }

    void OnDisable()
    {
        SetBordersActive(false);
    }

    void OnTransformParentChanged()
    {
        _canvas = GetComponentInParent<Canvas>();
        UpdateBorders();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled) return;
        UpdateBorders();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) return;
        UpdateBorders();
    }

    void EnsureBorders()
    {
        if (_top == null) _top = GetOrCreateEdge(childPrefix + "Top");
        if (_bottom == null) _bottom = GetOrCreateEdge(childPrefix + "Bottom");
        if (_left == null) _left = GetOrCreateEdge(childPrefix + "Left");
        if (_right == null) _right = GetOrCreateEdge(childPrefix + "Right");
    }

    Image GetOrCreateEdge(string name)
    {
        Transform t = transform.Find(name);
        GameObject go;
        if (t == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
        }
        else
        {
            go = t.gameObject;
            if (go.GetComponent<Image>() == null)
            {
                go.AddComponent<Image>();
            }
        }

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.color = borderColor;
        return img;
    }

    void SetBordersActive(bool active)
    {
        if (_top) _top.gameObject.SetActive(active);
        if (_bottom) _bottom.gameObject.SetActive(active);
        if (_left) _left.gameObject.SetActive(active);
        if (_right) _right.gameObject.SetActive(active);
    }

    void UpdateBorders()
    {
        if (_rt == null) return;
        EnsureBorders();

        float scale = (_canvas != null && _canvas.scaleFactor > 0f) ? _canvas.scaleFactor : 1f;
        float thicknessUnits = borderThickness / scale;

        // Top
        SetupEdge(_top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, thicknessUnits));
        // Bottom
        SetupEdge(_bottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, thicknessUnits));
        // Left
        SetupEdge(_left.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(thicknessUnits, 0f));
        // Right
        SetupEdge(_right.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(thicknessUnits, 0f));

        // Color sync
        if (_top) _top.color = borderColor;
        if (_bottom) _bottom.color = borderColor;
        if (_left) _left.color = borderColor;
        if (_right) _right.color = borderColor;
    }

    static void SetupEdge(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }
}


