using UnityEngine;
using UnityEngine.UI;

namespace PlatformIntegration
{
    /// <summary>
    /// Android桌面小组件启动器
    /// 专门用于在Android设备上打开系统小组件选择界面
    /// 直接挂载到Button上使用，非Android平台自动隐藏
    /// </summary>
    public class AndroidWidgetLauncher : MonoBehaviour
    {
    [Header("设置")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Button widgetButton;
    
    // 静态实例，用于接收Android回调
    private static AndroidWidgetLauncher instance;
        
    private void Start()
    {
        // 设置单例实例
        instance = this;
        InitializeButton();
    }
    
    private void OnDestroy()
    {
        // 清除单例实例
        if (instance == this)
        {
            instance = null;
        }
    }
        
        /// <summary>
        /// 初始化按钮
        /// </summary>
        private void InitializeButton()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            
            // Android平台：显示按钮并设置点击事件
            gameObject.SetActive(true);
            
            // 自动获取同一GameObject上的Button组件
            widgetButton = GetComponent<Button>();
            
            if (widgetButton != null)
            {
                widgetButton.onClick.RemoveAllListeners();
                widgetButton.onClick.AddListener(OnLaunchWidgetPicker);
            }
            else
            {
                Debug.LogError("[AndroidWidgetLauncher] 找不到Button组件，请确保此组件挂载到Button上");
            }
            
        // if (showDebugInfo)
        //     Debug.Log("[AndroidWidgetLauncher] Android平台，显示小组件启动按钮");
            
            #else
            
            // 非Android平台：隐藏按钮
            gameObject.SetActive(false);
            
        // if (showDebugInfo)
        //     Debug.Log("[AndroidWidgetLauncher] 非Android平台，隐藏小组件启动按钮");
            
            #endif
        }
        
        /// <summary>
        /// 启动小组件选择器
        /// </summary>
        private void OnLaunchWidgetPicker()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            
        // if (showDebugInfo)
        //     Debug.Log("[AndroidWidgetLauncher] 用户点击启动小组件选择器");
            
            try
            {
                // 调用Android原生方法
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass widgetHelperClass = new AndroidJavaClass("com.zher.meow.widget.WidgetHelper"))
                {
                    int result = widgetHelperClass.CallStatic<int>("openWidgetPicker", currentActivity);
                    
                    switch (result)
                    {
                        case 0: // 成功发起请求
                            // if (showDebugInfo)
                            //     Debug.Log("[AndroidWidgetLauncher] 已成功发起小组件固定请求");
                            
                            // 不显示游戏内提示，只等待系统弹窗和后续的成功回调
                            // ShowMessage("添加小组件", "已向系统发起添加请求！...");
                            break;
                            
                        case 1: // 已存在小组件
                            // if (showDebugInfo)
                            //     Debug.Log("[AndroidWidgetLauncher] 桌面已存在小组件");
                            ShowExistingWidgetMessage();
                            break;
                            
                        case -1: // 失败
                        default:
                            Debug.LogWarning("[AndroidWidgetLauncher] 小组件固定请求失败");
                            ShowMessage("添加桌面小组件", "当前启动器不支持自动添加小组件\n\n请手动添加：\n\n📱 操作步骤：\n1. 长按桌面空白区域\n2. 点击'小组件'或'添加工具'\n3. 找到'Miao屋桌面宠物'\n4. 拖拽到桌面\n\n✨ 添加后即可在桌面查看宠物状态！");
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 启动小组件选择器失败: {e.Message}");
                
                // 显示错误提示
                ShowMessage("添加小组件", "请手动添加小组件：\n\n1. 长按桌面空白区域\n2. 选择'小组件'或'添加工具'\n3. 找到'Miao屋虚拟宠物'\n4. 拖拽到桌面合适位置");
            }
            
            #else
            
            ShowMessage("不支持", "桌面小组件功能仅在Android设备上可用");
            
            #endif
        }
    
