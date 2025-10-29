using UnityEngine;

/// <summary>
/// 狗的交互脚本
/// 根据玩家是否拥有钥匙来切换不同的子节点显示
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DogInteraction : MonoBehaviour
{
    [Header("子节点配置")]
    [Tooltip("玩家没有钥匙时显示的子节点（如：狗挡路/警戒状态）")]
    public GameObject nodeWithoutKey;
    
    [Tooltip("玩家有钥匙时显示的子节点（如：狗友好/让路状态）")]
    public GameObject nodeWithKey;
    
    [Header("可选配置")]
    [Tooltip("是否在 Awake 时初始化状态（显示没有钥匙的节点）")]
    public bool initializeOnAwake = true;
    
    [Header("动画配置")]
    [Tooltip("狗的 Animator 组件（可选，如果需要触发动画）")]
    public Animator dogAnimator;
    
    [Tooltip("玩家有钥匙时触发的 Trigger 名称")]
    public string successTriggerName = "success";
    
    private Collider2D triggerCollider;
    private bool hasTriggeredSuccess = false; // 是否已经触发过 success（避免重复触发）
    
    void Awake()
    {
        // 获取触发器
        triggerCollider = GetComponent<Collider2D>();
        
        // 确保是触发器
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogError($"[DogInteraction] {gameObject.name} 没有 Collider2D 组件！", this);
        }
        
        // 如果没有手动配置 Animator，尝试自动获取
        if (dogAnimator == null)
        {
            dogAnimator = GetComponent<Animator>();
            if (dogAnimator == null)
            {
                dogAnimator = GetComponentInChildren<Animator>();
            }
        }
        
        // 验证子节点配置
        if (nodeWithoutKey == null)
        {
            Debug.LogWarning($"[DogInteraction] {gameObject.name} 未配置 nodeWithoutKey（没有钥匙时的子节点）", this);
        }
        
        if (nodeWithKey == null)
        {
            Debug.LogWarning($"[DogInteraction] {gameObject.name} 未配置 nodeWithKey（有钥匙时的子节点）", this);
        }
        
        // 初始化状态（默认显示没有钥匙的节点）
        if (initializeOnAwake)
        {
            SetNodeState(false);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否是玩家
        Player player = other.GetComponent<Player>();
        
        if (player == null)
        {
            return; // 不是玩家，忽略
        }
        
        // 根据玩家的 hasKey 状态切换子节点
        HandlePlayerInteraction(player);
    }
    
    /// <summary>
    /// 处理玩家交互
    /// </summary>
    /// <param name="player">玩家对象</param>
    private void HandlePlayerInteraction(Player player)
    {
        bool playerHasKey = player.hasKey;
        
        // 根据钥匙状态切换子节点
        SetNodeState(playerHasKey);
        
        // 如果玩家有钥匙，触发 success 动画
        if (playerHasKey && !hasTriggeredSuccess)
        {
            TriggerSuccessAnimation();
        }
        
        // Debug.Log($"[DogInteraction] 玩家进入，hasKey: {playerHasKey}");
    }
    
    /// <summary>
    /// 触发成功动画
    /// </summary>
    private void TriggerSuccessAnimation()
    {
        if (dogAnimator != null && !string.IsNullOrEmpty(successTriggerName))
        {
            dogAnimator.SetTrigger(successTriggerName);
            hasTriggeredSuccess = true; // 标记已触发，避免重复触发
            Debug.Log($"[DogInteraction] 触发动画 trigger: {successTriggerName}");
        }
    }
    
    /// <summary>
    /// 设置子节点的显示状态
    /// </summary>
    /// <param name="hasKey">玩家是否有钥匙</param>
    private void SetNodeState(bool hasKey)
    {
        if (hasKey)
        {
            // 玩家有钥匙：显示 nodeWithKey，隐藏 nodeWithoutKey
            if (nodeWithKey != null)
            {
                nodeWithKey.SetActive(true);
            }
            
            if (nodeWithoutKey != null)
            {
                nodeWithoutKey.SetActive(false);
            }
        }
        else
        {
            // 玩家没有钥匙：显示 nodeWithoutKey，隐藏 nodeWithKey
            if (nodeWithoutKey != null)
            {
                nodeWithoutKey.SetActive(true);
            }
            
            if (nodeWithKey != null)
            {
                nodeWithKey.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 手动设置状态（可以通过其他脚本调用）
    /// </summary>
    /// <param name="hasKey">是否有钥匙</param>
    public void ManualSetState(bool hasKey)
    {
        SetNodeState(hasKey);
        
        // 如果是有钥匙状态，触发成功动画
        if (hasKey && !hasTriggeredSuccess)
        {
            TriggerSuccessAnimation();
        }
    }
    
    /// <summary>
    /// 重置触发状态（允许再次触发 success 动画）
    /// </summary>
    public void ResetTriggerState()
    {
        hasTriggeredSuccess = false;
    }
    
    // Gizmos 显示触发器范围
    private void OnDrawGizmosSelected()
    {
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 绿色半透明
            Gizmos.DrawCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
}

