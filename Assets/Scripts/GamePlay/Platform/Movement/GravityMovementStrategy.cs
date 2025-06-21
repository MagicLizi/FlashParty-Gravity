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
        private bool wasInRotation = false;
        
        public bool IsMoving => isMoving;
        
        public void Initialize(MovingPlatform platform)
        {
            this.platform = platform;
            isInitialized = true;
            currentWaypointIndex = 0;
            wasInRotation = Const.InRotation; // 初始化旋转状态
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
                // 检查旋转状态变化
                bool currentlyInRotation = Const.InRotation;
                bool justFinishedRotation = wasInRotation && !currentlyInRotation;
                wasInRotation = currentlyInRotation;
                
                Debug.Log($"GravityCheckLoop: InRotation={currentlyInRotation}, JustFinished={justFinishedRotation}, IsMoving={isMoving}");
                
                // 如果在旋转中，暂停检测
                if (currentlyInRotation)
                {
                    Debug.Log("Skipping gravity check - in rotation");
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
                    Debug.Log("Platform not moving, checking conditions...");
                    // 调试信息：为什么不能移动
                    Vector3 gravityDir = GetCurrentGravityDirection();
                    int targetIndex = GetTargetWaypointByGravity();
                    
                    if (targetIndex != -1)
                    {
                        Vector3 targetPos = platform.WaypointPositions[targetIndex];
                        bool hasObstacle = HasObstacleInPath(platform.transform.position, targetPos);
                        float distanceToTarget = Vector3.Distance(platform.transform.position, targetPos);
                        bool canMove = CanMoveToNextWaypoint();
                        
                        Debug.Log($"Gravity platform waiting - GravityDir: {gravityDir}, TargetWaypoint: {targetIndex}, Distance: {distanceToTarget:F2}, HasObstacle: {hasObstacle}, CanMove: {canMove}");
                        
                        // 详细分析为什么不能移动
                        if (distanceToTarget < 0.05f)
                        {
                            Debug.Log("Reason: Too close to target (distance < 0.05)");
                        }
                        else if (hasObstacle)
                        {
                            Debug.Log("Reason: Obstacle detected in path");
                        }
                        else
                        {
                            Debug.Log("Reason: Unknown - should be able to move!");
                        }
                                    }
                else
                {
                    Debug.Log($"Gravity platform waiting - GravityDir: {gravityDir}, No matching waypoint found");
                }
            }
            else if (isMoving)
            {
                Debug.Log("Platform is currently moving, checking tweener status...");
                if (moveTweener != null)
                {
                    Debug.Log($"Tweener status: IsActive={moveTweener.IsActive()}, IsPlaying={moveTweener.IsPlaying()}");
                    
                    // 检查是否需要处理旋转状态
                    if (moveTweener.IsActive() && !moveTweener.IsPlaying())
                    {
                        Debug.Log("Tweener is paused, checking if we need to resume or stop...");
                        
                        // 如果不在旋转中，检查是否应该恢复或停止移动
                        if (!currentlyInRotation)
                        {
                            // 检查当前重力方向是否还匹配目标
                            int currentTargetIndex = GetTargetWaypointByGravity();
                            Debug.Log($"Current target waypoint while paused: {currentTargetIndex}");
                            
                            if (currentTargetIndex == -1)
                            {
                                // 重力方向不再匹配，停止移动
                                Debug.Log("Gravity direction no longer matches, stopping movement");
                                moveTweener.Kill();
                                isMoving = false;
                                wasPausedByRotation = false;
                                EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                            }
                            else
                            {
                                // 重力方向仍然匹配，恢复移动
                                Debug.Log("Gravity direction still matches, resuming movement");
                                moveTweener.Play();
                                wasPausedByRotation = false;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("Tweener is null but isMoving=true - this is a bug!");
                    isMoving = false; // 修复状态不一致
                }
            }
            
            // 如果刚完成旋转，立即重新检查（不等待）
            if (justFinishedRotation)
            {
                Debug.Log("Just finished rotation, checking immediately...");
                yield return null; // 只等待一帧
            }
            else
            {
                // 正常情况下等待检查间隔
                yield return new WaitForSeconds(platform.Config.gravityCheckInterval);
            }
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
            
            // 重力平台总是尝试移动到重力方向对应的路径点
            // 不需要检查是否已经在目标位置，因为重力方向可能会改变
            Vector3 currentPos = platform.transform.position;
            Vector3 targetPos = waypoints[targetIndex];
            
            // 只有距离太小时才不移动（避免抖动）
            if (Vector3.Distance(currentPos, targetPos) < 0.05f)
            {
                return false; // 已经非常接近目标位置
            }
            
            // 检查前方一小段距离是否有阻挡（而不是检查到目标点的整个路径）
            Vector3 direction = (targetPos - currentPos).normalized;
            Vector3 checkEndPos = currentPos + direction * platform.Config.obstacleCheckDistance;
            
            if (HasObstacleInPath(currentPos, checkEndPos))
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
            
            // 检查零向量，避免标准化错误
            Vector3 dirToWaypoint0 = dirToWaypoint0Raw.magnitude > 0.001f ? dirToWaypoint0Raw.normalized : Vector3.zero;
            Vector3 dirToWaypoint1 = dirToWaypoint1Raw.magnitude > 0.001f ? dirToWaypoint1Raw.normalized : Vector3.zero;
            
            // 调试：显示原始坐标和方向计算
            // Debug.Log($"Current pos: {currentPos}, Waypoint0: {waypoints[0]}, Waypoint1: {waypoints[1]}");
            // Debug.Log($"Raw dir to waypoint0: {waypoints[0] - currentPos}, Raw dir to waypoint1: {waypoints[1] - currentPos}");
            
            // 计算重力方向与两个移动方向的角度
            // 对于零向量（已经在目标位置），返回一个很大的角度表示不匹配
            float angleToWaypoint0 = dirToWaypoint0.magnitude < 0.001f ? 999f : Vector3.Angle(gravityDirection, dirToWaypoint0);
            float angleToWaypoint1 = dirToWaypoint1.magnitude < 0.001f ? 999f : Vector3.Angle(gravityDirection, dirToWaypoint1);
            
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
        /// 获取世界空间中的重力方向（始终向下，不跟随场景旋转）
        /// </summary>
        private Vector3 GetWorldGravityDirection()
        {
            // 重力方向始终是世界坐标的向下，不跟随场景旋转
            return Vector3.down;
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
            
            // 如果距离太小，不需要检测阻挡
            if (distance < 0.01f)
            {
                // Debug.Log($"Distance too small ({distance}), no obstacle check needed");
                return false;
            }
            
            // 使用BoxCast检测路径上的障碍物
            Bounds bounds = platformCollider.bounds;
            Vector2 boxSize = new Vector2(bounds.size.x, bounds.size.y);
            
            // 从平台中心开始检测
            Vector2 origin = bounds.center;
            
            // 执行BoxCastAll，获取所有碰撞的对象，使用配置的层级掩码
            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                origin, boxSize, 0f, direction, distance, platform.Config.obstacleLayerMask);
            
            Debug.Log($"BoxCast: origin={origin}, boxSize={boxSize}, direction={direction}, distance={distance}");
            Debug.Log($"Using layer mask: {platform.Config.obstacleLayerMask.value} (binary: {System.Convert.ToString(platform.Config.obstacleLayerMask.value, 2)})");
            Debug.Log($"Found {hits.Length} potential collisions");
            
            // 检查所有碰撞体，找到第一个不应该忽略的
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;
                
                Debug.Log($"Checking hit: {hit.collider.name}, distance={hit.distance}, point={hit.point}");
                
                // 忽略平台自身和相关组件
                if (hit.collider.gameObject == platform.gameObject)
                {
                    Debug.Log("Ignoring platform itself");
                    continue;
                }
                
                // 忽略平台的子组件（如PlayerDetector）
                if (hit.collider.transform.IsChildOf(platform.transform))
                {
                    Debug.Log($"Ignoring platform child component: {hit.collider.name}");
                    continue;
                }
                
                // 忽略所有PlayerDetector组件（不管属于哪个平台）
                if (hit.collider.GetComponent<PlayerDetector>() != null)
                {
                    Debug.Log($"Ignoring PlayerDetector from other platform: {hit.collider.name}");
                    continue;
                }
                
                // 检查是否是路径点
                bool isWaypoint = false;
                foreach (Transform waypoint in platform.Waypoints)
                {
                    if (hit.collider.gameObject == waypoint.gameObject)
                    {
                        Debug.Log($"Ignoring waypoint: {waypoint.name}");
                        isWaypoint = true;
                        break;
                    }
                }
                if (isWaypoint) continue;
                
                // 找到了真正的障碍物
                Debug.Log($"Obstacle detected: {hit.collider.name} at distance {hit.distance}");
                return true;
            }
            
            Debug.Log("No obstacle detected after checking all hits");
            return false;
            
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
            // 重力平台不需要跟踪currentWaypointIndex，因为它总是根据重力方向选择目标
            
            Debug.Log($"Gravity platform moving to waypoint {targetIndex}");
            EventManager.Instance.TriggerEvent(EventType.PlatformStartMove, platform);
            
            // 使用DOLocalMove移动本地坐标
            moveTweener = platform.transform.DOLocalMove(targetLocalPos, moveDuration)
                .SetEase(platform.Config.easeType)
                .OnComplete(() => {
                    isMoving = false;
                    EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                    Debug.Log($"Gravity platform reached waypoint {targetIndex}");
                });

            // 添加Update回调来检查旋转状态和碰撞检测
            moveTweener.OnUpdate(() => {
                CheckRotationStatus();
                CheckCollisionDuringMovement();
            });
        }

        /// <summary>
        /// 检查旋转状态并相应暂停/恢复
        /// </summary>
        private void CheckRotationStatus()
        {
            // 只处理暂停逻辑，恢复逻辑在重力检测循环中处理
            if (Const.InRotation && !wasPausedByRotation)
            {
                // 场景开始旋转，暂停移动
                wasPausedByRotation = true;
                if (moveTweener != null && moveTweener.IsActive() && moveTweener.IsPlaying())
                {
                    moveTweener.Pause();
                    Debug.Log("Platform movement paused due to scene rotation (OnUpdate)");
                }
            }
        }
        
        /// <summary>
        /// 在移动过程中检查碰撞
        /// </summary>
        private void CheckCollisionDuringMovement()
        {
            if (!isMoving || moveTweener == null || !moveTweener.IsActive() || !moveTweener.IsPlaying())
                return;
                
            // 获取当前目标路径点
            int targetIndex = GetTargetWaypointByGravity();
            if (targetIndex == -1) return;
            
            Vector3[] waypoints = platform.WaypointPositions;
            Vector3 targetPos = waypoints[targetIndex];
            
            // 检查前方一小段距离是否有障碍物（而不是检查到目标点的整个路径）
            Vector3 currentPos = platform.transform.position;
            Vector3 direction = (targetPos - currentPos).normalized;
            Vector3 checkEndPos = currentPos + direction * platform.Config.obstacleCheckDistance;
            
            if (HasObstacleInPath(currentPos, checkEndPos))
            {
                Debug.Log("Obstacle detected during movement, stopping platform");
                moveTweener.Kill();
                isMoving = false;
                wasPausedByRotation = false;
                EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
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
            Debug.Log("GravityMovementStrategy.StopMovement() called");
            
            if (gravityCheckCoroutine != null)
            {
                platform.StopCoroutine(gravityCheckCoroutine);
                gravityCheckCoroutine = null;
                Debug.Log("Gravity check coroutine stopped");
            }
            
            if (moveTweener != null && moveTweener.IsActive())
            {
                moveTweener.Kill();
                isMoving = false;
                wasPausedByRotation = false;
                EventManager.Instance.TriggerEvent(EventType.PlatformStopMove, platform);
                Debug.Log("Move tweener killed");
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