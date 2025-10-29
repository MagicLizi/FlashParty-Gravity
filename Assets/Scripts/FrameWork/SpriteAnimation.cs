using UnityEngine;

/// <summary>
/// 通用序列帧动画播放器
/// 适用于火把、旗帜等需要简单序列帧动画的场景物件
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimation : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("序列帧精灵数组（按播放顺序排列）")]
    public Sprite[] frames;
    
    [Tooltip("每秒播放的帧数（FPS）")]
    [Range(1f, 60f)]
    public float frameRate = 12f;
    
    [Tooltip("是否自动播放")]
    public bool playOnAwake = true;
    
    [Tooltip("是否循环播放")]
    public bool loop = true;
    
    [Header("可选设置")]
    [Tooltip("是否使用非缩放时间（不受Time.timeScale影响）")]
    public bool useUnscaledTime = false;
    
    private SpriteRenderer spriteRenderer;
    private int currentFrameIndex = 0;
    private float timer = 0f;
    private bool isPlaying = false;
    
    // 当前帧持续时间（秒）
    private float FrameDuration => 1f / frameRate;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"[SpriteAnimation] {gameObject.name} 没有 SpriteRenderer 组件！", this);
            enabled = false;
            return;
        }
        
        // 验证帧数组
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[SpriteAnimation] {gameObject.name} 没有配置动画帧！", this);
            enabled = false;
            return;
        }
    }
    
    void Start()
    {
        if (playOnAwake)
        {
            Play();
        }
    }
    
    void Update()
    {
        if (!isPlaying || frames == null || frames.Length == 0)
        {
            return;
        }
        
        // 累计时间
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        timer += deltaTime;
        
        // 检查是否需要切换到下一帧
        if (timer >= FrameDuration)
        {
            timer -= FrameDuration;
            NextFrame();
        }
    }
    
    /// <summary>
    /// 切换到下一帧
    /// </summary>
    private void NextFrame()
    {
        currentFrameIndex++;
        
        // 检查是否播放完毕
        if (currentFrameIndex >= frames.Length)
        {
            if (loop)
            {
                // 循环播放，回到第一帧
                currentFrameIndex = 0;
            }
            else
            {
                // 不循环，停在最后一帧
                currentFrameIndex = frames.Length - 1;
                isPlaying = false;
            }
        }
        
        // 更新显示的精灵
        UpdateSprite();
    }
    
    /// <summary>
    /// 更新精灵显示
    /// </summary>
    private void UpdateSprite()
    {
        if (currentFrameIndex >= 0 && currentFrameIndex < frames.Length && frames[currentFrameIndex] != null)
        {
            spriteRenderer.sprite = frames[currentFrameIndex];
        }
    }
    
    /// <summary>
    /// 开始播放动画
    /// </summary>
    public void Play()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[SpriteAnimation] {gameObject.name} 无法播放：没有配置动画帧", this);
            return;
        }
        
        isPlaying = true;
        currentFrameIndex = 0;
        timer = 0f;
        UpdateSprite();
    }
    
    /// <summary>
    /// 从指定帧开始播放
    /// </summary>
    /// <param name="frameIndex">起始帧索引</param>
    public void PlayFromFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[SpriteAnimation] {gameObject.name} 无法播放：没有配置动画帧", this);
            return;
        }
        
        if (frameIndex < 0 || frameIndex >= frames.Length)
        {
            Debug.LogWarning($"[SpriteAnimation] {gameObject.name} 帧索引 {frameIndex} 超出范围 [0, {frames.Length - 1}]", this);
            frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        }
        
        isPlaying = true;
        currentFrameIndex = frameIndex;
        timer = 0f;
        UpdateSprite();
    }
    
    /// <summary>
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }
    
    /// <summary>
    /// 恢复播放
    /// </summary>
    public void Resume()
    {
        if (frames != null && frames.Length > 0)
        {
            isPlaying = true;
        }
    }
    
    /// <summary>
    /// 停止播放（重置到第一帧）
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        currentFrameIndex = 0;
        timer = 0f;
        UpdateSprite();
    }
    
    /// <summary>
    /// 设置到指定帧（不播放）
    /// </summary>
    /// <param name="frameIndex">帧索引</param>
    public void SetFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }
        
        if (frameIndex < 0 || frameIndex >= frames.Length)
        {
            Debug.LogWarning($"[SpriteAnimation] {gameObject.name} 帧索引 {frameIndex} 超出范围 [0, {frames.Length - 1}]", this);
            return;
        }
        
        currentFrameIndex = frameIndex;
        UpdateSprite();
    }
    
    /// <summary>
    /// 设置播放速度（FPS）
    /// </summary>
    /// <param name="fps">每秒帧数</param>
    public void SetFrameRate(float fps)
    {
        frameRate = Mathf.Max(1f, fps);
    }
    
    /// <summary>
    /// 获取当前帧索引
    /// </summary>
    public int CurrentFrame => currentFrameIndex;
    
    /// <summary>
    /// 获取总帧数
    /// </summary>
    public int FrameCount => frames != null ? frames.Length : 0;
    
    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying;
}

