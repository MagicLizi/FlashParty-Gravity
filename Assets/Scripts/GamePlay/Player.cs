using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public enum FaceDir
{
    Left = -1,
    Right = 1,
}

public class Player : MonoBehaviour
{
    // public GameObject BallRes;

    // public GameObject BallCreater;

    // public Vector2 BallSpeed = Vector2.zero;

    public Animator animator;

    public BoxCollider2D boxCollider;

    public float CurXMoveSpeed = 0;

    public float MoveSpeed = 0;

    public float AirMoveSpeed = 0;

    public float AirDrag = 0;

    public float JumpSpeed = 4;

    public float BaseMoveSpeed = 4.5f; //基准值

    public float maxFallSpeed = -10f; //最大下落速度（负值）

    public bool inAir = false;

    public bool inAirTouchWall = false;
    
    private int currentAirJumpCount = 0; // 当前已使用的空中跳跃次数
    
    [Header("地面跳跃保护（土狼时间）")]
    [Tooltip("离开地面后仍可以执行地面跳跃的保护时间（秒）")]
    [Range(0f, 0.2f)]
    public float groundJumpGracePeriod = 0.03f; // 地面跳跃保护时间
    
    private float lastGroundedTime = -1f; // 上次在地面的时间（负值表示从未在地面）
    private bool wasGroundedLastFrame = false; // 上一帧是否在地面
    
    public int maxAirJumpCount = 1; // 最大空中跳跃次数
    
    public float AirJumpSpeed = 4; // 空中跳跃速度（可以和地面跳跃速度不同）

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    public string Name = "Sandboy";

    public string CurAnimName = "Idle";

    public LayerMask groundMask;

    private bool isDead = false;
    
    [Header("游戏状态")]
    [Tooltip("玩家是否拥有钥匙")]
    public bool hasKey = false;

    private float rayLength = 0.2f;

    public bool StopSpeed = false;
    
    [Header("斜面移动设置")]
    [Tooltip("可行走的最大斜面角度")]
    [Range(0f, 80f)]
    public float maxSlopeAngle = 55f;
    
    [Tooltip("斜面移动速度倍数（1.0为正常速度，>1加速，<1减速）")]
    [Range(0.5f, 2f)]
    public float slopeSpeedMultiplier = 1.0f;
    
    private bool isOnSlope = false; // 是否在斜面上
    private bool wasOnSlope = false; // 上一帧是否在斜面上
    private Vector2 slopeNormal = Vector2.up; // 当前斜面的法线
    private float slopeAngle = 0f; // 当前斜面角度
    private bool justJumped = false; // 刚跳跃，用于防止立即进入斜面逻辑
    private float slopeObstacleCheckDistance = 0.3f; // 斜面前方障碍检测距离

    private float originGvS = 1;

    private bool isLoseGravity = false;

    public bool isInAtk = false;

    //public bool isInSkill = false;

    public bool isInAirAtk = false;
    
    public bool isHitFlying = false; // 是否处于被击飞状态
    
    public bool isInHitStop = false; // 是否处于击飞停顿状态
    
    [Header("击飞状态的横向速度衰减")]
    [Tooltip("被击飞时横向速度衰减率（0-1，越小衰减越快）")]
    [Range(0.8f, 0.99f)]
    public float hitFlyDamping = 0.95f; // 横向速度衰减率
    
    [Tooltip("击飞时玩家输入对横向速度的影响速度（0-1，越小调整越慢）")]
    [Range(0.01f, 0.5f)]
    public float hitFlyControlSpeed = 0.1f; // 玩家输入的调整速度
    
    [Tooltip("击飞后多久才能通过输入改变横向速度（秒）")]
    [Range(0f, 0.5f)]
    public float hitFlyControlDelay = 0.1f; // 击飞初期的无控制时间
    
    [Header("击飞结束后的速度过渡")]
    [Tooltip("击飞结束后，速度过渡到InAir最大速度的时间（秒）")]
    [Range(0.1f, 2f)]
    public float hitFlyToAirTransitionTime = 0.5f; // 过渡时间
    
    private Tween hitFlyTween; // 击飞计时器
    private Tween hitStopTween; // 停顿计时器
    private Vector2 pendingLaunchForce; // 待施加的击飞力
    private bool hasPendingLaunch = false; // 是否有待处理的击飞
    private float hitFlyStartTime = 0f; // 击飞开始的时间
    
    // 击飞结束后的速度过渡相关
    private bool isTransitioningFromHitFly = false; // 是否正在从击飞过渡到 inAir
    private float hitFlyEndSpeed = 0f; // 击飞结束时的横向速度
    private float transitionStartTime = 0f; // 过渡开始时间

    public GameObject AtkCollider;

    public GameObject AirAtkCollider;

    SpriteRenderer spriteRenderer;

    public Wind isInWind = null;

    public Vector2 windSpeed = Vector2.zero;

    public LevelRotator curLevelRotator = null;

