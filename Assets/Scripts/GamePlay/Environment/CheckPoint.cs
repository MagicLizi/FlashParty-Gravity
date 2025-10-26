using UnityEngine;
using DG.Tweening;

namespace FlashParty.Environment
{
    /// <summary>
    /// 检查点/重生点机关
    /// 玩家触碰后会将此处设置为重生点
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CheckPoint : MonoBehaviour
    {
        [Header("视觉效果")]
        [Tooltip("激活特效子对象（会升起并渐变透明）")]
        [SerializeField] private GameObject effectPrefab;
        
        [Tooltip("激活时播放的音效")]
        [SerializeField] private AudioClip activateSound;
        
        [Header("动画设置")]
        [Tooltip("特效升起的高度")]
        [SerializeField] private float riseHeight = 2f;
        
        [Tooltip("升起和渐变的持续时间")]
        [SerializeField] private float animationDuration = 1.5f;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = false;
        
        private AudioSource audioSource;
        
        void Awake()
        {
            // 确保碰撞器是触发器
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                Debug.LogWarning($"[CheckPoint] {gameObject.name} 需要 Collider2D 组件！", this);
            }
            
            // 获取或添加AudioSource（如果需要音效）
            if (activateSound != null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            // 检查是否是玩家
            Player player = other.GetComponent<Player>();
            if (player == null) return;
            
            // 检查是否已经是当前重生点，如果是就不触发
            if (Const.LastReborn == gameObject)
            {
                if (showDebugInfo)
                {
                    //Debug.Log($"[CheckPoint] {gameObject.name} 已经是当前重生点，不触发表现");
                }
                return;
            }
            
            // 激活检查点（更新为当前重生点）
            ActivateCheckPoint();
        }
        
        /// <summary>
        /// 激活检查点
        /// </summary>
        private void ActivateCheckPoint()
        {
            // 更新全局重生点（使用自身位置）
            Const.LastReborn = gameObject;
            
            // 播放激活动画
            PlayActivationEffect();
            
            // 播放音效
            if (activateSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activateSound);
            }
            
            if (showDebugInfo)
            {
                //Debug.Log($"[CheckPoint] 检查点已激活: {gameObject.name}");
            }
        }
        
        /// <summary>
        /// 播放激活特效（升起并渐变透明）
        /// </summary>
        private void PlayActivationEffect()
        {
            if (effectPrefab == null)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[CheckPoint] {gameObject.name} 未配置 effectPrefab");
                }
                return;
            }
            
            // 激活特效对象
            effectPrefab.SetActive(true);
            
            // 获取所有渲染器（支持多个子对象）
            SpriteRenderer[] spriteRenderers = effectPrefab.GetComponentsInChildren<SpriteRenderer>();
            
            // 记录初始位置和颜色
            Vector3 startPos = effectPrefab.transform.localPosition;
            Vector3 targetPos = startPos + Vector3.up * riseHeight;
            
            // 升起动画
            effectPrefab.transform.DOLocalMove(targetPos, animationDuration).SetEase(Ease.OutQuad);
            
            // 渐变透明动画
            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null)
                {
                    Color startColor = sr.color;
                    sr.DOFade(0f, animationDuration).SetEase(Ease.InQuad);
                }
            }
            
            // 动画结束后重置并隐藏
            DOVirtual.DelayedCall(animationDuration, () =>
            {
                // 重置位置和透明度
                effectPrefab.transform.localPosition = startPos;
                foreach (SpriteRenderer sr in spriteRenderers)
                {
                    if (sr != null)
                    {
                        Color color = sr.color;
                        color.a = 1f;
                        sr.color = color;
                    }
                }
                effectPrefab.SetActive(false);
            });
        }
        
        /// <summary>
        /// 手动激活检查点（可以通过其他机关触发）
        /// </summary>
        public void ManualActivate()
        {
            // 只要不是当前重生点就可以激活
            if (Const.LastReborn != gameObject)
            {
                ActivateCheckPoint();
            }
        }
        
        void OnDrawGizmos()
        {
            // 在场景视图中显示检查点
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // 绘制向上的标记
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
        }
        
        void OnDrawGizmosSelected()
        {
            // 选中时显示更详细的信息
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            
            // 显示升起高度预览
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * riseHeight);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * riseHeight, 0.3f);
        }
    }
}
