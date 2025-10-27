using UnityEngine;

/// <summary>
/// 区域钥匙
/// 可以被放入AreaReceiver来触发机关
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AreaKey : MonoBehaviour
{
    [Header("钥匙配置")]
    [Tooltip("钥匙的Collider（用于判断范围）")]
    public Collider2D keyCollider;
    
    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;
    
    private Rigidbody2D rb;
    
    void Awake()
    {
        // 如果没有指定Collider，自动获取
        if (keyCollider == null)
        {
            keyCollider = GetComponent<Collider2D>();
            if (keyCollider == null)
            {
                Debug.LogError($"[AreaKey] {gameObject.name} 没有Collider2D！", this);
            }
        }
        
        // 获取或添加Rigidbody2D（Trigger碰撞检测必须）
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // 设置为Kinematic，防止受重力影响
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        if (showDebugInfo)
        {
            Debug.Log($"[AreaKey] {gameObject.name} 初始化完成，Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }
    
    /// <summary>
    /// 获取钥匙的世界范围
    /// </summary>
    public Bounds GetKeyBounds()
    {
        if (keyCollider != null)
        {
            return keyCollider.bounds;
        }
        return new Bounds(transform.position, Vector3.one);
    }
    
    void OnDrawGizmosSelected()
    {
        if (keyCollider != null)
        {
            Gizmos.color = Color.cyan;
            Bounds bounds = keyCollider.bounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                bounds.center + Vector3.up * (bounds.extents.y + 0.3f), 
                "区域钥匙"
            );
            #endif
        }
    }
}

