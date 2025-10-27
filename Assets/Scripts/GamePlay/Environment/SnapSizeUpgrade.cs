using UnityEngine;
using DG.Tweening;

/// <summary>
/// 截图能力扩展道具
/// 角色碰到后，截图能力扩展到5x5，道具渐渐消失
/// </summary>
public class SnapSizeUpgrade : MonoBehaviour
{
    [Header("视觉效果")]
    [Tooltip("消失动画持续时间")]
    public float disappearDuration = 0.5f;
    
    private bool isCollected = false; // 是否已被拾取
    private SnapShotManager snapShotManager;
    private Collider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        // 查找SnapShotManager
        snapShotManager = FindObjectOfType<SnapShotManager>();
        if (snapShotManager == null)
        {
            Debug.LogWarning($"[SnapSizeUpgrade] 未找到 SnapShotManager，道具可能无法正常工作", this);
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
            Debug.LogError($"[SnapSizeUpgrade] {gameObject.name} 没有 Collider2D，无法检测碰撞！", this);
        }
        
        // 获取SpriteRenderer用于淡出效果
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[SnapSizeUpgrade] {gameObject.name} 没有 SpriteRenderer，无法播放淡出动画", this);
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
        
        Debug.Log($"[SnapSizeUpgrade] 玩家拾取了截图扩展道具！扩展到 5x5");
        
        // 升级截图能力
        UpgradeSnapSize();
        
        // 播放淡出消失动画
        PlayFadeOutAnimation();
    }
    
    private void UpgradeSnapSize()
    {
        if (snapShotManager != null)
        {
            // 调用SnapShotManager的SetSnapSize5x5方法
            snapShotManager.SetSnapSize5x5();
            Debug.Log($"[SnapSizeUpgrade] 截图能力已扩展到 5x5");
        }
        else
        {
            Debug.LogError($"[SnapSizeUpgrade] SnapShotManager 不存在，无法升级截图能力！");
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

