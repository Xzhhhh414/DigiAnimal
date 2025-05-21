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
        // 获取组件
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 保存显示位置
        shownPosition = rectTransform.anchoredPosition;
        
        // 添加关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
        }
        
        // 初始状态隐藏
        HidePanel(false);
    }
    
    private void OnEnable()
    {
        // 订阅工具包状态改变事件
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged += OnToolkitStateChanged;
        }
    }
    
    private void OnDisable()
    {
        // 取消订阅工具包状态改变事件
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged -= OnToolkitStateChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 移除关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonClick);
        }
        
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
        
        // 激活面板
        gameObject.SetActive(true);
        
        // 确保面板处于可交互状态
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
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
    /// 隐藏面板
    /// </summary>
    private void HidePanel(bool animated = true)
    {
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
            gameObject.SetActive(false);
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡出动画
        currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase))
                       .Join(canvasGroup.DOFade(0, animationDuration * 0.7f));
        
        // 动画完成后设置交互状态并隐藏
        currentAnimation.OnComplete(() => {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        });
        
        // 播放动画
        currentAnimation.Play();
    }
} 