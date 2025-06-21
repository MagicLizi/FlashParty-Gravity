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

    // private bool isRotating = false;

    private BoxCollider2D zone;

    private Player curPlayer;

    [Tooltip("重新获取重力时机")]
    public float BackGVProgress = 0.9f;

    private bool hasBackGV = false;

    void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
        if (zone != null && !zone.isTrigger)
        {
            zone.isTrigger = true;
        }
    }

    /// <summary>
    /// 触发旋转
    /// </summary>
    public void TriggerRotation(Vector2 pivotPos)
    {
        if (Const.InRotation || levelRoot == null)
        {
            return;
        }

        Const.InRotation = true;


        // 1. 创建一个临时的GameObject作为旋转的轴心，位置在触发者处
        GameObject pivot = new GameObject("LevelRotationPivot");
        pivot.transform.position = pivotPos;

        // 记录原始父节点
        Transform originalParent = levelRoot.parent;

        // 2. 将关卡根节点设置为轴心的子对象，这样旋转轴心时，关卡会围绕它旋转
        levelRoot.SetParent(pivot.transform, true);

        float angleToRotate = clockwise ? -rotationAngle : rotationAngle;

        // 3. 使用DOTween旋转轴心
        // 使用RotateMode.LocalAxisAdd可以在当前旋转基础上增加旋转量

        pivot.transform.DORotate(new Vector3(0, 0, angleToRotate), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad)
            .OnUpdate(() =>
            {
                float progress = pivot.transform.eulerAngles.z / angleToRotate;
                Debug.Log("Rotation progress: " + progress);
                if (progress >= BackGVProgress && !hasBackGV)
                {
                    hasBackGV = true;
                    curPlayer.LoseGravity(false);
                }
            })
            .OnComplete(() =>
            {
                // 4. 旋转结束后，恢复关卡的父节点并销毁临时轴心
                levelRoot.SetParent(originalParent, true);
                Destroy(pivot);

                Const.InRotation = false;
                Debug.Log("Rotation complete!");
            });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        curPlayer = other.GetComponent<Player>();
        if (curPlayer != null)
        {
            hasBackGV = false;
            curPlayer.LoseGravity(true);
            Vector2 pivotPos = curPlayer.transform.position + new Vector3(0, curPlayer.boxCollider.bounds.size.y / 2);
            TriggerRotation(pivotPos);
        }
    }
}