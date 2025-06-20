using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Wind : MonoBehaviour
{
    [Tooltip("风的方向")]
    public Vector2 windDirection = Vector2.right;

    [Tooltip("风力大小")]
    public float windSpeed = 5f;


    public float ballWindSpeed = 30f;

    private BoxCollider2D windZone;

    private bool isOpen = false;

    public Animator animator;

    private void Awake()
    {
        windZone = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        // 确保碰撞体是触发器
        if (!windZone.isTrigger)
        {
            windZone.isTrigger = true;
            Debug.LogWarning("Wind component's BoxCollider2D was not set to trigger. It has been set automatically.");
        }
    }

    public void Open(bool active)
    {
        Debug.Log("Wind Open: " + active);
        isOpen = active;
        animator.enabled = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen)
        {
            ApplyWind(other);
        }
    }

    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     if (isOpen)
    //     {
    //         AddPlayerWind(other, windSpeed, 2);
    //     }
    // }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isOpen)
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.AddWindSpeed(Vector2.zero);
            }

            Ball ball = other.GetComponent<Ball>();
            if(ball != null)
            {
                ball.AddWindSpeed(Vector2.zero);
            }
        }

    }

    // void AddPlayerWind(Collider2D other, float speed, int type)
    // {
    //     Player player = other.GetComponent<Player>();
    //     if (player != null)
    //     {
    //         if(type == 1 || type == 2)
    //         {
    //             player.isInWind = this;
    //         }
    //         else if(type == 3)
    //         {
    //             player.isInWind = null;
    //         }
    //         player.SetWindSpeed(windDirection.normalized * speed);
    //     }
    // }

    private void ApplyWind(Collider2D other)
    {
        // 将本地风向转换为世界坐标系下的方向
        Vector2 worldWindDirection = transform.TransformDirection(windDirection.normalized);

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.AddWindSpeed(worldWindDirection * windSpeed);
            return;
        }

        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            ball.AddWindSpeed(worldWindDirection * ballWindSpeed);
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
        
        // Gizmos需要世界坐标系方向
        Vector3 direction = transform.TransformDirection(windDirection.normalized);

        float arrowHeadLength = 0.25f;
        float arrowHeadAngle = 20.0f;

        // 绘制风向主干线
        Gizmos.DrawLine(center, center + direction * 2);

        // 绘制箭头
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
        Gizmos.DrawRay(center + direction * 2, right * arrowHeadLength);
        Gizmos.DrawRay(center + direction * 2, left * arrowHeadLength);
    }
}