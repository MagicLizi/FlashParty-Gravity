using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using DG.Tweening;
using TapSDK.Core;
using TapSDK.Login;

public class GameStart : MonoBehaviour
{
    [Header("UI References")]
    public Text GameStartText; // 游戏开始提示文本
    public Text statusText; // 状态显示文本（用于显示校验状态）
    
    [Header("Audio")]
    public AudioSource audioSource; // 音效播放器
    public AudioClip startGameClip; // 开始游戏音效
    
    [Header("Settings")]
    public float blinkDuration = 2f; // 闪烁持续时间
    public float blinkInterval = 0.2f; // 闪烁间隔
    
    [Header("TapTap SDK Configuration")]
    public bool enableLog = false; // 是否开启日志
    
    // TapTap SDK 配置信息 (直接在代码中配置)
    private const string CLIENT_ID = "d4vsqlu1rezbhczem8";
    private const string CLIENT_TOKEN = "TFXRfs2dbrwjYpf8Cu0MjrpTcthIHlZtHluxnrtf";
    private const string CLIENT_PUBLIC_KEY = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsmK1AyG4T+BKN7V7UtQjA0Eyh+v2Rl1lPwj+p7o3pntYl3RX9jtZ7s5pEn+RuvF4hroRUzgxHjsfQbf1ENOzNeub9Jhdp6IPIm+uJHhqyLVOr0k0XtNwIX/5blSt+PQLY4zErbdhFokHLSgic9idiVjq2ddD3FIUo0YBQ3g0t4lDzPo+/h/D3+HRUCsdK6aZ9LAFJsYALhI6jkmgZyxlM9wAW2kos2ASaesn0BRwm/VzrgsAtANWFK/WsmmblTPAJvHmtWvph/B89dRv+uaoAGtHL74AHzX7+uGANQDFWSG8BrcsMjF2d5YI4vHfUoomk2iSOxGndFSVErV0xKoFSQIDAQAB";
    
    private System.IDisposable anyKeySubscription;
    private bool isTransitioning = false; // 防止重复触发
    private bool isTapTapVerified = false; // TapTap校验是否通过
    private bool isUserLoggedIn = false; // 用户是否已登录
    
    // Start is called before the first frame update
    void Start()
    {
        // 初始化UI
        InitializeUI();
        
        // 先进行TapTap SDK初始化和校验
        StartCoroutine(InitializeTapTapSDK());
    }
    
    void InitializeUI()
    {
        // 检查是否配置了游戏开始提示文本组件
        if (GameStartText == null)
        {
            Debug.LogWarning("未配置游戏开始提示文本组件，请在Inspector中设置GameStartText");
        }
        else
        {
            Debug.Log($"游戏开始提示文本组件已配置: {GameStartText.name}");
            GameStartText.text = "按任意键开始游戏";
        }
        
        // 检查状态文本组件
        if (statusText == null)
        {
            Debug.LogWarning("未配置状态文本组件，请在Inspector中设置statusText");
        }
        else
        {
            statusText.text = "正在连接TapTap...";
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

    private IEnumerator InitializeTapTapSDK()
    {
        // 更新状态文本
        if (statusText != null)
            statusText.text = "正在初始化TapTap SDK...";
        
        // 初始化TapTap SDK
        bool initSuccess = InitializeTapTapSDKCore();
        
        if (!initSuccess)
        {
            if (statusText != null)
                statusText.text = "SDK初始化失败，请重启游戏";
            yield break;
        }
        
        // 等待一帧确保初始化完成
        yield return null;
        
        // 更新状态文本
        if (statusText != null)
            statusText.text = "正在进行启动校验...";
        
        // 进行启动校验
        yield return StartCoroutine(PerformLaunchVerification());
    }
    
    private bool InitializeTapTapSDKCore()
    {
        try
        {
            // 初始化TapTap SDK
            TapTapSdkOptions coreOptions = new TapTapSdkOptions
            {
                clientId = CLIENT_ID,
                clientToken = CLIENT_TOKEN,
                region = TapTapRegionType.CN,
                enableLog = enableLog,
                clientPublicKey = CLIENT_PUBLIC_KEY
            };
            
            TapTapSDK.Init(coreOptions);
            Debug.Log("TapTap SDK 初始化完成");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TapTap SDK 初始化失败: {e.Message}");
            return false;
        }
    }
    
    private IEnumerator CheckAndPerformLogin()
    {
        // 更新状态文本
        if (statusText != null)
            statusText.text = "正在检查登录状态...";
        
        bool loginCompleted = false;
        bool loginResult = false;
        
        // 创建异步任务来检查和处理登录
        var loginTask = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                // 先检查用户是否已经登录
                TapTapAccount currentAccount = await TapTapLogin.Instance.GetCurrentTapAccount();
                
                if (currentAccount != null && !string.IsNullOrEmpty(currentAccount.openId))
                {
                    // 用户已登录
                    Debug.Log($"用户已登录，用户ID：{currentAccount.unionId}");
                    loginResult = true;
                    loginCompleted = true;
                    return true;
                }
                else
                {
                    // 用户未登录，进行登录
                    Debug.Log("用户未登录，开始登录流程");
                    
                    // 使用basic_info权限进行无感登录
                    var scopes = new List<string>
                    {
                        TapTapLogin.TAP_LOGIN_SCOPE_BASIC_INFO
                    };
                    
                    var userInfo = await TapTapLogin.Instance.LoginWithScopes(scopes.ToArray());
                    Debug.Log($"登录成功，当前用户ID：{userInfo.unionId}");
                    
                    loginResult = true;
                    loginCompleted = true;
                    return true;
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                Debug.Log("用户取消登录");
                loginResult = false;
                loginCompleted = true;
                return false;
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"登录失败，出现异常：{exception}");
                loginResult = false;
                loginCompleted = true;
                return false;
            }
        });
        
