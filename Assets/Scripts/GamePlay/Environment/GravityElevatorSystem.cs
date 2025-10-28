using UnityEngine;

namespace FlashParty.Environment
{
    /// <summary>
    /// 重力升降电梯系统
    /// 连接独立的电梯和控制器prefab，控制器往下时电梯往上，反之亦然
    /// 可以由电梯自动创建，也可以手动创建
    /// </summary>
    public class GravityElevatorSystem : MonoBehaviour
    {
        // 组件引用
        private GravityElevator elevator;
        private GravityElevatorController controller;
        
        // 速度参数（从电梯读取）
        private float elevatorSpeedUp;          // 电梯上升速度（m/s）
        private float elevatorSpeedDown;        // 电梯下降速度（m/s）
        private float controllerSpeedUp;        // 控制器上升速度（m/s，根据电梯速度计算）
        private float controllerSpeedDown;      // 控制器下降速度（m/s，根据电梯速度计算）
        
        // 状态
        private bool isPlayerOnElevator = false;
        private float currentPosition = 0f;  // 0 = 电梯在最下方，控制器在最上方；1 = 电梯在最上方，控制器在最下方
        
        // 延迟相关
        private float playerExitDelay = 0.7f;           // 玩家离开后的延迟时间
        private float playerExitTimer = 0f;             // 玩家离开计时器
        private bool isWaitingAfterPlayerExit = false;  // 是否在等待玩家离开后的延迟
        
        // 控制器被阻挡停顿
        private float controllerBlockedPauseDuration = 0.7f;  // 控制器被阻挡后的停顿时间
        private float controllerBlockedPauseTimer = 0f;       // 停顿计时器
        private bool isControllerPaused = false;              // 控制器是否在停顿中
        
        /// <summary>
        /// 由电梯自动创建时调用
        /// </summary>
        public void Initialize(GravityElevator elevatorInstance, GravityElevatorController controllerInstance)
        {
            elevator = elevatorInstance;
            controller = controllerInstance;
            
            // 从电梯读取速度配置
            elevatorSpeedUp = elevator.ElevatorSpeedUp;
            elevatorSpeedDown = elevator.ElevatorSpeedDown;
            
            // 计算控制器的速度，保证同时到达终点
            // 控制器和电梯的移动距离可能不同，但需要在相同时间内完成
            float elevatorDistance = elevator.MoveDistance;
            float controllerDistance = controller.MoveDistance;
            
            // 电梯上升时，控制器下降
            // 时间 = 电梯距离 / 电梯上升速度
            // 控制器下降速度 = 控制器距离 / 时间
            if (elevatorSpeedUp > 0f)
            {
                float timeUp = elevatorDistance / elevatorSpeedUp;
                controllerSpeedDown = controllerDistance / timeUp;
            }
            else
            {
                controllerSpeedDown = 1f; // 默认值
            }
            
            // 电梯下降时，控制器上升
            if (elevatorSpeedDown > 0f)
            {
                float timeDown = elevatorDistance / elevatorSpeedDown;
                controllerSpeedUp = controllerDistance / timeDown;
            }
            else
            {
                controllerSpeedUp = 1f; // 默认值
            }
            
            Debug.Log($"[GravityElevatorSystem] 初始化速度 - 电梯上升: {elevatorSpeedUp} m/s, 下降: {elevatorSpeedDown} m/s");
            Debug.Log($"[GravityElevatorSystem] 控制器上升: {controllerSpeedUp} m/s, 下降: {controllerSpeedDown} m/s");
            
            // 初始化电梯
            elevator.Initialize(this);
            
            // 初始化控制器
            controller.Initialize(this, elevator.ObstacleLayer, elevator.ObstacleCheckDistance);
            
            // 初始位置：电梯在最下方，控制器在最上方
            currentPosition = 0f;
            UpdatePositions(currentPosition);
            
            // 订阅事件
            SubscribeEvents();
        }
        
        void Start()
        {
            // 如果已经被初始化了，就不需要再做什么
            if (elevator != null && controller != null)
                return;
        }
        
        private void SubscribeEvents()
        {
            // 订阅玩家进入/离开平台事件
            EventManager.Instance.AddListener(EventType.PlatformPlayerOn, OnPlayerEnterPlatform);
            EventManager.Instance.AddListener(EventType.PlatformPlayerOff, OnPlayerExitPlatform);
        }
        
        void OnDestroy()
        {
            // 取消订阅
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener(EventType.PlatformPlayerOn, OnPlayerEnterPlatform);
                EventManager.Instance.RemoveListener(EventType.PlatformPlayerOff, OnPlayerExitPlatform);
            }
        }
        
