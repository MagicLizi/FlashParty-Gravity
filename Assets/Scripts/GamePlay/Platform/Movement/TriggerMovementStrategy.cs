using UnityEngine;
using DG.Tweening;

namespace FlashParty.Platform
{
    /// <summary>
    /// 触发移动策略 - 受机关控制的平台移动
    /// </summary>
    public class TriggerMovementStrategy : IMovementStrategy
    {
        private MovingPlatform platform;
        private Tweener moveTweener;
        private bool isInitialized = false;
        private bool isMoving = false;
        private int currentPathIndex = 0;
        private float lastTriggerTime = 0f;
        private Vector3 originalPosition;
        private bool wasPausedByRotation = false;
        
        public bool IsMoving => isMoving;
        
        public void Initialize(MovingPlatform platform)
        {
            this.platform = platform;
            isInitialized = true;
            
            // 记录初始位置（本地坐标）
            originalPosition = platform.transform.localPosition;
        }
        
        public void StartMovement()
        {
            if (!isInitialized || platform == null)
            {
                Debug.LogWarning("Trigger movement strategy not properly initialized");
                return;
            }
            
            // Debug.Log("Trigger movement strategy initialized, waiting for trigger events");
        }
        
        public void OnTriggerActivated(object triggerData)
        {
            // 检查冷却时间
            if (platform.Config.triggerCooldown > 0 && 
                Time.time - lastTriggerTime < platform.Config.triggerCooldown)
            {
                // Debug.Log($"Trigger on cooldown, remaining: {platform.Config.triggerCooldown - (Time.time - lastTriggerTime):F1}s");
                return;
            }

            // 检查是否在旋转中
            if (Const.InRotation)
            {
                // Debug.Log("Scene is rotating, delaying platform trigger");
                platform.StartCoroutine(WaitForRotationEndThenMove());
                return;
            }
            
            // 检查是否正在移动
            if (isMoving)
            {
                // Debug.Log("Platform is already moving, ignoring trigger");
                return;
            }
            
            ExecuteMovement();
        }

        /// <summary>
        /// 等待场景旋转结束然后移动
        /// </summary>
        private System.Collections.IEnumerator WaitForRotationEndThenMove()
        {
            while (Const.InRotation)
            {
                yield return null;
            }
            
            // 旋转结束后执行移动
            ExecuteMovement();
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        private void ExecuteMovement()
        {
            lastTriggerTime = Time.time;
            
            Vector3[] pathPoints = GetCurrentPath();
            if (pathPoints.Length == 0)
            {
                Debug.LogWarning("No valid path points for trigger movement");
                return;
            }
            
            // 计算移动时间
            float totalDistance = CalculatePathDistance(pathPoints);
            float moveDuration = totalDistance / platform.Config.moveSpeed;
            
            // 设置初始延迟
            if (platform.Config.initialDelay > 0)
            {
                DOVirtual.DelayedCall(platform.Config.initialDelay, () => StartPathMovement(pathPoints, moveDuration));
            }
            else
            {
                StartPathMovement(pathPoints, moveDuration);
            }
        }
        
        /// <summary>
        /// 获取当前路径
        /// </summary>
        private Vector3[] GetCurrentPath()
        {
            if (platform.Waypoints.Length < 2) return new Vector3[0];
            
            Vector3[] pathPoints;
            
            if (platform.Config.triggerMode == TriggerMode.SinglePath)
            {
                // 单路径模式：从当前位置移动到下一个路径点
                int nextIndex = (currentPathIndex + 1) % platform.Waypoints.Length;
                pathPoints = new Vector3[2];
                pathPoints[0] = platform.transform.parent.InverseTransformPoint(platform.transform.position);
                pathPoints[1] = platform.transform.parent.InverseTransformPoint(platform.Waypoints[nextIndex].position);
                currentPathIndex = nextIndex;
            }
            else
            {
                // 完整路径模式：移动完整路径
                pathPoints = new Vector3[platform.Waypoints.Length];
                for (int i = 0; i < platform.Waypoints.Length; i++)
                {
                    pathPoints[i] = platform.transform.parent.InverseTransformPoint(platform.Waypoints[i].position);
                }
            }
            
            return pathPoints;
        }
        
        /// <summary>
        /// 开始路径移动
        /// </summary>
        private void StartPathMovement(Vector3[] pathPoints, float moveDuration)
        {
            if (platform == null) return;
            
            isMoving = true;
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 根据路径类型选择移动方式
            DG.Tweening.PathType pathType = ConvertPathType(platform.Config.pathType);
            
            // 使用DOLocalPath移动本地坐标
            moveTweener = platform.transform.DOLocalPath(pathPoints, moveDuration, pathType)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                    
                    // 处理等待时间
                    if (platform.Config.waitTimeAtWaypoint > 0)
                    {
                        DOVirtual.DelayedCall(platform.Config.waitTimeAtWaypoint, () => {
                            // Debug.Log("Platform finished waiting at waypoint");
                        });
                    }
                });

            // 添加Update回调来检查旋转状态
            moveTweener.OnUpdate(() => {
                CheckRotationStatus();
            });
            
            // Debug.Log($"Platform started trigger movement, duration: {moveDuration:F2}s");
        }

        /// <summary>
        /// 检查旋转状态并相应暂停/恢复
        /// </summary>
        private void CheckRotationStatus()
        {
            if (Const.InRotation && !wasPausedByRotation)
            {
                // 场景开始旋转，暂停移动
                wasPausedByRotation = true;
                if (moveTweener != null && moveTweener.IsActive() && moveTweener.IsPlaying())
                {
                    moveTweener.Pause();
                    // Debug.Log("Platform movement paused due to scene rotation");
                }
            }
            else if (!Const.InRotation && wasPausedByRotation)
            {
                // 场景旋转结束，恢复移动
                wasPausedByRotation = false;
                if (moveTweener != null && moveTweener.IsActive() && !moveTweener.IsPlaying())
                {
                    moveTweener.Play();
                    // Debug.Log("Platform movement resumed after scene rotation");
                }
            }
        }
        
        /// <summary>
        /// 计算路径总距离
        /// </summary>
        private float CalculatePathDistance(Vector3[] pathPoints)
        {
            float totalDistance = 0f;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
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
                wasPausedByRotation = false;
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
            platform.transform.localPosition = originalPosition;
            currentPathIndex = 0;
            lastTriggerTime = 0f;
        }
        
        public void OnDestroy()
        {
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
            }
            moveTweener = null;
            platform = null;
        }
    }
} 