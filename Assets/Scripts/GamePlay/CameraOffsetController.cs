using UnityEngine;
using Cinemachine;
using DG.Tweening;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraOffsetController : MonoBehaviour
{
    [Header("偏移设置")]
    [Tooltip("默认的Y轴偏移值")]
    public float defaultYOffset = 5f;
    
    [Tooltip("最小的Y轴偏移值（按住下键时）")]
    public float minYOffset = 2f;
    
    [Header("过渡设置")]
    [Tooltip("偏移值变化的速度")]
    public float transitionSpeed = 3f;
    
    [Tooltip("是否使用平滑过渡")]
    public bool useSmoothTransition = true;
    
    [Header("延迟设置")]
    [Tooltip("触发相机偏移需要按住的时间（秒）")]
    public float holdThreshold = 0.3f;
    
    [Header("调试")]
    public bool showDebugLog = false;
    
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineFramingTransposer framingTransposer;
    private GameInputActions inputActions;
    private float targetYOffset;
    private bool isTransitioning = false;
    private Tweener offsetTweener;
    private bool isMovingHorizontally = false;
    private float downKeyHoldTime = 0f;
    private bool isHoldingDown = false;
    
    void Awake()
    {
        // 获取组件
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }
        
        // 初始化输入系统
        inputActions = new GameInputActions();
        
        // 设置初始偏移值
        targetYOffset = defaultYOffset;
        if (framingTransposer != null)
        {
            var offset = framingTransposer.m_TrackedObjectOffset;
            offset.y = defaultYOffset;
            framingTransposer.m_TrackedObjectOffset = offset;
        }
    }
    
    void OnEnable()
    {
        // 启用输入系统
        inputActions.Enable();
        
        // 订阅输入事件
        inputActions.Player.Move.performed += OnMoveInput;
        inputActions.Player.Move.canceled += OnMoveInputCanceled;
    }
    
    void OnDisable()
    {
        // 取消订阅输入事件
        inputActions.Player.Move.performed -= OnMoveInput;
        inputActions.Player.Move.canceled -= OnMoveInputCanceled;
        
        // 停用输入系统
        inputActions.Disable();
        
        // 停止所有过渡动画
        if (offsetTweener != null)
        {
            offsetTweener.Kill();
            offsetTweener = null;
        }
    }
    
    void Update()
    {
        // 处理按住下键的计时
        if (isHoldingDown && !isMovingHorizontally)
        {
            downKeyHoldTime += Time.deltaTime;
            
            // 当达到阈值时触发相机偏移
            if (downKeyHoldTime >= holdThreshold && targetYOffset != minYOffset)
            {
                SetTargetOffset(minYOffset);
                if (showDebugLog)
                {
                    Debug.Log($"下键按住{holdThreshold}秒，降低相机位置");
                }
            }
        }
    }
    
    /// <summary>
    /// 处理移动输入
    /// </summary>
    private void OnMoveInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        
        // 检查是否有水平移动
        isMovingHorizontally = Mathf.Abs(moveInput.x) > 0.1f;
        
        // 如果在水平移动，强制恢复到默认位置
        if (isMovingHorizontally)
        {
            ResetCameraState();
            if (showDebugLog)
            {
                Debug.Log("水平移动中，恢复相机位置");
            }
            return;
        }
        
        // 更新下键按住状态
        isHoldingDown = moveInput.y < -0.5f;
        
        // 如果松开下键或者向上移动，重置状态
        if (!isHoldingDown)
        {
            ResetCameraState();
        }
    }
    
    /// <summary>
    /// 处理移动输入结束
    /// </summary>
    private void OnMoveInputCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ResetCameraState();
        
        if (showDebugLog)
        {
            Debug.Log("输入结束，恢复相机位置");
        }
    }
    
    /// <summary>
    /// 设置目标偏移值
    /// </summary>
    private void SetTargetOffset(float newTargetOffset)
    {
        if (framingTransposer == null) return;
        
        targetYOffset = newTargetOffset;
        
        if (useSmoothTransition)
        {
            // 停止之前的过渡动画
            if (offsetTweener != null)
            {
                offsetTweener.Kill();
            }
            
            // 创建新的过渡动画
            var currentOffset = framingTransposer.m_TrackedObjectOffset;
            offsetTweener = DOTween.To(
                () => currentOffset.y,
                (float value) =>
                {
                    var offset = framingTransposer.m_TrackedObjectOffset;
                    offset.y = value;
                    framingTransposer.m_TrackedObjectOffset = offset;
                },
                targetYOffset,
                1f / transitionSpeed
            ).SetEase(Ease.OutQuad);
        }
        else
        {
            // 直接设置偏移值
            var offset = framingTransposer.m_TrackedObjectOffset;
            offset.y = targetYOffset;
            framingTransposer.m_TrackedObjectOffset = offset;
        }
    }
    
    /// <summary>
    /// 重置相机状态
    /// </summary>
    private void ResetCameraState()
    {
        isHoldingDown = false;
        isMovingHorizontally = false;
        downKeyHoldTime = 0f;
        SetTargetOffset(defaultYOffset);
    }
    
    /// <summary>
    /// 重置相机偏移到默认值
    /// </summary>
    [ContextMenu("重置相机偏移")]
    public void ResetOffset()
    {
        SetTargetOffset(defaultYOffset);
    }
} 