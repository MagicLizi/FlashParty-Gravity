using UnityEngine;

[DisallowMultipleComponent]
public class ElementSnapBound : MonoBehaviour
{
    [Header("Bound 设置")]
    public Vector2 centerOffset = Vector2.zero;
    public Vector2 size = new Vector2(1f, 1f);

    [Header("Gizmo 显示")]
    public bool drawAlways = true;
    public bool drawWhenSelected = true;
    public bool drawFilled = false;
    public Color gizmoColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    public Color wireColor = new Color(1f, 0.6f, 0.1f, 1f);

    public SpriteRenderer Render;

    public bool ignoreSnap = false;

    [Header("描边设置")]
    public Color outlineColor = Color.green;

    public Color interOutlineColor = Color.red;
    [Min(0f)] public float outlineWidth = 1f;
    private string outlineShaderName = "Universal Render Pipeline/2D/Sprite-Outline";
    private Material _outlineMat;

    public Vector3 GetWorldCenter()
    {
        return transform.position + new Vector3(centerOffset.x, centerOffset.y, 0f);
    }

    public Vector2 GetSize() => size;

    public Bounds GetWorldBounds()
    {
        Vector3 center = GetWorldCenter();
        Vector3 extents = new Vector3(Mathf.Max(0f, size.x) * 0.5f, Mathf.Max(0f, size.y) * 0.5f, 0.001f);
        return new Bounds(center, extents * 2f);
    }

    void OnDrawGizmos()
    {
        if (!drawAlways) return;
        DrawGizmoBounds();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawWhenSelected) return;
        DrawGizmoBounds();
    }

    void DrawGizmoBounds()
    {
        Bounds b = GetWorldBounds();
        Color prev = Gizmos.color;
        if (drawFilled)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(b.center, b.size);
        }
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(b.center, b.size);
        Gizmos.color = prev;
    }

    SnapShotManager mgr;

    void Awake()
    {
        mgr = FindObjectOfType<SnapShotManager>();
        mgr.AllElementSnapBound.Add(this);

        SpriteRenderer target = Render != null ? Render : GetComponentInChildren<SpriteRenderer>();
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(ElementSnapBound)}] 未找到 SpriteRenderer，无法应用描边。");
            return;
        }

        if (_outlineMat == null)
        {
            Shader s = Shader.Find(outlineShaderName);
            if (s == null)
            {
                Debug.LogWarning($"[{nameof(ElementSnapBound)}] 未找到 Shader: {outlineShaderName}");
                return;
            }
            _outlineMat = new Material(s);
            _outlineMat.hideFlags = HideFlags.HideAndDontSave;
        }

        target.material = _outlineMat;
        HiddenOutline();
    }

    void OnDestroy()
    {
        if (mgr != null && mgr.AllElementSnapBound != null)
        {
            for (int i = mgr.AllElementSnapBound.Count - 1; i >= 0; i--)
            {
                if (mgr.AllElementSnapBound[i] == this)
                {
                    mgr.AllElementSnapBound.RemoveAt(i);
                }
            }
        }

        if (_outlineMat != null)
        {
            Destroy(_outlineMat);
            _outlineMat = null;
        }
    }

    public void ShowAllInOutline()
    {
        if (_outlineMat.HasProperty("_OutlineColor"))
        {
            _outlineMat.SetColor("_OutlineColor", outlineColor);
        }
        if (_outlineMat.HasProperty("_OutlineSize"))
        {
            _outlineMat.SetFloat("_OutlineSize", outlineWidth);
        }
    }

    public void ShowInterOutline()
    {
        if (_outlineMat.HasProperty("_OutlineColor"))
        {
            _outlineMat.SetColor("_OutlineColor", interOutlineColor);
        }
        if (_outlineMat.HasProperty("_OutlineSize"))
        {
            _outlineMat.SetFloat("_OutlineSize", outlineWidth);
        }
    }

    public void HiddenOutline()
    {
        _outlineMat.SetFloat("_OutlineSize", 0);
    }
}


