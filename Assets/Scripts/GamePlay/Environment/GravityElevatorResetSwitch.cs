using UnityEngine;
using DG.Tweening;

namespace FlashParty.Environment
{
    /// <summary>
    /// 重力电梯重启开关
    /// 被角色的attack或airattack击中后，触发电梯系统复位
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravityElevatorResetSwitch : MonoBehaviour
    {
        [Header("关联配置")]
        [Tooltip("要重置的重力电梯（拖入场景中的GravityElevator）")]
        public GravityElevator targetElevator;
        
        private Collider2D triggerCollider;
        private Animator animator;
        private GravityElevatorSystem elevatorSystem; // 运行时获取
        
        void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            
            // 确保是触发器
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
            
            // 获取 Animator（如果有）
            animator = GetComponent<Animator>();
            
            // 验证配置
            if (targetElevator == null)
            {
                Debug.LogWarning($"[GravityElevatorResetSwitch] {gameObject.name} 未配置 targetElevator！", this);
            }
        }
        
        /// <summary>
        /// 获取电梯系统（延迟获取，确保电梯系统已创建）
        /// </summary>
        private GravityElevatorSystem GetElevatorSystem()
        {
            if (elevatorSystem == null && targetElevator != null)
            {
                elevatorSystem = targetElevator.System;
            }
            return elevatorSystem;
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 检测是否是攻击碰撞器
            Atk atk = other.GetComponent<Atk>();
            if (atk != null)
            {
                // 延迟触发，配合攻击动画
                DOVirtual.DelayedCall(atk.AtkTime, () =>
                {
                    TriggerReset(atk.CurPlayer);
                });
            }
        }
        
        /// <summary>
        /// 触发电梯系统复位
        /// </summary>
        private void TriggerReset(Player player)
        {
            // 获取电梯系统（延迟获取）
            GravityElevatorSystem system = GetElevatorSystem();
            
            if (system == null)
            {
                Debug.LogWarning($"[GravityElevatorResetSwitch] 无法触发复位：elevatorSystem 为空。请检查 targetElevator 是否配置正确。", this);
                return;
            }
            
            // 播放击中效果
            if (player != null)
            {
                player.AtkShow(gameObject);
            }
            
            // 播放视觉反馈
            PlayVisualFeedback();
            
            // 触发电梯系统复位
            system.StartReset();
            
            Debug.Log($"[GravityElevatorResetSwitch] 开关被激活，电梯系统开始复位");
        }
        
        /// <summary>
        /// 播放视觉反馈动画
        /// </summary>
        private void PlayVisualFeedback()
        {
            if (animator == null)
            {
                // Debug.Log($"[GravityElevatorResetSwitch] 没有Animator组件，跳过动画");
                return;
            }
            
            // 检查是否有Reset参数
            bool hasResetTrigger = false;
            foreach (var param in animator.parameters)
            {
                if (param.name == "Reset" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasResetTrigger = true;
                    break;
                }
            }
            
            if (hasResetTrigger)
            {
                // 触发Animator的Reset动画
                animator.SetTrigger("Reset");
            }
            else
            {
                Debug.LogWarning($"[GravityElevatorResetSwitch] Animator中没有'Reset' Trigger参数！", this);
            }
        }
        
        void OnDrawGizmos()
        {
            // 绘制与电梯的连接线
            if (targetElevator != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetElevator.transform.position);
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
    }
}

