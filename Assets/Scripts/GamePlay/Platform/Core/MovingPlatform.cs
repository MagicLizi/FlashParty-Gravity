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
            if (waypoints == null || waypoints.Length == 0)
                return new Vector3[0];
            
            Vector3[] positions = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                positions[i] = waypoints[i] != null ? waypoints[i].position : Vector3.zero;
            }
            return positions;
        }
        

        
        /// <summary>
        /// 验证平台设置
        /// </summary>
        private bool ValidateSetup()
        {
            // 检查路径点
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
            
            waypoints = newWaypoints;
            
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
                UnityEditor.Handles.Label(positions[i] + Vector3.up * 0.5f, i.ToString());
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
        }
    }
} 