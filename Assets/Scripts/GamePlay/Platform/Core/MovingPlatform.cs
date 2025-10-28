using UnityEngine;

namespace FlashParty.Platform
{
    /// <summary>
    /// 移动平台主控制器
    /// </summary>
    [RequireComponent(typeof(PlatformController))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("平台配置")]
        [SerializeField] private PlatformConfig config = new PlatformConfig();
        
        [Header("路径设置")]
        [Tooltip("使用偏移量模式（推荐）或Transform模式")]
        [SerializeField] private bool useOffsetMode = true;
        
        [Tooltip("相对于平台位置的偏移量（基于平台角度为0时）")]
        [SerializeField] private Vector2[] waypointOffsets = new Vector2[] 
        { 
            new Vector2(4f, 0f), 
            new Vector2(-4f, 0f) 
        };
        
        [Tooltip("传统方式：使用Transform数组（useOffsetMode=false时生效）")]
        [SerializeField] private Transform[] waypoints = new Transform[0];
        
        [Header("自动开始")]
        [SerializeField] private bool autoStart = true;
        
        // 组件引用
        private PlatformController platformController;
        private IMovementStrategy movementStrategy;
        
        // 属性访问器
        public PlatformConfig Config => config;
        public Transform[] Waypoints => waypoints;
        public Vector3[] WaypointPositions => GetWaypointPositions();
        public IMovementStrategy MovementStrategy => movementStrategy;
        public bool IsMoving => movementStrategy?.IsMoving ?? false;
        
        void Awake()
        {
            // 获取平台控制器
            platformController = GetComponent<PlatformController>();
            if (platformController == null)
            {
                platformController = gameObject.AddComponent<PlatformController>();
            }
            
            // 根据配置创建对应的移动策略
            CreateMovementStrategy();
        }
        
        void Start()
        {
            // 验证设置
            if (!ValidateSetup())
            {
                Debug.LogError($"Moving platform {gameObject.name} setup is invalid!", this);
                return;
            }
            
            // 初始化移动策略
            movementStrategy?.Initialize(this);
            
            // 自动开始移动
            if (autoStart)
            {
                StartMovement();
            }
        }
        
        /// <summary>
        /// 创建移动策略
        /// </summary>
        private void CreateMovementStrategy()
        {
            switch (config.movementType)
            {
                case MovementType.Loop:
                    movementStrategy = new LoopMovementStrategy();
                    break;
                case MovementType.Trigger:
                    movementStrategy = new TriggerMovementStrategy();
                    break;
                case MovementType.Gravity:
                    movementStrategy = new GravityMovementStrategy();
                    break;
                default:
                    Debug.LogError($"Unsupported movement type: {config.movementType}");
                    break;
            }
        }
        
        /// <summary>
        /// 获取路径点位置数组
        /// </summary>
        private Vector3[] GetWaypointPositions()
        {
            if (useOffsetMode)
            {
                // 使用偏移量模式
                return CalculateWaypointsFromOffsets();
            }
            else
            {
                // 使用传统Transform模式
                if (waypoints == null || waypoints.Length == 0)
                    return new Vector3[0];
                
                Vector3[] positions = new Vector3[waypoints.Length];
                for (int i = 0; i < waypoints.Length; i++)
                {
                    positions[i] = waypoints[i] != null ? waypoints[i].position : Vector3.zero;
                }
                return positions;
            }
        }
        
        /// <summary>
        /// 根据偏移量计算路径点世界坐标
        /// 支持平台旋转（只支持90度的倍数）
        /// </summary>
        private Vector3[] CalculateWaypointsFromOffsets()
        {
            if (waypointOffsets == null || waypointOffsets.Length == 0)
                return new Vector3[0];
            
            Vector3[] positions = new Vector3[waypointOffsets.Length];
            Vector3 platformPos = transform.position;
            float platformRotation = transform.eulerAngles.z;
            
            // 计算旋转矩阵（只支持90度的倍数）
            // 将角度规范化到 0-360 范围
            platformRotation = Mathf.Repeat(platformRotation, 360f);
            
            // 四舍五入到最接近的90度
            int rotationSteps = Mathf.RoundToInt(platformRotation / 90f);
            float normalizedRotation = rotationSteps * 90f;
            
            // 根据旋转转换偏移量
            for (int i = 0; i < waypointOffsets.Length; i++)
            {
                Vector2 offset = waypointOffsets[i];
                Vector2 rotatedOffset = RotateOffset(offset, normalizedRotation);
                positions[i] = platformPos + new Vector3(rotatedOffset.x, rotatedOffset.y, 0f);
            }
            
            return positions;
        }
        
