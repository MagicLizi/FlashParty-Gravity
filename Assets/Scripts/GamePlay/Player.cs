using UnityEngine;

public class Player : Unit
{

    public float JumpForce = 20;
    public override string GetName()
    {
        return "SnowWhite";
    }

    public override void AddEvents()
    {
        base.AddEvents();
        EventManager.Instance.AddListener(EventType.Jump, OnJump);
    }

    void OnJump(object data)
    {
        PlayAnimByName("Jump");
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
    }
}
