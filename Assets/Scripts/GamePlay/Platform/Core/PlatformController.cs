using System.Collections.Generic;
using UnityEngine;

namespace FlashParty.Platform
{
    /// <summary>
    /// 平台控制器 - 处理玩家与平台的物理交互
    /// </summary>
    public class PlatformController : MonoBehaviour
    {
        [Header("检测设置")]
        [SerializeField] private float detectionHeight = 0.1f;
        
        // 当前在平台上的玩家
        private List<Player> playersOnPlatform = new List<Player>();
        
        // 上一帧的位置，用于计算移动偏移
        private Vector3 lastPosition;
        
        // 触发器检测器
        private Collider2D triggerCollider;
        
        void Awake()
        {
            // 获取或创建触发器
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider == null)
            {
                // 如果没有碰撞器，创建一个用于检测的触发器
                triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            
            // 确保有一个触发器用于检测玩家
            if (!HasTriggerCollider())
            {
                GameObject triggerObj = new GameObject("PlayerDetector");
                triggerObj.transform.SetParent(transform);
                triggerObj.transform.localPosition = Vector3.up * detectionHeight;
                triggerObj.layer = gameObject.layer;
                
                BoxCollider2D trigger = triggerObj.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = triggerCollider.bounds.size;
                
                // 添加检测脚本
                PlayerDetector detector = triggerObj.AddComponent<PlayerDetector>();
                detector.Initialize(this);
            }
            
            lastPosition = transform.position;
        }
        
        void FixedUpdate()
        {
            // 计算平台移动的偏移量
            Vector3 deltaMove = transform.position - lastPosition;
            
            // 如果平台移动了，让所有在平台上的玩家跟随移动
            if (deltaMove.magnitude > 0.001f)
            {
                MovePlayers(deltaMove);
            }
            
            lastPosition = transform.position;
        }
        
        /// <summary>
        /// 移动所有在平台上的玩家
        /// </summary>
        /// <param name="deltaMove">移动偏移量</param>
        private void MovePlayers(Vector3 deltaMove)
        {
            for (int i = playersOnPlatform.Count - 1; i >= 0; i--)
            {
                Player player = playersOnPlatform[i];
                if (player == null)
                {
                    playersOnPlatform.RemoveAt(i);
                    continue;
                }
                
                // 直接修改玩家位置，保持物理系统的一致性
                player.transform.position += deltaMove;
            }
        }
        
        /// <summary>
        /// 玩家进入平台
        /// </summary>
        /// <param name="player">玩家对象</param>
        public void OnPlayerEnter(Player player)
        {
            if (player == null || playersOnPlatform.Contains(player))
                return;
                
            playersOnPlatform.Add(player);
            EventManager.Instance.TriggerEvent(EventType.PlatformPlayerOn, player);
            
            Debug.Log($"Player {player.name} entered platform {gameObject.name}");
        }
        
        /// <summary>
        /// 玩家离开平台
        /// </summary>
        /// <param name="player">玩家对象</param>
        public void OnPlayerExit(Player player)
        {
            if (player == null || !playersOnPlatform.Contains(player))
                return;
                
            playersOnPlatform.Remove(player);
            EventManager.Instance.TriggerEvent(EventType.PlatformPlayerOff, player);
            
            Debug.Log($"Player {player.name} left platform {gameObject.name}");
        }
        
        /// <summary>
        /// 检查是否有触发器碰撞器
        /// </summary>
        private bool HasTriggerCollider()
        {
            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 获取当前在平台上的玩家数量
        /// </summary>
        public int PlayerCount => playersOnPlatform.Count;
        
        /// <summary>
        /// 获取所有在平台上的玩家
        /// </summary>
        public List<Player> GetPlayersOnPlatform() => new List<Player>(playersOnPlatform);
        
        void OnDestroy()
        {
            // 清理玩家列表
            playersOnPlatform.Clear();
        }
        
        void OnDrawGizmosSelected()
        {
            // 显示检测区域
            Gizmos.color = Color.yellow;
            Vector3 detectorPos = transform.position + Vector3.up * detectionHeight;
            Vector3 size = GetComponent<Collider2D>()?.bounds.size ?? Vector3.one;
            Gizmos.DrawWireCube(detectorPos, size);
        }
    }
    
    /// <summary>
    /// 玩家检测器 - 检测玩家进入和离开平台
    /// </summary>
    public class PlayerDetector : MonoBehaviour
    {
        private PlatformController platformController;
        
        public void Initialize(PlatformController controller)
        {
            platformController = controller;
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                platformController.OnPlayerEnter(player);
            }
        }
        
        void OnTriggerExit2D(Collider2D other)
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                platformController.OnPlayerExit(player);
            }
        }
    }
} 