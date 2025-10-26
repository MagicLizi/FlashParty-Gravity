using UnityEngine;

namespace FlashParty.Environment
{
    /// <summary>
    /// 一次性开关的门
    /// 可以被其他机关调用解锁
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("门状态")]
        [SerializeField] private bool unlock = false;
        
        [Header("音效")]
        [Tooltip("解锁时播放的音效")]
        [SerializeField] private AudioClip unlockSound;
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = false;
        
        private Animator doorAnimator;
        private AudioSource audioSource;
        
        public bool IsUnlocked => unlock;
        
        void Awake()
        {
            // 获取Animator
            doorAnimator = GetComponent<Animator>();
            
            // 获取或添加AudioSource（如果需要音效）
            if (unlockSound != null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }
        
        void Start()
        {
            // 初始化门的状态
            UpdateDoorState();
        }
        
        #if UNITY_EDITOR
        void OnValidate()
        {
            // 在编辑器中修改unlock值时，同步到Animator
            if (Application.isPlaying)
            {
                if (doorAnimator == null)
                {
                    doorAnimator = GetComponent<Animator>();
                }
                UpdateDoorState();
            }
        }
        #endif
        
        /// <summary>
        /// 解锁门（可以被其他机关调用）
        /// </summary>
        public void Unlock()
        {
            if (unlock)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[Door] {gameObject.name} 已经解锁过了");
                }
                return;
            }
            
            unlock = true;
            UpdateDoorState();
            
            // 播放音效
            if (unlockSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[Door] {gameObject.name} 已解锁");
            }
        }
        
        /// <summary>
        /// 更新门的状态
        /// </summary>
        private void UpdateDoorState()
        {
            // 更新Animator参数
            if (doorAnimator != null)
            {
                doorAnimator.SetBool("unlock", unlock);
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"[Door] {gameObject.name} 未找到 Animator 组件");
            }
        }
        
        /// <summary>
        /// 检查门是否已解锁
        /// </summary>
        public bool CheckUnlocked()
        {
            return unlock;
        }
        
        /// <summary>
        /// 重置门状态（用于关卡重置）
        /// </summary>
        public void ResetDoor()
        {
            unlock = false;
            UpdateDoorState();
            
            if (showDebugInfo)
            {
                Debug.Log($"[Door] {gameObject.name} 已重置");
            }
        }
        
        void OnDrawGizmos()
        {
            // 在场景视图中显示门的状态
            Gizmos.color = unlock ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
        
        void OnDrawGizmosSelected()
        {
            // 选中时显示更详细的信息
            Gizmos.color = unlock ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
            
            #if UNITY_EDITOR
            string status = unlock ? "已解锁" : "未解锁";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, $"门: {status}");
            #endif
        }
    }
}

