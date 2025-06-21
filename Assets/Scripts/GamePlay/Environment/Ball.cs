using UnityEngine;
public class Ball : MonoBehaviour
{
    [Tooltip("定义哪些层级是场景/地面，当球碰到时会销毁")]
    public LayerMask sceneLayer;

    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log("Ball OnTriggerEnter2D");
        // 检查碰撞到的对象的层级是否在sceneLayer中
        // 使用位运算来检查: (1 << layer) & layerMask
        if ((sceneLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            // 如果是，则销毁自身
            Destroy(gameObject);
        }
    }
} 