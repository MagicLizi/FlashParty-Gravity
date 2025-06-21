using UnityEngine;
public class Ball : MonoBehaviour
{
    [Tooltip("定义哪些层级是场景/地面，当球碰到时会销毁")]
    public LayerMask sceneLayer;

    public Vector2 windSpeed = Vector2.zero;

    public Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        rb.velocity = rb.velocity + windSpeed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("Ball OnTriggerEnter2D");
        // 检查碰撞到的对象的层级是否在sceneLayer中
        // 使用位运算来检查: (1 << layer) & layerMask
        if (other.gameObject.layer == LayerMask.NameToLayer("Scene"))
        {
            Destroy(gameObject);
        }
        else if(other.gameObject.layer == LayerMask.NameToLayer("Mechanism "))
        {
            Wind wind = other.GetComponent<Wind>();
            if(wind == null)
            {
                Destroy(gameObject);
            }
        }
    }

    public void AddWindSpeed(Vector2 speed)
    {
        windSpeed = speed;
    }
}