using UnityEngine;

namespace FlashParty.Environment
{
    /// <summary>
    /// 重力电梯控制器（配重）- 可以独立作为 prefab
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class GravityElevatorController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveDistance = 5f;  // 移动距离
        
        [Header("可视化设置")]
        [SerializeField] private Color pathColor = Color.red;
        
        // 组件引用
        private Rigidbody2D rb;
        private Collider2D col;
        
        // 位置信息
        private Vector3 startPosition;
        private Vector3 endPosition;
        
        // 关联的系统
        private GravityElevatorSystem system;
        
        // 检测设置
        private LayerMask obstacleLayer;
        private float checkDistance = 0.1f;
        
        public Vector3 StartPosition => startPosition;
        public Vector3 EndPosition => endPosition;
        public float MoveDistance => moveDistance;
        public Rigidbody2D Rigidbody => rb;
        
        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // 使用 Kinematic 模式，完全手动控制移动，不受物理碰撞影响
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            col = GetComponent<Collider2D>();
            
            // 记录起始位置
            startPosition = transform.position;
            endPosition = startPosition + Vector3.down * moveDistance;
        }
        
        /// <summary>
        /// 由系统初始化
        /// </summary>
        public void Initialize(GravityElevatorSystem elevatorSystem, LayerMask obstacles, float obstacleCheckDistance)
        {
            system = elevatorSystem;
            obstacleLayer = obstacles;
            checkDistance = obstacleCheckDistance;
            
            // 更新位置（如果距离在运行时改变了）
            startPosition = transform.position;
            endPosition = startPosition + Vector3.down * moveDistance;
        }
        
        /// <summary>
        /// 设置位置（0-1之间）
        /// 注意：控制器的位置是反向的，0 = 上方，1 = 下方
        /// </summary>
        public void SetPosition(float t)
        {
            Vector3 targetPos = Vector3.Lerp(startPosition, endPosition, t);
            
            // Kinematic 模式下使用 MovePosition，符合物理引擎规范
            if (rb != null)
            {
                rb.MovePosition(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }
        }
        
        /// <summary>
        /// 检查指定方向是否被阻挡
        /// </summary>
        public bool IsBlocked(Vector3 direction)
        {
            if (col == null)
            {
                Debug.LogWarning($"[Controller] Collider is null!");
                return false;
            }
            
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
                checkDistance,
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
                    //Debug.Log($"[Controller] Ignoring self/child collider: {hit.collider.name}");
                    continue;
                }
                
                // 找到了真正的障碍物
                //Debug.Log($"[Controller] BLOCKED by {hit.collider.name}! Distance: {hit.distance:F3}");
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
            Vector3 end = start + Vector3.down * moveDistance;
            
            Gizmos.color = pathColor;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireCube(start, Vector3.one * 0.3f);
            Gizmos.DrawWireCube(end, Vector3.one * 0.3f);
        }
        
        void OnDrawGizmosSelected()
        {
            if (col == null)
                return;
            
            Bounds bounds = col.bounds;
            
            // 显示阻挡检测范围 - 向下
            Gizmos.color = Color.yellow;
            Vector3 downOrigin = new Vector3(transform.position.x, bounds.min.y, transform.position.z);
            Gizmos.DrawRay(downOrigin, Vector3.down * checkDistance);
            Gizmos.DrawWireCube(downOrigin + Vector3.down * checkDistance * 0.5f, 
                new Vector3(bounds.size.x * 0.9f, checkDistance, 0.1f));
            
            // 显示阻挡检测范围 - 向上
            Gizmos.color = Color.cyan;
            Vector3 upOrigin = new Vector3(transform.position.x, bounds.max.y, transform.position.z);
            Gizmos.DrawRay(upOrigin, Vector3.up * checkDistance);
            Gizmos.DrawWireCube(upOrigin + Vector3.up * checkDistance * 0.5f, 
                new Vector3(bounds.size.x * 0.9f, checkDistance, 0.1f));
        }
    }
}

