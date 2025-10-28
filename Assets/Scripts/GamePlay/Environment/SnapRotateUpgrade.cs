using UnityEngine;
using DG.Tweening;

/// <summary>
/// 截图旋转能力解锁道具
/// 角色碰到后，解锁截图旋转功能，道具渐渐消失
/// </summary>
public class SnapRotateUpgrade : MonoBehaviour
{
    [Header("视觉效果")]
    [Tooltip("消失动画持续时间")]
    public float disappearDuration = 0.5f;
    
    private bool isCollected = false; // 是否已被拾取
    private SnapUseManager snapUseManager;
    private Collider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        // 查找SnapUseManager
        snapUseManager = FindObjectOfType<SnapUseManager>();
        if (snapUseManager == null)
        {
            Debug.LogWarning($"[SnapRotateUpgrade] 未找到 SnapUseManager，道具可能无法正常工作", this);
        }
        
        // 获取自己的触发器
        triggerCollider = GetComponent<Collider2D>();
        
        // 确保触发器是 Trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[SnapRotateUpgrade] {gameObject.name} 没有 Collider2D，无法检测碰撞！", this);
        }
        
        // 获取SpriteRenderer用于淡出效果
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[SnapRotateUpgrade] {gameObject.name} 没有 SpriteRenderer，无法播放淡出动画", this);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 如果已经被拾取，忽略
        if (isCollected)
        {
            return;
        }
        
        // 检查是否是玩家
        Player player = other.GetComponent<Player>();
        if (player == null)
        {
            return; // 不是玩家，忽略
        }
        
        // 拾取道具
        CollectItem(player);
    }
    
    private void CollectItem(Player player)
    {
        isCollected = true;
        
        // 禁用触发器，防止重复触发
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
        
        Debug.Log($"[SnapRotateUpgrade] 玩家拾取了截图旋转解锁道具！");
        
        // 解锁旋转功能
        UnlockRotateFeature();
        
        // 播放淡出消失动画
        PlayFadeOutAnimation();
    }
    
    private void UnlockRotateFeature()
    {
        if (snapUseManager != null)
        {
            // 设置 CanRotate 为 true
            snapUseManager.CanRotate = true;
            Debug.Log($"[SnapRotateUpgrade] 截图旋转功能已解锁！");
        }
        else
        {
            Debug.LogError($"[SnapRotateUpgrade] SnapUseManager 不存在，无法解锁旋转功能！");
        }
    }
    
    private void PlayFadeOutAnimation()
    {
        if (spriteRenderer != null)
        {
            // 淡出动画：透明度从当前值渐变到0
            spriteRenderer.DOFade(0f, disappearDuration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            // 如果没有SpriteRenderer，直接销毁
            Destroy(gameObject, disappearDuration);
        }
    }
}


