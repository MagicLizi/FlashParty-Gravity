using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using DG.Tweening;

public class GameStart : MonoBehaviour
{
    [Header("UI References")]
    public Text promptText; // 提示文本
    
    [Header("Audio")]
    public AudioSource audioSource; // 音效播放器
    public AudioClip startGameClip; // 开始游戏音效
    
    [Header("Settings")]
    public float blinkDuration = 2f; // 闪烁持续时间
    public float blinkInterval = 0.2f; // 闪烁间隔
    
    private System.IDisposable anyKeySubscription;
    private bool isTransitioning = false; // 防止重复触发
    
    // Start is called before the first frame update
    void Start()
    {
        // 初始化UI
        InitializeUI();
        
        // 订阅任意按键事件
        anyKeySubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
    }
    
    void InitializeUI()
    {
        // 检查是否配置了提示文本组件
        if (promptText == null)
        {
            Debug.LogWarning("未配置提示文本组件，请在Inspector中设置promptText");
        }
        else
        {
            Debug.Log($"提示文本组件已配置: {promptText.name}");
        }
        
        // 检查是否配置了音效播放器
        if (audioSource == null)
        {
            Debug.LogWarning("未配置音效播放器，请在Inspector中设置audioSource");
        }
        else
        {
            Debug.Log($"音效播放器已配置: {audioSource.name}");
        }
        
        // 检查是否配置了音效文件
        if (startGameClip == null)
        {
            Debug.LogWarning("未配置开始游戏音效，请在Inspector中设置startGameClip");
        }
        else
        {
            Debug.Log($"开始游戏音效已配置: {startGameClip.name}");
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnAnyButtonPressed(InputControl control)
    {
        // 防止重复触发
        if (isTransitioning)
            return;
            
        isTransitioning = true;
        
        // 触发后立即取消订阅，避免重复触发
        anyKeySubscription?.Dispose();

        Debug.Log($"Any key pressed: {control.displayName}. Starting transition effect.");
        
        // 播放音效
        PlayStartGameSound();
        
        // 开始文本闪烁效果
        StartTextBlinkEffect();
        
        // 延迟跳转场景
        StartCoroutine(LoadGameMainAfterDelay());
    }
    
    private void PlayStartGameSound()
    {
        if (audioSource != null && startGameClip != null)
        {
            audioSource.PlayOneShot(startGameClip);
        }
    }
    
    private void StartTextBlinkEffect()
    {
        if (promptText != null)
        {
            // 使用DOTween实现文本闪烁效果
            promptText.DOFade(0f, blinkInterval)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId("TextBlink");
        }
    }
    
    private IEnumerator LoadGameMainAfterDelay()
    {
        yield return new WaitForSeconds(blinkDuration);
        
        // 停止文本闪烁
        DOTween.Kill("TextBlink");
        
        // 跳转到GameMain场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameMain");
    }
    
    private void OnDestroy()
    {
        // 清理订阅
        anyKeySubscription?.Dispose();
        
        // 停止DOTween动画
        DOTween.Kill("TextBlink");
    }
}
