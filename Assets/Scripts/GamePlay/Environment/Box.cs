using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Box : MonoBehaviour
{
    public List<Box> Boxes = new List<Box>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        Atk atk = other.GetComponent<Atk>();
        if (atk != null)
        {
            DOVirtual.DelayedCall(atk.AtkTime, () =>
            {
                atk.CurPlayer.AtkShow(atk.gameObject);
                BoxDead();
                foreach (var box in Boxes)
                {
                    box.BoxDead();
                }
            });
        }
    }

    public void BoxDead()
    {
        // 获取当前节点及其所有子节点的SpriteRenderer组件
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // 如果存在SpriteRenderer组件，创建淡出动画
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            // 使用DOTween创建淡出动画，透明度从1变为0，持续0.5秒
            foreach (var spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.DOFade(0f, 0.35f).SetEase(Ease.InOutQuad);
                }
            }
            
            // 延迟销毁游戏对象，等待动画完成
            DOVirtual.DelayedCall(0.35f, () =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            // 如果没有SpriteRenderer组件，直接销毁
            Destroy(gameObject);
        }
    }
}