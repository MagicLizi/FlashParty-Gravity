using UnityEngine;

namespace FlashParty.Platform
{
    /// <summary>
    /// 移动平台策略接口
    /// </summary>
    public interface IMovementStrategy
    {
        /// <summary>
        /// 初始化策略
        /// </summary>
        /// <param name="platform">关联的移动平台</param>
        void Initialize(MovingPlatform platform);
        
        /// <summary>
        /// 开始移动
        /// </summary>
        void StartMovement();
        
        /// <summary>
        /// 停止移动
        /// </summary>
        void StopMovement();
        
        /// <summary>
        /// 暂停移动
        /// </summary>
        void PauseMovement();
        
        /// <summary>
        /// 恢复移动
        /// </summary>
        void ResumeMovement();
        
        /// <summary>
        /// 处理触发事件
        /// </summary>
        /// <param name="triggerData">触发数据</param>
        void OnTriggerActivated(object triggerData);
        
        /// <summary>
        /// 获取当前是否在移动中
        /// </summary>
        bool IsMoving { get; }
        
        /// <summary>
        /// 销毁时清理资源
        /// </summary>
        void OnDestroy();
    }
} 