        /// <summary>
        /// 根据角度旋转偏移量（90度的倍数）
        /// </summary>
        private Vector2 RotateOffset(Vector2 offset, float angleDegrees)
        {
            // 只处理90度的倍数
            int steps = Mathf.RoundToInt(angleDegrees / 90f) % 4;
            if (steps < 0) steps += 4;
            
            switch (steps)
            {
                case 0: // 0度 - 不旋转
                    return offset;
                    
                case 1: // 90度 - 顺时针旋转90度
                    return new Vector2(offset.y, -offset.x);
                    
                case 2: // 180度
                    return new Vector2(-offset.x, -offset.y);
                    
                case 3: // 270度 - 顺时针旋转270度
                    return new Vector2(-offset.y, offset.x);
                    
                default:
                    return offset;
            }
        }
        

        
        /// <summary>
        /// 验证平台设置
        /// </summary>
        private bool ValidateSetup()
        {
            if (useOffsetMode)
            {
                // 偏移量模式验证
                if (waypointOffsets == null || waypointOffsets.Length < 2)
                {
                    Debug.LogError("Moving platform requires at least 2 waypoint offsets");
                    return false;
                }
            }
            else
            {
                // Transform模式验证
                if (waypoints == null || waypoints.Length < 2)
                {
                    Debug.LogError("Moving platform requires at least 2 waypoints");
                    return false;
                }
                
                // 检查Transform是否有效
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] == null)
                    {
                        Debug.LogError($"Waypoint {i} is null");
                        return false;
                    }
                }
            }
            
            // 检查移动速度
            if (config.moveSpeed <= 0)
            {
                Debug.LogError("Move speed must be greater than 0");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 开始移动
        /// </summary>
        public void StartMovement()
        {
            if (movementStrategy == null)
            {
                Debug.LogError("Movement strategy is not initialized");
                return;
            }
            
            movementStrategy.StartMovement();
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMovement()
        {
            movementStrategy?.StopMovement();
        }
        
        /// <summary>
        /// 暂停移动
        /// </summary>
        public void PauseMovement()
        {
            movementStrategy?.PauseMovement();
        }
        
        /// <summary>
        /// 恢复移动
        /// </summary>
        public void ResumeMovement()
        {
            movementStrategy?.ResumeMovement();
        }
        
        /// <summary>
        /// 触发移动（用于触发类型的平台）
        /// </summary>
        public void TriggerMovement()
        {
            if (config.movementType == MovementType.Trigger)
            {
                EventManager.Instance.TriggerEvent(EventType.PlatformTrigger, this);
            }
            else
            {
                Debug.LogWarning("TriggerMovement can only be called on trigger-type platforms");
            }
        }
        
        /// <summary>
        /// 设置新的路径点（Transform方式）
        /// </summary>
        /// <param name="newWaypoints">新的路径点数组</param>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            if (newWaypoints == null || newWaypoints.Length < 2)
            {
                Debug.LogError("Invalid waypoints array");
                return;
            }
            
            // 如果正在移动，先停止
            bool wasMoving = IsMoving;
            if (wasMoving)
            {
                StopMovement();
            }
            
            useOffsetMode = false;
            waypoints = newWaypoints;
            
            // 如果之前在移动，重新开始
            if (wasMoving)
            {
                StartMovement();
            }
        }
        
        /// <summary>
        /// 设置新的路径点（偏移量方式）
        /// </summary>
        /// <param name="newOffsets">新的偏移量数组</param>
        public void SetWaypointOffsets(Vector2[] newOffsets)
        {
            if (newOffsets == null || newOffsets.Length < 2)
            {
                Debug.LogError("Invalid offsets array");
                return;
            }
            
            // 如果正在移动，先停止
            bool wasMoving = IsMoving;
            if (wasMoving)
            {
                StopMovement();
            }
            
            useOffsetMode = true;
            waypointOffsets = newOffsets;
            
            // 如果之前在移动，重新开始
            if (wasMoving)
            {
                StartMovement();
            }
        }
        

        
        /// <summary>
        /// 更新平台配置
        /// </summary>
        /// <param name="newConfig">新的配置</param>
        public void UpdateConfig(PlatformConfig newConfig)
        {
            if (newConfig == null)
            {
                Debug.LogError("Config cannot be null");
                return;
            }
            
            bool needsStrategyChange = config.movementType != newConfig.movementType;
            bool wasMoving = IsMoving;
            
            // 如果正在移动，先停止
            if (wasMoving)
            {
                StopMovement();
            }
            
            config = newConfig;
            
            // 如果移动类型改变，重新创建策略
            if (needsStrategyChange)
            {
                movementStrategy?.OnDestroy();
                CreateMovementStrategy();
                movementStrategy?.Initialize(this);
            }
            
            // 如果之前在移动，重新开始
            if (wasMoving)
            {
                StartMovement();
            }
        }
        
        /// <summary>
        /// 获取平台控制器
        /// </summary>
        public PlatformController GetPlatformController()
        {
            return platformController;
        }
        
        /// <summary>
        /// 重置平台到初始状态
        /// </summary>
        public void ResetPlatform()
        {
            StopMovement();
            
            Vector3[] positions = GetWaypointPositions();
            if (positions.Length > 0)
            {
                // 使用本地坐标重置位置
                Vector3 targetLocalPos = transform.parent.InverseTransformPoint(positions[0]);
                transform.localPosition = targetLocalPos;
            }
            
            // 如果是触发类型，重置触发状态
            if (movementStrategy is TriggerMovementStrategy triggerStrategy)
            {
                triggerStrategy.ResetPlatform();
            }
            // 如果是重力类型，重置到最近的路径点
            else if (movementStrategy is GravityMovementStrategy gravityStrategy)
            {
                gravityStrategy.ResetToNearestWaypoint();
            }
        }
        
        /// <summary>
        /// 获取重力移动策略（如果是重力平台）
        /// </summary>
        public GravityMovementStrategy GetGravityStrategy()
        {
            return movementStrategy as GravityMovementStrategy;
        }
        
        /// <summary>
        /// 检查当前重力移动条件（仅重力平台有效）
        /// </summary>
        public bool CanMoveWithGravity()
        {
            if (movementStrategy is GravityMovementStrategy gravityStrategy)
            {
                return gravityStrategy.CanMoveNow();
            }
            return false;
        }
        
        /// <summary>
        /// 获取当前重力方向（仅重力平台有效）
        /// </summary>
        public Vector3 GetCurrentGravityDirection()
        {
            if (movementStrategy is GravityMovementStrategy gravityStrategy)
            {
                return gravityStrategy.GetCurrentGravityDirection();
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// 获取下一步移动方向（仅重力平台有效）
        /// </summary>
        public Vector3 GetNextMoveDirection()
        {
            if (movementStrategy is GravityMovementStrategy gravityStrategy)
            {
                return gravityStrategy.GetNextMoveDirection();
            }
            return Vector3.zero;
        }
        
        void OnDestroy()
        {
            movementStrategy?.OnDestroy();
        }
        
        void OnDrawGizmos()
        {
            Vector3[] positions = GetWaypointPositions();
            if (!config.showPath || positions == null || positions.Length < 2)
                return;
            
            // 绘制路径
            Gizmos.color = config.pathColor;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Gizmos.DrawLine(positions[i], positions[i + 1]);
            }
            
            // 如果是循环路径，连接最后一个点到第一个点
            if (config.isLooping && positions.Length > 2)
            {
                Gizmos.DrawLine(positions[positions.Length - 1], positions[0]);
            }
            
            // 绘制路径点
            Gizmos.color = config.waypointColor;
            for (int i = 0; i < positions.Length; i++)
            {
                Gizmos.DrawWireSphere(positions[i], 0.3f);
                
                // 绘制路径点编号
                #if UNITY_EDITOR
                string label = i.ToString();
                if (useOffsetMode && waypointOffsets != null && i < waypointOffsets.Length)
                {
                    label += $"\n({waypointOffsets[i].x:F1}, {waypointOffsets[i].y:F1})";
                }
                UnityEditor.Handles.Label(positions[i] + Vector3.up * 0.5f, label);
                #endif
            }
        }
        
        void OnDrawGizmosSelected()
        {
            Vector3[] positions = GetWaypointPositions();
            // 选中时显示更详细的信息
            if (positions == null || positions.Length == 0)
                return;
            
            // 显示平台当前位置到第一个路径点的连线
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, positions[0]);
            
            #if UNITY_EDITOR
            // 显示模式信息
            string modeInfo = useOffsetMode ? "偏移量模式" : "Transform模式";
            float rotation = Mathf.Round(transform.eulerAngles.z);
            string rotationInfo = $"旋转: {rotation}°";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"{modeInfo}\n{rotationInfo}");
            
            // 如果是偏移量模式，显示平台方向指示
            if (useOffsetMode)
            {
                Gizmos.color = Color.yellow;
                Vector3 forward = transform.up * 0.5f;
                Gizmos.DrawRay(transform.position, forward);
                Gizmos.DrawWireSphere(transform.position + forward, 0.1f);
            }
            #endif
        }
    }
} 