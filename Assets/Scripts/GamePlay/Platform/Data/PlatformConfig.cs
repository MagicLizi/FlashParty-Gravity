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
        Trigger     // 触发移动
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
        public bool canTriggerMultipleTimes = true;
        public float triggerCooldown = 1f;
        public bool returnToStart = false;
        
        [Header("路径设置")]
        public PathType pathType = PathType.Linear;
        public bool isLooping = true;
        
        [Header("调试设置")]
        public bool showPath = true;
        public Color pathColor = Color.green;
        public Color waypointColor = Color.red;
    }
} 