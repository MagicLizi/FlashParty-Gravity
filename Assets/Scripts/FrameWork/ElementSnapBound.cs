using UnityEngine;

[DisallowMultipleComponent]
public class ElementSnapBound : MonoBehaviour
{
    [Header("Snap 设置")]
    [Tooltip("忽略Snap功能（不参与Snap，也不显示Gizmo）")]
    public bool ignoreSnap = false;
    
    [Header("Bound 设置")]
    public Vector2 centerOffset = Vector2.zero;
    public Vector2 size = new Vector2(1f, 1f);

    [Header("Gizmo 显示")]
    public bool drawAlways = true;
    public bool drawWhenSelected = true;
    public bool drawFilled = false;
    public Color gizmoColor = new Color(1f, 0.6f, 0.1f, 0.8f);
    public Color wireColor = new Color(1f, 0.6f, 0.1f, 1f);

    [Header("描边设置")]
    [Tooltip("需要应用描边的所有 SpriteRenderer（为空则自动获取子对象的所有 SpriteRenderer）")]
    public SpriteRenderer[] Renders;
    
    [Header("描边颜色")]
    public Color outlineColor = Color.green;
    public Color interOutlineColor = Color.red;
    [Min(0f)] public float outlineWidth = 0.5f;
    
    private string outlineShaderName = "Universal Render Pipeline/2D/Sprite-Outline";
    private Material[] _outlineMats;

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
        if (ignoreSnap) return; // 忽略Snap时不显示Gizmo
        if (!drawAlways) return;
        DrawGizmoBounds();
    }

    void OnDrawGizmosSelected()
    {
        if (ignoreSnap) return; // 忽略Snap时不显示Gizmo
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

        // 如果未配置 Renders，自动获取子对象的所有 SpriteRenderer
        if (Renders == null || Renders.Length == 0)
        {
            Renders = GetComponentsInChildren<SpriteRenderer>();
        }

        if (Renders == null || Renders.Length == 0)
        {
            Debug.LogWarning($"[{nameof(ElementSnapBound)}] 未找到 SpriteRenderer，无法应用描边。");
            return;
        }

        // 为每个 SpriteRenderer 创建独立的材质
        Shader s = Shader.Find(outlineShaderName);
        if (s == null)
        {
            Debug.LogWarning($"[{nameof(ElementSnapBound)}] 未找到 Shader: {outlineShaderName}");
            return;
        }

        _outlineMats = new Material[Renders.Length];
        for (int i = 0; i < Renders.Length; i++)
        {
            if (Renders[i] != null)
            {
                _outlineMats[i] = new Material(s);
                _outlineMats[i].hideFlags = HideFlags.HideAndDontSave;
                Renders[i].material = _outlineMats[i];
            }
        }

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

        // 销毁所有材质
        if (_outlineMats != null)
        {
            foreach (var mat in _outlineMats)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
            _outlineMats = null;
        }
    }

    /// <summary>
    /// 显示成功描边（Bound在视口内）
    /// </summary>
    public void ShowAllInOutline()
    {
        if (_outlineMats == null) return;
        
        foreach (var mat in _outlineMats)
        {
            if (mat != null)
            {
                if (mat.HasProperty("_OutlineColor"))
                {
                    mat.SetColor("_OutlineColor", outlineColor);
                }
                if (mat.HasProperty("_OutlineSize"))
                {
                    mat.SetFloat("_OutlineSize", outlineWidth);
                }
            }
        }
    }

    /// <summary>
    /// 显示失败描边（Bound不在视口内）
    /// </summary>
    public void ShowInterOutline()
    {
        if (_outlineMats == null) return;
        
        foreach (var mat in _outlineMats)
        {
            if (mat != null)
            {
                if (mat.HasProperty("_OutlineColor"))
                {
                    mat.SetColor("_OutlineColor", interOutlineColor);
                }
                if (mat.HasProperty("_OutlineSize"))
                {
                    mat.SetFloat("_OutlineSize", outlineWidth);
                }
            }
        }
    }

    /// <summary>
    /// 隐藏描边
    /// </summary>
    public void HiddenOutline()
    {
        if (_outlineMats == null) return;
        
        foreach (var mat in _outlineMats)
        {
            if (mat != null)
            {
                mat.SetFloat("_OutlineSize", 0);
            }
        }
    }
}


