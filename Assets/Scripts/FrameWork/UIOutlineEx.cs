using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public class UIOutlineEx : Shadow
{
    [Min(0f)] public float outlineWidth = 4f;
    [Range(4, 24)] public int sampleCount = 8;

    public Color outlineColor
    {
        get { return effectColor; }
        set { effectColor = value; if (graphic != null) graphic.SetVerticesDirty(); }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh == null)
        {
            return;
        }

        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);
        int originalCount = verts.Count;
        if (originalCount == 0)
        {
            return;
        }

        float scale = 1f;
        if (graphic != null && graphic.canvas != null)
        {
            scale = Mathf.Max(0.0001f, graphic.canvas.scaleFactor);
        }
        float radius = outlineWidth / scale;

        Color32 col = effectColor;

        int steps = Mathf.Clamp(sampleCount, 4, 24);
        for (int i = 0; i < steps; i++)
        {
            float t = (i / (float)steps) * 6.28318530718f; // 2*PI
            float dx = Mathf.Cos(t) * radius;
            float dy = Mathf.Sin(t) * radius;
            ApplyShadow(verts, col, 0, originalCount, dx, dy);
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}


