using UnityEngine;

namespace PlatformIntegration
{
    /// <summary>
    /// 小组件回调接收器
    /// 专门用于接收Android小组件的回调消息
    /// 应该挂载到名为"WidgetCallbackReceiver"的独立GameObject上
    /// </summary>
    public class WidgetCallbackReceiver : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool showDebugInfo = false;
        
        [Header("Toast提示文本配置")]
        [SerializeField] private string widgetAddedSuccessMessage = "🎉 小组件添加成功！\n你的宠物现在可以在桌面上陪伴你了！";
        [SerializeField] private string toastManagerErrorMessage = "⚠️ 系统提示功能暂时不可用";
        
        private void Awake()
        {
            // 确保GameObject名称正确
            if (gameObject.name != "WidgetCallbackReceiver")
            {
                Debug.LogWarning($"[WidgetCallbackReceiver] GameObject名称应该是'WidgetCallbackReceiver'，当前是'{gameObject.name}'");
                Debug.LogWarning("[WidgetCallbackReceiver] Android回调可能无法正确接收！");
            }
            
            // 设置为DontDestroyOnLoad，确保在场景切换时不被销毁
            DontDestroyOnLoad(gameObject);
            
            // Debug.Log($"[WidgetCallbackReceiver] 小组件回调接收器已初始化 - GameObject: {gameObject.name}");
        }
        
        /// <summary>
        /// 接收小组件添加成功的回调
        /// 这个方法会被Android端通过UnitySendMessage调用
        /// </summary>
        /// <param name="message">回调消息</param>
        public void OnWidgetAddedSuccess(string message)
        {
            // Debug.Log($"[WidgetCallbackReceiver] 收到小组件添加成功回调: {message}");
            
            // 直接调用ToastManager显示成功提示
            ShowWidgetAddedSuccessToast();
        }
        
        /// <summary>
        /// 显示小组件添加成功的Toast提示
        /// </summary>
        private void ShowWidgetAddedSuccessToast()
        {
            ShowToast(widgetAddedSuccessMessage, "小组件添加成功");
        }
        
        /// <summary>
        /// 通用Toast显示方法
        /// </summary>
        /// <param name="message">要显示的消息</param>
        /// <param name="logContext">日志上下文</param>
        private void ShowToast(string message, string logContext)
        {
            try
            {
                // 检查消息是否为空
                if (string.IsNullOrEmpty(message))
                {
                    Debug.LogWarning($"[WidgetCallbackReceiver] {logContext}的消息为空，跳过显示");
                    return;
                }
                
                // 使用ToastManager单例显示提示
                if (ToastManager.Instance != null)
                {
                    ToastManager.Instance.ShowToast(message);
                    
                    // if (showDebugInfo)
                    //     Debug.Log($"[WidgetCallbackReceiver] 通过ToastManager显示{logContext}提示: {message}");
                }
                else
                {
                    Debug.LogWarning($"[WidgetCallbackReceiver] ToastManager.Instance为null，无法显示{logContext}提示");
                    
                    // 显示错误提示到Console（如果配置了）
                    if (!string.IsNullOrEmpty(toastManagerErrorMessage))
                    {
                        Debug.Log($"[提示消息] {toastManagerErrorMessage}");
                    }
                }
                
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WidgetCallbackReceiver] 显示{logContext}提示时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 接收小组件删除的回调（预留）
        /// </summary>
        /// <param name="message">回调消息</param>
        public void OnWidgetRemoved(string message)
        {
            // if (showDebugInfo)
            //     Debug.Log($"[WidgetCallbackReceiver] 收到小组件删除回调: {message}");
            
            // 小组件删除时用户通常不在游戏内，无需显示Toast提示
            // 这里可以添加其他处理逻辑，比如数据清理等
        }
        
        /// <summary>
        /// 接收小组件更新的回调（预留）
        /// </summary>
        /// <param name="message">回调消息</param>
        public void OnWidgetUpdated(string message)
        {
            // if (showDebugInfo)
            //     Debug.Log($"[WidgetCallbackReceiver] 收到小组件更新回调: {message}");
            
            // 小组件更新通常是自动后台行为，无需显示Toast提示
            // 这里可以添加其他处理逻辑，比如数据同步等
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的测试方法
        /// </summary>
        [ContextMenu("测试小组件添加成功回调")]
        private void TestWidgetAddedSuccess()
        {
            OnWidgetAddedSuccess("Test message from editor");
        }
        
        [ContextMenu("测试小组件删除回调")]
        private void TestWidgetRemoved()
        {
            OnWidgetRemoved("Test widget removed from editor");
        }
        
        [ContextMenu("测试小组件更新回调")]
        private void TestWidgetUpdated()
        {
            OnWidgetUpdated("Test widget updated from editor");
        }
        
        [ContextMenu("测试所有Toast消息")]
        private void TestAllToastMessages()
        {
            Debug.Log("=== 测试所有Toast消息 ===");
            
            if (!string.IsNullOrEmpty(widgetAddedSuccessMessage))
                Debug.Log($"添加成功消息: {widgetAddedSuccessMessage}");
                
            if (!string.IsNullOrEmpty(toastManagerErrorMessage))
                Debug.Log($"错误消息: {toastManagerErrorMessage}");
        }
#endif
    }
}
