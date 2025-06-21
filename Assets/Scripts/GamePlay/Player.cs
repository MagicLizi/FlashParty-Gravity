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
    public Animator animator;

    public BoxCollider2D boxCollider;

    public float CurXMoveSpeed = 0;

    public float MoveSpeed = 0;

    public float AirMoveSpeed = 0;

    public float AirDrag = 0;

    public float JumpSpeed = 4;

    public float BaseMoveSpeed = 4.5f; //基准值

    private bool inAir = false;

    private bool inAirTouchWall = false;

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    public string Name = "SnowWhite";

    public string CurAnimName = "Idle";

    public LayerMask groundMask;

    private bool isDead = false;

    private Vector2 AdditionWindSpeed = Vector2.zero;

    private float rayLength = 0.1f;

    public bool StopSpeed = false;

    private float originGvS = 1;

    private bool isLoseGravity = false;

    private bool isInAtk = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
        EventManager.Instance.AddListener(EventType.Action, OnAction);
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

    private void CheckInAtk()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        isInAtk = currentState.IsName("SnowWhite@Attack");
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }
        float targetSpeed;
        if (inAir)
        {
            targetSpeed = Mathf.Lerp(rb.velocity.x, CurXMoveSpeed, AirDrag);
            if (targetSpeed * (int)CurFaceDir < 0 || Mathf.Abs(rb.velocity.x) < Mathf.Abs(CurXMoveSpeed))
            {
                // Debug.Log("空中转向");
                targetSpeed = CurXMoveSpeed;
            }
            // Debug.Log($"No Speed AirDrag: {rb.velocity.x} {CurXMoveSpeed} {targetSpeed}");
            if (inAirTouchWall && targetSpeed * (int)CurFaceDir > 0)
            {
                targetSpeed = 0;
            }
        }
        else
        {
            targetSpeed = CurXMoveSpeed;
        }
        AnimateSetTrigger("CanMove", targetSpeed != 0);
        Vector2 velocity = new Vector2(targetSpeed, rb.velocity.y) + AdditionWindSpeed;
        if (isLoseGravity)
        {
            velocity = new Vector2(velocity.x, 0);
        }
        if (isInAtk)
        {
            velocity = new Vector2(0, rb.velocity.y);
        }
        rb.velocity = velocity;
    }

    void CheckFaceDir()
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

    void OnMove(object data)
    {
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
        if (inAir)
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
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        CheckFaceDir();
    }

    bool startJump = false;
    void OnJump(object data)
    {
        rb.velocity = new Vector2(rb.velocity.x, JumpSpeed);
        AnimateSetTrigger("Jump");
    }

    void CheckInAir()
    {
        inAir = !IsGrounded();
        AnimateSetTrigger("inAir", inAir);
        // Debug.Log($"inAir: {inAir} {CurAnimName}");
        if (inAir)
        {
            inAirTouchWall = IsTouchWall();
            startJump = false;
        }
        else
        {

        }
    }

    bool IsGrounded()
    {
        // groundRayCast = (Vector2)transform.position + new Vector2(0, -boxCollider.bounds.size.y * 0.5f + 0.02f);
        return Physics2D.Raycast((Vector2)transform.position, Vector2.down, rayLength, groundMask);
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


    void OnDrawGizmos()
    {
        Vector2 origin = (Vector2)transform.position;
        Debug.DrawLine(origin, origin + Vector2.down * rayLength, Color.red);
        if (boxCollider != null)
        {
            Vector2 direction = Vector2.zero;
            if (CurFaceDir == FaceDir.Right)
            {
                direction = Vector2.right;
            }
            else
            {
                direction = Vector2.left;
            }

            Vector2 middleOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y / 2);
            Debug.DrawLine(middleOrigin, middleOrigin + direction * rayLength, Color.blue);

            Vector2 downOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, 0);
            Debug.DrawLine(downOrigin, downOrigin + direction * rayLength, Color.blue);

            Vector2 upOrigin = (Vector2)transform.position + new Vector2(((int)CurFaceDir) * boxCollider.bounds.size.x / 2, boxCollider.bounds.size.y);
            Debug.DrawLine(upOrigin, upOrigin + direction * rayLength, Color.blue);
        }
    }

    void OnPlatformEnter(object data)
    {
        if (data is Player player && player == this)
        {
            Debug.Log("Player entered moving platform");
            // 可以在这里添加进入平台的特殊逻辑，比如改变状态或播放音效
        }
    }

    void OnPlatformExit(object data)
    {
        if (data is Player player && player == this)
        {
            Debug.Log("Player left moving platform");
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
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOn, OnPlatformEnter);
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOff, OnPlatformExit);
        }
    }

    public void SetWindSpeed(Vector2 speedVec)
    {
        AdditionWindSpeed = AdditionWindSpeed + speedVec;
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

    public void Dead(Vector2 rebornPos)
    {
        if (isDead) return;
        isDead = true;
        InputManager.Instance.Enable(false);
        // 可以在此处添加更多死亡逻辑，例如禁用输入、延迟后重新加载场景等
        Debug.Log("Player has died.");
        // this.enabled = false; // 禁用Player脚本，停止Update和FixedUpdate
        // 使用DOTween创建闪烁效果
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 创建闪烁动画，透明度在0.3和1之间变化，持续0.2秒，重复-1次（无限循环）
            TweenerCore<Color, Color, ColorOptions> fadeTween = spriteRenderer.DOFade(0.3f, 0.1f).SetLoops(-1, LoopType.Yoyo);
            // 使用DOTween移动角色到指定位置
            transform.DOMove(rebornPos, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                spriteRenderer.DOFade(1, 0.1f);
                fadeTween.Kill();
                isDead = false;
                InputManager.Instance.Enable(true);
            });
        }
    }

    void OnAction(object data)
    {
        AnimateSetTrigger("Attack");
    }

    void AnimateSetTrigger(string triggerName, bool trigger)
    {
        animator.SetBool(triggerName, trigger);
    }

    void AnimateSetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }
}
