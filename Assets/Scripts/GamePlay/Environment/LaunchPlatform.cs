using DG.Tweening;
using UnityEngine;

public class LaunchPlatform : MonoBehaviour
{
    [Header("弹射设置")]
    [Tooltip("主要弹射力度（垂直于平台表面的方向）")]
    public float launchPower = 15f;
    
    [Tooltip("侧面平台（90度/-90度）额外的垂直向上力度")]
    public float sideUpwardForce = 5f;
    
    // 判断为侧面平台的角度容差（度）
    private const float SIDE_PLATFORM_ANGLE_TOLERANCE = 30f;
    
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
        // 获取平台的朝向
        Vector2 platformUp = transform.up; // 主要弹射方向（垂直于平台表面）
        
        // 主要弹射力：沿着平台的 up 方向
        Vector2 mainForce = platformUp * launchPower;
        
        // 检测是否为侧面平台（90度或-90度）
        // 计算平台 up 方向与世界 up 方向的夹角
        float angleToWorldUp = Vector2.Angle(platformUp, Vector2.up);
        
        // 如果角度接近90度（在容差范围内），认为是侧面平台
        bool isSidePlatform = Mathf.Abs(angleToWorldUp - 90f) < SIDE_PLATFORM_ANGLE_TOLERANCE;
        
        Vector2 upwardForce = Vector2.zero;
        if (isSidePlatform && sideUpwardForce > 0)
        {
            // 侧面平台额外添加垂直向上的力
            upwardForce = Vector2.up * sideUpwardForce;
        }
        
        // 合成最终的力向量
        Vector2 totalForce = mainForce + upwardForce;
        
        // Debug.Log($"[LaunchPlatform] 弹射计算 - " +
        //           $"平台朝向: {platformUp}, 与世界up夹角: {angleToWorldUp:F1}度, " +
        //           $"是侧面平台: {isSidePlatform}, " +
        //           $"主要力: {mainForce}, 上升力: {upwardForce}, " +
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
        Vector3 platformUp = transform.up; // 主要弹射方向
        
        float forceScale = 0.1f; // 力的可视化缩放
        
        // 计算是否为侧面平台
        float angleToWorldUp = Vector2.Angle(platformUp, Vector2.up);
        bool isSidePlatform = Mathf.Abs(angleToWorldUp - 90f) < SIDE_PLATFORM_ANGLE_TOLERANCE;
        
        // 绘制主要弹射力
        Gizmos.color = Color.green;
        Vector3 mainForceVec = platformUp * launchPower * forceScale;
        Vector3 mainForceEnd = platformPos + mainForceVec;
        Gizmos.DrawLine(platformPos, mainForceEnd);
        Gizmos.DrawSphere(mainForceEnd, 0.15f);
        
        // 如果是侧面平台，绘制额外的垂直向上力
        if (isSidePlatform && sideUpwardForce > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 upwardForceVec = Vector3.up * sideUpwardForce * forceScale;
            Vector3 upwardForceEnd = platformPos + upwardForceVec;
            Gizmos.DrawLine(platformPos, upwardForceEnd);
            Gizmos.DrawSphere(upwardForceEnd, 0.12f);
            
            // 绘制合成力
            Gizmos.color = Color.cyan;
            Vector3 totalForceVec = mainForceVec + upwardForceVec;
            Vector3 totalForceEnd = platformPos + totalForceVec;
            Gizmos.DrawLine(platformPos, totalForceEnd);
            Gizmos.DrawSphere(totalForceEnd, 0.18f);
        }
        
        // 绘制平台方向指示器（短箭头）
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(platformPos, platformPos + platformUp * 0.5f);
        Gizmos.DrawSphere(platformPos + platformUp * 0.5f, 0.08f);
        
        // 绘制触发器范围
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // 青色
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
}
