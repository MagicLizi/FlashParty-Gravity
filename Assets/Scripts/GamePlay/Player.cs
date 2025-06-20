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

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    public string Name = "SnowWhite";

    public string CurAnimName = "Idle";

    public LayerMask groundMask;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        PlayAnimByName("Idle");
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
        // 添加平台事件监听
        EventManager.Instance.AddListener(EventType.PlatformPlayerOn, OnPlatformEnter);
        EventManager.Instance.AddListener(EventType.PlatformPlayerOff, OnPlatformExit);
    }

    void Update()
    {
        CheckInAir();
        CheckAnimPlay();
    }

    void FixedUpdate()
    {
        float targetSpeed;
        if (inAir)
        {
            targetSpeed = Mathf.Lerp(rb.velocity.x, CurXMoveSpeed, AirDrag);
            if(targetSpeed * (int)CurFaceDir < 0 || Mathf.Abs(rb.velocity.x) < Mathf.Abs(CurXMoveSpeed))
            {
                Debug.Log("空中转向");
                targetSpeed = CurXMoveSpeed;
            }
            // Debug.Log($"No Speed AirDrag: {rb.velocity.x} {CurXMoveSpeed} {targetSpeed}");
        }
        else
        {
            targetSpeed = CurXMoveSpeed;
        }
        rb.velocity = new Vector2(targetSpeed, rb.velocity.y);
    }

    protected void PlayAnimByName(string name)
    {
        CurAnimName = string.Format("{0}@{1}", Name, name);
    }

    void CheckAnimPlay()
    {
        string currentAnimName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        // Debug.Log($"当前播放的动画: {currentAnimName}");
        if (!currentAnimName.Equals(CurAnimName))
        {
            // Debug.Log($"当前播放的动画: {currentAnimName} 需要切换到 {CurAnimName}");
            animator.Play(CurAnimName);
        }
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
        Vector2 moveDir = (Vector2)data;
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
                PlayAnimByName("Dash");
                CurXMoveSpeed = moveDirX * MoveSpeed;
                animator.speed = Mathf.Abs(CurXMoveSpeed / BaseMoveSpeed);
            }
            else
            {
                PlayAnimByName("Idle");
                animator.speed = 1;
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        CheckFaceDir();
    }

    void OnJump(object data)
    {
        rb.velocity = new Vector2(rb.velocity.x, JumpSpeed);
    }

    void CheckInAir()
    {
        inAir = !IsGrounded();
        // Debug.Log($"inAir: {inAir} {CurAnimName}");
        if (inAir)
        {
            PlayAnimByName("JumpStart");
        }
        else
        {
            if (CurAnimName.Contains("JumpStart"))
            {
                PlayAnimByName("Idle");
            }
        }
    }

    bool IsGrounded()
    {
        // groundRayCast = (Vector2)transform.position + new Vector2(0, -boxCollider.bounds.size.y * 0.5f + 0.02f);
        return Physics2D.Raycast((Vector2)transform.position, Vector2.down, 0.05f, groundMask);
    }

    void OnDrawGizmos()
    {
        Vector2 origin = (Vector2)transform.position;
        Debug.DrawLine(origin, origin + Vector2.down * 0.05f, Color.red);
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
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOn, OnPlatformEnter);
            EventManager.Instance.RemoveListener(EventType.PlatformPlayerOff, OnPlatformExit);
        }
    }
}