        void FixedUpdate()
        {
            // 如果在等待玩家离开后的延迟
            if (isWaitingAfterPlayerExit)
            {
                playerExitTimer -= Time.fixedDeltaTime;
                if (playerExitTimer <= 0f)
                {
                    isWaitingAfterPlayerExit = false;
                }
                return; // 延迟期间不移动
            }
            
            // 如果控制器在停顿中
            if (isControllerPaused)
            {
                controllerBlockedPauseTimer -= Time.fixedDeltaTime;
                if (controllerBlockedPauseTimer <= 0f)
                {
                    isControllerPaused = false;
                }
                return; // 停顿期间不移动
            }
            
            if (isPlayerOnElevator)
            {
                // 玩家在电梯上：电梯往下，控制器往上
                HandlePlayerOnElevator();
            }
            else
            {
                // 默认情况：控制器受重力往下，电梯往上
                HandleDefaultGravity();
            }
        }
        
        /// <summary>
        /// 处理玩家在电梯上的情况
        /// </summary>
        private void HandlePlayerOnElevator()
        {
            // 电梯需要往下移动，检查电梯下方是否被阻挡
            if (elevator.IsBlocked(Vector3.down))
            {
                // 电梯被阻挡，停止
                return;
            }
            
            // 控制器需要往上移动，检查控制器上方是否被阻挡
            if (controller.IsBlocked(Vector3.up))
            {
                // 控制器被阻挡，电梯停止
                return;
            }
            
            // 电梯往下移动（currentPosition 减小）
            // 将物理速度转换为 [0,1] 空间的移动
            float elevatorDistance = elevator.MoveDistance;
            float deltaMove = (elevatorSpeedDown * Time.fixedDeltaTime) / elevatorDistance;
            currentPosition = Mathf.Max(0f, currentPosition - deltaMove);
            
            UpdatePositions(currentPosition);
        }
        
        /// <summary>
        /// 处理默认重力情况
        /// </summary>
        private void HandleDefaultGravity()
        {
            // 电梯需要往上移动，检查电梯上方是否被阻挡
            if (elevator.IsBlocked(Vector3.up))
            {
                // 电梯被阻挡，停止
                return;
            }
            
            // 控制器需要往下移动，检查控制器下方是否被阻挡
            if (controller.IsBlocked(Vector3.down))
            {
                // 控制器被阻挡，进入停顿状态
                if (!isControllerPaused)
                {
                    isControllerPaused = true;
                    controllerBlockedPauseTimer = controllerBlockedPauseDuration;
                }
                return;
            }
            
            // 电梯往上移动（currentPosition 增加）
            // 将物理速度转换为 [0,1] 空间的移动
            float elevatorDistance = elevator.MoveDistance;
            float deltaMove = (elevatorSpeedUp * Time.fixedDeltaTime) / elevatorDistance;
            currentPosition = Mathf.Min(1f, currentPosition + deltaMove);
            
            UpdatePositions(currentPosition);
        }
        
        /// <summary>
        /// 根据位置百分比更新电梯和控制器的位置
        /// </summary>
        /// <param name="position">位置百分比 (0-1)</param>
        private void UpdatePositions(float position)
        {
            elevator.SetPosition(position);
            controller.SetPosition(position);
        }
        
        /// <summary>
        /// 玩家进入平台事件处理
        /// </summary>
        private void OnPlayerEnterPlatform(object data)
        {
            if (data is Player player && elevator != null)
            {
                // 检查是否是这个电梯的平台
                if (elevator.PlatformController.GetPlayersOnPlatform().Contains(player))
                {
                    isPlayerOnElevator = true;
                    
                    // 取消延迟状态（如果玩家重新站上电梯）
                    isWaitingAfterPlayerExit = false;
                    playerExitTimer = 0f;
                    
                    // 取消控制器停顿状态（玩家站上电梯需要立即响应）
                    if (isControllerPaused)
                    {
                        isControllerPaused = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 玩家离开平台事件处理
        /// </summary>
        private void OnPlayerExitPlatform(object data)
        {
            if (data is Player player && elevator != null)
            {
                // 首先检查玩家是否曾经在这个电梯上
                // 如果 isPlayerOnElevator 为 false，说明玩家不在这个电梯上，直接返回
                if (!isPlayerOnElevator)
                {
                    return;
                }
                
                // 检查这个电梯上是否还有玩家
                if (elevator.PlatformController.PlayerCount == 0)
                {
                    isPlayerOnElevator = false;
                    
                    // 开始延迟计时
                    isWaitingAfterPlayerExit = true;
                    playerExitTimer = playerExitDelay;
                }
            }
        }
        
        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void ResetElevator()
        {
            currentPosition = 0f;
            UpdatePositions(currentPosition);
            isPlayerOnElevator = false;
        }
        
        void OnDrawGizmosSelected()
        {
            if (elevator == null || controller == null)
                return;
            
            // 绘制连接线
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(elevator.transform.position, controller.transform.position);
        }
    }
}

