using UnityEngine;

public enum FaceDir
{
    Left = 1,
    Right = 2,
}
public class Unit : MonoBehaviour
{
    public Animator animator;

    public float CurMoveSpeed = 0;

    public float MoveSpeed = 0;

    public float AirMoveSpeed = 0;

    public float BaseMoveSpeed = 4.5f; //基准值

    private bool inAir = false;

    public Rigidbody2D rb;

    public FaceDir CurFaceDir = FaceDir.Right;

    void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        PlayAnimByName("Idle");
        AddEvents();
    }

    void Update()
    {
        OnUpdate();
    }

    void FixedUpdate()
    {
        OnFixedUpdate();
    }

    protected virtual void OnUpdate()
    {

    }

    protected virtual void OnFixedUpdate()
    {
        Debug.Log($"OnFixedUpdate: {CurMoveSpeed}");
        rb.velocity = new Vector2(CurMoveSpeed, rb.velocity.y);
        Debug.Log($"OnFixedUpdate: {rb.velocity} {rb.drag} ");
    }

    public virtual string GetName()
    {
        return "Unit";
    }

    protected void PlayAnimByName(string name)
    {
        string animName = string.Format("{0}@{1}", GetName(), name);
        // Debug.Log($"PlayAnimByName: {animName}");
        animator.Play(animName);
    }

    public virtual void AddEvents()
    {
        EventManager.Instance.AddListener(EventType.Move, OnMove);
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
    }

    void CheckIdle()
    {
        if (CurMoveSpeed == 0 && !inAir)
        {
            animator.speed = 1;
            PlayAnimByName("Idle");
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
        // rb.velocity = new Vector2(CurMoveSpeed, rb.velocity.y); // 冲刺保留Y轴速度
        // Debug.Log($"rb.velocity: {rb.velocity}");
    }

    void OnJump(object data)
    {
        // inAir = true;
        // PlayAnimByName("JumpStart");
    }
}
