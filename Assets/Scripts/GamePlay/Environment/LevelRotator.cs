using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;

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

     TweenerCore<Color, Color, ColorOptions> fadeTween;

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
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                Debug.Log(pivot.transform.eulerAngles.z);
                float curAngle = clockwise ? 360 - pivot.transform.eulerAngles.z : pivot.transform.eulerAngles.z;
                float progress = Mathf.Abs(curAngle / angleToRotate);
                Debug.Log($"Rotation progress: {progress}");
                if (progress >= BackGVProgress && !hasBackGV)
                {
                    hasBackGV = true;
                    curPlayer.LoseGravity(false);
                    curPlayer.AnimateSetBool("LossG", false);
                    curPlayer.Shine(false);
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

    /// <summary>
    /// 以传入的点为中心，将关卡旋转重置为0
    /// </summary>
    /// <param name="pivotPos">旋转的中心点</param>
    public void ResetRotation(Vector2 pivotPos, Action onComplete)
    {
        if (Const.InRotation || levelRoot == null)
        {
            return;
        }

                // 计算回到0度需要旋转的角度
        float angleToReset = -levelRoot.transform.eulerAngles.z;
        if(angleToReset == 0)
        {
            onComplete?.Invoke();
            return;
        }

        Const.InRotation = true;

        GameObject pivot = new GameObject("LevelResetPivot");
        pivot.transform.position = pivotPos;

        Transform originalParent = levelRoot.parent;
        levelRoot.SetParent(pivot.transform, true);

        // 使用DOTween旋转轴心
        pivot.transform.DORotate(new Vector3(0, 0, angleToReset), rotationDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // 恢复父节点
                levelRoot.SetParent(originalParent, true);
                
                // 为避免浮点数误差，动画结束后直接将角度设置为0
                levelRoot.eulerAngles = Vector3.zero;
                
                // 销毁临时轴心
                Destroy(pivot);

                Const.InRotation = false;
                Debug.Log("Level rotation has been reset.");
                onComplete?.Invoke();
            });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Atk atk = other.GetComponent<Atk>();
        if (atk != null)
        {
            curPlayer = atk.CurPlayer;
            hasBackGV = false;
            curPlayer.LoseGravity(true);
            DOVirtual.DelayedCall(atk.AtkTime - 0.35f, () =>
            {
                curPlayer.Shine(true);
            });
            DOVirtual.DelayedCall(atk.AtkTime, () =>
            {
                curPlayer.AnimateSetBool("LossG", true);
                Vector2 pivotPos = curPlayer.transform.position + new Vector3(0, curPlayer.boxCollider.bounds.size.y / 2);
                TriggerRotation(pivotPos);
            });
            // curPlayer.LoseGravity(true);
            // Vector2 pivotPos = curPlayer.transform.position + new Vector3(0, curPlayer.boxCollider.bounds.size.y / 2);
            // TriggerRotation(pivotPos);
        }
    }
}