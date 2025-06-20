using System.Collections;
using System.Collections.Generic;
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

    public float BaseMoveSpeed = 4.5f; //基准值

    public FaceDir CurFaceDir = FaceDir.Right;

    void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        animator = GetComponent<Animator>();
        PlayAnimByName("Idle");
        AddEvents();
    }

    void Update()
    {
        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
        CheckIdle();
        CheckFaceDir();
        CheckMove();
    }

    public virtual string GetName()
    {
        return "Unit";
    }

    protected void PlayAnimByName(string name)
    {
        string animName = string.Format("{0}@{1}", GetName(), name);
        animator.Play(animName);
    }

    public virtual void AddEvents()
    {
        EventManager.Instance.AddListener(EventType.Move, OnMove);
    }

    void CheckIdle()
    {
        if (CurMoveSpeed == 0)
        {
            animator.speed = 1;
            PlayAnimByName("Idle");
        }
    }

    void CheckMove()
    {
        if (CurMoveSpeed != 0)
        {
            PlayAnimByName("Run");
            animator.speed = Mathf.Abs(CurMoveSpeed/ BaseMoveSpeed);
            transform.Translate(Vector3.right * CurMoveSpeed * Time.deltaTime);
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
        CurMoveSpeed = moveDir.x * MoveSpeed;
        Debug.Log($"OnMove: {moveDir} {CurMoveSpeed}");
    }
}
