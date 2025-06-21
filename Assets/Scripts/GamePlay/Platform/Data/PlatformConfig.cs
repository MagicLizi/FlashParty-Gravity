using UnityEngine;
using DG.Tweening;

namespace FlashParty.Platform
{
    /// <summary>
    /// 移动平台类型
    /// </summary>
    public enum MovementType
    {
        Loop,       // 循环移动
        Trigger,    // 触发移动
        Gravity     // 重力移动
    }
    
    /// <summary>
    /// 路径类型
    /// </summary>
    public enum PathType
    {
        Linear,         // 直线
        CatmullRom,     // 曲线
        Custom          // 自定义
    }
    
    /// <summary>
    /// 触发模式
    /// </summary>
    public enum TriggerMode
    {
        SinglePath,     // 单段路径：每次触发移动到下一个路径点
        FullPath        // 完整路径：每次触发移动完整路径
    }
    
    /// <summary>
    /// 移动平台配置数据
    /// </summary>
    [System.Serializable]
    public class PlatformConfig
    {
        [Header("基础设置")]
        public MovementType movementType = MovementType.Loop;
        public float moveSpeed = 2f;
        public Ease easeType = Ease.Linear;
        
        [Header("循环移动设置")]
        public float initialDelay = 0f;
        public float waitTimeAtWaypoint = 0f;
        public bool reverseOnComplete = true;
        
        [Header("触发移动设置")]
        public TriggerMode triggerMode = TriggerMode.SinglePath;
        public bool canTriggerMultipleTimes = true;
        public float triggerCooldown = 1f;
        public bool returnToStart = false;
        
        [Header("重力移动设置")]
        public float obstacleCheckDistance = 0.5f;      // 阻挡检测距离
        public float gravityCheckInterval = 0.1f;       // 重力检测间隔
        public float gravityTolerance = 15f;            // 重力方向容差角度
        public LayerMask obstacleLayerMask = -1;        // 障碍物检测层级掩码（默认检测所有层）
        
        [Header("路径设置")]
        public PathType pathType = PathType.Linear;
        public bool isLooping = true;
        
        [Header("调试设置")]
        public bool showPath = true;
        public Color pathColor = Color.green;
        public Color waypointColor = Color.red;
    }
} 