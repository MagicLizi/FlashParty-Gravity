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
                    Debug.Log("Gravity platform conditions met, starting movement");
                    MoveToNextWaypoint();
                }
                else if (!isMoving)
                {
                    // 调试信息：为什么不能移动
                    Vector3 gravityDir = GetCurrentGravityDirection();
                    int targetIndex = GetTargetWaypointByGravity();
                    
                    if (targetIndex != -1)
                    {
                        Vector3 targetPos = platform.WaypointPositions[targetIndex];
                        bool hasObstacle = HasObstacleInPath(platform.transform.position, targetPos);
                        float distanceToTarget = Vector3.Distance(platform.transform.position, targetPos);
                        
                        Debug.Log($"Gravity platform waiting - GravityDir: {gravityDir}, TargetWaypoint: {targetIndex}, Distance: {distanceToTarget:F2}, HasObstacle: {hasObstacle}");
                    }
                    else
                    {
                        Debug.Log($"Gravity platform waiting - GravityDir: {gravityDir}, No matching waypoint found");
                    }
                }
                
                // 等待一小段时间再次检查
                yield return new WaitForSeconds(platform.Config.gravityCheckInterval);
            }
        }
        
        /// <summary>
        /// 检查是否可以移动到重力方向对应的路径点
        /// </summary>
        private bool CanMoveToNextWaypoint()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            
            // 重力平台必须有且仅有两个路径点
            if (waypoints.Length != 2)
            {
                Debug.LogWarning("Gravity platform must have exactly 2 waypoints");
                return false;
            }
            
            // 根据重力方向选择目标路径点
            int targetIndex = GetTargetWaypointByGravity();
            if (targetIndex == -1)
            {
                return false; // 重力方向不匹配任何路径点
            }
            
            // 如果已经在目标位置，不需要移动
            Vector3 currentPos = platform.transform.position;
            Vector3 targetPos = waypoints[targetIndex];
            if (Vector3.Distance(currentPos, targetPos) < 0.1f)
            {
                return false; // 已经在目标位置
            }
            
            // 检查路径上是否有阻挡
            if (HasObstacleInPath(currentPos, targetPos))
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 根据重力方向选择目标路径点
        /// </summary>
        private int GetTargetWaypointByGravity()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            Vector3 gravityDirection = GetCurrentGravityDirection();
            
            // 强制重力方向为2D（忽略Z轴）
            gravityDirection.z = 0;
            gravityDirection = gravityDirection.normalized;
            
            Vector3 currentPos = platform.transform.position;
            
            // 计算到两个路径点的方向（强制2D计算，忽略Z轴）
            Vector3 dirToWaypoint0Raw = waypoints[0] - currentPos;
            Vector3 dirToWaypoint1Raw = waypoints[1] - currentPos;
            
            // 强制Z轴为0，确保2D计算
            dirToWaypoint0Raw.z = 0;
            dirToWaypoint1Raw.z = 0;
            
            Vector3 dirToWaypoint0 = dirToWaypoint0Raw.normalized;
            Vector3 dirToWaypoint1 = dirToWaypoint1Raw.normalized;
            
            // 调试：显示原始坐标和方向计算
            Debug.Log($"Current pos: {currentPos}, Waypoint0: {waypoints[0]}, Waypoint1: {waypoints[1]}");
            Debug.Log($"Raw dir to waypoint0: {waypoints[0] - currentPos}, Raw dir to waypoint1: {waypoints[1] - currentPos}");
            
            // 计算重力方向与两个移动方向的角度
            float angleToWaypoint0 = Vector3.Angle(gravityDirection, dirToWaypoint0);
            float angleToWaypoint1 = Vector3.Angle(gravityDirection, dirToWaypoint1);
            
            Debug.Log($"Gravity: {gravityDirection}, DirTo0: {dirToWaypoint0} (angle: {angleToWaypoint0}°), DirTo1: {dirToWaypoint1} (angle: {angleToWaypoint1}°), Tolerance: {platform.Config.gravityTolerance}°");
            
            // 选择角度最小且在容差范围内的路径点
            Debug.Log($"Checking waypoint selection: angle0={angleToWaypoint0}° <= tolerance={platform.Config.gravityTolerance}°? {angleToWaypoint0 <= platform.Config.gravityTolerance}");
            Debug.Log($"Checking waypoint selection: angle1={angleToWaypoint1}° <= tolerance={platform.Config.gravityTolerance}°? {angleToWaypoint1 <= platform.Config.gravityTolerance}");
            
            if (angleToWaypoint0 <= platform.Config.gravityTolerance && 
                angleToWaypoint0 <= angleToWaypoint1)
            {
                Debug.Log($"Target waypoint: 0 (angle: {angleToWaypoint0}°)");
                return 0;
            }
            else if (angleToWaypoint1 <= platform.Config.gravityTolerance)
            {
                Debug.Log($"Target waypoint: 1 (angle: {angleToWaypoint1}°)");
                return 1;
            }
            
            Debug.Log($"No waypoint matches gravity direction within tolerance. Config tolerance: {platform.Config.gravityTolerance}°");
            return -1; // 没有匹配的路径点
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
            
            // 执行BoxCast，使用Unity的物理层级交互设置
            RaycastHit2D hit = Physics2D.BoxCast(
                origin, boxSize, 0f, direction, distance);
            
            if (hit.collider != null)
            {
                // 只忽略平台自身和路径点
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
                
                // 所有其他碰撞体（包括Player）都会阻挡平台移动
                // Debug.Log($"Obstacle detected: {hit.collider.name} at distance {hit.distance}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 移动到重力方向对应的路径点
        /// </summary>
        private void MoveToNextWaypoint()
        {
            Vector3[] waypoints = platform.WaypointPositions;
            
            // 根据重力方向选择目标路径点
            int targetIndex = GetTargetWaypointByGravity();
            if (targetIndex == -1) return;
            
            // 转换为本地坐标
            Vector3 targetLocalPos = platform.transform.parent.InverseTransformPoint(waypoints[targetIndex]);
            
            // 计算移动时间
            float distance = Vector3.Distance(platform.transform.position, waypoints[targetIndex]);
            float moveDuration = distance / platform.Config.moveSpeed;
            
            isMoving = true;
            currentWaypointIndex = targetIndex;
            
            Debug.Log($"Gravity platform moving to waypoint {targetIndex}");
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 使用DOLocalMove移动本地坐标
            moveTweener = platform.transform.DOLocalMove(targetLocalPos, moveDuration)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                    Debug.Log($"Gravity platform reached waypoint {currentWaypointIndex}");
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
            if (waypoints.Length != 2) return Vector3.zero;
            
            int targetIndex = GetTargetWaypointByGravity();
            if (targetIndex == -1) return Vector3.zero;
            
            Vector3 currentPos = platform.transform.position;
            Vector3 targetPos = waypoints[targetIndex];
            
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