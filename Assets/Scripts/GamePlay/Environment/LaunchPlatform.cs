using DG.Tweening;
using UnityEngine;

public class LaunchPlatform : MonoBehaviour
{
    [Header("弹射设置")]
    [Tooltip("垂直弹射力度")]
    public float launchPower = 15f;
    
    [Tooltip("水平弹射力度系数（0-1，相对于垂直力度的比例）")]
    [Range(0f, 1f)]
    public float horizontalForceFactor = 0.5f;
    
    [Tooltip("最大检测宽度（用于归一化计算，超出此范围的偏移会被限制，单位：米）")]
    public float maxDetectionWidth = 3f;
    
    [Tooltip("垂直力衰减系数（0-1，越大衰减越明显）")]
    [Range(0f, 0.5f)]
    public float verticalDecay = 0.3f;
    
    [Header("时间设置")]
    [Tooltip("触发后延迟多久弹射")]
    public float launchDelay = 0.1f;
    
    [Tooltip("弹射后冷却时间")]
    public float cooldownTime = 2f;
    
    [Tooltip("角色被击飞持续时间")]
    public float hitFlyDuration = 0.5f;
    
    [Header("触发器配置（可选）")]
    [Tooltip("如果触发器在子对象，可在此指定；为空则使用当前对象的触发器")]
    public Collider2D triggerCollider;
    