    public GameObject RebornSivi;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
        EventManager.Instance.AddListener(EventType.Reset, OnReset);
        EventManager.Instance.AddListener(EventType.Action, OnAction);
        // EventManager.Instance.AddListener(EventType.Special, OnSpecial);
        // 添加平台事件监听
        EventManager.Instance.AddListener(EventType.PlatformPlayerOn, OnPlatformEnter);
        EventManager.Instance.AddListener(EventType.PlatformPlayerOff, OnPlatformExit);
        originGvS = rb.gravityScale;
    }

    void Update()
    {
        CheckInAir();
        CheckInAtk();
    }

    public void SetSuccess()
    {
        InputManager.Instance.Enable(false);
    }

    private void CheckInAtk()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        isInAtk = currentState.IsName("Sandboy@Attack");
        isInAirAtk = currentState.IsName("Sandboy@AttackAir");
        // isHitFlying 由代码逻辑控制（GetLaunched/EndHitFlying），不应该被动画状态机覆盖
        // isHitFlying = currentState.IsName("Sandboy@HitFlying");
        //isInSkill = currentState.IsName("Sandboy@Skill");
        AtkCollider.gameObject.SetActive(isInAtk);
        AirAtkCollider.gameObject.SetActive(isInAirAtk);
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }
        
        // 停顿状态：速度为0，不受重力影响
        if (isInHitStop)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        // 被击飞状态下的特殊物理处理
        if (isHitFlying)
        {
            // 如果有待施加的击飞力，先施加
            if (hasPendingLaunch)
            {
                // Debug.Log($"[Player] FixedUpdate 准备施加击飞力 - isHitFlying: {isHitFlying}, hasPendingLaunch: {hasPendingLaunch}, force: {pendingLaunchForce}");
                rb.velocity = pendingLaunchForce;
                hasPendingLaunch = false;
                // Debug.Log($"[Player] FixedUpdate 已施加击飞力，新速度: {rb.velocity}");
                return; // 本帧只施加力，不做衰减
            }
            
            // 检测是否接触地面或墙面
            bool touchedGround = IsGrounded();
            bool touchedWall = IsTouchWallAny(); // 使用双向墙壁检测，不受角色朝向限制
            
            // 判断是否应该结束击飞：
            // 1. 如果接触墙面，立即结束击飞
            // 2. 如果接触地面但是平地（不是斜面或斜角很小），结束击飞
            // 3. 如果接触斜面，继续保持击飞状态
            bool shouldEndHitFly = false;
            
            if (touchedWall)
            {
                // 撞墙立即结束击飞
                shouldEndHitFly = true;
                // Debug.Log($"[Player] 击飞中接触墙面，立即结束击飞");
            }
            else if (touchedGround)
            {
                // 接触地面，但需要判断是平地还是斜面
                // 只有在平地上才结束击飞，斜面上继续击飞
                if (!isOnSlope)
                {
                    // 平地（0度或超过最大可行走角度），结束击飞
                    shouldEndHitFly = true;
                    // Debug.Log($"[Player] 击飞中接触平地，结束击飞。斜面角度: {slopeAngle}");
                }
                else
                {
                    // 斜面（角度在 0 到 maxSlopeAngle 之间），继续击飞
                    // Debug.Log($"[Player] 击飞中接触斜面，继续击飞。斜面角度: {slopeAngle}");
                }
            }
            
            if (shouldEndHitFly)
            {
                EndHitFlying();
                // EndHitFlying 后 isHitFlying 变为 false，本帧不再处理击飞逻辑
                // 下一帧会进入正常的地面/空中物理处理
                return;
            }
            
            // 横向速度处理：只在有输入时才调整，否则保持弹射力的横向速度
            float hitFlyTargetSpeed = rb.velocity.x;
            
            // 检查是否已经过了无控制延迟时间
            bool canControl = (Time.time - hitFlyStartTime) >= hitFlyControlDelay;
            
            // 只有当玩家有实际输入时（非零移动速度）且过了延迟时间，才进行速度调整
            if (canControl && Mathf.Abs(CurXMoveSpeed) > 0.01f)
            {
                // 判断输入方向和当前速度是否同向
                bool isSameDirection = (CurXMoveSpeed * rb.velocity.x) > 0;
                
                // 如果同向且当前速度的绝对值更大，保持当前速度（不减速）
                if (isSameDirection && Mathf.Abs(rb.velocity.x) > Mathf.Abs(CurXMoveSpeed))
                {
                    // 保持当前速度，只应用后续的衰减
                    hitFlyTargetSpeed = rb.velocity.x;
                }
                else
                {
                    // 反向或需要加速时，使用 Lerp 平滑过渡
                    // 使用 hitFlyControlSpeed 控制调整速度（值越小，调整越慢）
                    hitFlyTargetSpeed = Mathf.Lerp(rb.velocity.x, CurXMoveSpeed, hitFlyControlSpeed);
                }
            }
            
            // 在目标速度基础上应用击飞衰减
            hitFlyTargetSpeed *= hitFlyDamping;
            
            rb.velocity = new Vector2(hitFlyTargetSpeed, rb.velocity.y);
            
            // 限制Y轴下落速度
            if (rb.velocity.y < maxFallSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
            }
            
            // 只在前几帧输出日志
            // if (Time.frameCount % 10 == 0)  // 每10帧输出一次
            // {
            //     Debug.Log($"[Player] FixedUpdate 击飞中 - 目标速度: {hitFlyTargetSpeed}, 新速度: {rb.velocity}, damping: {hitFlyDamping}");
            // }
            
            return;
        }
        
        // Debug.Log($"rb.velocity y: {rb.velocity.y}");
        float targetSpeed;
        if (inAir)
        {
            // 检查是否正在从击飞过渡到 inAir
            if (isTransitioningFromHitFly)
            {
                float elapsed = Time.time - transitionStartTime;
                float t = Mathf.Clamp01(elapsed / hitFlyToAirTransitionTime);
                
                // 计算目标最大速度（考虑方向）
                float maxSpeed = Mathf.Abs(AirMoveSpeed) * Mathf.Sign(hitFlyEndSpeed);
                
                // 从击飞结束速度平滑过渡到最大 InAir 速度
                float transitionSpeed = Mathf.Lerp(hitFlyEndSpeed, maxSpeed, t);
                
                // 如果玩家输入了相反方向，允许控制
                if (CurXMoveSpeed != 0 && Mathf.Sign(CurXMoveSpeed) != Mathf.Sign(hitFlyEndSpeed))
                {
                    // 玩家想转向，使用正常的空中控制
                    targetSpeed = Mathf.Lerp(rb.velocity.x, CurXMoveSpeed, AirDrag);
                    isTransitioningFromHitFly = false; // 结束过渡
                }
                else if (CurXMoveSpeed != 0 && Mathf.Abs(CurXMoveSpeed) > Mathf.Abs(transitionSpeed))
                {
                    // 玩家输入同向且想加速（不超过过渡速度）
                    targetSpeed = Mathf.Max(Mathf.Abs(CurXMoveSpeed), Mathf.Abs(transitionSpeed)) * Mathf.Sign(hitFlyEndSpeed);
                }
                else
                {
                    // 保持过渡速度
                    targetSpeed = transitionSpeed;
                }
                
                // 过渡完成
                if (t >= 1f)
                {
                    isTransitioningFromHitFly = false;
                    // Debug.Log($"[Player] 速度过渡完成 - 最终速度: {targetSpeed}");
                }
            }
            else
            {
                // 正常的空中移动逻辑
                targetSpeed = Mathf.Lerp(rb.velocity.x, CurXMoveSpeed, AirDrag);
                if (targetSpeed * (int)CurFaceDir < 0 || Mathf.Abs(rb.velocity.x) < Mathf.Abs(CurXMoveSpeed))
                {
                    // Debug.Log("空中转向");
                    targetSpeed = CurXMoveSpeed;
                }
            }
            
            // Debug.Log($"No Speed AirDrag: {rb.velocity.x} {CurXMoveSpeed} {targetSpeed}");
            // Debug.Log($"inAirTouchWall: {inAirTouchWall} {targetSpeed * (int)CurFaceDir}");
            if (inAirTouchWall && targetSpeed * (int)CurFaceDir >= 0)
            {
                targetSpeed = 0;
                // Debug.Log("蜘蛛侠设置速度为0");
            }
        }
        else
        {
            targetSpeed = CurXMoveSpeed;
            // 在斜面上应用速度倍数
            if (isOnSlope && Mathf.Abs(targetSpeed) > 0.01f)
            {
                targetSpeed *= slopeSpeedMultiplier;
                // 同时调整动画速度
                if (animator != null && BaseMoveSpeed > 0)
                {
                    animator.speed = Mathf.Abs(targetSpeed / BaseMoveSpeed);
                }
            }
        }
        AnimateSetBool("CanMove", targetSpeed != 0);
        
        // 检测从斜面过渡到平地（在地面上，刚从斜面离开）
        bool justLeftSlope = wasOnSlope && !isOnSlope && !inAir;
        
        Vector2 velocity;
        
        // 如果刚跳跃，在真正离地前不进入斜面逻辑
        if (justJumped && rb.velocity.y > 0.5f)
        {
            // 跳跃上升中，不处理斜面逻辑
            velocity = new Vector2(targetSpeed, rb.velocity.y);
            // 保持重力正常
            if (rb.gravityScale != originGvS)
            {
                rb.gravityScale = originGvS;
            }
        }
        // 在斜面上时，减小重力来防止下滑
        else if (!inAir && isOnSlope)
        {
            // 临时减小重力，防止在斜面上下滑
            rb.gravityScale = 0;
            
            // 重置跳跃标记（已经落地了）
            justJumped = false;
            
            if (Mathf.Abs(targetSpeed) > 0.01f)
            {
                // 检查斜面移动方向是否有障碍物
                bool hasObstacleAhead = CheckSlopeObstacle(targetSpeed);
                
                if (hasObstacleAhead)
                {
                    // 前方有障碍，使用水平移动而不是斜面移动，防止飞起
                    velocity = new Vector2(targetSpeed, Mathf.Min(rb.velocity.y, 0f));
                }
                else
                {
                    // 在斜面上移动：保持水平速度与平地一致
                    // 计算斜面方向的垂直分量
                    Vector2 slopeDirection = new Vector2(slopeNormal.y, -slopeNormal.x);
                    
                    // 根据移动方向调整斜面方向
                    if (targetSpeed < 0)
                    {
                        slopeDirection = -slopeDirection;
                    }
                    
                    // targetSpeed 已经在前面应用了倍数
                    // 保持水平速度不变，根据斜面角度计算垂直速度
                    float slopeSpeedY = -Mathf.Abs(targetSpeed) * Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(slopeDirection.x);
                    
                    velocity = new Vector2(targetSpeed, slopeSpeedY);
                    velocity.y -= 0.1f; // 添加小的向下力，确保贴合斜面
                }
            }
            else
            {
                // 在斜面上静止：施加小的向下速度，确保贴合斜面
                velocity = new Vector2(0f, -0.1f);
            }
        }
        else
        {
            // 不在斜面上：恢复正常重力
            if (rb.gravityScale != originGvS)
            {
                rb.gravityScale = originGvS;
            }
            
            // 如果在空中，重置跳跃标记
            if (inAir)
            {
                justJumped = false;
            }
            
            // 平地或空中：正常水平移动
            velocity = new Vector2(targetSpeed, rb.velocity.y);
            
            // 刚离开斜面进入平地：强制清除向上的Y速度分量，防止腾空
            if (justLeftSlope)
            {
                // 如果有向上的速度（从斜面带来的），直接归零，确保贴地
                velocity.y = Mathf.Min(velocity.y, -0.5f);
                justJumped = false; // 落地了，重置跳跃标记
                
                // 恢复正常动画速度
                if (animator != null && BaseMoveSpeed > 0 && Mathf.Abs(CurXMoveSpeed) > 0.01f)
                {
                    animator.speed = Mathf.Abs(CurXMoveSpeed / BaseMoveSpeed);
                }
            }
            // 在地面上但不在斜面上，确保有向下的力贴地
            else if (!inAir && !isOnSlope && Mathf.Abs(velocity.y) < 0.1f)
            {
                velocity.y = -0.5f;
            }
        }
        
        // 更新wasOnSlope状态
        wasOnSlope = isOnSlope;
        
        if (isLoseGravity)
        {
            velocity = new Vector2(0, 0);
        }
        //if (isInAtk || isInSkill)
        if (isInAtk)
        {
            velocity = new Vector2(0, rb.velocity.y);
        }
        rb.velocity = velocity + windSpeed;

        // 限制Y轴下落速度，防止从高处落下时速度过快
        if (rb.velocity.y < maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
        }

        // Debug.Log($"rb.velocity y1: {rb.velocity.y}");
    }

    void CheckFaceDir()
    {
        if (!isInAtk)
        {
            if (CurXMoveSpeed > 0)
            {
                CurFaceDir = FaceDir.Right;
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (CurXMoveSpeed < 0)
            {
                CurFaceDir = FaceDir.Left;
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }
    }

    void OnMove(object data)
    {
        // 停顿状态下无法移动
        if (isInHitStop)
        {
            return;
        }
        
        MoveData moveData = (MoveData)data;
        Vector2 moveDir = moveData.moveDir;
        int moveDirX = 0;
        if (moveDir.x > 0)
        {
            moveDirX = 1;
        }
        else if (moveDir.x < 0)
        {
            moveDirX = -1;
        }
        if (Mathf.Abs(moveDir.x) < 0.3f)
        {
            moveDirX = 0;
            //Debug.Log("moveDirX 0");
        }
        
        // 被击飞状态下也可以调整横向速度，使用空中移动速度
        if (isHitFlying)
        {
            CurXMoveSpeed = moveDirX * AirMoveSpeed;
        }
        else if (inAir)
        {
            CurXMoveSpeed = moveDirX * AirMoveSpeed;
        }
        else
        {
            CurXMoveSpeed = moveDirX * MoveSpeed;
            // Debug.Log($"CurMoveSpeed: {CurXMoveSpeed}");
            if (CurXMoveSpeed != 0)
            {
                CurXMoveSpeed = moveDirX * MoveSpeed;
                animator.speed = Mathf.Abs(CurXMoveSpeed / BaseMoveSpeed);
                // AnimateSetTrigger("CanMove", true);
            }
            else
            {
                animator.speed = 1;
                // 不要在这里直接设置velocity，让FixedUpdate处理
                // 特别是在斜面上，我们需要保持抵消重力的速度
                // rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        CheckFaceDir();
    }


    void OnJump(object data)
    {
        // 攻击中、被击飞状态或停顿状态下不能跳跃
        if (isInAtk || isInAirAtk || isHitFlying || isInHitStop)
        {
            return;
        }
        
        // 检查是否在地面跳跃保护时间内
        bool isInGroundJumpGracePeriod = inAir && 
                                         lastGroundedTime >= 0f && 
                                         (Time.time - lastGroundedTime) <= groundJumpGracePeriod;
        
        if (!inAir || isInGroundJumpGracePeriod)
        {
            // 地面跳跃（包括斜面上的跳跃和保护时间内的跳跃）
            // 在斜面上跳跃时，保持当前的横向速度分量
            float currentXVelocity = rb.velocity.x;
            
            // 如果在斜面上移动，保持移动速度；如果静止，则使用当前速度
            if (isOnSlope && Mathf.Abs(CurXMoveSpeed) > 0.01f)
            {
                currentXVelocity = CurXMoveSpeed;
            }
            
            // 如果在斜面上，跳跃时立即恢复重力
            if (isOnSlope)
            {
                rb.gravityScale = originGvS;
                justJumped = true; // 标记刚跳跃，防止立即被斜面逻辑覆盖
            }
            
            rb.velocity = new Vector2(currentXVelocity, JumpSpeed);
            AnimateSetTrigger("Jump");
            
            // 使用地面跳跃后，清除保护时间（避免连续触发）
            if (isInGroundJumpGracePeriod)
            {
                lastGroundedTime = -1f; // 清除保护时间
                // Debug.Log($"[Player] 地面跳跃保护触发！离地时间: {Time.time - lastGroundedTime:F3}秒");
            }
        }
        else if (currentAirJumpCount < maxAirJumpCount)
        {
            // 空中跳跃（二段跳）
            rb.velocity = new Vector2(rb.velocity.x, AirJumpSpeed);
            AnimateSetTrigger("JumpAir");
            currentAirJumpCount++;
            
            // 二段跳时取消击飞速度过渡
            if (isTransitioningFromHitFly)
            {
                isTransitioningFromHitFly = false;
            }
        }
    }

    void OnReset(object data)
    {
        if (Const.LastReborn != null)
        {
            Dead(Const.LastReborn);
        }
    }

    void CheckInAir()
    {
        inAir = !IsGrounded();
        AnimateSetBool("inAir", inAir);
        
        if (inAir)
        {
            inAirTouchWall = IsTouchWall();
        }
        else
        {
            // 在地面上，更新地面时间
            lastGroundedTime = Time.time;
            
            // 落地后重置空中跳跃次数
            currentAirJumpCount = 0;
            
            // 落地后取消击飞到空中的速度过渡
            if (isTransitioningFromHitFly)
            {
                isTransitioningFromHitFly = false;
                // Debug.Log($"[Player] 落地，取消速度过渡");
            }
        }
        
        // 更新上一帧状态
        wasGroundedLastFrame = !inAir;
    }

    bool IsGrounded()
    {
        // groundRayCast = (Vector2)transform.position + new Vector2(0, -boxCollider.bounds.size.y * 0.5f + 0.02f);
        Vector2 middle = (Vector2)transform.position;
        Vector2 left = middle + new Vector2(-boxCollider.bounds.size.x / 2, 0);
        Vector2 right = middle + new Vector2(boxCollider.bounds.size.x / 2, 0);
        
        RaycastHit2D hitMiddle = Physics2D.Raycast(middle, Vector2.down, rayLength, groundMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(left, Vector2.down, rayLength, groundMask);
        RaycastHit2D hitRight = Physics2D.Raycast(right, Vector2.down, rayLength, groundMask);
        
        bool grounded = hitMiddle || hitLeft || hitRight;
        
        if (grounded)
        {
            // 检测斜面（优先使用中间的射线，其次左右）
            RaycastHit2D hit = hitMiddle ? hitMiddle : (hitLeft ? hitLeft : hitRight);
            DetectSlope(hit);
        }
        else
        {
            // 不在地面时重置斜面信息
            isOnSlope = false;
            slopeNormal = Vector2.up;
            slopeAngle = 0f;
        }
        
        return grounded;
    }
    
    /// <summary>
    /// 检测斜面信息
    /// </summary>
    private void DetectSlope(RaycastHit2D hit)
    {
        slopeNormal = hit.normal;
        slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);
        
        // 判断是否在可行走的斜面上
        if (slopeAngle > 0f && slopeAngle <= maxSlopeAngle)
        {
            isOnSlope = true;
        }
        else
        {
            isOnSlope = false;
        }
    }
    
    /// <summary>
    /// 检查斜面移动方向前方是否有障碍物
    /// </summary>
    private bool CheckSlopeObstacle(float moveDirection)
    {
        if (boxCollider == null) return false;
        
        // 确定检测方向（左或右）
        Vector2 checkDir = moveDirection > 0 ? Vector2.right : Vector2.left;
        
        // 从角色中心位置检测
        Vector2 origin = transform.position;
        
        // 使用BoxCast检测前方障碍物
        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            boxCollider.size * 0.8f, // 使用略小的检测框
            0f,
            checkDir,
            slopeObstacleCheckDistance,
            groundMask
        );
        
        // 如果检测到障碍物，返回true
        return hit.collider != null;
    }

    bool IsTouchWall()
    {
        Vector2 downOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, 0);
        Vector2 middleOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y / 2);
        Vector2 upOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y);
        Vector2 direction = Vector2.zero;
        if (CurFaceDir == FaceDir.Right)
        {
            direction = Vector2.right;
        }
        else
        {
            direction = Vector2.left;
        }
        return Physics2D.Raycast(middleOrigin, direction, rayLength, groundMask) ||
                Physics2D.Raycast(downOrigin, direction, rayLength, groundMask) ||
                Physics2D.Raycast(upOrigin, direction, rayLength, groundMask);
    }

    /// <summary>
    /// 检测左右两侧是否有墙壁（用于击飞状态，不受角色朝向限制）
    /// </summary>
    bool IsTouchWallAny()
    {
        float halfWidth = boxCollider.bounds.size.x / 2;
        float height = boxCollider.bounds.size.y;
        
        // 检测左侧墙壁
        Vector2 leftDown = (Vector2)transform.position + new Vector2(-halfWidth, 0);
        Vector2 leftMiddle = (Vector2)transform.position + new Vector2(-halfWidth, height / 2);
        Vector2 leftUp = (Vector2)transform.position + new Vector2(-halfWidth, height);
        
        bool leftWall = Physics2D.Raycast(leftMiddle, Vector2.left, rayLength, groundMask) ||
                        Physics2D.Raycast(leftDown, Vector2.left, rayLength, groundMask) ||
                        Physics2D.Raycast(leftUp, Vector2.left, rayLength, groundMask);
        
        // 检测右侧墙壁
        Vector2 rightDown = (Vector2)transform.position + new Vector2(halfWidth, 0);
        Vector2 rightMiddle = (Vector2)transform.position + new Vector2(halfWidth, height / 2);
        Vector2 rightUp = (Vector2)transform.position + new Vector2(halfWidth, height);
        
        bool rightWall = Physics2D.Raycast(rightMiddle, Vector2.right, rayLength, groundMask) ||
                         Physics2D.Raycast(rightDown, Vector2.right, rayLength, groundMask) ||
                         Physics2D.Raycast(rightUp, Vector2.right, rayLength, groundMask);
        
        return leftWall || rightWall;
    }


    void OnDrawGizmos()
    {
        // 可视化斜面信息（调试用）
        if (Application.isPlaying && isOnSlope && boxCollider != null)
        {
            Vector2 playerPos = transform.position;
            
            // 绘制斜面法线（绿色）
            Gizmos.color = Color.green;
            Gizmos.DrawLine(playerPos, playerPos + slopeNormal * 1f);
            
            // 绘制斜面方向（黄色）
            Vector2 slopeDir = new Vector2(slopeNormal.y, -slopeNormal.x);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerPos, playerPos + slopeDir * 1f);
            Gizmos.DrawLine(playerPos, playerPos - slopeDir * 1f);
        }
        
        // if (boxCollider != null)
        // {
        //     Vector2 middle = (Vector2)transform.position;
        //     Vector2 left = middle + new Vector2(-boxCollider.bounds.size.x / 2, 0);
        //     Vector2 right = middle + new Vector2(boxCollider.bounds.size.x / 2, 0);
        //     Debug.DrawLine(middle, middle + Vector2.down * rayLength, Color.red);
        //     Debug.DrawLine(left, left + Vector2.down * rayLength, Color.red);
        //     Debug.DrawLine(right, right + Vector2.down * rayLength, Color.red);

        //     Vector2 direction = Vector2.zero;
        //     if (CurFaceDir == FaceDir.Right)
        //     {
        //         direction = Vector2.right;
        //     }
        //     else
        //     {
        //         direction = Vector2.left;
        //     }

        //     Vector2 middleOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y / 2);
        //     Debug.DrawLine(middleOrigin, middleOrigin + direction * rayLength, Color.blue);

        //     Vector2 downOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, 0);
        //     Debug.DrawLine(downOrigin, downOrigin + direction * rayLength, Color.blue);

        //     Vector2 upOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y);
        //     Debug.DrawLine(upOrigin, upOrigin + direction * rayLength, Color.blue);
        // }
    }

    void OnPlatformEnter(object data)
    {
        if (data is Player player && player == this)
        {
           // Debug.Log("Player entered moving platform");
            // 可以在这里添加进入平台的特殊逻辑，比如改变状态或播放音效
        }
    }

    void OnPlatformExit(object data)
    {
        if (data is Player player && player == this)
        {
           // Debug.Log("Player left moving platform");
            // 可以在这里添加离开平台的特殊逻辑
        }
    }

    void OnDestroy()
    {
        // 清理事件监听，避免内存泄漏
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(EventType.Move, OnMove);
            EventManager.Instance.RemoveListener(EventType.Jump, OnJump);
            EventManager.Instance.RemoveListener(EventType.Action, OnAction);
            // EventManager.Instance.RemoveListener(EventType.Special, OnSpecial);
            EventManager.Instance.RemoveListener(EventType.Reset, OnReset);
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOn, OnPlatformEnter);
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOff, OnPlatformExit);
        }
    }

    public void AddWindSpeed(Vector2 speedVec)
    {
        Debug.Log($"AddWindSpeed: {speedVec}");
        windSpeed = speedVec;
    }

    public void LoseGravity(bool lose)
    {
        if (lose)
        {
            rb.gravityScale = 0;
        }
        else
        {
            rb.gravityScale = originGvS;
        }
        isLoseGravity = lose;
    }

    public void Dead(GameObject rebornPt)
    {
        if(Const.InRotation)
        {
            return;
        }
        if (isDead) return;
        Const.LastReborn = rebornPt;
        
        // 立即停止所有物理运动和状态
        ResetPlayerStateForRebirth();
        
        AnimateSetBool("LossG", true);
        isDead = true;
        InputManager.Instance.Enable(false);
        //Debug.Log("Player has died.");
        
        // 使用DOTween创建闪烁效果
        if (spriteRenderer != null)
        {
            Shine(true);
            LoseGravity(true);
            float rotateAngle = 0 - curLevelRotator.transform.eulerAngles.z;
            curLevelRotator.ResetRotation(transform.position, () =>
            {
                Shine(false);
                RebornSivi.SetActive(true);
                
                // 立即开始移动角色到重生点
                transform.DOMove(rebornPt.transform.position, 1.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
                {
                    isDead = false;
                    LoseGravity(false);
                    AnimateSetBool("LossG", false);
                    RebornSivi.SetActive(false);
                    InputManager.Instance.Enable(true);
                    
                    // 重生完成后重新启用碰撞器
                    if (boxCollider != null)
                    {
                        boxCollider.enabled = true;
                    }
                });
            });
        }
    }
    
    /// <summary>
    /// 重置玩家状态以准备重生
    /// </summary>
    private void ResetPlayerStateForRebirth()
    {
        // 1. 立即停止所有速度，避免受重力影响往下落
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // 2. 重置击飞相关状态，避免击飞状态下还会位移
        isHitFlying = false;
        isInHitStop = false;
        hasPendingLaunch = false;
        AnimateSetBool("isHitFlying", false);
        
        // 取消击飞和停顿的计时器
        if (hitFlyTween != null)
        {
            hitFlyTween.Kill();
            hitFlyTween = null;
        }
        if (hitStopTween != null)
        {
            hitStopTween.Kill();
            hitStopTween = null;
        }
        
        // 3. 禁用碰撞器，避免和地刺等机关发生交互
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
        
        // 4. 重置其他移动相关状态
        CurXMoveSpeed = 0;
        windSpeed = Vector2.zero;
        StopSpeed = false;
    }
    TweenerCore<Color, Color, ColorOptions> fadeTween;
    public void Shine(bool shine)
    {
        if (shine)
        {
            if (fadeTween == null)
            {
                // 创建闪烁动画，透明度在0.3和1之间变化，持续0.2秒，重复-1次（无限循环）
                fadeTween = spriteRenderer.DOFade(0.3f, 0.1f).SetLoops(-1, LoopType.Yoyo);
            }
        }
        else
        {
            if (fadeTween != null)
            {
                spriteRenderer.DOFade(1, 0.1f);
                fadeTween.Kill();
                fadeTween = null;
            }
        }
    }


    void OnAction(object data)
    {
        // 只有不在攻击状态、不在被击飞状态、不在停顿状态时才能攻击，防止 Trigger 缓冲
        if (!isInAtk && !isInAirAtk && !isHitFlying && !isInHitStop)
        {
            AnimateSetTrigger("Attack");
        }
    }

    void OnSpecial(object data)
    {
        if (!inAir)
        {
            AnimateSetTrigger("Skill");
            // DOVirtual.DelayedCall(0.25f, () =>
            // {
            //     GameObject ball = Instantiate(BallRes, BallCreater.transform.position, Quaternion.identity);
            //     Atk ballAtk = ball.GetComponent<Atk>();
            //     ballAtk.CurPlayer = this;
            //     ballAtk.AtkTime = 0;
            //     ball.GetComponent<Rigidbody2D>().velocity = new Vector2(BallSpeed.x * (int)CurFaceDir, BallSpeed.y);
            // });
        }
    }

    public void AnimateSetBool(string triggerName, bool trigger)
    {
        if (isDead)
        {
            return;
        }
        animator.SetBool(triggerName, trigger);
    }

    public void AnimateSetTrigger(string triggerName)
    {
        if (isDead)
        {
            return;
        }
        animator.SetTrigger(triggerName);
    }

    public GameObject AtkFx;


    public GameObject AtkAudio;
    public void AtkShow(GameObject atkGo)
    {
        // if (AtkFx != null)
        // {
        //     GameObject fx = Instantiate(AtkFx, atkGo.transform.position, Quaternion.identity);
        //     Destroy(fx, 2f);
        // }

        if (AtkAudio != null)
        {
            GameObject atkAudio = Instantiate(AtkAudio, atkGo.transform.position, Quaternion.identity);
            Destroy(atkAudio, 2f);
        }

        // 为被攻击对象添加闪烁动画
        SpriteRenderer targetSpriteRenderer = atkGo.GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer != null)
        {
            // 创建闪烁动画序列
            DG.Tweening.Sequence blinkSequence = DOTween.Sequence();

            // 闪烁3次，每次透明度在0.3和1之间切换
            for (int i = 0; i < 3; i++)
            {
                blinkSequence.Append(targetSpriteRenderer.DOFade(0.3f, 0.1f));
                blinkSequence.Append(targetSpriteRenderer.DOFade(1f, 0.1f));
            }

            // 确保动画完成后透明度恢复正常
            blinkSequence.OnComplete(() =>
            {
                targetSpriteRenderer.color = new Color(targetSpriteRenderer.color.r,
                                                       targetSpriteRenderer.color.g,
                                                       targetSpriteRenderer.color.b,
                                                       1f);
            });
        }
    }
    
    /// <summary>
    /// 被弹射平台击飞
    /// </summary>
    /// <param name="force">弹射力</param>
    /// <param name="hitFlyDuration">被击飞持续时间</param>
    public void GetLaunched(Vector2 force, float hitFlyDuration)
    {
        // Debug.Log($"[Player] GetLaunched 被调用 - 施加力: {force}, 当前击飞状态: {isHitFlying}, 待处理: {hasPendingLaunch}");
        
        // 取消之前的击飞计时器（如果有）
        if (hitFlyTween != null)
        {
            hitFlyTween.Kill();
            hitFlyTween = null;
            // Debug.Log($"[Player] 取消了之前的击飞计时器");
        }
        
        // 立即进入被击飞状态
        isHitFlying = true;
        
        // 记录击飞开始时间
        hitFlyStartTime = Time.time;
        
        // 保存要施加的力，等下一个 FixedUpdate 再施加
        pendingLaunchForce = force;
        hasPendingLaunch = true;
        
        // Debug.Log($"[Player] 设置击飞状态完成 - isHitFlying: {isHitFlying}, hasPendingLaunch: {hasPendingLaunch}, force: {pendingLaunchForce}, startTime: {hitFlyStartTime}");
        
        // 触发击飞动画并设置 Bool 值
        AnimateSetTrigger("HitFlying");  // Trigger 用于触发动画
        AnimateSetBool("isHitFlying", true);  // Bool 用于保持状态
        
        // 倒计时结束后重置 Bool 值
        hitFlyTween = DOVirtual.DelayedCall(hitFlyDuration, () =>
        {
            // Debug.Log($"[Player] 击飞时间到，当前速度: {rb.velocity}");
            EndHitFlying();
        });
    }
    
    /// <summary>
    /// 结束击飞状态
    /// </summary>
    private void EndHitFlying()
    {
        if (!isHitFlying && !isInHitStop)
        {
            // Debug.Log($"[Player] EndHitFlying 被调用，但已经不在击飞或停顿状态");
            return; // 已经结束了，不重复处理
        }
        
        // Debug.Log($"[Player] EndHitFlying - 结束击飞状态");
        
        // 取消所有相关计时器
        if (hitFlyTween != null)
        {
            hitFlyTween.Kill();
            hitFlyTween = null;
        }
        
        if (hitStopTween != null)
        {
            hitStopTween.Kill();
            hitStopTween = null;
        }
        
        // 如果还在停顿状态，恢复重力和动画速度
        if (isInHitStop)
        {
            rb.gravityScale = originGvS;
            if (animator != null)
            {
                animator.speed = 1;
            }
        }
        
        // 记录击飞结束时的横向速度，用于平滑过渡
        float currentXSpeed = rb.velocity.x;
        float maxAirSpeed = Mathf.Abs(AirMoveSpeed);
        
        // 只有当前速度超过 InAir 最大速度时才需要过渡
        if (Mathf.Abs(currentXSpeed) > maxAirSpeed)
        {
            isTransitioningFromHitFly = true;
            hitFlyEndSpeed = currentXSpeed;
            transitionStartTime = Time.time;
            // Debug.Log($"[Player] 开始速度过渡 - 当前速度: {currentXSpeed}, 最大空中速度: {maxAirSpeed}");
        }
        else
        {
            isTransitioningFromHitFly = false;
        }
        
        // 重置所有相关标记
        isHitFlying = false;
        isInHitStop = false;
        hasPendingLaunch = false;
        
        // 重置击飞动画 Bool 值
        AnimateSetBool("isHitFlying", false);
    }
    
    /// <summary>
    /// 被地刺或其他机关击飞（带停顿效果）
    /// </summary>
    /// <param name="force">弹射力</param>
    /// <param name="hitFlyDuration">被击飞持续时间</param>
    /// <param name="hitStopDuration">停顿时间</param>
    public void GetLaunchedWithHitStop(Vector2 force, float hitFlyDuration, float hitStopDuration)
    {
        // Debug.Log($"[Player] GetLaunchedWithHitStop 被调用 - 力: {force}, 停顿时间: {hitStopDuration}, 击飞时间: {hitFlyDuration}");
        
        // 取消之前的所有相关计时器
        if (hitFlyTween != null)
        {
            hitFlyTween.Kill();
            hitFlyTween = null;
        }
        
        if (hitStopTween != null)
        {
            hitStopTween.Kill();
            hitStopTween = null;
        }
        
        // 保存要施加的力
        pendingLaunchForce = force;
        
        // 进入停顿状态
        isInHitStop = true;
        isHitFlying = false; // 先不进入击飞状态
        
        // 取消重力（使用初始保存的 originGvS 而不是当前的 gravityScale，避免连续击飞时保存错误值）
        rb.gravityScale = 0;
        
        // 触发击飞动画（虽然还没真正击飞，但动画先播放）
        AnimateSetTrigger("HitFlying");
        AnimateSetBool("isHitFlying", true);
        
        // 暂停动画，停在第一帧
        if (animator != null)
        {
            animator.speed = 0;
        }
        
        // Debug.Log($"[Player] 进入停顿状态 - isInHitStop: {isInHitStop}, 当前速度: {rb.velocity}, 动画暂停");
        
        // 停顿结束后进入真正的击飞状态
        hitStopTween = DOVirtual.DelayedCall(hitStopDuration, () =>
        {
            // Debug.Log($"[Player] 停顿结束，开始击飞");
            
            // 结束停顿状态
            isInHitStop = false;
            
            // 恢复重力（使用初始保存的 originGvS）
            rb.gravityScale = originGvS;
            
            // 恢复动画播放
            if (animator != null)
            {
                animator.speed = 1;
            }
            
            // 进入击飞状态
            isHitFlying = true;
            hitFlyStartTime = Time.time;
            hasPendingLaunch = true;
            
            // 击飞持续时间结束后
            hitFlyTween = DOVirtual.DelayedCall(hitFlyDuration, () =>
            {
                // Debug.Log($"[Player] 击飞时间到，当前速度: {rb.velocity}");
                EndHitFlying();
            });
        });
    }
}

