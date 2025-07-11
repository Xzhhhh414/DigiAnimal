using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BottomPanelController : MonoBehaviour
{
    [Header("面板设置")]
    [SerializeField] private RectTransform panelRect; // 底部面板的RectTransform
    [SerializeField] private RectTransform buttonRect; // 按钮的RectTransform
    [SerializeField] private Image toggleButtonImage; // 按钮图片，用于旋转或更换图标
    [SerializeField] private Sprite expandSprite; // 展开状态的图标
    [SerializeField] private Sprite collapseSprite; // 收起状态的图标
    
    [Header("按钮位置设置")]
    [SerializeField] private float buttonExpandedY = 0f; // 面板展开时按钮的Y位置
    [SerializeField] private float buttonCollapsedY = -20f; // 面板收起时按钮的Y位置
    
    [Header("功能按钮")]
    [SerializeField] private Button heartShopButton;   // 爱心商店按钮
    [SerializeField] private Button toolsButton;       // 工具背包按钮
    [SerializeField] private Button houseButton;       // 家装按钮
    [SerializeField] private Button settingsButton;    // 设置按钮
    
    [Header("爱心商店按钮组件")]
    [SerializeField] private Image heartShopBackgroundImage;    // 爱心商店按钮底图
    [SerializeField] private Image heartShopIconImage;          // 爱心商店购物车图片
    [SerializeField] private Sprite heartShopBgNormalSprite;    // 按钮底图正常状态
    [SerializeField] private Sprite heartShopBgPressedSprite;   // 按钮底图按下状态
    [SerializeField] private Sprite heartShopIconNormalSprite;  // 购物车图片正常状态
    [SerializeField] private Sprite heartShopIconPressedSprite; // 购物车图片按下状态
    
    [Header("其他按钮状态图片")]
    [SerializeField] private Sprite toolsNormalSprite;       // 工具正常状态
    [SerializeField] private Sprite toolsPressedSprite;      // 工具按下状态
    [SerializeField] private Sprite houseNormalSprite;       // 家装正常状态
    [SerializeField] private Sprite housePressedSprite;      // 家装按下状态
    [SerializeField] private Sprite settingsNormalSprite;    // 设置正常状态
    [SerializeField] private Sprite settingsPressedSprite;   // 设置按下状态
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private float collapsedYPosition = -200f; // 收起状态的Y位置（负值）
    [SerializeField] private float expandedYPosition = 0f; // 展开状态的Y位置
    
    // 面板当前状态
    private bool isExpanded = true;
    
    // 是否正在动画中
    private bool isAnimating = false;
    
    // 当前激活的功能类型
    public enum FunctionType
    {
        None,
        HeartShop,
        Tools,
        House,
        Settings
    }
    
    private FunctionType currentActiveFunction = FunctionType.None;
    
    // 缓存的系统设置面板实例
    private SystemSettingsPanel cachedSystemSettingsPanel;
    
    private void Start()
    {
        // 确保面板初始状态为展开
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, expandedYPosition);
        
        // 设置按钮初始位置
        if (buttonRect != null)
        {
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonExpandedY);
        }
        
        // 确保按钮图标正确
        UpdateButtonIcon();
        
        // 设置功能按钮初始状态为可点击
        SetFunctionButtonsInteractable(true);
        
        // 设置所有功能按钮事件
        SetupFunctionButtons();
        
        // 初始化所有按钮状态
        UpdateAllButtonStates();
        
        // 确保系统设置面板初始状态为关闭
        InitializeSystemSettingsPanel();
        
        // 订阅UIManager的工具包状态变化事件
        SubscribeToUIManagerEvents();
    }
    
    // 按钮点击事件处理
    public void TogglePanel()
    {
        // 如果正在动画中，不处理点击
        if (isAnimating) return;
        
        // 切换状态
        isExpanded = !isExpanded;
        
        // 更新按钮图标
        UpdateButtonIcon();
        
        // 立即设置功能按钮状态
        SetFunctionButtonsInteractable(isExpanded);
        
        // 执行面板动画
        AnimatePanel();
    }
    
    // 更新按钮图标
    private void UpdateButtonIcon()
    {
        if (toggleButtonImage != null)
        {
            // 切换图标
            toggleButtonImage.sprite = isExpanded ? collapseSprite : expandSprite;
        }
    }
    
    // 执行面板动画
    private void AnimatePanel()
    {
        isAnimating = true;
        
        // 目标Y位置
        float targetPanelY = isExpanded ? expandedYPosition : collapsedYPosition;
        float targetButtonY = isExpanded ? buttonExpandedY : buttonCollapsedY;
        
        // 使用协程实现动画
        StartCoroutine(AnimatePanelCoroutine(targetPanelY, targetButtonY));
    }
    
    // 协程实现动画
    private IEnumerator AnimatePanelCoroutine(float targetPanelY, float targetButtonY)
    {
        float startPanelY = panelRect.anchoredPosition.y;
        float startButtonY = buttonRect != null ? buttonRect.anchoredPosition.y : 0;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            float easedT = 1 - (1 - t) * (1 - t); // 简单的Out Quad缓动
            
            // 更新面板位置
            float newPanelY = Mathf.Lerp(startPanelY, targetPanelY, easedT);
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, newPanelY);
            
            // 更新按钮位置
            if (buttonRect != null)
            {
                float newButtonY = Mathf.Lerp(startButtonY, targetButtonY, easedT);
                buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, newButtonY);
            }
            
            yield return null;
        }
        
        // 确保最终位置精确
        panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, targetPanelY);
        
        if (buttonRect != null)
        {
            buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, targetButtonY);
        }
        
        isAnimating = false;
    }
    
    // 设置功能按钮可交互性
    private void SetFunctionButtonsInteractable(bool interactable)
    {
        if (heartShopButton != null) heartShopButton.interactable = interactable;
        if (toolsButton != null) toolsButton.interactable = interactable;
        if (houseButton != null) houseButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
    }
    
    // 可选：强制展开面板（用于外部调用）
    public void ExpandPanel(bool animate = true)
    {
        if (isExpanded) return; // 如果已经是展开状态，不做任何事
        
        isExpanded = true;
        UpdateButtonIcon();
        
        // 立即设置功能按钮状态
        SetFunctionButtonsInteractable(true);
        
        if (animate)
        {
            AnimatePanel();
        }
        else
        {
            // 直接设置到展开位置
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, expandedYPosition);
            
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonExpandedY);
            }
        }
    }
    
    // 可选：强制收起面板（用于外部调用）
    public void CollapsePanel(bool animate = true)
    {
        if (!isExpanded) return; // 如果已经是收起状态，不做任何事
        
        isExpanded = false;
        UpdateButtonIcon();
        
        // 立即设置功能按钮状态
        SetFunctionButtonsInteractable(false);
        
        if (animate)
        {
            AnimatePanel();
        }
        else
        {
            // 直接设置到收起位置
            panelRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, collapsedYPosition);
            
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonCollapsedY);
            }
        }
    }
    
    // 获取当前面板状态
    public bool IsExpanded()
    {
        return isExpanded;
    }
    
    /// <summary>
    /// 设置所有功能按钮事件
    /// </summary>
    private void SetupFunctionButtons()
    {
        if (heartShopButton != null)
        {
            heartShopButton.onClick.AddListener(() => OnFunctionButtonClick(FunctionType.HeartShop));
        }
        
        if (toolsButton != null)
        {
            toolsButton.onClick.AddListener(() => OnFunctionButtonClick(FunctionType.Tools));
        }
        
        if (houseButton != null)
        {
            houseButton.onClick.AddListener(() => OnFunctionButtonClick(FunctionType.House));
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => OnFunctionButtonClick(FunctionType.Settings));
        }
    }
    
    /// <summary>
    /// 功能按钮点击事件
    /// </summary>
    private void OnFunctionButtonClick(FunctionType functionType)
    {
        // 如果点击的是当前激活的功能，则关闭它
        if (currentActiveFunction == functionType)
        {
            CloseFunctionPanel(functionType);
            SetActiveFunction(FunctionType.None);
            return;
        }
        
        // 关闭当前激活的功能（如果有）
        if (currentActiveFunction != FunctionType.None)
        {
            CloseFunctionPanel(currentActiveFunction);
        }
        
        // 打开新的功能
        OpenFunctionPanel(functionType);
        SetActiveFunction(functionType);
    }
    
    /// <summary>
    /// 设置当前激活的功能并更新按钮状态
    /// </summary>
    private void SetActiveFunction(FunctionType functionType)
    {
        currentActiveFunction = functionType;
        UpdateAllButtonStates();
    }
    
    /// <summary>
    /// 更新所有按钮的状态
    /// </summary>
    private void UpdateAllButtonStates()
    {
        // 特殊处理爱心商店按钮（复合结构）
        UpdateHeartShopButtonState(currentActiveFunction == FunctionType.HeartShop);
        
        // 处理其他简单按钮
        UpdateButtonState(toolsButton, toolsNormalSprite, toolsPressedSprite, currentActiveFunction == FunctionType.Tools);
        UpdateButtonState(houseButton, houseNormalSprite, housePressedSprite, currentActiveFunction == FunctionType.House);
        UpdateButtonState(settingsButton, settingsNormalSprite, settingsPressedSprite, currentActiveFunction == FunctionType.Settings);
    }
    
    /// <summary>
    /// 更新爱心商店按钮的复合状态
    /// </summary>
    private void UpdateHeartShopButtonState(bool isPressed)
    {
        // 更新按钮底图
        if (heartShopBackgroundImage != null && heartShopBgNormalSprite != null && heartShopBgPressedSprite != null)
        {
            heartShopBackgroundImage.sprite = isPressed ? heartShopBgPressedSprite : heartShopBgNormalSprite;
        }
        
        // 更新购物车图片
        if (heartShopIconImage != null && heartShopIconNormalSprite != null && heartShopIconPressedSprite != null)
        {
            heartShopIconImage.sprite = isPressed ? heartShopIconPressedSprite : heartShopIconNormalSprite;
        }
        
        // 爱心货币数量文本不变，由其他系统管理
    }
    
    /// <summary>
    /// 更新单个按钮的状态
    /// </summary>
    private void UpdateButtonState(Button button, Sprite normalSprite, Sprite pressedSprite, bool isPressed)
    {
        if (button == null) return;
        
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && normalSprite != null && pressedSprite != null)
        {
            buttonImage.sprite = isPressed ? pressedSprite : normalSprite;
        }
    }
    
    /// <summary>
    /// 打开指定功能面板
    /// </summary>
    private void OpenFunctionPanel(FunctionType functionType)
    {
        switch (functionType)
        {
            case FunctionType.HeartShop:
                if (HeartShopController.Instance != null)
                {
                    HeartShopController.Instance.OpenShop();
                }
                else
                {
                    Debug.LogWarning("HeartShopController未找到！");
                }
                break;
                
            case FunctionType.Tools:
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenToolkit();
                }
                else
                {
                    Debug.LogWarning("UIManager未找到！");
                }
                break;
                
            case FunctionType.House:
                // TODO: 实现家装面板打开逻辑
                // Debug.Log("打开家装面板");
                break;
                
            case FunctionType.Settings:
                OpenSystemSettingsPanel();
                break;
        }
    }
    
    /// <summary>
    /// 关闭指定功能面板
    /// </summary>
    private void CloseFunctionPanel(FunctionType functionType)
    {
        switch (functionType)
        {
            case FunctionType.HeartShop:
                if (HeartShopController.Instance != null)
                {
                    HeartShopController.Instance.CloseShop();
                }
                break;
                
            case FunctionType.Tools:
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.CloseToolkit();
                }
                break;
                
            case FunctionType.House:
                // TODO: 实现家装面板关闭逻辑
                // Debug.Log("关闭家装面板");
                break;
                
            case FunctionType.Settings:
                CloseSystemSettingsPanel();
                break;
        }
    }
    
    /// <summary>
    /// 确保系统设置面板初始状态为关闭
    /// </summary>
    private void InitializeSystemSettingsPanel()
    {
        cachedSystemSettingsPanel = FindObjectOfType<SystemSettingsPanel>();
        if (cachedSystemSettingsPanel != null)
        {
            // 等待SystemSettingsPanel完成自己的初始化
            StartCoroutine(InitializeSystemSettingsPanelDelayed());
        }
        else
        {
            Debug.LogError("场景中未找到SystemSettingsPanel！请确保场景中存在SystemSettingsPanel组件。");
        }
    }
    
    /// <summary>
    /// 延迟初始化系统设置面板状态
    /// </summary>
    private System.Collections.IEnumerator InitializeSystemSettingsPanelDelayed()
    {
        // 等待一帧，确保SystemSettingsPanel的Start方法已执行完成
        yield return null;
        
        if (cachedSystemSettingsPanel != null)
        {
            // 调用面板的HidePanel方法设置正确的隐藏状态
            cachedSystemSettingsPanel.HidePanel(false); // 不使用动画，立即设置为隐藏状态
            cachedSystemSettingsPanel.gameObject.SetActive(false);
            // Debug.Log("系统设置面板已初始化为关闭状态");
        }
    }
    
    /// <summary>
    /// 打开系统设置面板
    /// </summary>
    private void OpenSystemSettingsPanel()
    {
        if (cachedSystemSettingsPanel == null)
        {
            Debug.LogError("系统设置面板未找到！请确保场景中存在SystemSettingsPanel组件。");
            return;
        }
        
        // 激活面板并播放进入动画
        cachedSystemSettingsPanel.gameObject.SetActive(true);
        cachedSystemSettingsPanel.ShowPanel(true);
        // Debug.Log("系统设置面板已打开");
    }
    
    /// <summary>
    /// 关闭系统设置面板
    /// </summary>
    private void CloseSystemSettingsPanel()
    {
        if (cachedSystemSettingsPanel == null)
        {
            Debug.LogError("系统设置面板未找到！");
            return;
        }
        
        cachedSystemSettingsPanel.HidePanel(true);
        // Debug.Log("系统设置面板已关闭");
    }
    
    /// <summary>
    /// 外部调用：通知某个功能面板被关闭
    /// </summary>
    public void OnFunctionPanelClosed(FunctionType functionType)
    {
        if (currentActiveFunction == functionType)
        {
            SetActiveFunction(FunctionType.None);
        }
    }
    
    /// <summary>
    /// 获取当前激活的功能类型
    /// </summary>
    public FunctionType GetCurrentActiveFunction()
    {
        return currentActiveFunction;
    }
    
    /// <summary>
    /// 手动更新爱心商店按钮状态（供外部调用）
    /// </summary>
    /// <param name="isPressed">是否为按下状态</param>
    public void ManualUpdateHeartShopButtonState(bool isPressed)
    {
        UpdateHeartShopButtonState(isPressed);
    }
    
    /// <summary>
    /// 获取爱心商店按钮的组件引用（供外部系统更新货币显示等）
    /// </summary>
    public Button GetHeartShopButton()
    {
        return heartShopButton;
    }

    /// <summary>
    /// 订阅UIManager的事件
    /// </summary>
    private void SubscribeToUIManagerEvents()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged += OnToolkitStateChanged;
        }
    }

    /// <summary>
    /// 取消订阅UIManager的事件
    /// </summary>
    private void UnsubscribeFromUIManagerEvents()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged -= OnToolkitStateChanged;
        }
    }

    /// <summary>
    /// 响应工具包状态变化
    /// </summary>
    /// <param name="isOpen">工具包是否打开</param>
    private void OnToolkitStateChanged(bool isOpen)
    {
        // 当工具包关闭时，如果当前激活的功能是Tools，则重置按钮状态
        if (!isOpen && currentActiveFunction == FunctionType.Tools)
        {
            SetActiveFunction(FunctionType.None);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅UIManager事件
        UnsubscribeFromUIManagerEvents();
    }
} 