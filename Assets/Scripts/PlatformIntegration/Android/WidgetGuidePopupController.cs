using UnityEngine;
using UnityEngine.UI;

namespace PlatformIntegration
{
    /// <summary>
    /// 小组件添加指引弹窗控制器
    /// 用于控制弹窗的显示和关闭
    /// </summary>
    public class WidgetGuidePopupController : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private bool showDebugInfo = false;
        
        [Header("弹窗控制")]
        [SerializeField] private Button closeButton; // 关闭按钮（可选，会自动查找）
        
        [Header("网页链接")]
        [SerializeField] private Button linkButton; // 网页链接按钮（可选，会自动查找）
        [SerializeField] private string webUrl = "https://example.com"; // 要打开的网页URL
        
        private void Start()
        {
            InitializePopup();
        }
        
        /// <summary>
        /// 初始化弹窗
        /// </summary>
        private void InitializePopup()
        {
            // 如果没有设置关闭按钮，尝试自动查找
            if (closeButton == null)
            {
                closeButton = GetComponentInChildren<Button>();
                if (closeButton == null)
                {
                    // 尝试通过名称查找关闭按钮
                    Transform closeButtonTransform = transform.Find("CloseButton");
                    if (closeButtonTransform == null)
                        closeButtonTransform = transform.Find("Close");
                    if (closeButtonTransform == null)
                        closeButtonTransform = transform.Find("BtnClose");
                    
                    if (closeButtonTransform != null)
                    {
                        closeButton = closeButtonTransform.GetComponent<Button>();
                    }
                }
            }
            
            // 设置关闭按钮事件
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ClosePopup);
                
                if (showDebugInfo)
                    Debug.Log($"[WidgetGuidePopupController] 关闭按钮已设置: {closeButton.name}");
            }
            else
            {
                Debug.LogWarning("[WidgetGuidePopupController] 未找到关闭按钮，请手动设置或确保按钮命名为 CloseButton/Close/BtnClose");
            }
            
            // 如果没有设置链接按钮，尝试自动查找
            if (linkButton == null)
            {
                // 尝试通过名称查找链接按钮
                Transform linkButtonTransform = transform.Find("LinkButton");
                if (linkButtonTransform == null)
                    linkButtonTransform = transform.Find("WebLinkButton");
                if (linkButtonTransform == null)
                    linkButtonTransform = transform.Find("BtnLink");
                if (linkButtonTransform == null)
                    linkButtonTransform = transform.Find("HelpButton");
                
                if (linkButtonTransform != null)
                {
                    linkButton = linkButtonTransform.GetComponent<Button>();
                }
            }
            
            // 设置链接按钮事件
            if (linkButton != null)
            {
                linkButton.onClick.RemoveAllListeners();
                linkButton.onClick.AddListener(OpenWebLink);
                
                if (showDebugInfo)
                    Debug.Log($"[WidgetGuidePopupController] 链接按钮已设置: {linkButton.name}");
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log("[WidgetGuidePopupController] 未找到链接按钮，如需使用请手动设置或确保按钮命名为 LinkButton/WebLinkButton/BtnLink/HelpButton");
            }
            
            // 初始状态设为隐藏
            gameObject.SetActive(false);
            
            if (showDebugInfo)
                Debug.Log($"[WidgetGuidePopupController] 弹窗控制器初始化完成 - Panel: {gameObject.name}");
        }
        
        /// <summary>
        /// 显示弹窗
        /// </summary>
        public void ShowPopup()
        {
            try
            {
                gameObject.SetActive(true);
                
                if (showDebugInfo)
                    Debug.Log("[WidgetGuidePopupController] 弹窗已显示");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WidgetGuidePopupController] 显示弹窗时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 关闭弹窗
        /// </summary>
        public void ClosePopup()
        {
            try
            {
                gameObject.SetActive(false);
                
                if (showDebugInfo)
                    Debug.Log("[WidgetGuidePopupController] 弹窗已关闭");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WidgetGuidePopupController] 关闭弹窗时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 切换弹窗显示状态
        /// </summary>
        public void TogglePopup()
        {
            if (gameObject.activeInHierarchy)
            {
                ClosePopup();
            }
            else
            {
                ShowPopup();
            }
        }
        
        /// <summary>
        /// 检查弹窗是否显示
        /// </summary>
        public bool IsPopupVisible()
        {
            return gameObject.activeInHierarchy;
        }
        
        /// <summary>
        /// 打开网页链接
        /// </summary>
        public void OpenWebLink()
        {
            try
            {
                if (string.IsNullOrEmpty(webUrl))
                {
                    Debug.LogWarning("[WidgetGuidePopupController] 网页URL为空，无法打开链接");
                    return;
                }
                
                if (showDebugInfo)
                    Debug.Log($"[WidgetGuidePopupController] 准备打开网页: {webUrl}");
                
                Application.OpenURL(webUrl);
                
                if (showDebugInfo)
                    Debug.Log("[WidgetGuidePopupController] 网页链接已打开");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[WidgetGuidePopupController] 打开网页链接时发生错误: {e.Message}");
            }
        }
        
        /// <summary>
        /// 设置网页URL
        /// </summary>
        public void SetWebUrl(string url)
        {
            webUrl = url;
            
            if (showDebugInfo)
                Debug.Log($"[WidgetGuidePopupController] 网页URL已设置为: {webUrl}");
        }
        
        /// <summary>
        /// 手动设置关闭按钮（供外部调用）
        /// </summary>
        public void SetCloseButton(Button button)
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
            }
            
            closeButton = button;
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePopup);
                
                if (showDebugInfo)
                    Debug.Log($"[WidgetGuidePopupController] 手动设置关闭按钮: {closeButton.name}");
            }
        }
        
        /// <summary>
        /// 手动设置链接按钮（供外部调用）
        /// </summary>
        public void SetLinkButton(Button button)
        {
            if (linkButton != null)
            {
                linkButton.onClick.RemoveAllListeners();
            }
            
            linkButton = button;
            
            if (linkButton != null)
            {
                linkButton.onClick.AddListener(OpenWebLink);
                
                if (showDebugInfo)
                    Debug.Log($"[WidgetGuidePopupController] 手动设置链接按钮: {linkButton.name}");
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器测试方法
        /// </summary>
        [ContextMenu("测试显示弹窗")]
        private void TestShowPopup()
        {
            ShowPopup();
        }
        
        [ContextMenu("测试关闭弹窗")]
        private void TestClosePopup()
        {
            ClosePopup();
        }
        
        [ContextMenu("测试切换弹窗")]
        private void TestTogglePopup()
        {
            TogglePopup();
        }
        
        [ContextMenu("测试打开网页链接")]
        private void TestOpenWebLink()
        {
            OpenWebLink();
        }
        
        [ContextMenu("显示弹窗信息")]
        private void ShowPopupInfo()
        {
            Debug.Log($"=== 小组件指引弹窗信息 ===");
            Debug.Log($"弹窗GameObject: {gameObject.name}");
            Debug.Log($"关闭按钮: {(closeButton != null ? closeButton.name : "未设置")}");
            Debug.Log($"链接按钮: {(linkButton != null ? linkButton.name : "未设置")}");
            Debug.Log($"网页URL: {(string.IsNullOrEmpty(webUrl) ? "未设置" : webUrl)}");
            Debug.Log($"当前可见: {IsPopupVisible()}");
        }
#endif
    }
}
