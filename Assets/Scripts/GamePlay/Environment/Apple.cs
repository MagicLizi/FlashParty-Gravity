using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Apple : MonoBehaviour
{

    public GameObject Success;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Atk atk = other.GetComponent<Atk>();
        if (atk != null)
        {
            DOVirtual.DelayedCall(atk.AtkTime, () =>
            {
                atk.CurPlayer.AtkShow(gameObject);
                Transform successChild = transform.Find("Success");
                if (successChild != null)
                {
                    successChild.gameObject.SetActive(true);
                    // 创建胜利动画序列
                    Vector3 originalScale = successChild.localScale;
                    Vector3 originalPosition = successChild.localPosition;

                    // 1. 先放大并向上弹跳
                    Sequence victorySequence = DOTween.Sequence();
                    victorySequence.Append(successChild.DOScale(originalScale * 1.3f, 0.2f).SetEase(Ease.OutBack));
                    victorySequence.Join(successChild.DOLocalMoveY(originalPosition.y + 0.5f, 0.2f).SetEase(Ease.OutQuad));

                    // 2. 缩小回正常大小并回到原位
                    victorySequence.Append(successChild.DOScale(originalScale, 0.15f).SetEase(Ease.InOutQuad));
                    victorySequence.Join(successChild.DOLocalMoveY(originalPosition.y, 0.15f).SetEase(Ease.InOutQuad));

                    // 3. 添加轻微的左右摇摆效果
                    victorySequence.Append(successChild.DORotate(new Vector3(0, 0, 15f), 0.1f).SetEase(Ease.InOutQuad));
                    victorySequence.Append(successChild.DORotate(new Vector3(0, 0, -15f), 0.1f).SetEase(Ease.InOutQuad));
                    victorySequence.Append(successChild.DORotate(Vector3.zero, 0.1f).SetEase(Ease.InOutQuad));

                    // 4. 最后添加一个轻微的弹跳效果
                    victorySequence.Append(successChild.DOScale(originalScale * 1.1f, 0.1f).SetEase(Ease.OutQuad));
                    victorySequence.Append(successChild.DOScale(originalScale, 0.1f).SetEase(Ease.InOutQuad));

                    // 播放胜利音效（如果有的话）
                    // AudioManager.Instance?.PlaySound("victory");
                }
            });
            Success.SetActive(true);
        }
    }
}