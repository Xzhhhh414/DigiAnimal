using UnityEngine;

namespace PlatformIntegration
{
    /// <summary>
    /// iOS平台UI控制器
    /// 专门用于控制UI控件在iOS平台上的显示和隐藏
    /// 挂载到需要iOS平台专用显示的UI控件上
    /// </summary>
    public class iOSPlatformUIController : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool showDebugInfo = false;
        
        [Header("显示控制")]
        [SerializeField] private bool hideOnNonIOSPlatforms = true; // 是否在非iOS平台隐藏
        
        private void Awake()
        {
            // 初始化平台显示控制
            InitializePlatformVisibility();
        }
        
        /// <summary>
        /// 初始化平台显示控制
        /// </summary>
        private void InitializePlatformVisibility()
        {
#if UNITY_IOS || UNITY_EDITOR
            
            // iOS平台或编辑器：显示控件
            SetControlVisibility(true);
            
            if (showDebugInfo)
            {
#if UNITY_EDITOR
                Debug.Log($"[iOSPlatformUIController] Unity编辑器，显示UI控件（测试模式）: {gameObject.name}");
#else
                Debug.Log($"[iOSPlatformUIController] iOS平台，显示UI控件: {gameObject.name}");
#endif
            }
            
#else
            
            // 非iOS平台：根据设置决定是否隐藏
            if (hideOnNonIOSPlatforms)
            {
                SetControlVisibility(false);
                
                if (showDebugInfo)
                    Debug.Log($"[iOSPlatformUIController] 非iOS平台，隐藏UI控件: {gameObject.name}");
            }
            else
            {
                SetControlVisibility(true);
                
                if (showDebugInfo)
                    Debug.Log($"[iOSPlatformUIController] 非iOS平台，但设置为显示UI控件: {gameObject.name}");
            }
            
#endif
        }
        
        /// <summary>
        /// 设置控件可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        private void SetControlVisibility(bool visible)
        {
            // 使用SetActive控制显示
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// 手动设置可见性（供外部调用）
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            SetControlVisibility(visible);
            
            if (showDebugInfo)
                Debug.Log($"[iOSPlatformUIController] 手动设置UI控件可见性: {visible}, GameObject: {gameObject.name}");
        }
        
        /// <summary>
        /// 检查当前是否在iOS平台
        /// </summary>
        /// <returns>是否为iOS平台</returns>
        public bool IsIOSPlatform()
        {
#if UNITY_IOS || UNITY_EDITOR
            return true; // iOS平台或编辑器测试模式
#else
            return false;
#endif
        }
        
        /// <summary>
        /// 检查控件是否应该显示
        /// </summary>
        /// <returns>是否应该显示</returns>
        public bool ShouldShowOnCurrentPlatform()
        {
#if UNITY_IOS || UNITY_EDITOR
            return true; // iOS平台或编辑器总是显示
#else
            return !hideOnNonIOSPlatforms; // 非iOS平台根据设置决定
#endif
        }
        
        /// <summary>
        /// 获取当前控件的可见状态
        /// </summary>
        /// <returns>当前是否可见</returns>
        public bool IsCurrentlyVisible()
        {
            return gameObject.activeInHierarchy;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器中的测试方法
        /// </summary>
        [ContextMenu("测试显示控件")]
        private void TestShowControl()
        {
            SetVisible(true);
        }
        
        [ContextMenu("测试隐藏控件")]
        private void TestHideControl()
        {
            SetVisible(false);
        }
        
        [ContextMenu("切换控件可见性")]
        private void ToggleControlVisibility()
        {
            bool currentVisible = IsCurrentlyVisible();
            SetVisible(!currentVisible);
        }
        
        [ContextMenu("显示平台信息")]
        private void ShowPlatformInfo()
        {
            Debug.Log($"=== iOS平台UI控制器信息 ===");
            Debug.Log($"GameObject: {gameObject.name}");
            Debug.Log($"当前平台是iOS: {IsIOSPlatform()}");
            Debug.Log($"应该在当前平台显示: {ShouldShowOnCurrentPlatform()}");
            Debug.Log($"当前可见状态: {IsCurrentlyVisible()}");
            Debug.Log($"控制方式: SetActive");
        }
#endif
    }
}
