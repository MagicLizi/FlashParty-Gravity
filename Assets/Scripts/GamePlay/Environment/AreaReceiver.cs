using UnityEngine;
using FlashParty.Environment;
using System.Collections.Generic;

/// <summary>
/// 区域接收器机关
/// 当AreaKey完全放入接收区域内时，触发门打开
/// </summary>
public class AreaReceiver : MonoBehaviour
{
    [Header("接收区域配置")]
    [Tooltip("接收区域的BoxCollider2D（必须设为Trigger）")]
    public BoxCollider2D receiverArea;
    
    [Header("触发配置")]
    [Tooltip("要触发打开的门")]
    public Door door;
    
    [Header("调试显示")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;
    
    [Tooltip("在场景中显示接收区域")]
    public bool showGizmos = true;
    
    [Tooltip("接收区域颜色（未放入）")]
    public Color areaColorEmpty = new Color(1f, 0.5f, 0f, 0.3f);
    
    [Tooltip("接收区域颜色（已放入）")]
    public Color areaColorFilled = new Color(0f, 1f, 0f, 0.3f);
    
    private bool isActivated = false; // 是否已触发
    private HashSet<AreaKey> keysInContact = new HashSet<AreaKey>(); // 接触中的钥匙
    
    void Awake()
    {
        // 如果没有指定接收区域，尝试获取自己的BoxCollider2D
        if (receiverArea == null)
        {
            receiverArea = GetComponent<BoxCollider2D>();
            if (receiverArea == null)
            {
                Debug.LogError($"[AreaReceiver] {gameObject.name} 未配置接收区域，且自身没有BoxCollider2D！", this);
            }
        }
        
        // 确保接收区域是Trigger
        if (receiverArea != null)
        {
            receiverArea.isTrigger = true;
        }
        
        // 检查门是否配置
        if (door == null)
        {
            Debug.LogWarning($"[AreaReceiver] {gameObject.name} 未配置门，无法触发！", this);
        }
        
        //Debug.Log($"[AreaReceiver] {gameObject.name} 初始化完成，Layer: {LayerMask.LayerToName(gameObject.layer)}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log($"[AreaReceiver] OnTriggerEnter2D 被触发！碰撞对象: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        // 如果已激活，不再处理
        if (isActivated)
        {
            //Debug.Log($"[AreaReceiver] 已激活，忽略");
            return;
        }
        
        // 检查是否是AreaKey
        AreaKey key = other.GetComponent<AreaKey>();
        if (key != null)
        {
            keysInContact.Add(key);
            //Debug.Log($"[AreaReceiver] ✓ 钥匙 {key.gameObject.name} 进入接收区域，当前接触钥匙数: {keysInContact.Count}");
        }
        else
        {
            //Debug.Log($"[AreaReceiver] ✗ 对象 {other.gameObject.name} 没有AreaKey组件");
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // 如果已激活，不再处理
        if (isActivated)
        {
            return;
        }
        
        // 检查是否是AreaKey
        AreaKey key = other.GetComponent<AreaKey>();
        if (key != null && keysInContact.Contains(key))
        {
            // 检查钥匙是否完全在区域内
            CheckKeyFullyInside(key);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // 检查是否是AreaKey
        AreaKey key = other.GetComponent<AreaKey>();
        if (key != null)
        {
            keysInContact.Remove(key);
            //Debug.Log($"[AreaReceiver] 钥匙 {key.gameObject.name} 离开接收区域，当前接触钥匙数: {keysInContact.Count}");
        }
    }
    
    /// <summary>
    /// 检查钥匙是否完全在接收区域内
    /// </summary>
    private void CheckKeyFullyInside(AreaKey key)
    {
        if (receiverArea == null || key == null)
        {
            return;
        }
        
        // 获取接收区域的世界坐标范围
        Bounds receiverBounds = receiverArea.bounds;
        Rect receiverRect = new Rect(
            receiverBounds.min.x,
            receiverBounds.min.y,
            receiverBounds.size.x,
            receiverBounds.size.y
        );
        
        // 获取钥匙的世界坐标范围
        Bounds keyBounds = key.GetKeyBounds();
        
        // 判断钥匙是否完全在接收区域内
        bool isFullyInside = BoundsInsideRect2D(keyBounds, receiverRect);
        
        if (showDebugInfo)
        {
            //Debug.Log($"[AreaReceiver] 检查钥匙 {key.gameObject.name} 是否完全在区域内: {isFullyInside}");
            //Debug.Log($"  接收区域: {receiverRect}");
            //Debug.Log($"  钥匙范围: min({keyBounds.min.x:F2}, {keyBounds.min.y:F2}), max({keyBounds.max.x:F2}, {keyBounds.max.y:F2})");
        }
        
        if (isFullyInside)
        {
            // 触发机关
            ActivateMechanism(key);
        }
    }
    
    /// <summary>
    /// 判断Bounds是否完全在Rect内
    /// </summary>
    private bool BoundsInsideRect2D(Bounds bounds, Rect rect)
    {
        return bounds.min.x >= rect.xMin && 
               bounds.max.x <= rect.xMax && 
               bounds.min.y >= rect.yMin && 
               bounds.max.y <= rect.yMax;
    }
    
    /// <summary>
    /// 激活机关，打开门
    /// </summary>
    private void ActivateMechanism(AreaKey key)
    {
        if (isActivated)
        {
            return;
        }
        
        isActivated = true;
        
        //Debug.Log($"[AreaReceiver] ✓✓✓ {gameObject.name} 钥匙 {key.gameObject.name} 已完全放入区域，触发门打开！");
        
        // 打开门
        if (door != null)
        {
            door.Unlock();
        }
        else
        {
            Debug.LogWarning($"[AreaReceiver] {gameObject.name} 未配置门，无法触发！", this);
        }
    }
    
    /// <summary>
    /// 重置机关（用于关卡重置）
    /// </summary>
    public void ResetMechanism()
    {
        isActivated = false;
        keysInContact.Clear();
        
        if (showDebugInfo)
        {
            //Debug.Log($"[AreaReceiver] {gameObject.name} 已重置");
        }
    }
    
    /// <summary>
    /// 检查是否已激活
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }
    
    // Gizmos显示
    void OnDrawGizmos()
    {
        if (!showGizmos || receiverArea == null)
        {
            return;
        }
        
        // 绘制接收区域
        Bounds bounds = receiverArea.bounds;
        Gizmos.color = isActivated ? areaColorFilled : areaColorEmpty;
        Gizmos.DrawCube(bounds.center, bounds.size);
        
        // 绘制边框
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
    
    void OnDrawGizmosSelected()
    {
        if (receiverArea == null)
        {
            return;
        }
        
        // 选中时绘制更明显的边框
        Bounds bounds = receiverArea.bounds;
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size * 1.05f);
        
        #if UNITY_EDITOR
        // 显示状态文字
        string status = isActivated ? "已激活" : "未激活";
        string keyCount = $"接触钥匙数: {keysInContact.Count}";
        UnityEditor.Handles.Label(
            bounds.center + Vector3.up * (bounds.extents.y + 0.5f), 
            $"区域接收器\n状态: {status}\n{keyCount}"
        );
        
        // 绘制所有接触中的钥匙
        if (keysInContact.Count > 0)
        {
            foreach (AreaKey key in keysInContact)
            {
                if (key != null)
                {
                    Bounds keyBounds = key.GetKeyBounds();
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(bounds.center, keyBounds.center);
                    Gizmos.DrawWireCube(keyBounds.center, keyBounds.size);
                }
            }
        }
        #endif
    }
}

