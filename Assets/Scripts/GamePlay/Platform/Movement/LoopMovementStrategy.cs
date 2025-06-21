using UnityEngine;
using DG.Tweening;
using System.Collections;

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
        private bool wasPausedByRotation = false;
        private Coroutine rotationMonitorCoroutine;
        
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

            // 检查是否在旋转中，如果是则等待旋转结束
            if (Const.InRotation)
            {
                // Debug.Log("Scene is rotating, delaying platform movement");
                platform.StartCoroutine(WaitForRotationEnd());
                return;
            }
            
            // 如果已经在移动，先停止
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
            }
            
            StartLoopMovement();
        }

        /// <summary>
        /// 等待场景旋转结束
        /// </summary>
        private System.Collections.IEnumerator WaitForRotationEnd()
        {
            while (Const.InRotation)
            {
                yield return null;
            }
            
            // 旋转结束后开始移动
            StartLoopMovement();
        }

        /// <summary>
        /// 开始循环移动
        /// </summary>
        private void StartLoopMovement()
        {
            // 创建本地坐标路径点数组
            Vector3[] pathPoints = new Vector3[platform.Waypoints.Length];
            for (int i = 0; i < platform.Waypoints.Length; i++)
            {
                if (platform.transform.parent != null)
                {
                    // 转换为相对于父对象的本地坐标
                    pathPoints[i] = platform.transform.parent.InverseTransformPoint(platform.Waypoints[i].position);
                }
                else
                {
                    // 如果没有父对象，直接使用世界坐标
                    pathPoints[i] = platform.Waypoints[i].position;
                }
                // Debug.Log($"Waypoint {i}: World={platform.Waypoints[i].position}, Local={pathPoints[i]}");
            }
            
            // 计算移动时间
            float totalDistance = CalculatePathDistance(pathPoints);
            float moveDuration = totalDistance / platform.Config.moveSpeed;
            
            // Debug.Log($"Starting loop movement: Distance={totalDistance}, Duration={moveDuration}s, Speed={platform.Config.moveSpeed}");
            
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
            wasPausedByRotation = false; // 重置暂停状态
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 根据路径类型选择移动方式
            DG.Tweening.PathType pathType = ConvertPathType(platform.Config.pathType);
            
            // Debug.Log($"Creating DOLocalPath with {pathPoints.Length} points, duration={moveDuration}s");
            // Debug.Log($"Current platform local position: {platform.transform.localPosition}");
            
            // 简化测试：如果只有两个路径点，使用DOLocalMove往返
            if (pathPoints.Length == 2)
            {
                StartSimpleMovement(pathPoints);
            }
            else
            {
                // 使用DOLocalPath移动本地坐标
                moveTweener = platform.transform.DOLocalPath(pathPoints, moveDuration, pathType)
                    .SetEase(platform.Config.easeType)
                    .SetLoops(-1, platform.Config.reverseOnComplete ? LoopType.Yoyo : LoopType.Restart)
                    .OnComplete(() => {
                        isMoving = false;
                        EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                        // Debug.Log("Platform loop movement completed (this shouldn't happen with infinite loops)");
                    });
                
                // Debug.Log($"DOTween created: IsActive={moveTweener.IsActive()}, IsPlaying={moveTweener.IsPlaying()}");
                
                // 如果设置了路径点等待时间，添加回调
                if (platform.Config.waitTimeAtWaypoint > 0)
                {
                    AddWaypointPauses(pathPoints);
                }
            }

            // 启动独立的旋转监控协程
            if (rotationMonitorCoroutine != null)
            {
                platform.StopCoroutine(rotationMonitorCoroutine);
            }
            rotationMonitorCoroutine = platform.StartCoroutine(MonitorRotationStatus());
        }

        /// <summary>
        /// 简单的两点往返移动（用于调试）
        /// </summary>
        private void StartSimpleMovement(Vector3[] pathPoints)
        {
            // Debug.Log("Starting simple two-point movement");
            
            // 计算单段移动时间
            float distance = Vector3.Distance(pathPoints[0], pathPoints[1]);
            float segmentDuration = distance / platform.Config.moveSpeed;
            
            // Debug.Log($"Simple movement: from {pathPoints[0]} to {pathPoints[1]}, distance={distance}, duration={segmentDuration}s");
            
            // 移动到第一个点，然后开始往返
            platform.transform.localPosition = pathPoints[0];
            
            moveTweener = platform.transform.DOLocalMove(pathPoints[1], segmentDuration)
                .SetEase(platform.Config.easeType)
                .SetLoops(-1, LoopType.Yoyo);
                
            // Debug.Log($"Simple DOTween created: IsActive={moveTweener.IsActive()}, IsPlaying={moveTweener.IsPlaying()}");
        }

        /// <summary>
        /// 独立的旋转状态监控协程
        /// </summary>
        private System.Collections.IEnumerator MonitorRotationStatus()
        {
            // Debug.Log("Started rotation status monitoring");
            
            while (moveTweener != null && moveTweener.IsActive())
            {
                if (Const.InRotation && !wasPausedByRotation)
                {
                    // 场景开始旋转，暂停移动
                    wasPausedByRotation = true;
                    // Debug.Log($"Platform movement paused due to scene rotation. IsPlaying: {moveTweener.IsPlaying()}");
                    moveTweener.Pause();
                }
                else if (!Const.InRotation && wasPausedByRotation)
                {
                    // 场景旋转结束，恢复移动
                    wasPausedByRotation = false;
                    // Debug.Log($"Platform movement resumed after scene rotation. IsPlaying before resume: {moveTweener.IsPlaying()}");
                    moveTweener.Play();
                    // Debug.Log($"Platform movement resumed after scene rotation. IsPlaying after resume: {moveTweener.IsPlaying()}");
                }
                
                yield return new WaitForSeconds(0.1f); // 每0.1秒检查一次
            }
            
            // Debug.Log("Rotation status monitoring stopped");
        }

        /// <summary>
        /// 检查旋转状态并相应暂停/恢复（保留作为备用方法）
        /// </summary>
        private void CheckRotationStatus()
        {
            if (Const.InRotation && !wasPausedByRotation)
            {
                // 场景开始旋转，暂停移动
                wasPausedByRotation = true;
                if (moveTweener != null && moveTweener.IsActive())
                {
                    // Debug.Log($"Platform movement paused due to scene rotation. IsPlaying: {moveTweener.IsPlaying()}");
                    moveTweener.Pause();
                }
            }
            else if (!Const.InRotation && wasPausedByRotation)
            {
                // 场景旋转结束，恢复移动
                wasPausedByRotation = false;
                if (moveTweener != null && moveTweener.IsActive())
                {
                    // Debug.Log($"Platform movement resumed after scene rotation. IsPlaying before resume: {moveTweener.IsPlaying()}");
                    moveTweener.Play();
                    // Debug.Log($"Platform movement resumed after scene rotation. IsPlaying after resume: {moveTweener.IsPlaying()}");
                }
                else
                {
                    // Debug.LogWarning("Cannot resume platform movement - tweener is null or not active");
                    // 如果tweener无效，重新开始移动
                    if (platform != null)
                    {
                        // Debug.Log("Restarting platform movement after rotation");
                        StartLoopMovement();
                    }
                }
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
            float totalDistance = 0f;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            }
            
            // 如果不是反向循环，还要加上最后一点到第一点的距离
            if (!platform.Config.reverseOnComplete)
            {
                totalDistance += Vector3.Distance(pathPoints[pathPoints.Length - 1], pathPoints[0]);
            }
            
            return totalDistance;
        }
        
        /// <summary>
        /// 计算每段路径的时间比例
        /// </summary>
        private float[] CalculateSegmentTimes(Vector3[] pathPoints)
        {
            float[] distances = new float[pathPoints.Length - 1];
            float totalDistance = 0f;
            
            // 计算每段距离
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                distances[i] = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
                totalDistance += distances[i];
            }
            
            // 计算时间比例
            float[] timeRatios = new float[distances.Length];
            float accumulatedTime = 0f;
            
            for (int i = 0; i < distances.Length; i++)
            {
                accumulatedTime += distances[i] / totalDistance;
                timeRatios[i] = accumulatedTime;
            }
            
            return timeRatios;
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
            // 停止旋转监控协程
            if (rotationMonitorCoroutine != null)
            {
                platform.StopCoroutine(rotationMonitorCoroutine);
                rotationMonitorCoroutine = null;
            }
            
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
        
        public void OnTriggerActivated(object triggerData)
        {
            // 循环移动策略不响应触发事件
            // Debug.Log("Loop movement strategy does not respond to trigger events");
        }
        
        public void OnDestroy()
        {
            // 停止旋转监控协程
            if (rotationMonitorCoroutine != null)
            {
                platform.StopCoroutine(rotationMonitorCoroutine);
                rotationMonitorCoroutine = null;
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