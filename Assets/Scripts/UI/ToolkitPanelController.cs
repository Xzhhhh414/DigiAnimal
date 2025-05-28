using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 工具包弹窗控制器
/// </summary>
public class ToolkitPanelController : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button useToolButton; // 使用工具按钮
    
    [Header("工具按钮")]
    [SerializeField] private Button[] toolButtons; // 工具按钮数组（预期有3个）
    [SerializeField] private Image[] toolIcons; // 工具图标数组（与toolButtons对应）
    [SerializeField] private Text selectedToolDescription; // 选中工具的描述文本（单个组件）
    [SerializeField] private GameObject toolSelectionIndicator; // 工具选择指示器
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 50); // 隐藏时的位置偏移
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 shownPosition;
    private Sequence currentAnimation;
    
    // 当前选中的工具索引（-1表示未选择）
    private int selectedToolIndex = -1;
    
    private void Awake()
    {
        // 获取组件
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ToolkitPanelController: 无法获取RectTransform组件！");
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 验证引用设置
        ValidateReferences();
        
        // 保存显示位置
        shownPosition = rectTransform.anchoredPosition;
        
        // 添加关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }
        else
        {
            Debug.LogWarning("ToolkitPanelController: 未设置关闭按钮引用！");
        }
        
        // 添加使用工具按钮事件
        if (useToolButton != null)
        {
            useToolButton.onClick.AddListener(OnUseToolButtonClick);
            // 初始时使用按钮应该禁用，直到选择了工具
            useToolButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("ToolkitPanelController: 未设置使用工具按钮引用！");
        }
        
        // 设置工具按钮事件
        InitializeToolButtons();
        
        // 初始状态隐藏选择指示器
        if (toolSelectionIndicator != null)
        {
            toolSelectionIndicator.SetActive(false);
        }
        
        // 初始状态隐藏工具描述文本
        if (selectedToolDescription != null)
        {
            selectedToolDescription.text = string.Empty;
            selectedToolDescription.gameObject.SetActive(false);
        }
        
        // 初始状态隐藏(但不禁用游戏对象，只使其不可见和不可交互)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        }
        
        // 启动延迟初始化协程，确保订阅事件
        StartCoroutine(DelayedInitialization());
    }
    
    // 验证引用设置
    private void ValidateReferences()
    {
        // 验证工具按钮数组
        if (toolButtons == null || toolButtons.Length == 0)
        {
            Debug.LogError("ToolkitPanelController: toolButtons数组未设置或为空！");
        }
        else
        {
            for (int i = 0; i < toolButtons.Length; i++)
            {
                if (toolButtons[i] == null)
                {
                    Debug.LogError($"ToolkitPanelController: toolButtons[{i}] 引用为空！");
                }
            }
        }
        
        // 验证工具图标数组
        if (toolIcons == null || toolIcons.Length == 0)
        {
            Debug.LogError("ToolkitPanelController: toolIcons数组未设置或为空！");
        }
        else
        {
            for (int i = 0; i < toolIcons.Length; i++)
            {
                if (toolIcons[i] == null)
                {
                    Debug.LogError($"ToolkitPanelController: toolIcons[{i}] 引用为空！");
                }
            }
        }
        
        // 验证工具描述文本
        if (selectedToolDescription == null)
        {
            Debug.LogError("ToolkitPanelController: selectedToolDescription引用为空！");
        }
        
        // 验证数组长度是否一致
        if (toolButtons != null && toolIcons != null && selectedToolDescription != null)
        {
            if (toolButtons.Length != toolIcons.Length)
            {
                Debug.LogWarning($"ToolkitPanelController: 数组长度不一致！按钮:{toolButtons.Length}, 图标:{toolIcons.Length}");
            }
        }
    }
    
    // 初始化工具按钮
    private void InitializeToolButtons()
    {
        if (toolButtons == null || toolButtons.Length == 0)
        {
            Debug.LogWarning("ToolkitPanelController: 未设置工具按钮引用！");
            return;
        }
        
        for (int i = 0; i < toolButtons.Length; i++)
        {
            int index = i; // 捕获索引值用于事件回调
            if (toolButtons[i] != null)
            {
                toolButtons[i].onClick.AddListener(() => OnToolButtonClick(index));
            }
        }
        
        // 加载工具信息
        LoadToolInformation();
    }
    
    // 加载工具信息
    private void LoadToolInformation()
    {
        // 等待ToolInteractionManager初始化
        StartCoroutine(LoadToolInformationCoroutine());
    }
    
    private IEnumerator LoadToolInformationCoroutine()
    {
        // 等待ToolInteractionManager可用
        while (ToolInteractionManager.Instance == null)
        {
            yield return null;
        }
        
        // 获取工具信息并设置到按钮上
        var toolManager = ToolInteractionManager.Instance;
        
        // 通过反射或公共方法获取工具信息
        // 由于tools字段是private，我们需要添加一个公共方法来获取工具信息
        var tools = toolManager.GetTools();
        
        if (tools != null)
        {
            for (int i = 0; i < toolButtons.Length && i < tools.Length; i++)
            {
                // 设置工具图标
                if (toolIcons != null && i < toolIcons.Length && toolIcons[i] != null)
                {
                    toolIcons[i].sprite = tools[i].toolIcon;
                    toolIcons[i].gameObject.SetActive(tools[i].toolIcon != null);
                }
                
                // 启用按钮
                if (toolButtons[i] != null)
                {
                    toolButtons[i].interactable = true;
                }
            }
            
            // 禁用多余的按钮
            for (int i = tools.Length; i < toolButtons.Length; i++)
            {
                if (toolButtons[i] != null)
                {
                    toolButtons[i].interactable = false;
                }
                
                if (toolIcons != null && i < toolIcons.Length && toolIcons[i] != null)
                {
                    toolIcons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogError("ToolkitPanelController: 无法获取工具信息数组");
        }
    }
    
    // 工具按钮点击事件
    private void OnToolButtonClick(int toolIndex)
    {
        // 检查工具索引是否有效
        if (ToolInteractionManager.Instance == null)
        {
            Debug.LogError("ToolInteractionManager实例不存在！");
            return;
        }
        
        var tools = ToolInteractionManager.Instance.GetTools();
        if (tools == null || toolIndex >= tools.Length)
        {
            Debug.LogError($"工具索引无效: {toolIndex}");
            return;
        }
        
        // 如果已经选择了该工具，则取消选择
        if (selectedToolIndex == toolIndex)
        {
            UnselectTool();
            return;
        }
        
        // 选择新工具
        SelectTool(toolIndex);
    }
    
    // 选择工具
    private void SelectTool(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= toolButtons.Length)
            return;
        
        selectedToolIndex = toolIndex;
        
        // 显示选择指示器在选中的工具上
        if (toolSelectionIndicator != null)
        {
            toolSelectionIndicator.SetActive(true);
            toolSelectionIndicator.transform.position = toolButtons[toolIndex].transform.position;
        }
        
        // 启用使用按钮
        if (useToolButton != null)
        {
            useToolButton.interactable = true;
        }
        
        // 设置选中工具的描述
        if (selectedToolDescription != null && ToolInteractionManager.Instance != null)
        {
            var tools = ToolInteractionManager.Instance.GetTools();
            if (tools != null && toolIndex < tools.Length)
            {
                selectedToolDescription.text = tools[toolIndex].toolDescription;
                selectedToolDescription.gameObject.SetActive(!string.IsNullOrEmpty(tools[toolIndex].toolDescription));
            }
        }
    }
    
    // 取消选择工具
    private void UnselectTool()
    {
        selectedToolIndex = -1;
        
        // 隐藏选择指示器
        if (toolSelectionIndicator != null)
        {
            toolSelectionIndicator.SetActive(false);
        }
        
        // 禁用使用按钮
        if (useToolButton != null)
        {
            useToolButton.interactable = false;
        }
        
        // 清除选中工具的描述
        if (selectedToolDescription != null)
        {
            selectedToolDescription.text = string.Empty;
            selectedToolDescription.gameObject.SetActive(false);
        }
    }
    
    // 使用工具按钮点击事件
    private void OnUseToolButtonClick()
    {
        if (selectedToolIndex == -1)
            return;
        
        // 通知工具交互管理器进入工具使用状态
        if (ToolInteractionManager.Instance != null)
        {
            ToolInteractionManager.Instance.StartToolUse(selectedToolIndex);
        }
        
        // 不再手动关闭工具包，让UIManager的工具使用模式来处理UI的隐藏和恢复
        // 这样当工具使用结束时，工具包可以重新显示
    }
    
    // 协程：延迟初始化
    private IEnumerator DelayedInitialization()
    {
        // 等待一帧，确保所有其他Awake方法都执行完成
        yield return null;
        
        // 尝试订阅事件
        SubscribeToUIManagerEvents();
    }
    
    // 尝试订阅UIManager事件
    private void SubscribeToUIManagerEvents()
    {
        // 先确保没有重复订阅
        UnsubscribeFromUIManagerEvents();
        
        // 尝试获取UIManager实例并订阅事件
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged += OnToolkitStateChanged;
            
            // 同步当前状态
            bool isOpen = UIManager.Instance.IsToolkitOpen;
            if (isOpen)
            {
                ShowPanel();
            }
            else
            {
                HidePanel(false);
            }
        }
        else
        {
            Debug.LogError("ToolkitPanelController: 无法获取UIManager实例，请确保场景中有UIManager对象！");
        }
    }
    
    // 取消订阅UIManager事件
    private void UnsubscribeFromUIManagerEvents()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged -= OnToolkitStateChanged;
        }
    }
    
    private void OnEnable()
    {
        // 尝试重新订阅事件
        SubscribeToUIManagerEvents();
    }
    
    private void OnDisable()
    {
        // 不取消订阅，因为我们希望即使面板禁用也能接收事件
    }
    
    private void OnDestroy()
    {
        // 移除关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
        }
        
        // 移除使用工具按钮事件
        if (useToolButton != null)
        {
            useToolButton.onClick.RemoveListener(OnUseToolButtonClick);
        }
        
        // 移除工具按钮事件
        if (toolButtons != null)
        {
            for (int i = 0; i < toolButtons.Length; i++)
            {
                int index = i;
                if (toolButtons[i] != null)
                {
                    toolButtons[i].onClick.RemoveAllListeners();
                }
            }
        }
        
        // 取消订阅事件
        UnsubscribeFromUIManagerEvents();
        
        // 清理可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
    
    /// <summary>
    /// 响应工具包状态改变
    /// </summary>
    private void OnToolkitStateChanged(bool isOpen)
    {
        if (isOpen)
        {
            ShowPanel();
        }
        else
        {
            // 当面板关闭时，重置工具选择状态
            UnselectTool();
            HidePanel();
        }
    }
    
    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    private void OnCloseButtonClick()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseToolkit();
        }
    }
    
    /// <summary>
    /// 显示面板
    /// </summary>
    private void ShowPanel(bool animated = true)
    {
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 确保游戏对象处于激活状态 - 关键点，必须激活游戏对象才能看到UI
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
        // 确保面板处于可交互状态
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // 立即选中第一个工具（在动画开始前）
        SelectFirstToolIfAvailable();
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = shownPosition;
            return;
        }
        
        // 设置初始位置
        rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        canvasGroup.alpha = 0;
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡入动画
        currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition, animationDuration).SetEase(animationEase))
                       .Join(canvasGroup.DOFade(1, animationDuration * 0.7f));
        
        // 播放动画
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 默认选中第一个可用的工具
    /// </summary>
    private void SelectFirstToolIfAvailable()
    {
        // 检查是否有可用的工具
        if (ToolInteractionManager.Instance != null)
        {
            var tools = ToolInteractionManager.Instance.GetTools();
            if (tools != null && tools.Length > 0 && toolButtons != null && toolButtons.Length > 0)
            {
                // 选中第一个工具
                SelectTool(0);
            }
        }
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    private void HidePanel(bool animated = true)
    {
        // 如果游戏对象已经处于非激活状态，无需再次隐藏
        if (!gameObject.activeSelf)
        {
            return;
        }
        
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
            
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡出动画
        currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase))
                       .Join(canvasGroup.DOFade(0, animationDuration * 0.7f));
        
        // 动画完成后设置交互状态
        currentAnimation.OnComplete(() => {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
        
        // 播放动画
        currentAnimation.Play();
    }
} 