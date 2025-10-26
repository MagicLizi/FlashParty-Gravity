// TilemapOutlineNoPhysics.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[DisallowMultipleComponent]
public class TilemapOutlineNoPhysics : MonoBehaviour
{
    [Header("外描边外观")]
    public float outlineWidth = 0.08f;                               // 世界单位
    public Color outlineColor = new Color(0f, 1f, 1f, 0.85f);         // 颜色+透明度
    [Range(0, 8)] public int roundness = 3;                           // 线拐角/端帽细分

    [Header("渲染层级")]
    public bool drawBehindTilemap = true;                             // 放到Tilemap后面只露外半边
    public int sortingOrderOffset = -1;                               // 相对TilemapRenderer的偏移
    public Material customMaterial;                                   // 可选自定义材质

    [Header("刷新")]
    public bool autoRebuildInEditor = true;                           // 编辑器改动时自动重建
    public bool compressBoundsBeforeBuild = true;                     // 减少遍历区域

    const string ContainerName = "_OutlineContainer";

    Tilemap _tm;
    TilemapRenderer _tmr;
    Transform _container;

    struct Edge { public Vector2 a, b; public Edge(Vector2 a, Vector2 b){ this.a=a; this.b=b; } }

    static readonly Vector3Int[] Neigh4 = { new(1,0,0), new(0,1,0), new(-1,0,0), new(0,-1,0) };

    void OnEnable()
    {
        _tm = GetComponent<Tilemap>();
        _tmr = GetComponent<TilemapRenderer>();
        RebuildOutline();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        if (autoRebuildInEditor) RebuildOutline();
    }
#endif

    public void RebuildOutline()
    {
        if (!_tm) _tm = GetComponent<Tilemap>();
        if (!_tmr) _tmr = GetComponent<TilemapRenderer>();

        if (compressBoundsBeforeBuild) _tm.CompressBounds();

        ClearOutline();

        var edges = CollectBoundaryEdges(_tm);
        if (edges.Count == 0) return;

        var loops = StitchLoops(edges);

        _container = transform.Find(ContainerName);
        if (!_container)
        {
            var go = new GameObject(ContainerName);
            go.transform.SetParent(transform, false);
            _container = go.transform;
        }

        foreach (var loop in loops)
            CreateLine(loop);
    }

    void ClearOutline()
    {
        var t = transform.Find(ContainerName);
        if (!t) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(t.gameObject);
        else Destroy(t.gameObject);
#else
        Destroy(t.gameObject);
#endif
    }

    List<Edge> CollectBoundaryEdges(Tilemap tm)
    {
        var edges = new List<Edge>(256);
        if (!tm) return edges;

        var grid = tm.layoutGrid;
        Vector3 cellSize = grid.cellSize;
        Vector3 vx = grid.transform.right * cellSize.x;
        Vector3 vy = grid.transform.up    * cellSize.y;

        BoundsInt b = tm.cellBounds;

        var filled = new HashSet<Vector3Int>();
        foreach (var c in b.allPositionsWithin)
            if (tm.HasTile(c)) filled.Add(c);

        foreach (var c in filled)
        {
            Vector3 bl = tm.CellToWorld(c);
            Vector3 br = bl + vx;
            Vector3 tl = bl + vy;
            Vector3 tr = bl + vx + vy;

            if (!filled.Contains(c + Neigh4[0])) edges.Add(new Edge(br, tr)); // 右
            if (!filled.Contains(c + Neigh4[1])) edges.Add(new Edge(tl, tr)); // 上
            if (!filled.Contains(c + Neigh4[2])) edges.Add(new Edge(bl, tl)); // 左
            if (!filled.Contains(c + Neigh4[3])) edges.Add(new Edge(bl, br)); // 下
        }
        return edges;
    }

