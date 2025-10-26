using UnityEngine;
using FlashParty.Platform;

namespace FlashParty.Environment
{
    /// <summary>
    /// 重力电梯 - 可以独立作为 prefab
    /// </summary>
    [RequireComponent(typeof(PlatformController))]
    public class GravityElevator : MonoBehaviour
    {
        [Header("关联配置")]
        [SerializeField] private GravityElevatorController linkedController;  // 关联的控制器
        
        [Header("移动设置")]
        [SerializeField] private float moveDistance = 5f;           // 移动距离
        [SerializeField] private float controllerGravity = 1f;      // 控制器下落速度（电梯上升）
        [SerializeField] private float playerWeightForce = 3f;      // 电梯下降速度（玩家站上时）
        
        [Header("检测设置")]
        [SerializeField] private LayerMask obstacleLayer;           // 阻挡层
        [SerializeField] private float obstacleCheckDistance = 0.5f;  // 阻挡检测距离（建议至少0.5）
        
        [Header("可视化设置")]
        [SerializeField] private Color pathColor = Color.green;
        
        // 组件引用
        private PlatformController platformController;
        private GravityElevatorSystem system;
        private Collider2D col;
        
        // 位置信息
        private Vector3 startPosition;
        private Vector3 endPosition;
        
        public PlatformController PlatformController => platformController;
        public GravityElevatorController LinkedController => linkedController;
        public Vector3 StartPosition => startPosition;
        public Vector3 EndPosition => endPosition;
        public float MoveDistance => moveDistance;
        public float ControllerGravity => controllerGravity;
        public float PlayerWeightForce => playerWeightForce;
        public LayerMask ObstacleLayer => obstacleLayer;
        public float ObstacleCheckDistance => obstacleCheckDistance;
        
        void Awake()
        {
            platformController = GetComponent<PlatformController>();
            if (platformController == null)
            {
                platformController = gameObject.AddComponent<PlatformController>();
            }
            
            col = GetComponent<Collider2D>();
            
            // 记录起始位置
            startPosition = transform.position;
            endPosition = startPosition + Vector3.up * moveDistance;
        }
        
        void Start()
        {
            // 自动创建系统
            if (linkedController != null && system == null)
            {
                CreateSystem();
            }
        }
        
        /// <summary>
        /// 创建电梯系统
        /// </summary>
        private void CreateSystem()
        {
            // 创建系统GameObject
            GameObject systemObj = new GameObject($"ElevatorSystem_{gameObject.name}");
            systemObj.transform.SetParent(transform.parent);
            systemObj.transform.position = transform.position;
            
            // 添加系统组件
            system = systemObj.AddComponent<GravityElevatorSystem>();
            system.Initialize(this, linkedController);
        }
        
        /// <summary>
        /// 由系统初始化（用于手动创建的情况）
        /// </summary>
        public void Initialize(GravityElevatorSystem elevatorSystem)
        {
            system = elevatorSystem;
            startPosition = transform.position;
            endPosition = startPosition + Vector3.up * moveDistance;
        }
        
        /// <summary>
        /// 设置位置（0-1之间）
        /// </summary>
        public void SetPosition(float t)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
        }
        
        /// <summary>
        /// 检查是否有玩家在电梯上
        /// </summary>
        public bool HasPlayer()
        {
            return platformController != null && platformController.PlayerCount > 0;
        }
        
        /// <summary>
        /// 检查指定方向是否被阻挡
        /// </summary>
        public bool IsBlocked(Vector3 direction)
        {
            if (col == null)
                return false;
            
            // 获取碰撞器边界
            Bounds bounds = col.bounds;
            Vector2 origin = transform.position;
            
            // 根据方向调整射线起点到碰撞器边缘
            if (direction.y > 0) // 向上
            {
                origin.y = bounds.max.y;
            }
            else if (direction.y < 0) // 向下
            {
                origin.y = bounds.min.y;
            }
            
            // 使用BoxCastAll检测所有碰撞，避免只检测到自己
            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                origin,
                new Vector2(bounds.size.x * 0.9f, 0.1f), // 使用碰撞器宽度的90%
                0f,
                direction,
                obstacleCheckDistance,
                obstacleLayer
            );
            
            // 遍历所有碰撞，排除自己和子对象
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null)
                    continue;
                
                // 检查是否是自己或子对象的碰撞器
                if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                
                // 找到了真正的障碍物
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 重置到起始位置
        /// </summary>
        public void ResetPosition()
        {
            transform.position = startPosition;
        }
        
        void OnDrawGizmos()
        {
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Vector3 end = start + Vector3.up * moveDistance;
            
            Gizmos.color = pathColor;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(start, Vector3.one * 0.3f);
            Gizmos.DrawWireCube(end, Vector3.one * 0.3f);
            
            // 绘制与控制器的连接线
            if (linkedController != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, linkedController.transform.position);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (col == null)
                return;
            
            Bounds bounds = col.bounds;
            
            // 显示阻挡检测范围 - 向上
            Gizmos.color = Color.green;
            Vector3 upOrigin = new Vector3(transform.position.x, bounds.max.y, transform.position.z);
            Gizmos.DrawRay(upOrigin, Vector3.up * obstacleCheckDistance);
            Gizmos.DrawWireCube(upOrigin + Vector3.up * obstacleCheckDistance * 0.5f, 
                new Vector3(bounds.size.x * 0.9f, obstacleCheckDistance, 0.1f));
            
            // 显示阻挡检测范围 - 向下
            Gizmos.color = Color.magenta;
            Vector3 downOrigin = new Vector3(transform.position.x, bounds.min.y, transform.position.z);
            Gizmos.DrawRay(downOrigin, Vector3.down * obstacleCheckDistance);
            Gizmos.DrawWireCube(downOrigin + Vector3.down * obstacleCheckDistance * 0.5f, 
                new Vector3(bounds.size.x * 0.9f, obstacleCheckDistance, 0.1f));
        }
    }
}

