using UnityEngine;

namespace FlashParty.Platform
{
    /// <summary>
    /// 平台触发器基类
    /// </summary>
    public class PlatformTrigger : MonoBehaviour
    {
        [Header("触发设置")]
        [SerializeField] protected MovingPlatform[] targetPlatforms;
        [SerializeField] protected bool requirePlayerInteraction = true;
        [SerializeField] protected bool oneTimeUse = false;
        [SerializeField] protected float cooldownTime = 1f;
        
        [Header("视觉反馈")]
        [SerializeField] protected GameObject activeVisual;
        [SerializeField] protected GameObject inactiveVisual;
        
        protected bool isTriggered = false;
        protected bool isOnCooldown = false;
        protected float lastTriggerTime = 0f;
        
        protected virtual void Start()
        {
            UpdateVisualState();
        }
        
        /// <summary>
        /// 触发平台移动
        /// </summary>
        public virtual void Trigger()
        {
            if (!CanTrigger())
            {
                return;
            }
            
            Debug.Log($"Platform trigger {gameObject.name} activated");
            
            // 标记为已触发
            isTriggered = true;
            lastTriggerTime = Time.time;
            
            // 触发所有关联的平台
            foreach (var platform in targetPlatforms)
            {
                if (platform != null)
                {
                    platform.TriggerMovement();
                }
            }
            
            // 更新视觉状态
            UpdateVisualState();
            
            // 处理冷却时间
            if (cooldownTime > 0 && !oneTimeUse)
            {
                isOnCooldown = true;
                Invoke(nameof(EndCooldown), cooldownTime);
            }
            
            OnTriggerActivated();
        }
        
        /// <summary>
        /// 检查是否可以触发
        /// </summary>
        protected virtual bool CanTrigger()
        {
            // 检查是否已经使用过（一次性使用）
            if (oneTimeUse && isTriggered)
            {
                Debug.Log("This trigger can only be used once");
                return false;
            }
            
            // 检查冷却时间
            if (isOnCooldown)
            {
                Debug.Log("Trigger is on cooldown");
                return false;
            }
            
            // 检查是否有关联的平台
            if (targetPlatforms == null || targetPlatforms.Length == 0)
            {
                Debug.LogWarning("No target platforms assigned to trigger");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 结束冷却时间
        /// </summary>
        protected virtual void EndCooldown()
        {
            isOnCooldown = false;
            if (!oneTimeUse)
            {
                isTriggered = false;
            }
            UpdateVisualState();
        }
        
        /// <summary>
        /// 更新视觉状态
        /// </summary>
        protected virtual void UpdateVisualState()
        {
            bool isActive = CanTrigger();
            
            if (activeVisual != null)
            {
                activeVisual.SetActive(isActive);
            }
            
            if (inactiveVisual != null)
            {
                inactiveVisual.SetActive(!isActive);
            }
        }
        
        /// <summary>
        /// 触发器激活时的回调（子类可重写）
        /// </summary>
        protected virtual void OnTriggerActivated()
        {
            // 子类可以重写此方法来添加特殊效果
        }
        
        /// <summary>
        /// 重置触发器状态
        /// </summary>
        public virtual void ResetTrigger()
        {
            isTriggered = false;
            isOnCooldown = false;
            lastTriggerTime = 0f;
            CancelInvoke();
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置目标平台
        /// </summary>
        /// <param name="platforms">目标平台数组</param>
        public void SetTargetPlatforms(MovingPlatform[] platforms)
        {
            targetPlatforms = platforms;
        }
        
        /// <summary>
        /// 添加目标平台
        /// </summary>
        /// <param name="platform">要添加的平台</param>
        public void AddTargetPlatform(MovingPlatform platform)
        {
            if (platform == null) return;
            
            // 检查是否已经存在
            foreach (var existing in targetPlatforms)
            {
                if (existing == platform) return;
            }
            
            // 扩展数组并添加新平台
            MovingPlatform[] newArray = new MovingPlatform[targetPlatforms.Length + 1];
            targetPlatforms.CopyTo(newArray, 0);
            newArray[targetPlatforms.Length] = platform;
            targetPlatforms = newArray;
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (targetPlatforms == null) return;
            
            // 绘制到目标平台的连线
            Gizmos.color = Color.yellow;
            foreach (var platform in targetPlatforms)
            {
                if (platform != null)
                {
                    Gizmos.DrawLine(transform.position, platform.transform.position);
                    Gizmos.DrawWireSphere(platform.transform.position, 0.5f);
                }
            }
        }
    }
} 