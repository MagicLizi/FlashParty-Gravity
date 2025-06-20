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

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        PlayAnimByName("Idle");
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(CurMoveSpeed, rb.velocity.y);
    }

    void LateUpdate()
    {

    }

    protected void PlayAnimByName(string name)
    {
        string animName = string.Format("{0}@{1}", Name, name);
        animator.Play(animName);
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
        if (inAir)
        {
            CurMoveSpeed = moveDir.x * AirMoveSpeed;
        }
        else
        {
            CurMoveSpeed = moveDir.x * MoveSpeed;
            if (CurMoveSpeed != 0)
            {
                PlayAnimByName("Dash");
                CurMoveSpeed = moveDir.x * MoveSpeed;
                animator.speed = Mathf.Abs(CurMoveSpeed / BaseMoveSpeed);
            }
            else
            {
                PlayAnimByName("Idle");
                animator.speed = 1;
            }
        }
        CheckFaceDir();
    }

    void OnJump(object data)
    {
    }
}
