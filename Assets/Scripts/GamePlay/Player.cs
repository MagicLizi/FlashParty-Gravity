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

    public bool inAir = false;

    public bool inAirTouchWall = false;

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    public string Name = "SnowWhite";

    public string CurAnimName = "Idle";

    public LayerMask groundMask;

    private bool isDead = false;

    private float rayLength = 0.2f;

    public bool StopSpeed = false;

    private float originGvS = 1;

    private bool isLoseGravity = false;

    public bool isInAtk = false;

    public bool isInAirAtk = false;

    public GameObject AtkCollider;

    public GameObject AirAtkCollider;

    SpriteRenderer spriteRenderer;

    public int CurJumpCnt = 1;

    public Wind isInWind = null;

    public Vector2 windSpeed = Vector2.zero;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
        EventManager.Instance.AddListener(EventType.Action, OnAction);
        EventManager.Instance.AddListener(EventType.Special, OnSpecial);
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
        isInAirAtk = currentState.IsName("SnowWhite@AttackAir");
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
        // Debug.Log($"rb.velocity y: {rb.velocity.y}");
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
        }
        AnimateSetBool("CanMove", targetSpeed != 0);
        Vector2 velocity = new Vector2(targetSpeed, rb.velocity.y);
        if (isLoseGravity)
        {
            velocity = new Vector2(0, 0);
        }
        if (isInAtk)
        {
            velocity = new Vector2(0, rb.velocity.y);
        }
        rb.velocity = velocity + windSpeed;
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


    private bool startJump = false;
    void OnJump(object data)
    {
        if (CurJumpCnt > 0 && !isInAtk)
        {
            rb.velocity = new Vector2(rb.velocity.x, JumpSpeed);
            AnimateSetTrigger("Jump");
            startJump = true;
        }
    }

    void CheckInAir()
    {
        inAir = !IsGrounded();
        AnimateSetBool("inAir", inAir);
        // Debug.Log($"inAir: {inAir} {CurAnimName}");
        if (inAir)
        {
            inAirTouchWall = IsTouchWall();
            if (startJump)
            {
                startJump = false;
                CurJumpCnt--;
            }
        }
        else
        {
            CurJumpCnt = 1;
        }
    }

    bool IsGrounded()
    {
        // groundRayCast = (Vector2)transform.position + new Vector2(0, -boxCollider.bounds.size.y * 0.5f + 0.02f);
        Vector2 middle = (Vector2)transform.position;
        Vector2 left = middle + new Vector2(-boxCollider.bounds.size.x / 2, 0);
        Vector2 right = middle + new Vector2(boxCollider.bounds.size.x / 2, 0);
        return Physics2D.Raycast(middle, Vector2.down, rayLength, groundMask) ||
                 Physics2D.Raycast(left, Vector2.down, rayLength, groundMask) ||
                Physics2D.Raycast(right, Vector2.down, rayLength, groundMask);
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
        if (boxCollider != null)
        {
            Vector2 middle = (Vector2)transform.position;
            Vector2 left = middle + new Vector2(-boxCollider.bounds.size.x / 2, 0);
            Vector2 right = middle + new Vector2(boxCollider.bounds.size.x / 2, 0);
            Debug.DrawLine(middle, middle + Vector2.down * rayLength, Color.red);
            Debug.DrawLine(left, left + Vector2.down * rayLength, Color.red);
            Debug.DrawLine(right, right + Vector2.down * rayLength, Color.red);

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

    void OnSpecial(object data)
    {

    }

    void OnDestroy()
    {
        // 清理事件监听，避免内存泄漏
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(EventType.Move, OnMove);
            EventManager.Instance.RemoveListener(EventType.Jump, OnJump);
            EventManager.Instance.RemoveListener(EventType.Action, OnAction);
            EventManager.Instance.RemoveListener(EventType.Special, OnSpecial);
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

    public void Dead(Vector2 rebornPos)
    {
        if (isDead) return;
        isDead = true;
        InputManager.Instance.Enable(false);
        // 可以在此处添加更多死亡逻辑，例如禁用输入、延迟后重新加载场景等
        Debug.Log("Player has died.");
        // this.enabled = false; // 禁用Player脚本，停止Update和FixedUpdate
        // 使用DOTween创建闪烁效果
        if (spriteRenderer != null)
        {
            // 创建闪烁动画，透明度在0.3和1之间变化，持续0.2秒，重复-1次（无限循环）
            Shine(true);
            // 使用DOTween移动角色到指定位置
            transform.DOMove(rebornPos, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                Shine(false);
                isDead = false;
                InputManager.Instance.Enable(true);
            });
        }
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
        AnimateSetTrigger("Attack");
    }

    public void AnimateSetBool(string triggerName, bool trigger)
    {
        animator.SetBool(triggerName, trigger);
    }

    public void AnimateSetTrigger(string triggerName)
    {
        animator.SetTrigger(triggerName);
    }
}