        // 等待登录完成
        while (!loginCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 处理登录结果
        if (loginResult)
        {
            Debug.Log("TapTap 登录成功");
            isUserLoggedIn = true;
            
            // 登录成功，隐藏状态文本，显示游戏开始提示
            if (statusText != null)
            {
                statusText.text = "登录成功";
                statusText.gameObject.SetActive(false);
            }
            
            if (GameStartText != null)
            {
                GameStartText.gameObject.SetActive(true);
            }
            
            // 订阅任意按键事件
            anyKeySubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }
        else
        {
            Debug.Log("TapTap 登录失败");
            isUserLoggedIn = false;
            
            // 登录失败，隐藏游戏开始提示，显示错误状态
            if (GameStartText != null)
            {
                GameStartText.gameObject.SetActive(false);
            }
            
            if (statusText != null)
            {
                statusText.text = "登录失败";
                statusText.gameObject.SetActive(true);
            }
            
            // 可以选择重试或者退出
            // 这里暂时不订阅按键事件，阻止继续游戏
        }
    }
    
    private IEnumerator PerformLaunchVerification()
    {
        bool verificationCompleted = false;
        bool verificationResult = false;
        
        // 创建异步任务来进行校验
        var verificationTask = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                bool result = await TapTapSDK.IsLaunchedFromTapTapPC();
                verificationResult = result;
                verificationCompleted = true;
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TapTap 启动校验失败: {e.Message}");
                verificationCompleted = true;
                return false;
            }
        });
        
        // 等待校验完成
        while (!verificationCompleted)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 处理校验结果
        if (verificationResult)
        {
            Debug.Log("TapTap PC 端校验通过");
            isTapTapVerified = true;
            
            // 校验通过后，检查用户登录状态
            yield return StartCoroutine(CheckAndPerformLogin());
        }
        else
        {
            Debug.Log("TapTap PC 端校验未通过");
            isTapTapVerified = false;
            
            // 校验未通过，隐藏游戏开始提示，显示错误状态
            if (GameStartText != null)
            {
                GameStartText.gameObject.SetActive(false);
            }
            
            if (statusText != null)
            {
                statusText.text = "校验未通过，请重新启动";
                statusText.gameObject.SetActive(true);
            }
            
            // 不订阅按键事件，阻止继续游戏
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnAnyButtonPressed(InputControl control)
    {
        // 只有在TapTap校验通过后才允许继续
        if (!isTapTapVerified)
        {
            Debug.LogWarning("TapTap校验未通过，无法继续游戏");
            return;
        }
        
        // 只有在用户已登录后才允许继续
        if (!isUserLoggedIn)
        {
            Debug.LogWarning("用户未登录，无法继续游戏");
            return;
        }
        
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
        if (GameStartText != null)
        {
            // 使用DOTween实现文本闪烁效果
            GameStartText.DOFade(0f, blinkInterval)
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
    
    /// <summary>
    /// 获取当前登录的TapTap用户信息
    /// </summary>
    /// <returns>用户信息，如果未登录返回null</returns>
    public async System.Threading.Tasks.Task<TapTapAccount> GetCurrentUserInfo()
    {
        try
        {
            if (isUserLoggedIn)
            {
                return await TapTapLogin.Instance.GetCurrentTapAccount();
            }
            else
            {
                Debug.LogWarning("用户未登录，无法获取用户信息");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取用户信息失败：{e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 用户登出
    /// </summary>
    public void LogoutUser()
    {
        TapTapLogin.Instance.Logout();
        isUserLoggedIn = false;
        Debug.Log("用户已登出");
    }
    
    /// <summary>
    /// 检查用户是否已登录
    /// </summary>
    /// <returns>是否已登录</returns>
    public bool IsUserLoggedIn()
    {
        return isUserLoggedIn;
    }
    
    private void OnDestroy()
    {
        // 清理订阅
        anyKeySubscription?.Dispose();
        
        // 停止DOTween动画
        DOTween.Kill("TextBlink");
    }
}
