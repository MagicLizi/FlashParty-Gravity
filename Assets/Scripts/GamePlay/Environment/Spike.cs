using DG.Tweening;
using UnityEngine;

public enum SpikeType
{
    Static,  // 静态：一直处于打开状态
    Dynamic  // 动态：有冷却时间，周期性打开/关闭
}

public class Spike : MonoBehaviour
{
    [Header("地刺类型")]
    [Tooltip("静态：一直打开；动态：周期性打开/关闭")]
    public SpikeType spikeType = SpikeType.Static;
    
    [Header("弹射设置")]
    [Tooltip("主要弹射力度（垂直于地刺表面的方向）")]
    public float launchPower = 15f;
    
    [Tooltip("侧面地刺（90度/-90度）额外的垂直向上力度")]
    public float sideUpwardForce = 5f;
    
    // 判断为侧面地刺的角度容差（度）
    private const float SIDE_SPIKE_ANGLE_TOLERANCE = 30f;
    
    [Header("时间设置")]
    [Tooltip("碰到地刺后的停顿时间（无法操作，不受重力）")]
    public float hitStopDuration = 0.15f;
    
    [Tooltip("角色被击飞持续时间")]
    public float hitFlyDuration = 0.5f;
    
    [Header("动态地刺设置（仅 Dynamic 类型生效）")]
    [Tooltip("触发Open后的冷却时间，冷却结束后再次触发Open")]
    public float cooldownTime = 2f;
    
    [Header("触发器配置（可选）")]
    [Tooltip("如果触发器在子对象，可在此指定；为空则使用当前对象的触发器")]
    public Collider2D triggerCollider;
    
    [Header("特效配置（可选）")]
    [Tooltip("命中特效prefab，在角色和地刺相交位置生成")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("特效持续时间（秒），之后自动销毁")]
    public float hitEffectDuration = 1f;
    
    private Animator animator;
    
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
    
