using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapGridLinesDrawer : MonoBehaviour
{
    [Header("Grid Line Settings")]
    public Color lineColor = Color.green;
    [Tooltip("线宽（世界单位）")] public float lineThickness = 0.02f;

    [Tooltip("仅在运行时绘制")] public bool onlyWhilePlaying = true;

    private Tilemap targetTilemap;
    private Material lineMaterial;

    void Awake()
    {
        targetTilemap = GetComponent<Tilemap>();
    }

    void OnEnable()
    {
        EnsureMaterial();
    }

    void OnDisable()
    {
        if (lineMaterial != null)
        {
            DestroyImmediate(lineMaterial);
            lineMaterial = null;
        }
    }

    void EnsureMaterial()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetColor("_Color", Color.white);
            lineMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            lineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
    }

    void OnRenderObject()
    {
        if (onlyWhilePlaying && !Application.isPlaying)
        {
            return;
        }

        if (targetTilemap == null || lineMaterial == null)
        {
            return;
        }

        BoundsInt bounds = targetTilemap.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0)
        {
            return;
        }

        // Precompute vertical extents (world space)
        Vector3 cellSize = targetTilemap.cellSize;
        float yMinWorld = BoundaryYWorld(bounds.yMin);
        float yMaxWorld = BoundaryYWorld(bounds.yMax);

        // Precompute horizontal extents (world space)
        float xMinWorld = BoundaryXWorld(bounds.xMin);
        float xMaxWorld = BoundaryXWorld(bounds.xMax);

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.QUADS);
        GL.Color(lineColor);

        // Vertical lines (x boundaries)
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            float xWorld = BoundaryXWorld(x);
            DrawThickLine(new Vector3(xWorld, yMinWorld, 0f), new Vector3(xWorld, yMaxWorld, 0f), lineThickness);
        }

        // Horizontal lines (y boundaries)
        for (int y = bounds.yMin; y <= bounds.yMax; y++)
        {
            float yWorld = BoundaryYWorld(y);
            DrawThickLine(new Vector3(xMinWorld, yWorld, 0f), new Vector3(xMaxWorld, yWorld, 0f), lineThickness);
        }

        GL.End();
        GL.PopMatrix();
    }

    float BoundaryXWorld(int x)
    {
        // 使用 yMin 作为参考行，取该列的左边界（x 边界）
        Vector3 center = targetTilemap.GetCellCenterWorld(new Vector3Int(x, targetTilemap.cellBounds.yMin, 0));
        return center.x - targetTilemap.cellSize.x * 0.5f;
    }

    float BoundaryYWorld(int y)
    {
        // 使用 xMin 作为参考列，取该行的下边界（y 边界）
        Vector3 center = targetTilemap.GetCellCenterWorld(new Vector3Int(targetTilemap.cellBounds.xMin, y, 0));
        return center.y - targetTilemap.cellSize.y * 0.5f;
    }

    void DrawThickLine(Vector3 start, Vector3 end, float thickness)
    {
        Vector3 dir = (end - start);
        if (dir.sqrMagnitude < 1e-8f)
        {
            return;
        }
        Vector3 n = new Vector3(-dir.y, dir.x, 0f).normalized * (thickness * 0.5f);

        Vector3 v0 = start - n;
        Vector3 v1 = start + n;
        Vector3 v2 = end + n;
        Vector3 v3 = end - n;

        GL.Vertex(v0);
        GL.Vertex(v1);
        GL.Vertex(v2);
        GL.Vertex(v3);
    }
}


