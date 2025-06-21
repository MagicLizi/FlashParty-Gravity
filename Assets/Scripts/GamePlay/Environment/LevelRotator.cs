using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class LevelRotator : MonoBehaviour
{
    [Tooltip("要旋转的关卡根节点")]
    public Transform levelRoot;

    [Tooltip("每次旋转的角度")]
    public float rotationAngle = 90f;

    [Tooltip("旋转持续时间")]
    public float rotationDuration = 1.0f;

    [Tooltip("顺时针旋转")]
    public bool clockwise = true;

    private bool isRotating = false;

    private BoxCollider2D zone;

    void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// 触发旋转
    /// </summary>
    public void TriggerRotation()
    {
        if (isRotating || levelRoot == null)
        {
            return;
        }

        isRotating = true;

        float angleToRotate = clockwise ? -rotationAngle : rotationAngle;

        Vector3 currentRotation = levelRoot.eulerAngles;
        Vector3 targetRotation = new Vector3(currentRotation.x, currentRotation.y, currentRotation.z + angleToRotate);

        levelRoot.DORotate(targetRotation, rotationDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                isRotating = false;
                Debug.Log("Rotation complete!");
            });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            TriggerRotation();
        }
    }
}