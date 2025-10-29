using UnityEngine;
using DG.Tweening;

/// <summary>
/// 钥匙道具
/// 角色碰到后，设置角色的 hasKey = true，钥匙消失
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyItem : MonoBehaviour
{
    [Header("视觉效果")]
    [Tooltip("消失动画持续时间")]
    public float disappearDuration = 0.5f;
    
    [Header("UI提示（可选）")]
    [Tooltip("拾取后显示的提示文本")]
    public string tooltipMessage = "获得了钥匙！";
    
    [Tooltip("SimpleTooltip组件（如果为空则自动查找）")]
    public SimpleTooltip tooltip;
    
    private bool isCollected = false; // 是否已被拾取
    private Collider2D triggerCollider;
    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        // 查找SimpleTooltip（如果没有手动配置）
        if (tooltip == null)
        {
            tooltip = FindObjectOfType<SimpleTooltip>();
            if (tooltip == null)
            {
                Debug.LogWarning($"[KeyItem] 未找到 SimpleTooltip，将不会显示拾取提示", this);
            }
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
            Debug.LogError($"[KeyItem] {gameObject.name} 没有 Collider2D，无法检测碰撞！", this);
        }
        
        // 获取SpriteRenderer用于淡出效果
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[KeyItem] {gameObject.name} 没有 SpriteRenderer，无法播放淡出动画", this);
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
        
        // 拾取钥匙
        CollectKey(player);
    }
    
    private void CollectKey(Player player)
    {
        isCollected = true;
        
        // 禁用触发器，防止重复触发
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
        
        Debug.Log($"[KeyItem] 玩家拾取了钥匙！");
        
        // 设置玩家的 hasKey 属性
        SetPlayerKey(player);
        
        // 显示UI提示
        ShowTooltip();
        
        // 播放淡出消失动画
        PlayFadeOutAnimation();
    }
    
    /// <summary>
    /// 设置玩家拥有钥匙
    /// </summary>
    private void SetPlayerKey(Player player)
    {
        if (player != null)
        {
            player.hasKey = true;
            Debug.Log($"[KeyItem] 玩家 hasKey 已设置为 true");
        }
        else
        {
            Debug.LogError($"[KeyItem] Player 对象为空，无法设置 hasKey！");
        }
    }
    
    /// <summary>
    /// 显示UI提示
    /// </summary>
    private void ShowTooltip()
    {
        if (tooltip != null && !string.IsNullOrEmpty(tooltipMessage))
        {
            tooltip.Show(tooltipMessage);
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