    /// <summary>
    /// 显示已存在小组件的消息
    /// </summary>
    private void ShowExistingWidgetMessage()
    {
        try
        {
            // 尝试从 WidgetCallbackReceiver 获取配置的消息
            var widgetCallbackReceiver = FindObjectOfType<WidgetCallbackReceiver>();
            if (widgetCallbackReceiver != null)
            {
                // 使用反射获取 toastManagerErrorMessage 字段
                var field = widgetCallbackReceiver.GetType().GetField("toastManagerErrorMessage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    string configuredMessage = field.GetValue(widgetCallbackReceiver) as string;
                    if (!string.IsNullOrEmpty(configuredMessage))
                    {
                        ShowToastDirect(configuredMessage);
                        return;
                    }
                }
            }
            
            // 备用消息
            ShowToastDirect("📱 桌面已存在小组件");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetLauncher] 获取配置消息失败: {e.Message}");
            ShowToastDirect("📱 桌面已存在小组件");
        }
    }
    
    /// <summary>
    /// 直接显示Toast消息
    /// </summary>
    private void ShowToastDirect(string message)
    {
        try
        {
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowToast(message);
                
                // if (showDebugInfo)
                //     Debug.Log($"[AndroidWidgetLauncher] 通过ToastManager显示消息: {message}");
            }
            else
            {
                Debug.LogWarning("[AndroidWidgetLauncher] ToastManager.Instance为null，无法显示消息");
                Debug.Log($"[提示消息] {message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetLauncher] 显示Toast消息时发生错误: {e.Message}");
        }
    }
        
    /// <summary>
    /// 显示消息（可根据你的UI系统进行调整）
    /// </summary>
    private void ShowMessage(string title, string message)
    {
        Debug.Log($"[UI消息] {title}: {message}");
        
        // 使用ToastManager显示消息
        try
        {
            if (ToastManager.Instance != null)
            {
                // 合并标题和消息内容
                string fullMessage = $"{title}\n\n{message}";
                ToastManager.Instance.ShowToast(fullMessage);
                
            // if (showDebugInfo)
            //     Debug.Log("[AndroidWidgetLauncher] 通过ToastManager显示消息");
            }
            else
            {
                Debug.LogWarning("[AndroidWidgetLauncher] ToastManager.Instance为null，无法显示消息");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetLauncher] 显示消息时发生错误: {e.Message}");
        }
    }
    
    /// <summary>
    /// Android回调：小组件添加成功
    /// 这个方法会被Android端通过UnitySendMessage调用
    /// </summary>
    public void OnWidgetAddedSuccess(string message)
    {
        if (showDebugInfo)
            Debug.Log("[AndroidWidgetLauncher] 收到小组件添加成功回调: " + message);
        
        // 显示成功提示
        ShowMessage("添加成功", "🎉 小组件添加成功！\n\n你的宠物现在可以在桌面上陪伴你了！\n\n✨ 点击小组件按钮与宠物互动吧！");
    }
    
    /// <summary>
    /// 静态方法：供外部调用以处理成功回调
    /// </summary>
    public static void HandleWidgetAddedSuccess()
    {
        if (instance != null)
        {
            instance.OnWidgetAddedSuccess("Widget added successfully");
        }
        else
        {
            Debug.LogWarning("[AndroidWidgetLauncher] 无法处理成功回调，实例不存在");
        }
    }
    
        
        /// <summary>
        /// 检查是否支持小组件功能
        /// </summary>
        public bool IsWidgetSupported()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return true;
            #else
            return false;
            #endif
        }
        
        /// <summary>
        /// 检查启动器是否可见
        /// </summary>
        public bool IsLauncherVisible()
        {
            return gameObject.activeInHierarchy;
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的测试方法
        /// </summary>
        [ContextMenu("测试启动小组件选择器")]
        private void TestLaunchWidgetPicker()
        {
            OnLaunchWidgetPicker();
        }
        #endif
    }
}