    // —— 修复版：将边拼成闭合轮廓（世界坐标），无“飞线到(0,0)”问题 ——
    List<List<Vector3>> StitchLoops(List<Edge> edges)
    {
        const float Q = 10000f; // 1e-4 量化，避免浮点误差

        long Key(in Vector2 p)
        {
            long xi = Mathf.RoundToInt(p.x * Q);
            long yi = Mathf.RoundToInt(p.y * Q);
            return (xi << 32) ^ (uint)yi;
        }
        static (long, long) EdgeKey(long a, long b) => a < b ? (a, b) : (b, a);

        var adj = new Dictionary<long, List<long>>();   // 点key -> 邻接点key
        var key2pt = new Dictionary<long, Vector2>();   // key -> 世界坐标

        void AddAdj(in Vector2 ap, in Vector2 bp)
        {
            long a = Key(ap), b = Key(bp);
            if (!adj.TryGetValue(a, out var la)) { la = new List<long>(); adj[a] = la; }
            if (!adj.TryGetValue(b, out var lb)) { lb = new List<long>(); adj[b] = lb; }
            la.Add(b); lb.Add(a);
            if (!key2pt.ContainsKey(a)) key2pt[a] = ap;
            if (!key2pt.ContainsKey(b)) key2pt[b] = bp;
        }

        foreach (var e in edges) AddAdj(e.a, e.b);

        var used = new HashSet<(long, long)>();         // 已走过的无向边
        var loops = new List<List<Vector3>>();

        foreach (var kv in adj)
        {
            long startK = kv.Key;
            var neighs = kv.Value;

            foreach (var nbK in neighs)
            {
                var ek = EdgeKey(startK, nbK);
                if (used.Contains(ek)) continue;

                var loop = new List<Vector3>(64);

                long aK = startK, bK = nbK;
                used.Add(ek);

                Vector2 a = key2pt[aK];
                Vector2 b = key2pt[bK];

                loop.Add(a);
                loop.Add(b);

                while (true)
                {
                    if (!adj.TryGetValue(bK, out var nbs) || nbs.Count == 0) break;

                    Vector2 dirPrev = (b - a).normalized;
                    long bestK = 0;
                    float bestScore = float.NegativeInfinity;

                    for (int i = 0; i < nbs.Count; i++)
                    {
                        long cK = nbs[i];
                        if (cK == aK) continue; // 不回头

                        var e2 = EdgeKey(bK, cK);
                        if (used.Contains(e2)) continue;

                        Vector2 c   = key2pt[cK];
                        Vector2 dir = (c - b).normalized;

                        float cross = dirPrev.x * dir.y - dirPrev.y * dir.x; // “左转”优先
                        float dot   = Vector2.Dot(dirPrev, dir);
                        float score = cross * 10f - Mathf.Abs(dot) * 0.1f;

                        if (score > bestScore) { bestScore = score; bestK = cK; }
                    }

                    if (bestScore == float.NegativeInfinity) break;

                    used.Add(EdgeKey(bK, bestK));

                    aK = bK; a = b;
                    bK = bestK; b = key2pt[bK];

                    if (bK == startK) break; // 闭合
                    loop.Add(b);
                }

                if (loop.Count >= 3)
                    loops.Add(loop);
            }
        }
        return loops;
    }

    void CreateLine(List<Vector3> loop)
    {
        if (loop == null || loop.Count < 2) return;

        if (!_container)
        {
            var go = new GameObject(ContainerName);
            go.transform.SetParent(transform, false);
            _container = go.transform;
        }

        var goLine = new GameObject("OutlineLoop");
        goLine.transform.SetParent(_container, false);

        var lr = goLine.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = loop.Count;
        lr.widthMultiplier = Mathf.Max(0.0001f, outlineWidth);
        lr.numCornerVertices = Mathf.Clamp(roundness, 0, 8);
        lr.numCapVertices    = Mathf.Clamp(roundness, 0, 8);
        lr.alignment         = LineAlignment.View;

        for (int i = 0; i < loop.Count; i++)
            lr.SetPosition(i, new Vector3(loop[i].x, loop[i].y, transform.position.z));

        lr.startColor = lr.endColor = outlineColor;
        lr.material = customMaterial ? customMaterial : GetDefaultSpriteMat();

        if (_tmr)
        {
            lr.sortingLayerID = _tmr.sortingLayerID;
            lr.sortingOrder   = drawBehindTilemap ? _tmr.sortingOrder + sortingOrderOffset
                                                  : _tmr.sortingOrder + 1;
        }
    }

    static Material _defaultMat;
    static Material GetDefaultSpriteMat()
    {
        if (_defaultMat) return _defaultMat;
        var s = Shader.Find("Sprites/Default");
        _defaultMat = new Material(s) { name = "Outline(Sprites-Default)" };
        _defaultMat.renderQueue = 3000;
        return _defaultMat;
    }
}
