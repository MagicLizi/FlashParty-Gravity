using DG.Tweening;
using UnityEngine;

public class LaunchPlatform : MonoBehaviour
{
    [Header("弹射设置")]
    [Tooltip("主要弹射力度（垂直于平台表面的方向）")]
    public float launchPower = 15f;
    
    [Tooltip("次要方向力度系数（0-1，沿着平台表面的方向，相对于主要力度的比例）")]
    [Range(0f, 1f)]
    public float tangentialForceFactor = 0.5f;
    
    [Tooltip("最大检测宽度（用于归一化计算，超出此范围的偏移会被限制，单位：米）")]
    public float maxDetectionWidth = 3f;
    
    [Tooltip("主要方向力度衰减系数（0-1，越大衰减越明显）")]
    [Range(0f, 0.5f)]
    public float mainForceDecay = 0.3f;
    
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
    /// 计算弹射力（支持任意方向：使用平台的局部坐标系统）
    /// </summary>
    private Vector2 CalculateLaunchForce(Player player)
    {
        // 从平台中心指向角色的向量
        Vector2 platformPos = transform.position;
        Vector2 playerPos = player.transform.position;
        Vector2 offset = playerPos - platformPos;
        
        // 获取平台的局部坐标系
        Vector2 platformUp = transform.up;      // 主要弹射方向（垂直于平台表面）
        Vector2 platformRight = transform.right; // 次要方向（沿着平台表面）
        
        // 将偏移投影到平台的 right 方向上，得到沿着平台表面的偏移量
        float tangentialOffset = Vector2.Dot(offset, platformRight);
        
        // 使用配置的最大检测宽度的一半作为归一化参考
        float maxTangentialRange = maxDetectionWidth * 0.5f;
        
        // 归一化到 [-1, 1]
        float normalizedTangential = Mathf.Clamp(tangentialOffset / maxTangentialRange, -1f, 1f);
        float absNormalizedTangential = Mathf.Abs(normalizedTangential);
        
        // 使用平方曲线计算次要方向力：小偏移时力很小，大偏移时才明显增加
        float tangentialCurve = normalizedTangential * absNormalizedTangential; // 保留符号，应用平方
        float tangentialForceMagnitude = tangentialCurve * launchPower * tangentialForceFactor;
        
        // 主要方向力随偏移增大而衰减：中心最强，边缘较弱
        float mainForceMagnitude = launchPower * (1f - absNormalizedTangential * mainForceDecay);
        
        // 合成最终的力向量
        Vector2 mainForce = platformUp * mainForceMagnitude;
        Vector2 tangentialForce = platformRight * tangentialForceMagnitude;
        Vector2 totalForce = mainForce + tangentialForce;
        
        // Debug用于调试弹射平台的弹射力计算各项参数
        // Debug.Log($"[LaunchPlatform] 弹射计算 - " +
        //           $"平台位置: {platformPos}, 角色位置: {playerPos}, " +
        //           $"平台朝向: up={platformUp}, right={platformRight}, " +
        //           $"切向偏移: {tangentialOffset:F2}, 最大范围: {maxTangentialRange:F2}, " +
        //           $"归一化: {normalizedTangential:F2}, 曲线: {tangentialCurve:F2}, " +
        //           $"主要力: {mainForceMagnitude:F2}, 次要力: {tangentialForceMagnitude:F2}, " +
        //           $"最终力向量: {totalForce}");
        
        return totalForce;
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
    
    // 可选：Gizmos 显示弹射效果（支持任意方向）
    private void OnDrawGizmosSelected()
    {
        Vector3 platformPos = transform.position;
        Vector3 platformUp = transform.up;      // 主要弹射方向
        Vector3 platformRight = transform.right; // 次要方向（沿着平台表面）
        float halfWidth = maxDetectionWidth * 0.5f;
        
        // 绘制平台方向指示器（短箭头）
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(platformPos, platformPos + platformUp * 0.5f);
        Gizmos.DrawSphere(platformPos + platformUp * 0.5f, 0.1f);
        
        // 绘制最大检测宽度范围（沿着平台的 right 方向）
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Vector3 leftBound = platformPos - platformRight * halfWidth;
        Vector3 rightBound = platformPos + platformRight * halfWidth;
        
        // 绘制检测范围的线段
        Gizmos.color = Color.yellow;
        Vector3 boundOffset = platformUp * 0.1f; // 稍微偏移以便可见
        Gizmos.DrawLine(leftBound + boundOffset, rightBound + boundOffset);
        Gizmos.DrawLine(leftBound - boundOffset, rightBound - boundOffset);
        
        // 绘制边界标记
        Gizmos.DrawLine(leftBound + boundOffset, leftBound - boundOffset);
        Gizmos.DrawLine(rightBound + boundOffset, rightBound - boundOffset);
        
        float forceScale = 0.1f; // 力的可视化缩放
        
        // 绘制中心位置的弹射力（最大主要力，无次要力）
        Gizmos.color = Color.green;
        float centerMainForce = launchPower * forceScale;
        Vector3 centerEnd = platformPos + platformUp * centerMainForce;
        Gizmos.DrawLine(platformPos, centerEnd);
        Gizmos.DrawSphere(centerEnd, 0.15f);
        
        // 计算并绘制边缘的弹射力
        float edgeTangentialCurve = -1f * 1f; // normalized = -1, 平方后 = -1
        float edgeTangentialForce = edgeTangentialCurve * launchPower * tangentialForceFactor * forceScale;
        float edgeMainForce = launchPower * (1f - 1f * mainForceDecay) * forceScale;
        
        // 左边缘
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // 橙色
        Vector3 leftPos = platformPos - platformRight * halfWidth;
        Vector3 leftForce = platformUp * edgeMainForce + platformRight * edgeTangentialForce;
        Vector3 leftEnd = leftPos + leftForce;
        Gizmos.DrawLine(leftPos, leftEnd);
        Gizmos.DrawWireSphere(leftEnd, 0.1f);
        
        // 右边缘（次要力取反）
        Vector3 rightPos = platformPos + platformRight * halfWidth;
        Vector3 rightForce = platformUp * edgeMainForce - platformRight * edgeTangentialForce;
        Vector3 rightEnd = rightPos + rightForce;
        Gizmos.DrawLine(rightPos, rightEnd);
        Gizmos.DrawWireSphere(rightEnd, 0.1f);
        
        // 绘制中间位置示例（1/2位置）
        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.7f); // 浅绿
        float halfNormalized = 0.5f;
        float halfTangentialCurve = halfNormalized * halfNormalized; // 0.25
        float halfTangentialForce = halfTangentialCurve * launchPower * tangentialForceFactor * forceScale;
        float halfMainForce = launchPower * (1f - halfNormalized * mainForceDecay) * forceScale;
        
        Vector3 halfRightPos = platformPos + platformRight * (halfWidth * 0.5f);
        Vector3 halfForce = platformUp * halfMainForce + platformRight * halfTangentialForce;
        Vector3 halfRightEnd = halfRightPos + halfForce;
        Gizmos.DrawLine(halfRightPos, halfRightEnd);
        Gizmos.DrawWireSphere(halfRightEnd, 0.08f);
        
        // 绘制触发器范围
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 青色
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
    
}

