using UnityEngine;
using DG.Tweening;

namespace FlashParty.Platform
{
    /// <summary>
    /// 触发移动策略 - 平台响应机关控制移动
    /// </summary>
    public class TriggerMovementStrategy : IMovementStrategy
    {
        private MovingPlatform platform;
        private Tweener moveTweener;
        private bool isInitialized = false;
        private bool isMoving = false;
        private float lastTriggerTime = 0f;
        private int currentPathIndex = 0;
        private Vector3 originalPosition;
        
        public bool IsMoving => isMoving;
        
        public void Initialize(MovingPlatform platform)
        {
            this.platform = platform;
            originalPosition = platform.transform.position;
            isInitialized = true;
            
            // 注册触发事件
            EventManager.Instance.AddListener(EventType.PlatformTrigger, OnTriggerActivated);
        }
        
        public void StartMovement()
        {
            // 触发移动策略不会自动开始移动，需要等待触发事件
            Debug.Log("Trigger movement strategy is ready, waiting for trigger events");
        }
        
        public void OnTriggerActivated(object triggerData)
        {
            if (!isInitialized || platform == null)
            {
                Debug.LogWarning("Trigger movement strategy not properly initialized");
                return;
            }
            
            // 检查冷却时间
            if (Time.time - lastTriggerTime < platform.Config.triggerCooldown)
            {
                Debug.Log("Platform trigger is on cooldown");
                return;
            }
            
            // 检查是否允许多次触发
            if (!platform.Config.canTriggerMultipleTimes && currentPathIndex > 0)
            {
                Debug.Log("Platform can only be triggered once");
                return;
            }
            
            // 如果正在移动，根据配置决定是否中断
            if (isMoving)
            {
                Debug.Log("Platform is already moving");
                return;
            }
            
            ExecuteTriggerMovement();
        }
        
        /// <summary>
        /// 执行触发移动
        /// </summary>
        private void ExecuteTriggerMovement()
        {
            if (platform.Waypoints.Length < 2)
            {
                Debug.LogWarning("Insufficient waypoints for trigger movement");
                return;
            }
            
            lastTriggerTime = Time.time;
            
            // 创建路径点数组
            Vector3[] pathPoints = new Vector3[platform.Waypoints.Length];
            for (int i = 0; i < platform.Waypoints.Length; i++)
            {
                pathPoints[i] = platform.Waypoints[i].position;
            }
            
            // 计算移动时间
            float totalDistance = CalculatePathDistance(pathPoints);
            float moveDuration = totalDistance / platform.Config.moveSpeed;
            
            // 开始移动
            StartTriggerMovement(pathPoints, moveDuration);
        }
        
        /// <summary>
        /// 开始触发移动
        /// </summary>
        private void StartTriggerMovement(Vector3[] pathPoints, float moveDuration)
        {
            isMoving = true;
            currentPathIndex++;
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 根据路径类型选择移动方式
            DG.Tweening.PathType pathType = ConvertPathType(platform.Config.pathType);
            
            moveTweener = platform.transform.DOPath(pathPoints, moveDuration, pathType)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    OnMovementComplete();
                });
        }
        
        /// <summary>
        /// 移动完成回调
        /// </summary>
        private void OnMovementComplete()
        {
            isMoving = false;
            EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
            
            // 如果设置了返回起始位置
            if (platform.Config.returnToStart)
            {
                DOVirtual.DelayedCall(1f, () => ReturnToStart());
            }
        }
        
        /// <summary>
        /// 返回起始位置
        /// </summary>
        private void ReturnToStart()
        {
            if (platform == null || isMoving) return;
            
            float returnDistance = Vector3.Distance(platform.transform.position, originalPosition);
            float returnDuration = returnDistance / platform.Config.moveSpeed;
            
            isMoving = true;
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            moveTweener = platform.transform.DOMove(originalPosition, returnDuration)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                    
                    // 重置路径索引，允许再次触发
                    if (platform.Config.canTriggerMultipleTimes)
                    {
                        currentPathIndex = 0;
                    }
                });
        }
        
        /// <summary>
        /// 计算路径总距离
        /// </summary>
        private float CalculatePathDistance(Vector3[] pathPoints)
        {
            float totalDistance = 0;
            for (int i = 1; i < pathPoints.Length; i++)
            {
                totalDistance += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            }
            return totalDistance;
        }
        
        /// <summary>
        /// 转换路径类型
        /// </summary>
        private DG.Tweening.PathType ConvertPathType(FlashParty.Platform.PathType pathType)
        {
            switch (pathType)
            {
                case FlashParty.Platform.PathType.Linear:
                    return DG.Tweening.PathType.Linear;
                case FlashParty.Platform.PathType.CatmullRom:
                    return DG.Tweening.PathType.CatmullRom;
                default:
                    return DG.Tweening.PathType.Linear;
            }
        }
        
        public void StopMovement()
        {
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
                isMoving = false;
                EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
            }
        }
        
        public void PauseMovement()
        {
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Pause();
            }
        }
        
        public void ResumeMovement()
        {
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Play();
            }
        }
        
        /// <summary>
        /// 手动触发移动（用于测试或特殊情况）
        /// </summary>
        public void ManualTrigger()
        {
            OnTriggerActivated(null);
        }
        
        /// <summary>
        /// 重置平台状态
        /// </summary>
        public void ResetPlatform()
        {
            StopMovement();
            platform.transform.position = originalPosition;
            currentPathIndex = 0;
            lastTriggerTime = 0f;
        }
        
        public void OnDestroy()
        {
            // 移除事件监听
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener(EventType.PlatformTrigger, OnTriggerActivated);
            }
            
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
            }
            moveTweener = null;
            platform = null;
        }
    }
} 