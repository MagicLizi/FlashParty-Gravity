using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class TilemapCellsOutlineDrawer : MonoBehaviour
{
    [Header("Line Settings")]
    public Color lineColor = Color.cyan;
    [Tooltip("线宽（世界单位）")] public float lineWidth = 0.05f;
    [Tooltip("排序层名称（为空沿用 Tilemap）")] public string sortingLayerName = "";
    [Tooltip("排序层序号（相对 Tilemap 排序）")] public int sortingOrderOffset = 10;

    private Tilemap _tilemap;
    private readonly List<LineRenderer> _activeLines = new List<LineRenderer>();

    void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    public void ClearOutline()
    {
        for (int i = 0; i < _activeLines.Count; i++)
        {
            if (_activeLines[i] != null)
            {
                Destroy(_activeLines[i].gameObject);
            }
        }
        _activeLines.Clear();
    }

    public void DrawOutline(IEnumerable<Vector3Int> cells)
    {
        if (_tilemap == null)
        {
            _tilemap = GetComponent<Tilemap>();
        }
        if (_tilemap == null)
        {
            return;
        }

        ClearOutline();

        // 1) 统计边：同边出现两次（共享）则移除，只保留外边
        var cellSet = new HashSet<Vector3Int>();
        foreach (var c in cells)
        {
            var pos = new Vector3Int(c.x, c.y, _tilemap.origin.z);
            if (_tilemap.HasTile(pos))
            {
                cellSet.Add(pos);
            }
        }
        if (cellSet.Count == 0)
        {
            return;
        }

        var edgeCount = new Dictionary<Edge, int>(new EdgeComparer());
        foreach (var c in cellSet)
        {
            Vector2Int bl = new Vector2Int(c.x, c.y);
            Vector2Int br = new Vector2Int(c.x + 1, c.y);
            Vector2Int tr = new Vector2Int(c.x + 1, c.y + 1);
            Vector2Int tl = new Vector2Int(c.x, c.y + 1);

            Accumulate(edgeCount, new Edge(bl, br));
            Accumulate(edgeCount, new Edge(br, tr));
            Accumulate(edgeCount, new Edge(tr, tl));
            Accumulate(edgeCount, new Edge(tl, bl));
        }

        // 边界边：只出现一次
        var borderEdges = new HashSet<Edge>(new EdgeComparer());
        foreach (var kv in edgeCount)
        {
            if (kv.Value == 1)
            {
                borderEdges.Add(kv.Key);
            }
        }
        if (borderEdges.Count == 0)
        {
            return;
        }

        // 2) 计算每条边的“内法线”（用于内描边）
        var edgeNormals = new Dictionary<Edge, Vector3>(new EdgeComparer());
        foreach (var e in borderEdges)
        {
            Vector2Int a = e.a;
            Vector2Int b = e.b;
            Vector3 inward = Vector3.zero;
            if (a.y == b.y)
            {
                // 水平边：上/下单元
                int x = Mathf.Min(a.x, b.x);
                int y = a.y;
                var above = new Vector3Int(x, y, _tilemap.origin.z);
                var below = new Vector3Int(x, y - 1, _tilemap.origin.z);
                inward = cellSet.Contains(above) ? Vector3.up : Vector3.down;
            }
            else if (a.x == b.x)
            {
                // 垂直边：左/右单元
                int x = a.x;
                int y = Mathf.Min(a.y, b.y);
                var right = new Vector3Int(x, y, _tilemap.origin.z);
                var left = new Vector3Int(x - 1, y, _tilemap.origin.z);
                inward = cellSet.Contains(right) ? Vector3.right : Vector3.left;
            }
            edgeNormals[e] = inward;
        }

        // 3) 将边链接为闭合回路
        var vertexToNeighbors = new Dictionary<Vector2Int, List<Vector2Int>>();
        foreach (var e in borderEdges)
        {
            if (!vertexToNeighbors.TryGetValue(e.a, out var la))
            {
                la = new List<Vector2Int>();
                vertexToNeighbors[e.a] = la;
            }
            if (!vertexToNeighbors.TryGetValue(e.b, out var lb))
            {
                lb = new List<Vector2Int>();
                vertexToNeighbors[e.b] = lb;
            }
            la.Add(e.b);
            lb.Add(e.a);
        }

        var visited = new HashSet<Edge>(new EdgeComparer());
        foreach (var e in borderEdges)
        {
            if (visited.Contains(e)) continue;

            // 从该边开始构造一条闭合多段线
            var loopGrid = TraceLoop(e, borderEdges, visited, vertexToNeighbors);
            if (loopGrid != null && loopGrid.Count > 1)
            {
                // 将角点转换为内描边顶点（闭合）
                var go = new GameObject("TileOutlineLoop");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lr);

                int count = loopGrid.Count;
                lr.positionCount = count;
                float inset = Mathf.Max(0f, lineWidth * 0.5f) + 1e-4f;
                for (int i = 0; i < count; i++)
                {
                    Vector2Int vPrev = loopGrid[(i - 1 + count) % count];
                    Vector2Int vCurr = loopGrid[i];
                    Vector2Int vNext = loopGrid[(i + 1) % count];

                    var ePrev = new Edge(MinFirst(vPrev, vCurr).a, MinFirst(vPrev, vCurr).b);
                    var eNext = new Edge(MinFirst(vCurr, vNext).a, MinFirst(vCurr, vNext).b);
                    Vector3 nPrev = edgeNormals.TryGetValue(ePrev, out var np) ? np : Vector3.zero;
                    Vector3 nNext = edgeNormals.TryGetValue(eNext, out var nn) ? nn : Vector3.zero;
                    Vector3 n = nPrev + nNext;
                    if (n.sqrMagnitude < 1e-6f)
                    {
                        n = (nPrev.sqrMagnitude > 0f ? nPrev : nNext);
                    }
                    n = n.normalized;
                    Vector3 worldCorner = CornerToWorld(vCurr);
                    lr.SetPosition(i, worldCorner + n * inset);
                }
                _activeLines.Add(lr);
            }
        }
    }

    Edge MinFirst(Vector2Int a, Vector2Int b)
    {
        return new Edge(a, b); // 构造函数已保证较小在前
    }

    void ConfigureLineRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        var sr = GetComponent<TilemapRenderer>();
        if (sr != null)
        {
            if (!string.IsNullOrEmpty(sortingLayerName))
            {
                lr.sortingLayerName = sortingLayerName;
            }
            else
            {
                lr.sortingLayerID = sr.sortingLayerID;
            }
            lr.sortingOrder = sr.sortingOrder + sortingOrderOffset;
        }
    }

    List<Vector2Int> TraceLoop(Edge start, HashSet<Edge> allEdges, HashSet<Edge> visited, Dictionary<Vector2Int, List<Vector2Int>> neighbors)
    {
        var loop = new List<Vector2Int>();
        Vector2Int v0 = start.a;
        Vector2Int v1 = start.b;

        loop.Add(v0);
        loop.Add(v1);
        visited.Add(start);

        Vector2Int prev = v0;
        Vector2Int curr = v1;
        int guard = 0;
        while (guard++ < 100000)
        {
            if (!neighbors.TryGetValue(curr, out var neigh)) break;
            Vector2Int next = default;
            bool found = false;
            for (int i = 0; i < neigh.Count; i++)
            {
                var n = neigh[i];
                if (n == prev) continue;
                var e = new Edge(curr, n);
                if (!allEdges.Contains(e) || visited.Contains(e)) continue;
                next = n;
                found = true;
                visited.Add(e);
                break;
            }

            if (!found)
            {
                // 回到起点则闭合
                if (curr == v0) break;
                // 或没有可走的边，结束
                break;
            }

            loop.Add(next);
            prev = curr;
            curr = next;

            if (curr == v0)
            {
                // 闭合回到起点
                break;
            }
        }
        return loop;
    }

    void Accumulate(Dictionary<Edge, int> map, Edge e)
    {
        if (map.TryGetValue(e, out var cnt))
        {
            map[e] = cnt + 1;
        }
        else
        {
            map[e] = 1;
        }
    }

    Vector3 CornerToWorld(Vector2Int corner)
    {
        // 角点采用 corner 对应单元格的世界左下角
        Vector3 center = _tilemap.GetCellCenterWorld(new Vector3Int(corner.x, corner.y, _tilemap.origin.z));
        Vector3 half = new Vector3(_tilemap.cellSize.x * 0.5f, _tilemap.cellSize.y * 0.5f, 0f);
        return center - half;
    }

    struct Edge
    {
        public Vector2Int a;
        public Vector2Int b;

        public Edge(Vector2Int a, Vector2Int b)
        {
            // 规范化顺序（较小在前）
            if (a.x < b.x || (a.x == b.x && a.y <= b.y))
            {
                this.a = a; this.b = b;
            }
            else
            {
                this.a = b; this.b = a;
            }
        }
    }

    class EdgeComparer : IEqualityComparer<Edge>
    {
        public bool Equals(Edge x, Edge y)
        {
            return x.a == y.a && x.b == y.b;
        }

        public int GetHashCode(Edge obj)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + obj.a.x;
                h = h * 31 + obj.a.y;
                h = h * 31 + obj.b.x;
                h = h * 31 + obj.b.y;
                return h;
            }
        }
    }
}


