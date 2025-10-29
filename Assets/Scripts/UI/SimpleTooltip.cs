using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 简单的提示UI控制器
/// 挂载到Canvas中的Text上
/// </summary>
[RequireComponent(typeof(Text))]
public class SimpleTooltip : MonoBehaviour
{
    [Header("显示设置")]
    [Tooltip("提示显示持续时间（秒）")]
    [SerializeField] private float displayDuration = 2f;
    
    [Tooltip("渐隐持续时间（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;
    
    private Text text;
    private CanvasGroup canvasGroup;
    private Tween currentTween;
    
    void Awake()
    {
        text = GetComponent<Text>();
        
        // 添加CanvasGroup用于控制透明度
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始隐藏（只用alpha，不禁用GameObject）
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false; // 不阻挡射线检测
        canvasGroup.interactable = false;
    }
    
    /// <summary>
    /// 显示提示
    /// </summary>
    /// <param name="message">提示内容</param>
    public void Show(string message)
    {
        // 确保组件已初始化
        if (text == null)
        {
            text = GetComponent<Text>();
            if (text == null)
            {
                Debug.LogError("[SimpleTooltip] 找不到Text组件！请确保脚本挂在有Text组件的对象上。", this);
                return;
            }
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // 取消之前的动画
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        
        // 设置文本
        text.text = message;
        
        // 显示
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false; // 提示不阻挡点击
        
        // 延迟后渐隐
        currentTween = DOVirtual.DelayedCall(displayDuration, () =>
        {
            FadeOut();
        });
    }
    
    /// <summary>
    /// 渐隐消失
    /// </summary>
    private void FadeOut()
    {
        currentTween = canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            canvasGroup.blocksRaycasts = false;
        });
    }
    
    /// <summary>
    /// 立即隐藏
    /// </summary>
    public void Hide()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    void OnDestroy()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }
}