    void Start()
    {
        // 根据类型初始化
        if (spikeType == SpikeType.Static)
        {
            // 静态地刺：触发器由动画状态机控制，或者一直启用
            // 这里不做额外操作
        }
        else // Dynamic
        {
            // 动态地刺：开始循环（冷却 → Open → 冷却 → Open...）
            StartCooldownCycle();
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
        
        // 击飞玩家（触发器的启用/禁用由动画状态机控制）
        LaunchPlayer(player, other);
    }
    
    private void LaunchPlayer(Player player, Collider2D playerCollider)
    {
        // 生成命中特效
        SpawnHitEffect(player, playerCollider);
        
        // 计算弹射力方向
        Vector2 launchForce = CalculateLaunchForce(player);
        
        // 给角色施加弹射力，带停顿效果
        player.GetLaunchedWithHitStop(launchForce, hitFlyDuration, hitStopDuration);
    }
    
    /// <summary>
    /// 生成命中特效
    /// </summary>
    private void SpawnHitEffect(Player player, Collider2D playerCollider)
    {
        if (hitEffectPrefab == null)
        {
            return; // 没有配置特效prefab
        }
        
        // 计算相交位置：角色和地刺的最近点
        Vector2 hitPosition;
        
        if (triggerCollider != null && playerCollider != null)
        {
            // 获取地刺碰撞器上最接近角色的点
            Vector2 playerCenter = playerCollider.bounds.center;
            Vector2 closestPointOnSpike = triggerCollider.ClosestPoint(playerCenter);
            
            // 获取角色碰撞器上最接近地刺的点
            Vector2 spikeCenter = triggerCollider.bounds.center;
            Vector2 closestPointOnPlayer = playerCollider.ClosestPoint(spikeCenter);
            
            // 相交位置取两个最近点的中点
            hitPosition = (closestPointOnSpike + closestPointOnPlayer) * 0.5f;
        }
        else
        {
            // fallback：使用角色和地刺位置的中点
            hitPosition = (player.transform.position + transform.position) * 0.5f;
        }
        
        // 生成特效
        GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        
        // 设置特效朝向（可选：让特效朝向地刺的反方向）
        // effect.transform.up = transform.up;
        
        // 延迟销毁
        Destroy(effect, hitEffectDuration);
    }
    
    /// <summary>
    /// 计算弹射力（逻辑与弹射平台相同）
    /// </summary>
    private Vector2 CalculateLaunchForce(Player player)
    {
        // 获取地刺的朝向
        Vector2 spikeUp = transform.up; // 主要弹射方向（垂直于地刺表面）
        
        // 主要弹射力：沿着地刺的 up 方向
        Vector2 mainForce = spikeUp * launchPower;
        
        // 检测是否为侧面地刺（90度或-90度）
        // 计算地刺 up 方向与世界 up 方向的夹角
        float angleToWorldUp = Vector2.Angle(spikeUp, Vector2.up);
        
        // 如果角度接近90度（在容差范围内），认为是侧面地刺
        bool isSideSpike = Mathf.Abs(angleToWorldUp - 90f) < SIDE_SPIKE_ANGLE_TOLERANCE;
        
        Vector2 upwardForce = Vector2.zero;
        if (isSideSpike && sideUpwardForce > 0)
        {
            // 侧面地刺额外添加垂直向上的力
            upwardForce = Vector2.up * sideUpwardForce;
        }
        
        // 合成最终的力向量
        Vector2 totalForce = mainForce + upwardForce;
        
        // Debug.Log($"[Spike] 击飞计算 - " +
        //           $"地刺朝向: {spikeUp}, 与世界up夹角: {angleToWorldUp:F1}度, " +
        //           $"是侧面地刺: {isSideSpike}, " +
        //           $"主要力: {mainForce}, 上升力: {upwardForce}, " +
        //           $"最终力向量: {totalForce}");
        
        return totalForce;
    }
    
    /// <summary>
    /// 开始冷却循环（仅动态地刺）
    /// </summary>
    private void StartCooldownCycle()
    {
        if (spikeType != SpikeType.Dynamic)
        {
            return;
        }
        
        // 冷却结束后触发 Open，然后继续下一轮循环
        DOVirtual.DelayedCall(cooldownTime, () =>
        {
            // 触发打开动画
            if (animator != null)
            {
                animator.SetTrigger("Open");
            }
            
            // 继续下一轮循环
            StartCooldownCycle();
        });
    }
    
    // 可选：Gizmos 显示击飞效果
    private void OnDrawGizmosSelected()
    {
        Vector3 spikePos = transform.position;
        Vector3 spikeUp = transform.up; // 主要弹射方向
        
        float forceScale = 0.1f; // 力的可视化缩放
        
        // 计算是否为侧面地刺
        float angleToWorldUp = Vector2.Angle(spikeUp, Vector2.up);
        bool isSideSpike = Mathf.Abs(angleToWorldUp - 90f) < SIDE_SPIKE_ANGLE_TOLERANCE;
        
        // 绘制主要弹射力
        Gizmos.color = Color.red;
        Vector3 mainForceVec = spikeUp * launchPower * forceScale;
        Vector3 mainForceEnd = spikePos + mainForceVec;
        Gizmos.DrawLine(spikePos, mainForceEnd);
        Gizmos.DrawSphere(mainForceEnd, 0.15f);
        
        // 如果是侧面地刺，绘制额外的垂直向上力
        if (isSideSpike && sideUpwardForce > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 upwardForceVec = Vector3.up * sideUpwardForce * forceScale;
            Vector3 upwardForceEnd = spikePos + upwardForceVec;
            Gizmos.DrawLine(spikePos, upwardForceEnd);
            Gizmos.DrawSphere(upwardForceEnd, 0.12f);
            
            // 绘制合成力
            Gizmos.color = Color.magenta;
            Vector3 totalForceVec = mainForceVec + upwardForceVec;
            Vector3 totalForceEnd = spikePos + totalForceVec;
            Gizmos.DrawLine(spikePos, totalForceEnd);
            Gizmos.DrawSphere(totalForceEnd, 0.18f);
        }
        
        // 绘制地刺方向指示器
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawLine(spikePos, spikePos + spikeUp * 0.5f);
        Gizmos.DrawSphere(spikePos + spikeUp * 0.5f, 0.08f);
        
        // 绘制触发器范围
        if (triggerCollider != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 红色
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
}
