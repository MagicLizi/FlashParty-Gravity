using UnityEngine;

public enum FaceDir
{
    Left = 1,
    Right = 2,
}

public class Player : MonoBehaviour
{
    public Animator animator;

    public float CurMoveSpeed = 0;

    public float MoveSpeed = 0;

    public float AirMoveSpeed = 0;

    public float BaseMoveSpeed = 4.5f; //基准值

    private bool inAir = false;

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    public string Name = "SnowWhite";

    public string CurAnimName = "Idle";

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        PlayAnimByName("Idle");
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
    }

    void Update()
    {
        CheckAnimPlay();
        rb.velocity = new Vector2(CurMoveSpeed, rb.velocity.y);
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
            Debug.Log($"当前播放的动画: {currentAnimName} 需要切换到 {CurAnimName}");
            animator.Play(CurAnimName);
        }
    }

    void CheckFaceDir()
    {
        if (CurMoveSpeed > 0)
        {
            CurFaceDir = FaceDir.Right;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (CurMoveSpeed < 0)
        {
            CurFaceDir = FaceDir.Left;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void OnMove(object data)
    {
        Vector2 moveDir = (Vector2)data;
        int moveDirX = moveDir.x < 0 ? -1 : 1;
        if (moveDir.x == 0)
        {
            moveDirX = 0;
        }
        if (inAir)
        {
            CurMoveSpeed = moveDirX * AirMoveSpeed;
        }
        else
        {
            CurMoveSpeed = moveDirX * MoveSpeed;
            // Debug.Log($"CurMoveSpeed: {CurMoveSpeed}");
            if (CurMoveSpeed != 0)
            {
                PlayAnimByName("Dash");
                CurMoveSpeed = moveDirX * MoveSpeed;
                animator.speed = Mathf.Abs(CurMoveSpeed / BaseMoveSpeed);
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

    }
}
