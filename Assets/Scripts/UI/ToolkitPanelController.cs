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
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 50); // 隐藏时的位置偏移
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 shownPosition;
    private Sequence currentAnimation;
    
    private void Awake()
    {
        Debug.Log("ToolkitPanelController: Awake开始执行");
        
        // 获取组件
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("ToolkitPanelController: 无法获取RectTransform组件！");
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.Log("ToolkitPanelController: 添加CanvasGroup组件");
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 保存显示位置
        shownPosition = rectTransform.anchoredPosition;
        Debug.Log($"ToolkitPanelController: 保存显示位置 = {shownPosition}");
        
        // 添加关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
            Debug.Log("ToolkitPanelController: 已添加关闭按钮事件");
        }
        else
        {
            Debug.LogWarning("ToolkitPanelController: 未设置关闭按钮引用！");
        }
        
        // 初始状态隐藏(但不禁用游戏对象，只使其不可见和不可交互)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        }
        Debug.Log("ToolkitPanelController: Awake完成，面板已设为不可见");
        
        // 启动延迟初始化协程，确保订阅事件
        StartCoroutine(DelayedInitialization());
    }
    
    // 协程：延迟初始化
    private IEnumerator DelayedInitialization()
    {
        Debug.Log("ToolkitPanelController: 开始延迟初始化");
        
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
            Debug.Log("ToolkitPanelController: 成功获取UIManager，订阅事件");
            UIManager.Instance.OnToolkitStateChanged += OnToolkitStateChanged;
            
            // 同步当前状态
            bool isOpen = UIManager.Instance.IsToolkitOpen;
            Debug.Log($"ToolkitPanelController: 同步当前工具包状态: {isOpen}");
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
        Debug.Log("ToolkitPanelController: OnEnable被调用");
        
        // 尝试重新订阅事件
        SubscribeToUIManagerEvents();
    }
    
    private void OnDisable()
    {
        Debug.Log("ToolkitPanelController: OnDisable被调用");
        // 不取消订阅，因为我们希望即使面板禁用也能接收事件
    }
    
    private void OnDestroy()
    {
        Debug.Log("ToolkitPanelController: OnDestroy被调用");
        
        // 移除关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
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
        Debug.Log($"ToolkitPanelController: 接收到工具包状态变化: {isOpen}");
        if (isOpen)
        {
            ShowPanel();
        }
        else
        {
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
        Debug.Log("ToolkitPanelController: 开始显示面板" + (animated ? "（带动画）" : "（无动画）"));
        
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
            Debug.Log("ToolkitPanelController: 激活游戏对象");
        }
        
        // 确保面板处于可交互状态
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = shownPosition;
            Debug.Log("ToolkitPanelController: 面板已立即显示（无动画）");
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
        Debug.Log("ToolkitPanelController: 面板显示动画已开始播放");
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    private void HidePanel(bool animated = true)
    {
        Debug.Log("ToolkitPanelController: 开始隐藏面板" + (animated ? "（带动画）" : "（无动画）"));
        
        // 如果游戏对象已经处于非激活状态，无需再次隐藏
        if (!gameObject.activeSelf)
        {
            Debug.Log("ToolkitPanelController: 游戏对象已经处于非激活状态，无需再次隐藏");
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
            
            // 注意：通常我们在无动画时会禁用游戏对象，但这可能导致事件订阅问题
            // 所以这里我们只设置为不可见，而不禁用游戏对象
            // gameObject.SetActive(false);
            
            Debug.Log("ToolkitPanelController: 面板已立即隐藏（无动画）");
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
            
            // 注意：我们保持游戏对象激活，只是使其不可见
            // gameObject.SetActive(false);
            
            Debug.Log("ToolkitPanelController: 隐藏动画完成，面板已设为不可见");
        });
        
        // 播放动画
        currentAnimation.Play();
        Debug.Log("ToolkitPanelController: 隐藏面板动画已开始播放");
    }
} 