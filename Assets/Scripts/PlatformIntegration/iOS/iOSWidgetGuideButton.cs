using UnityEngine;
using UnityEngine.UI;

namespace PlatformIntegration
{
    /// <summary>
    /// iOS桌面宠物添加指引按钮
    /// 专门用于在iOS设备上指导用户如何添加桌面宠物
    /// 挂载到按钮上使用，非iOS平台自动隐藏
    /// </summary>
    public class iOSWidgetGuideButton : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool showDebugInfo = false;
        
        [Header("Toast提示文本配置")]
        [SerializeField] private string widgetGuideMessage = "📱 iOS桌面宠物添加指南\n\n1. 长按桌面空白区域\n2. 点击左上角的 + 号\n3. 搜索并找到本应用\n4. 选择小组件大小\n5. 点击「添加小组件」\n6. 完成设置\n\n✨ 你的宠物就能在桌面陪伴你了！";
        [SerializeField] private string toastManagerErrorMessage = "⚠️ 系统提示功能暂时不可用";
        
        [Header("Toast显示设置")]
        [SerializeField] private float customToastDuration = 5f; // 自定义Toast显示时长（秒）
        
        private Button guideButton;
        
        private void Start()
        {
            InitializeButton();
        }
        
        /// <summary>
        /// 初始化按钮
        /// </summary>
        private void InitializeButton()
        {
#if UNITY_IOS || UNITY_EDITOR
            
            // iOS平台或编辑器：显示按钮并设置点击事件
            gameObject.SetActive(true);
            
            // 自动获取同一GameObject上的Button组件
            guideButton = GetComponent<Button>();
            
            if (guideButton != null)
            {
                guideButton.onClick.RemoveAllListeners();
                guideButton.onClick.AddListener(OnShowWidgetGuide);
            }
            else
            {
                Debug.LogError("[iOSWidgetGuideButton] 找不到Button组件，请确保此组件挂载到Button上");
            }
            
            if (showDebugInfo)
            {
#if UNITY_EDITOR
                Debug.Log("[iOSWidgetGuideButton] Unity编辑器，显示桌面宠物指引按钮（测试模式）");
#else
                Debug.Log("[iOSWidgetGuideButton] iOS平台，显示桌面宠物指引按钮");
#endif
            }
            
#else
            
            // 非iOS平台：隐藏按钮
            gameObject.SetActive(false);
            
            if (showDebugInfo)
                Debug.Log("[iOSWidgetGuideButton] 非iOS平台，隐藏桌面宠物指引按钮");
            
#endif
        }
        
        /// <summary>
        /// 显示iOS桌面宠物添加指引
        /// </summary>
        private void OnShowWidgetGuide()
        {
#if UNITY_IOS || UNITY_EDITOR
            
            if (showDebugInfo)
            {
#if UNITY_EDITOR
                Debug.Log("[iOSWidgetGuideButton] 用户点击显示iOS桌面宠物指引（编辑器测试模式）");
#else
                Debug.Log("[iOSWidgetGuideButton] 用户点击显示iOS桌面宠物指引");
#endif
            }
            
            ShowWidgetGuideToast();
            
#else
            
            ShowToast("不支持", "桌面宠物功能仅在iOS设备上可用");
            
#endif
        }
        
        /// <summary>
        /// 显示桌面宠物指引Toast
        /// </summary>
        private void ShowWidgetGuideToast()
        {
            ShowToast(widgetGuideMessage, "iOS桌面宠物指引");
        }
        
        /// <summary>
        /// 通用Toast显示方法
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="logContext">日志上下文</param>
        private void ShowToast(string message, string logContext)
        {
            ShowToast(message, logContext, customToastDuration);
        }
        
        /// <summary>
        /// 通用Toast显示方法（自定义显示时长）
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="logContext">日志上下文</param>
        /// <param name="duration">显示时长（秒）</param>
        private void ShowToast(string message, string logContext, float duration)
        {
            try
            {
                // 检查消息是否为空
                if (string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning($"[iOSWidgetGuideButton] {logContext}的消息为空，跳过显示");
                    return;
                }
                
                // 使用ToastManager单例显示提示
                if (ToastManager.Instance != null)
                {
                    ToastManager.Instance.ShowToast(message, duration);
                    
                    if (showDebugInfo)
                        Debug.Log($"[iOSWidgetGuideButton] 通过ToastManager显示{logContext}提示（{duration}秒）: {message}");
                }
                else
                {
                    Debug.LogWarning($"[iOSWidgetGuideButton] ToastManager.Instance为null，无法显示{logContext}提示");
                    
                    // 显示错误提示到Console（如果配置了）
                    if (!string.IsNullOrEmpty(toastManagerErrorMessage))
                    {
                        Debug.Log($"[提示消息] {toastManagerErrorMessage}");
                    }
                }
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[iOSWidgetGuideButton] 显示{logContext}提示时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 手动显示指引（供外部调用）
        /// </summary>
        public void ShowGuideManually()
        {
            OnShowWidgetGuide();
        }
        
        /// <summary>
        /// 检查是否支持桌面宠物功能
        /// </summary>
        public bool IsWidgetSupported()
        {
#if UNITY_IOS || UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
        
        /// <summary>
        /// 检查按钮是否可见
        /// </summary>
        public bool IsButtonVisible()
        {
            return gameObject.activeInHierarchy;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的测试方法
        /// </summary>
        [ContextMenu("测试显示iOS桌面宠物指引")]
        private void TestShowWidgetGuide()
        {
            OnShowWidgetGuide();
        }
        
        [ContextMenu("测试所有Toast消息")]
        private void TestAllToastMessages()
        {
            Debug.Log("=== 测试所有Toast消息 ===");
            
            if (!string.IsNullOrEmpty(widgetGuideMessage))
                Debug.Log($"指引消息: {widgetGuideMessage}");
                
            if (!string.IsNullOrEmpty(toastManagerErrorMessage))
                Debug.Log($"错误消息: {toastManagerErrorMessage}");
        }
        
        [ContextMenu("显示组件信息")]
        private void ShowComponentInfo()
        {
            Debug.Log($"=== iOS桌面宠物指引按钮信息 ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"支持桌面宠物: {IsWidgetSupported()}");
            Debug.Log($"按钮可见: {IsButtonVisible()}");
            Debug.Log($"Button组件: {(GetComponent<Button>() != null ? "已找到" : "未找到")}");
        }
#endif
    }
}
