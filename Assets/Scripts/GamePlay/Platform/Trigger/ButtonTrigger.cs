using UnityEngine;

namespace FlashParty.Platform
{
    /// <summary>
    /// 按钮触发器 - 玩家可以通过按E键或靠近触发
    /// </summary>
    public class ButtonTrigger : PlatformTrigger
    {
        [Header("按钮设置")]
        [SerializeField] private bool triggerOnProximity = false; // 靠近自动触发
        [SerializeField] private bool requireInteractionKey = true; // 需要按键交互
        [SerializeField] private float triggerRadius = 1.5f;
        
        [Header("UI提示")]
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private string promptText = "Press E to activate";
        
        private bool playerInRange = false;
        private Player currentPlayer = null;
        
        protected override void Start()
        {
            base.Start();
            
            // 初始隐藏交互提示
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            
            // 监听Action事件（对应E键）
            if (requireInteractionKey)
            {
                EventManager.Instance.AddListener(EventType.Action, OnActionPressed);
            }
        }
        
        void Update()
        {
            CheckPlayerProximity();
            
            // 如果设置为靠近自动触发
            if (triggerOnProximity && playerInRange && CanTrigger())
            {
                Trigger();
            }
        }
        
        /// <summary>
        /// 检查玩家是否在范围内
        /// </summary>
        private void CheckPlayerProximity()
        {
            // 查找场景中的玩家
            Player player = FindObjectOfType<Player>();
            if (player == null) return;
            
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= triggerRadius;
            
            // 状态改变时更新UI
            if (playerInRange != wasInRange)
            {
                currentPlayer = playerInRange ? player : null;
                UpdateInteractionPrompt();
            }
        }
        
        /// <summary>
        /// 更新交互提示
        /// </summary>
        private void UpdateInteractionPrompt()
        {
            if (interactionPrompt == null) return;
            
            bool shouldShow = playerInRange && requireInteractionKey && CanTrigger();
            interactionPrompt.SetActive(shouldShow);
            
            // 如果提示对象有Text组件，更新提示文本
            if (shouldShow && !string.IsNullOrEmpty(promptText))
            {
                var textComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
                if (textComponent != null)
                {
                    textComponent.text = promptText;
                }
                
                // 如果使用的是TextMeshPro
                var tmpComponent = interactionPrompt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmpComponent != null)
                {
                    tmpComponent.text = promptText;
                }
            }
        }
        
        /// <summary>
        /// 处理Action按键事件
        /// </summary>
        private void OnActionPressed(object data)
        {
            if (playerInRange && CanTrigger())
            {
                Trigger();
            }
        }
        
        protected override void OnTriggerActivated()
        {
            base.OnTriggerActivated();
            
            // 触发后隐藏提示
            UpdateInteractionPrompt();
            
            // 可以在这里添加按钮按下的视觉/音效反馈
            Debug.Log("Button pressed!");
        }
        
        protected override void UpdateVisualState()
        {
            base.UpdateVisualState();
            UpdateInteractionPrompt();
        }
        
        /// <summary>
        /// 手动触发（用于测试）
        /// </summary>
        [ContextMenu("Test Trigger")]
        public void TestTrigger()
        {
            Trigger();
        }
        
        void OnDestroy()
        {
            // 移除事件监听
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener(EventType.Action, OnActionPressed);
            }
        }
        
        void OnDrawGizmos()
        {
            // 绘制触发范围
            Gizmos.color = playerInRange ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
        
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // 绘制触发范围
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
} 