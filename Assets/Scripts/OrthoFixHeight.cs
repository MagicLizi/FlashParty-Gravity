using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class OrthoFixHeight : MonoBehaviour
{
    [Min(0f)] public float targetWorldHeight = 12f;
    public bool lockEveryFrame = true; // 勾上则每帧强制维持

    Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        Apply();
    }

    void OnValidate() { if (cam) Apply(); }
    void Update() { if (lockEveryFrame) Apply(); }

    void Apply()
    {
        float size = Mathf.Max(0.0001f, targetWorldHeight * 0.5f);
        if (!Mathf.Approximately(cam.orthographicSize, size))
            cam.orthographicSize = size;
    }
}
