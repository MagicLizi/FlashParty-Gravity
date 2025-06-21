using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace FlashParty.Platform
{
    /// <summary>
    /// 重力移动策略 - 根据重力方向移动的平台
    /// </summary>
    public class GravityMovementStrategy : IMovementStrategy
    {
        private MovingPlatform platform;
        private Tweener moveTweener;
        private bool isInitialized = false;
        private bool isMoving = false;
        private int currentWaypointIndex = 0;
        private Coroutine gravityCheckCoroutine;
        private bool wasPausedByRotation = false;
        
        public bool IsMoving => isMoving;
        
        public void Initialize(MovingPlatform platform)
        {
            this.platform = platform;
            isInitialized = true;
            currentWaypointIndex = 0;
        }
        
        public void StartMovement()
        {
            if (!isInitialized || platform == null)
            {
                Debug.LogWarning("Gravity movement strategy not properly initialized");
                return;
            }
            
            // 开始重力检测协程
            if (gravityCheckCoroutine == null)
            {
                gravityCheckCoroutine = platform.StartCoroutine(GravityCheckLoop());
            }
            
            // Debug.Log("Gravity movement strategy started, waiting for gravity conditions");
        }
        
        /// <summary>
        /// 重力检测循环
        /// </summary>
        private IEnumerator GravityCheckLoop()
        {
            while (true)
            {
                // 检查是否在旋转中，如果是则暂停检测
                if (Const.InRotation)
                {
                    yield return null;
                    continue;
                }

                // 如果不在移动状态，检查是否可以移动
                if (!isMoving && CanMoveToNextWaypoint())
                {
                    MoveToNextWaypoint();
                }
                
                // 等待一小段时间再次检查
                yield return new WaitForSeconds(platform.Config.gravityCheckInterval);
            }
        }
        
        /// <summary>
        /// 检查是否可以移动到下一个路径点
        /// </summary>
        private bool CanMoveToNextWaypoint()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            
            // 计算下一个路径点索引
            int nextIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Vector3 currentPos = platform.transform.position;
            Vector3 targetPos = waypoints[nextIndex];
            
            // 计算移动方向
            Vector3 moveDirection = (targetPos - currentPos).normalized;
            
            // 检查重力方向是否匹配
            if (!IsGravityDirectionMatching(moveDirection))
            {
                return false;
            }
            
            // 检查路径上是否有阻挡
            if (HasObstacleInPath(currentPos, targetPos))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查重力方向是否匹配移动方向
        /// </summary>
        private bool IsGravityDirectionMatching(Vector3 moveDirection)
        {
            // 获取当前重力方向（考虑场景旋转）
            Vector3 gravityDirection = GetWorldGravityDirection();
            
            // 如果重力为零，不移动
            if (gravityDirection.magnitude < 0.01f)
            {
                return false;
            }
            
            // 计算移动方向和重力方向的角度
            float angle = Vector3.Angle(moveDirection, gravityDirection);
            
            // 在容差范围内认为方向匹配
            // Debug.Log($"Move direction: {moveDirection}, Gravity direction: {gravityDirection}, Angle: {angle}°");
            return angle <= platform.Config.gravityTolerance;
        }
        
        /// <summary>
        /// 获取世界空间中的重力方向（考虑场景旋转）
        /// </summary>
        private Vector3 GetWorldGravityDirection()
        {
            // 基础重力方向（向下）
            Vector3 baseGravity = Vector3.down;
            
            // 尝试找到场景根节点来获取旋转信息
            Transform levelRoot = FindLevelRoot();
            if (levelRoot != null)
            {
                // 将基础重力方向转换到场景旋转后的世界空间
                return levelRoot.TransformDirection(baseGravity);
            }
            
            // 如果找不到场景根节点，使用Physics2D.gravity
            return Physics2D.gravity.normalized;
        }
        
        /// <summary>
        /// 查找场景根节点
        /// </summary>
        private Transform FindLevelRoot()
        {
            // 尝试通过多种方式找到场景根节点
            
            // 方法1：通过LevelRotator找到
            LevelRotator[] rotators = Object.FindObjectsOfType<LevelRotator>();
            if (rotators.Length > 0 && rotators[0].levelRoot != null)
            {
                return rotators[0].levelRoot;
            }
            
            // 方法2：查找名为"Level"的GameObject
            GameObject levelObj = GameObject.Find("Level");
            if (levelObj != null)
            {
                return levelObj.transform;
            }
            
            // 方法3：检查平台的父级链，看是否有合适的根节点
            Transform current = platform.transform.parent;
            while (current != null)
            {
                if (current.name.ToLower().Contains("level"))
                {
                    return current;
                }
                current = current.parent;
            }
            
            return null;
        }
        
        /// <summary>
        /// 检查路径上是否有阻挡
        /// </summary>
        private bool HasObstacleInPath(Vector3 startPos, Vector3 endPos)
        {
            // 获取平台的碰撞盒
            Collider2D platformCollider = platform.GetComponent<Collider2D>();
            if (platformCollider == null)
            {
                Debug.LogWarning("Platform has no collider for obstacle detection");
                return false;
            }
            
            // 计算移动方向和距离
            Vector3 direction = (endPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, endPos);
            
            // 使用BoxCast检测路径上的障碍物
            Bounds bounds = platformCollider.bounds;
            Vector2 boxSize = new Vector2(bounds.size.x, bounds.size.y);
            
            // 从平台中心开始检测
            Vector2 origin = bounds.center;
            
            // 执行BoxCast
            RaycastHit2D hit = Physics2D.BoxCast(
                origin, boxSize, 0f, direction, distance, platform.Config.obstacleLayerMask);
            
            if (hit.collider != null)
            {
                // 忽略平台自身和路径点
                if (hit.collider.gameObject == platform.gameObject)
                {
                    return false;
                }
                
                // 检查是否是路径点
                foreach (Transform waypoint in platform.Waypoints)
                {
                    if (hit.collider.gameObject == waypoint.gameObject)
                    {
                        return false;
                    }
                }
                
                // Debug.Log($"Obstacle detected: {hit.collider.name} at distance {hit.distance}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 移动到下一个路径点
        /// </summary>
        private void MoveToNextWaypoint()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            int nextIndex = (currentWaypointIndex + 1) % waypoints.Length;
            
            // 转换为本地坐标
            Vector3 targetLocalPos = platform.transform.parent.InverseTransformPoint(waypoints[nextIndex]);
            
            // 计算移动时间
            float distance = Vector3.Distance(platform.transform.position, waypoints[nextIndex]);
            float moveDuration = distance / platform.Config.moveSpeed;
            
            isMoving = true;
            currentWaypointIndex = nextIndex;
            
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 使用DOLocalMove移动本地坐标
            moveTweener = platform.transform.DOLocalMove(targetLocalPos, moveDuration)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                    // Debug.Log($"Platform reached waypoint {currentWaypointIndex}");
                });

            // 添加Update回调来检查旋转状态
            moveTweener.OnUpdate(() => {
                CheckRotationStatus();
            });
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
        /// 获取当前重力方向（考虑场景旋转）
        /// </summary>
        public Vector3 GetCurrentGravityDirection()
        {
            return GetWorldGravityDirection();
        }
        
        /// <summary>
        /// 获取到下一个路径点的方向
        /// </summary>
        public Vector3 GetNextMoveDirection()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            if (waypoints.Length < 2) return Vector3.zero;
            
            int nextIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Vector3 currentPos = platform.transform.position;
            Vector3 targetPos = waypoints[nextIndex];
            
            return (targetPos - currentPos).normalized;
        }
        
        /// <summary>
        /// 检查当前是否可以移动
        /// </summary>
        public bool CanMoveNow()
        {
            return CanMoveToNextWaypoint();
        }
        
        public void StopMovement()
        {
            if (gravityCheckCoroutine != null)
            {
                platform.StopCoroutine(gravityCheckCoroutine);
                gravityCheckCoroutine = null;
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
            // 重力移动策略不响应触发事件
            // Debug.Log("Gravity movement strategy does not respond to trigger events");
        }
        
        /// <summary>
        /// 重置到最近的路径点
        /// </summary>
        public void ResetToNearestWaypoint()
        {
            if (platform == null || platform.Waypoints.Length == 0) return;
            
            // 停止当前移动
            StopMovement();
            
            // 找到最近的路径点
            Vector3 currentPos = platform.transform.position;
            float minDistance = float.MaxValue;
            int nearestIndex = 0;
            
            for (int i = 0; i < platform.Waypoints.Length; i++)
            {
                float distance = Vector3.Distance(currentPos, platform.Waypoints[i].position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
            
            // 移动到最近的路径点（本地坐标）
            Vector3 targetLocalPos = platform.transform.parent.InverseTransformPoint(platform.Waypoints[nearestIndex].position);
            platform.transform.localPosition = targetLocalPos;
            currentWaypointIndex = nearestIndex;
            
            // 重新开始移动
            StartMovement();
        }
        
        /// <summary>
        /// 强制移动到指定路径点索引
        /// </summary>
        public void ForceMoveTo(int waypointIndex)
        {
            Vector3[] waypoints = platform.WaypointPositions;
            if (waypointIndex < 0 || waypointIndex >= waypoints.Length) return;
            
            StopMovement();
            currentWaypointIndex = waypointIndex;
            platform.transform.position = waypoints[waypointIndex];
            StartMovement();
        }
        
        public void OnDestroy()
        {
            if (gravityCheckCoroutine != null)
            {
                platform.StopCoroutine(gravityCheckCoroutine);
                gravityCheckCoroutine = null;
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