using UnityEngine;
using DG.Tweening;

namespace FlashParty.Platform
{
    /// <summary>
    /// 循环移动策略 - 平台按固定轨迹循环移动
    /// </summary>
    public class LoopMovementStrategy : IMovementStrategy
    {
        private MovingPlatform platform;
        private Tweener moveTweener;
        private bool isInitialized = false;
        private bool isMoving = false;
        
        public bool IsMoving => isMoving;
        
        public void Initialize(MovingPlatform platform)
        {
            this.platform = platform;
            isInitialized = true;
        }
        
        public void StartMovement()
        {
            if (!isInitialized || platform == null || platform.Waypoints.Length < 2)
            {
                Debug.LogWarning("Loop movement strategy not properly initialized or insufficient waypoints");
                return;
            }
            
            // 如果已经在移动，先停止
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
            }
            
            // 创建路径点数组
            Vector3[] pathPoints = new Vector3[platform.Waypoints.Length];
            for (int i = 0; i < platform.Waypoints.Length; i++)
            {
                pathPoints[i] = platform.Waypoints[i].position;
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
        /// 开始路径移动
        /// </summary>
        private void StartPathMovement(Vector3[] pathPoints, float moveDuration)
        {
            if (platform == null) return;
            
            isMoving = true;
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 根据路径类型选择移动方式
            DG.Tweening.PathType pathType = ConvertPathType(platform.Config.pathType);
            
            moveTweener = platform.transform.DOPath(pathPoints, moveDuration, pathType)
                .SetEase(platform.Config.easeType)
                .SetLoops(-1, platform.Config.reverseOnComplete ? LoopType.Yoyo : LoopType.Restart)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                });
            
            // 如果设置了路径点等待时间，添加回调
            if (platform.Config.waitTimeAtWaypoint > 0)
            {
                AddWaypointPauses(pathPoints);
            }
        }
        
        /// <summary>
        /// 添加路径点停留功能
        /// </summary>
        private void AddWaypointPauses(Vector3[] pathPoints)
        {
            // 计算每个路径点的时间比例
            float[] segmentTimes = CalculateSegmentTimes(pathPoints);
            
            for (int i = 0; i < segmentTimes.Length; i++)
            {
                float timeRatio = segmentTimes[i];
                moveTweener.OnStepComplete(() => {
                    PauseMovement();
                    DOVirtual.DelayedCall(platform.Config.waitTimeAtWaypoint, ResumeMovement);
                });
            }
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
        /// 计算各段路径时间比例
        /// </summary>
        private float[] CalculateSegmentTimes(Vector3[] pathPoints)
        {
            float[] segmentTimes = new float[pathPoints.Length - 1];
            float totalDistance = CalculatePathDistance(pathPoints);
            
            float currentTime = 0;
            for (int i = 1; i < pathPoints.Length; i++)
            {
                float segmentDistance = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
                currentTime += segmentDistance / totalDistance;
                segmentTimes[i - 1] = currentTime;
            }
            
            return segmentTimes;
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
        
        public void OnTriggerActivated(object triggerData)
        {
            // 循环移动策略不响应触发事件
            Debug.Log("Loop movement strategy does not respond to trigger events");
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