    private Animator animator;
    private bool isOnCooldown = false; // 是否在冷却中
    private bool isLaunching = false; // 是否正在弹射中
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 如果没有指定触发器，尝试从自身或子对象获取
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
            {
                triggerCollider = GetComponentInChildren<Collider2D>();
            }
        }
        
        // 确保触发器是 Trigger
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log($"[LaunchPlatform] 触发器被触碰: {other.gameObject.name}");
        
        // 检查是否是玩家
        Player player = other.GetComponent<Player>();
        
        if (player == null)
        {
            // Debug.Log($"[LaunchPlatform] 不是玩家，忽略");
            return; // 不是玩家，忽略
        }
        
        // Debug.Log($"[LaunchPlatform] 检测到玩家！冷却: {isOnCooldown}, 弹射中: {isLaunching}");
        
        // 如果是玩家且不在冷却中，触发弹射
        if (!isOnCooldown && !isLaunching)
        {
            // Debug.Log($"[LaunchPlatform] 开始弹射玩家！");
            LaunchPlayer(player);
        }
    }
    
    private void LaunchPlayer(Player player)
    {
        isLaunching = true;
        
        // 禁用触发器，防止在弹射和冷却期间重复触发
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
        
        // 触发平台打开动画
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }
        
        // 延迟弹射
        DOVirtual.DelayedCall(launchDelay, () =>
        {
            // 计算弹射力方向
            Vector2 launchForce = CalculateLaunchForce(player);
            
            // 给角色施加弹射力
            player.GetLaunched(launchForce, hitFlyDuration);
            
            isLaunching = false;
            
            // 进入冷却
            StartCooldown();
        });
    }
    
    /// <summary>
    /// 计算弹射力（动态计算：中心垂直力最大，边缘垂直力衰减并增加水平力）
    /// </summary>
    private Vector2 CalculateLaunchForce(Player player)
    {
        // 从平台中心指向角色的方向
        Vector2 platformPos = transform.position;
        Vector2 playerPos = player.transform.position;
        Vector2 offset = playerPos - platformPos;
        
        // 使用配置的最大检测宽度的一半作为归一化参考
        float maxHorizontalRange = maxDetectionWidth * 0.5f;
        
        // 归一化到 [-1, 1]
        float normalizedX = Mathf.Clamp(offset.x / maxHorizontalRange, -1f, 1f);
        float absNormalizedX = Mathf.Abs(normalizedX);
        
        // 使用平方曲线计算水平力：小偏移时水平力很小，大偏移时才明显增加
        // 例如：0.1 -> 0.01, 0.5 -> 0.25, 1.0 -> 1.0
        float horizontalCurve = normalizedX * absNormalizedX; // 保留符号，应用平方
        float horizontalForce = horizontalCurve * launchPower * horizontalForceFactor;
        
        // 垂直力随偏移增大而衰减：中心最强，边缘较弱
        // verticalForce = launchPower * (1 - absNormalizedX * verticalDecay)
        float verticalForce = launchPower * (1f - absNormalizedX * verticalDecay);
        
        Debug.Log($"[LaunchPlatform] 弹射计算 - " +
                  $"平台位置: {platformPos}, " +
                  $"角色位置: {playerPos}, " +
                  $"偏移: {offset.x:F2}, " +
                  $"最大范围: {maxHorizontalRange:F2}, " +
                  $"归一化X: {normalizedX:F2} (abs: {absNormalizedX:F2}), " +
                  $"水平曲线: {horizontalCurve:F2}, " +
                  $"水平力: {horizontalForce:F2}, " +
                  $"垂直力: {verticalForce:F2}");
        
        return new Vector2(horizontalForce, verticalForce);
    }
    
    private void StartCooldown()
    {
        isOnCooldown = true;
        
        // 冷却结束后触发关闭动画并重新启用触发器
        DOVirtual.DelayedCall(cooldownTime, () =>
        {
            if (animator != null)
            {
                animator.SetTrigger("Close");
            }
            
            isOnCooldown = false;
            
            // 重新启用触发器，允许下一次弹射
            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }
        });
    }
    
    // 可选：Gizmos 显示弹射效果
    private void OnDrawGizmosSelected()
    {
        Vector3 platformPos = transform.position;
        float halfWidth = maxDetectionWidth * 0.5f;
        
        // 绘制最大检测宽度范围（黄色半透明矩形）
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Vector3 rangeSize = new Vector3(maxDetectionWidth, 0.2f, 0.1f);
        Gizmos.DrawCube(platformPos, rangeSize);
        
        // 绘制中心位置的弹射力（最大垂直力，无水平力）
        Gizmos.color = Color.green;
        float centerVertical = launchPower * 0.1f;
        Vector3 centerEnd = platformPos + Vector3.up * centerVertical;
        Gizmos.DrawLine(platformPos, centerEnd);
        Gizmos.DrawSphere(centerEnd, 0.15f);
        
        // 计算并绘制左边缘的弹射力（平方曲线）
        float edgeHorizontalCurve = -1f * 1f; // normalizedX = -1, 平方后 = -1
        float edgeHorizontalForce = edgeHorizontalCurve * launchPower * horizontalForceFactor * 0.1f;
        float edgeVerticalForce = launchPower * (1f - 1f * verticalDecay) * 0.1f;
        
        // 左边缘
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // 橙色
        Vector3 leftPos = platformPos + Vector3.left * halfWidth;
        Vector3 leftEnd = leftPos + new Vector3(edgeHorizontalForce, edgeVerticalForce, 0);
        Gizmos.DrawLine(leftPos, leftEnd);
        Gizmos.DrawWireSphere(leftEnd, 0.1f);
        
        // 右边缘（水平力取反）
        Vector3 rightPos = platformPos + Vector3.right * halfWidth;
        Vector3 rightEnd = rightPos + new Vector3(-edgeHorizontalForce, edgeVerticalForce, 0);
        Gizmos.DrawLine(rightPos, rightEnd);
        Gizmos.DrawWireSphere(rightEnd, 0.1f);
        
        // 绘制中间位置示例（1/2位置）
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.7f); // 浅绿
        float halfNormalized = 0.5f;
        float halfHorizontalCurve = halfNormalized * halfNormalized; // 0.25
        float halfHorizontalForce = halfHorizontalCurve * launchPower * horizontalForceFactor * 0.1f;
        float halfVerticalForce = launchPower * (1f - halfNormalized * verticalDecay) * 0.1f;
        
        Vector3 halfRightPos = platformPos + Vector3.right * (halfWidth * 0.5f);
        Vector3 halfRightEnd = halfRightPos + new Vector3(halfHorizontalForce, halfVerticalForce, 0);
        Gizmos.DrawLine(halfRightPos, halfRightEnd);
        Gizmos.DrawWireSphere(halfRightEnd, 0.08f);
        
        // 绘制触发器范围
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 青色
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
        
        // 绘制检测范围边界线
        Gizmos.color = Color.yellow;
        Vector3 topLeft = platformPos + new Vector3(-halfWidth, 0.1f, 0);
        Vector3 bottomLeft = platformPos + new Vector3(-halfWidth, -0.1f, 0);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        Vector3 topRight = platformPos + new Vector3(halfWidth, 0.1f, 0);
        Vector3 bottomRight = platformPos + new Vector3(halfWidth, -0.1f, 0);
        Gizmos.DrawLine(topRight, bottomRight);
    }
    
}

