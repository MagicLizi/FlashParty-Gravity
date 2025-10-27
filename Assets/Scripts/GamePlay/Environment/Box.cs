using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Box : MonoBehaviour
{
    public List<Box> Boxes = new List<Box>();
    
    [Header("特效配置（可选）")]
    [Tooltip("被击中时的特效prefab")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("特效持续时间（秒），之后自动销毁")]
    public float hitEffectDuration = 1f;
    
    private Collider2D boxCollider;
    
    void Awake()
    {
        boxCollider = GetComponent<Collider2D>();
        boxCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Atk atk = other.GetComponent<Atk>();
        if (atk != null)
        {
            // 保存碰撞位置信息，用于特效生成
            Collider2D atkCollider = other;
            
            DOVirtual.DelayedCall(atk.AtkTime, () =>
            {
                // 生成命中特效
                SpawnHitEffect(atkCollider);
                
                atk.CurPlayer.AtkShow(gameObject);
                BoxDead();
                foreach (var box in Boxes)
                {
                    box.BoxDead();
                }
            });
        }
    }
    
    /// <summary>
    /// 生成命中特效
    /// </summary>
    private void SpawnHitEffect(Collider2D atkCollider)
    {
        if (hitEffectPrefab == null)
        {
            return; // 没有配置特效prefab
        }
        
        // 计算相交位置：Box和攻击碰撞器的最近点
        Vector2 hitPosition;
        
        if (boxCollider != null && atkCollider != null)
        {
            // 获取Box碰撞器上最接近攻击的点
            Vector2 atkCenter = atkCollider.bounds.center;
            Vector2 closestPointOnBox = boxCollider.ClosestPoint(atkCenter);
            
            // 获取攻击碰撞器上最接近Box的点
            Vector2 boxCenter = boxCollider.bounds.center;
            Vector2 closestPointOnAtk = atkCollider.ClosestPoint(boxCenter);
            
            // 相交位置取两个最近点的中点
            hitPosition = (closestPointOnBox + closestPointOnAtk) * 0.5f;
        }
        else
        {
            // fallback：使用Box中心位置
            hitPosition = transform.position;
        }
        
        // 生成特效
        GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        
        // 延迟销毁
        Destroy(effect, hitEffectDuration);
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