using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Wind : MonoBehaviour
{
    [Tooltip("风的方向")]
    public Vector2 windDirection = Vector2.right;

    [Tooltip("风力大小")]
    public float windSpeed = 5f;

    private BoxCollider2D windZone;

    private void Awake()
    {
        windZone = GetComponent<BoxCollider2D>();
        // 确保碰撞体是触发器
        if (!windZone.isTrigger)
        {
            windZone.isTrigger = true;
            Debug.LogWarning("Wind component's BoxCollider2D was not set to trigger. It has been set automatically.");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 检查进入触发器的是否是Player
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.AddSpeed(windDirection.normalized * windSpeed);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.AddSpeed(Vector2.zero);
        }
    }

    private void OnDrawGizmos()
    {
        if (windZone == null)
        {
            windZone = GetComponent<BoxCollider2D>();
        }

        // 在Scene视图中绘制风向的箭头以方便调试
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + (Vector3)windZone.offset;
        Vector3 direction = windDirection.normalized;

        float arrowHeadLength = 0.25f;
        float arrowHeadAngle = 20.0f;

        // 绘制风向主干线
        Gizmos.DrawLine(center, center + (Vector3)direction * 2);

        // 绘制箭头
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(center + (Vector3)direction * 2, right * arrowHeadLength);
        Gizmos.DrawRay(center + (Vector3)direction * 2, left * arrowHeadLength);
    }
}