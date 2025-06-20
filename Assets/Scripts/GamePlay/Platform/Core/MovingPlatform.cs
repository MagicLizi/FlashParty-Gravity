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
        [SerializeField] private bool useTransformWaypoints = false;
        [SerializeField] private Transform[] transformWaypoints = new Transform[0];
        [SerializeField] private Vector3[] positionWaypoints = new Vector3[2] { Vector3.left * 2, Vector3.right * 2 };
        
        [Header("自动开始")]
        [SerializeField] private bool autoStart = true;
        
        // 组件引用
        private PlatformController platformController;
        private IMovementStrategy movementStrategy;
        
        // 属性访问器
        public PlatformConfig Config => config;
        public Transform[] Waypoints => GetWaypointTransforms();
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
            if (useTransformWaypoints)
            {
                if (transformWaypoints == null || transformWaypoints.Length == 0)
                    return new Vector3[0];
                
                Vector3[] positions = new Vector3[transformWaypoints.Length];
                for (int i = 0; i < transformWaypoints.Length; i++)
                {
                    positions[i] = transformWaypoints[i] != null ? transformWaypoints[i].position : Vector3.zero;
                }
                return positions;
            }
            else
            {
                return positionWaypoints ?? new Vector3[0];
            }
        }
        
        /// <summary>
        /// 获取路径点Transform数组（为了兼容性）
        /// </summary>
        private Transform[] GetWaypointTransforms()
        {
            if (useTransformWaypoints)
            {
                return transformWaypoints ?? new Transform[0];
            }
            else
            {
                // 如果使用位置坐标，创建临时的Transform数组
                Vector3[] positions = GetWaypointPositions();
                Transform[] transforms = new Transform[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    GameObject tempWaypoint = new GameObject($"TempWaypoint_{i}");
                    tempWaypoint.transform.position = positions[i];
                    tempWaypoint.transform.SetParent(transform);
                    tempWaypoint.hideFlags = HideFlags.HideInHierarchy;
                    transforms[i] = tempWaypoint.transform;
                }
                return transforms;
            }
        }
        
        /// <summary>
        /// 验证平台设置
        /// </summary>
        private bool ValidateSetup()
        {
            Vector3[] positions = GetWaypointPositions();
            
            // 检查路径点
            if (positions == null || positions.Length < 2)
            {
                Debug.LogError("Moving platform requires at least 2 waypoints");
                return false;
            }
            
            // 如果使用Transform方式，检查Transform是否有效
            if (useTransformWaypoints)
            {
                for (int i = 0; i < transformWaypoints.Length; i++)
                {
                    if (transformWaypoints[i] == null)
                    {
                        Debug.LogError($"Transform waypoint {i} is null");
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
            
            useTransformWaypoints = true;
            transformWaypoints = newWaypoints;
            
            // 如果之前在移动，重新开始
            if (wasMoving)
            {
                StartMovement();
            }
        }
        
        /// <summary>
        /// 设置新的路径点（位置方式）
        /// </summary>
        /// <param name="newPositions">新的位置数组</param>
        public void SetWaypointPositions(Vector3[] newPositions)
        {
            if (newPositions == null || newPositions.Length < 2)
            {
                Debug.LogError("Invalid positions array");
                return;
            }
            
            // 如果正在移动，先停止
            bool wasMoving = IsMoving;
            if (wasMoving)
            {
                StopMovement();
            }
            
            useTransformWaypoints = false;
            positionWaypoints = newPositions;
            
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
                transform.position = positions[0];
            }
            
            // 如果是触发类型，重置触发状态
            if (movementStrategy is TriggerMovementStrategy triggerStrategy)
            {
                triggerStrategy.ResetPlatform();
            }
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