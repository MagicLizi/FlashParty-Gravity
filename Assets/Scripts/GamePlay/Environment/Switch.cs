using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Switch : MonoBehaviour
{
    [Tooltip("要控制开关状态的目标游戏对象")]
    public Wind targetObject;

    [Tooltip("开关'开'状态时显示的图片")]
    public Sprite spriteOn;

    [Tooltip("开关'关'状态时显示的图片")]
    public Sprite spriteOff;

    [Tooltip("开关的初始状态是否为'开'")]
    public bool startsOn = false;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // 获取SpriteRenderer组件
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 确保BoxCollider2D是触发器
        GetComponent<BoxCollider2D>().isTrigger = true;

        // 初始化状态
        UpdateVisuals(startsOn);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Atk atk = other.GetComponent<Atk>();
        if (atk != null)
        {
            startsOn = !startsOn;
            UpdateVisuals(startsOn);
        }
    }

    /// <summary>
    /// 根据传入的状态更新目标对象和开关的图片
    /// </summary>
    /// <param name="isOn">开关是否为'开'状态</param>
    private void UpdateVisuals(bool isOn)
    {
        // 更新目标对象的激活状态
        if (targetObject != null)
        {
            targetObject.Open(isOn);
        }

        // 更新开关自身的图片
        spriteRenderer.sprite = isOn ? spriteOn : spriteOff;
    }
}