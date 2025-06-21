using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Spike : MonoBehaviour
{
    private void Awake()
    {
        // 确保碰撞体是触发器，这样它就不会在物理上阻挡玩家
        var boxCollider = GetComponent<BoxCollider2D>();
        if (!boxCollider.isTrigger)
        {
            boxCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.Dead();
        }
    }
} 