using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    
    [Header("系统特定弹窗界面")]
    [SerializeField] private GameObject miuiWidgetGuidePanel; // MIUI系统的小组件添加指引弹窗
    [SerializeField] private GameObject vivoWidgetGuidePanel; // vivo FuntouchOS系统的小组件添加指引弹窗
    [SerializeField] private GameObject generalWidgetGuidePanel; // 通用Android系统的小组件添加指引弹窗
    
    private Button widgetButton;
    
    // 静态实例，用于接收Android回调
    private static AndroidWidgetLauncher instance;
        
    private void Start()
    {
        // 设置单例实例
        instance = this;
        InitializeButton();
        
        // 初始化时隐藏所有弹窗
        InitializePopupPanels();
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
            
        Debug.Log("[AndroidWidgetLauncher] 用户点击启动小组件选择器");
            
            try
            {
                Debug.Log("[AndroidWidgetLauncher] 准备调用Android原生方法...");
                
                // 调用Android原生方法
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass widgetHelperClass = new AndroidJavaClass("com.zher.meow.widget.WidgetHelper"))
                {
                    Debug.Log("[AndroidWidgetLauncher] Android对象已创建，开始调用openWidgetPicker...");
                    
                    int result = widgetHelperClass.CallStatic<int>("openWidgetPicker", currentActivity);
                    
                    Debug.Log($"[AndroidWidgetLauncher] openWidgetPicker返回结果: {result}");
                    
                    switch (result)
                    {
                        case 0: // 成功发起请求
                            Debug.Log("[AndroidWidgetLauncher] 已成功发起小组件固定请求");
                            
                            // 不显示游戏内提示，只等待系统弹窗和后续的成功回调
                            // ShowMessage("添加小组件", "已向系统发起添加请求！...");
                            break;
                            
                        case 1: // 已存在小组件
                            Debug.Log("[AndroidWidgetLauncher] 桌面已存在小组件");
                            ShowExistingWidgetMessage();
                            break;
                            
                        case -2: // MIUI特殊处理：显示游戏内弹窗指引
                            Debug.Log("[AndroidWidgetLauncher] MIUI系统：显示游戏内弹窗指引");
                            ShowMIUIWidgetGuidePanel();
                            break;
                            
                        case -3: // vivo特殊处理：显示游戏内弹窗指引
                            Debug.Log("[AndroidWidgetLauncher] vivo系统：显示游戏内弹窗指引");
                            ShowVivoWidgetGuidePanel();
                            break;
                            
                        case -1: // 失败
                        default:
                            Debug.LogWarning("[AndroidWidgetLauncher] 小组件固定请求失败");
                            ShowGeneralWidgetGuidePanel();
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
        /// 显示MIUI系统的小组件添加指引弹窗
        /// </summary>
        private void ShowMIUIWidgetGuidePanel()
        {
            Debug.Log("[AndroidWidgetLauncher] 开始显示MIUI弹窗...");
            
            try
            {
                if (miuiWidgetGuidePanel != null)
                {
                    Debug.Log($"[AndroidWidgetLauncher] MIUI弹窗GameObject存在: {miuiWidgetGuidePanel.name}");
                    Debug.Log($"[AndroidWidgetLauncher] 弹窗当前状态: {miuiWidgetGuidePanel.activeInHierarchy}");
                    
                    // 显示MIUI弹窗
                    miuiWidgetGuidePanel.SetActive(true);
                    Debug.Log($"[AndroidWidgetLauncher] MIUI弹窗显示完成，当前状态: {miuiWidgetGuidePanel.activeInHierarchy}");
                }
                else
                {
                    Debug.LogWarning("[AndroidWidgetLauncher] MIUI弹窗界面未设置，使用备用Toast提示");
                    ShowMessage("添加桌面小组件", "📱 小米MIUI系统添加步骤：\n\n1. 长按桌面空白区域\n2. 点击底部'添加工具'或'+'号\n3. 滑动找到'Miao屋桌面宠物'\n4. 点击添加到桌面\n\n💡 提示：添加后可直接在桌面查看宠物状态！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 显示MIUI弹窗时发生错误: {e.Message}");
                // 备用方案：使用Toast
                ShowMessage("添加桌面小组件", "📱 小米MIUI系统添加步骤：\n\n1. 长按桌面空白区域\n2. 点击底部'添加工具'或'+'号\n3. 滑动找到'Miao屋桌面宠物'\n4. 点击添加到桌面\n\n💡 提示：添加后可直接在桌面查看宠物状态！");
            }
        }
        
        /// <summary>
        /// 显示vivo系统的小组件添加指引弹窗
        /// </summary>
        private void ShowVivoWidgetGuidePanel()
        {
            Debug.Log("[AndroidWidgetLauncher] 开始显示vivo弹窗...");
            
            try
            {
                if (vivoWidgetGuidePanel != null)
                {
                    Debug.Log($"[AndroidWidgetLauncher] vivo弹窗GameObject存在: {vivoWidgetGuidePanel.name}");
                    Debug.Log($"[AndroidWidgetLauncher] 弹窗当前状态: {vivoWidgetGuidePanel.activeInHierarchy}");
                    
                    // 显示vivo弹窗
                    vivoWidgetGuidePanel.SetActive(true);
                    Debug.Log($"[AndroidWidgetLauncher] vivo弹窗显示完成，当前状态: {vivoWidgetGuidePanel.activeInHierarchy}");
                }
                else
                {
                    Debug.LogWarning("[AndroidWidgetLauncher] vivo FuntouchOS弹窗界面未设置，使用备用Toast提示");
                    ShowMessage("添加桌面小组件", "📱 vivo FuntouchOS系统添加步骤：\n\n1. 长按桌面空白区域\n2. 点击底部'+'号或'添加'\n3. 选择'小组件'或'工具'\n4. 滑动找到'Miao屋桌面宠物'\n5. 点击添加到桌面\n\n💡 提示：添加后可直接在桌面查看宠物状态！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 显示vivo FuntouchOS弹窗时发生错误: {e.Message}");
                // 备用方案：使用Toast
                ShowMessage("添加桌面小组件", "📱 vivo FuntouchOS系统添加步骤：\n\n1. 长按桌面空白区域\n2. 点击底部'+'号或'添加'\n3. 选择'小组件'或'工具'\n4. 滑动找到'Miao屋桌面宠物'\n5. 点击添加到桌面\n\n💡 提示：添加后可直接在桌面查看宠物状态！");
            }
        }
        
        /// <summary>
        /// 显示通用Android系统的小组件添加指引弹窗
        /// </summary>
        private void ShowGeneralWidgetGuidePanel()
        {
            try
            {
                if (generalWidgetGuidePanel != null)
                {
                    generalWidgetGuidePanel.SetActive(true);
                    Debug.Log("[AndroidWidgetLauncher] 通用Android弹窗界面已显示");
                }
                else
                {
                    Debug.LogWarning("[AndroidWidgetLauncher] 通用Android弹窗界面未设置，使用备用Toast提示");
                    ShowMessage("添加桌面小组件", "当前启动器不支持自动添加小组件\n\n请手动添加：\n\n📱 操作步骤：\n1. 长按桌面空白区域\n2. 点击'小组件'或'添加工具'\n3. 找到'Miao屋桌面宠物'\n4. 拖拽到桌面\n\n✨ 添加后即可在桌面查看宠物状态！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 显示通用Android弹窗时发生错误: {e.Message}");
                // 备用方案：使用Toast
                ShowMessage("添加桌面小组件", "当前启动器不支持自动添加小组件\n\n请手动添加：\n\n📱 操作步骤：\n1. 长按桌面空白区域\n2. 点击'小组件'或'添加工具'\n3. 找到'Miao屋桌面宠物'\n4. 拖拽到桌面\n\n✨ 添加后即可在桌面查看宠物状态！");
            }
        }
        
        /// <summary>
        /// 关闭MIUI系统的小组件添加指引弹窗
        /// </summary>
        public void CloseMIUIWidgetGuidePanel()
        {
            try
            {
                if (miuiWidgetGuidePanel != null)
                {
                    miuiWidgetGuidePanel.SetActive(false);
                    Debug.Log("[AndroidWidgetLauncher] MIUI弹窗界面已关闭");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 关闭MIUI弹窗时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 关闭通用Android系统的小组件添加指引弹窗
        /// </summary>
        public void CloseGeneralWidgetGuidePanel()
        {
            try
            {
                if (generalWidgetGuidePanel != null)
                {
                    generalWidgetGuidePanel.SetActive(false);
                    Debug.Log("[AndroidWidgetLauncher] 通用Android弹窗界面已关闭");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 关闭通用Android弹窗时发生错误: {e.Message}");
            }
        }
        
        
        /// <summary>
        /// 关闭vivo FuntouchOS系统的小组件添加指引弹窗
        /// </summary>
        public void CloseVivoWidgetGuidePanel()
        {
            try
            {
                if (vivoWidgetGuidePanel != null)
                {
                    vivoWidgetGuidePanel.SetActive(false);
                    Debug.Log("[AndroidWidgetLauncher] vivo FuntouchOS弹窗界面已关闭");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AndroidWidgetLauncher] 关闭vivo FuntouchOS弹窗时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 延迟显示MIUI弹窗（协程）
        /// </summary>
        private IEnumerator ShowMIUIWidgetPanelDelayed()
        {
            // 等待一帧，确保当前帧的所有UI操作完成
            yield return null;
            
            if (miuiWidgetGuidePanel != null)
            {
                Debug.Log("[AndroidWidgetLauncher] 协程开始检查MIUI弹窗状态");
                Debug.Log($"[AndroidWidgetLauncher] 弹窗父级对象: {(miuiWidgetGuidePanel.transform.parent != null ? miuiWidgetGuidePanel.transform.parent.name : "null")}");
                Debug.Log($"[AndroidWidgetLauncher] 弹窗层级路径: {GetGameObjectPath(miuiWidgetGuidePanel)}");
                
                // 检查父级对象是否都是激活状态
                Transform current = miuiWidgetGuidePanel.transform;
                bool hierarchyActive = true;
                while (current != null)
                {
                    bool isActive = current.gameObject.activeInHierarchy;
                    Debug.Log($"[AndroidWidgetLauncher] 层级检查 - {current.name}: {isActive}");
                    if (!isActive && current != miuiWidgetGuidePanel.transform)
                    {
                        hierarchyActive = false;
                    }
                    current = current.parent;
                }
                
                if (!hierarchyActive)
                {
                    Debug.LogError("[AndroidWidgetLauncher] 发现父级对象未激活，这可能是弹窗不显示的原因！");
                }
                
                // 如果弹窗还没有激活，强制激活
                if (!miuiWidgetGuidePanel.activeInHierarchy)
                {
                    Debug.Log("[AndroidWidgetLauncher] 弹窗仍未激活，强制激活");
                    miuiWidgetGuidePanel.SetActive(true);
                }
                
                // 强制刷新Canvas
                Canvas canvas = miuiWidgetGuidePanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    Debug.Log($"[AndroidWidgetLauncher] 找到Canvas: {canvas.name}, enabled: {canvas.enabled}");
                    Debug.Log($"[AndroidWidgetLauncher] Canvas sortingOrder: {canvas.sortingOrder}");
                    canvas.enabled = false;
                    canvas.enabled = true;
                    Debug.Log("[AndroidWidgetLauncher] Canvas已强制刷新");
                }
                else
                {
                    Debug.LogError("[AndroidWidgetLauncher] 未找到Canvas组件！这是弹窗不显示的重要原因！");
                }
                
                Debug.Log($"[AndroidWidgetLauncher] MIUI弹窗最终状态: {miuiWidgetGuidePanel.activeInHierarchy}");
                
                RectTransform rectTransform = miuiWidgetGuidePanel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Debug.Log($"[AndroidWidgetLauncher] 弹窗RectTransform: {rectTransform.rect}");
                    Debug.Log($"[AndroidWidgetLauncher] 弹窗位置: {rectTransform.anchoredPosition}");
                    Debug.Log($"[AndroidWidgetLauncher] 弹窗缩放: {rectTransform.localScale}");
                }
            }
        }
        
        /// <summary>
        /// 延迟显示vivo弹窗（协程）
        /// </summary>
        private IEnumerator ShowVivoWidgetPanelDelayed()
        {
            // 等待一帧，确保当前帧的所有UI操作完成
            yield return null;
            
            // 再等待一小段时间，确保UI系统完全准备就绪
            yield return new WaitForSeconds(0.1f);
            
            if (vivoWidgetGuidePanel != null)
            {
                Debug.Log("[AndroidWidgetLauncher] 协程开始显示vivo弹窗");
                vivoWidgetGuidePanel.SetActive(true);
                
                // 强制刷新Canvas
                Canvas canvas = vivoWidgetGuidePanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = false;
                    canvas.enabled = true;
                    Debug.Log("[AndroidWidgetLauncher] Canvas已强制刷新");
                }
                
                Debug.Log($"[AndroidWidgetLauncher] vivo弹窗最终状态: {vivoWidgetGuidePanel.activeInHierarchy}");
            }
        }
        
        /// <summary>
        /// 初始化弹窗面板
        /// </summary>
        private void InitializePopupPanels()
        {
            Debug.Log("[AndroidWidgetLauncher] 初始化弹窗面板...");
            
            // 隐藏所有弹窗面板
            if (miuiWidgetGuidePanel != null)
            {
                miuiWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] MIUI弹窗已初始化为隐藏状态");
            }
            
            if (vivoWidgetGuidePanel != null)
            {
                vivoWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] vivo弹窗已初始化为隐藏状态");
            }
            
            if (generalWidgetGuidePanel != null)
            {
                generalWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] 通用弹窗已初始化为隐藏状态");
            }
            
            Debug.Log("[AndroidWidgetLauncher] 所有弹窗面板初始化完成");
        }
        
        /// <summary>
        /// 获取GameObject的完整路径
        /// </summary>
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
        
        /// <summary>
        /// 关闭所有弹窗
        /// </summary>
        public void CloseAllPopups()
        {
            Debug.Log("[AndroidWidgetLauncher] 开始关闭所有弹窗...");
            
            if (miuiWidgetGuidePanel != null && miuiWidgetGuidePanel.activeInHierarchy)
            {
                miuiWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] MIUI弹窗已关闭");
            }
            
            if (vivoWidgetGuidePanel != null && vivoWidgetGuidePanel.activeInHierarchy)
            {
                vivoWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] vivo弹窗已关闭");
            }
            
            if (generalWidgetGuidePanel != null && generalWidgetGuidePanel.activeInHierarchy)
            {
                generalWidgetGuidePanel.SetActive(false);
                Debug.Log("[AndroidWidgetLauncher] 通用弹窗已关闭");
            }
            
            Debug.Log("[AndroidWidgetLauncher] 所有弹窗已关闭");
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
