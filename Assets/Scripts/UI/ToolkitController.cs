using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToolkitController : MonoBehaviour
{
    [Header("引用设置")]
    [SerializeField] private Button toolkitButton; // 底部工具包入口按钮
    [SerializeField] private GameObject toolkitPanel; // 工具包弹窗界面
    [SerializeField] private Button closeButton; // 弹窗上的关闭按钮
    
    [Header("按钮图片设置")]
    [SerializeField] private Image toolkitButtonImage; // 工具包按钮图片组件
    [SerializeField] private Sprite defaultButtonSprite; // 默认状态按钮图片
    [SerializeField] private Sprite activeButtonSprite; // 激活状态按钮图片
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 50); // 隐藏时的位置偏移
    
    private RectTransform panelRectTransform; // 弹窗的RectTransform
    private CanvasGroup panelCanvasGroup; // 弹窗的CanvasGroup
    private Vector2 shownPosition; // 弹窗显示位置
    private Sequence currentAnimation; // 当前动画序列
    private bool isToolkitOpen = false; // 工具包当前是否打开
    
    void Awake()
    {
        // 获取工具包弹窗组件
        if (toolkitPanel != null)
        {
            panelRectTransform = toolkitPanel.GetComponent<RectTransform>();
            panelCanvasGroup = toolkitPanel.GetComponent<CanvasGroup>();
            
            // 如果没有CanvasGroup，添加一个
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = toolkitPanel.AddComponent<CanvasGroup>();
            }
            
            // 保存显示位置
            shownPosition = panelRectTransform.anchoredPosition;
        }
        
        // 添加按钮事件监听
        if (toolkitButton != null)
        {
            toolkitButton.onClick.AddListener(ToggleToolkit);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseToolkit);
        }
        
        // 确保工具包开始时是关闭的
        HideToolkitPanel(false);
        UpdateButtonState(false);
    }
    
    void OnDestroy()
    {
        // 移除事件监听
        if (toolkitButton != null)
        {
            toolkitButton.onClick.RemoveListener(ToggleToolkit);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseToolkit);
        }
        
        // 清理可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
    }
    
    // 切换工具包状态
    public void ToggleToolkit()
    {
        isToolkitOpen = !isToolkitOpen;
        
        if (isToolkitOpen)
        {
            ShowToolkitPanel();
        }
        else
        {
            CloseToolkit();
        }
    }
    
    // 关闭工具包
    public void CloseToolkit()
    {
        isToolkitOpen = false;
        HideToolkitPanel();
        UpdateButtonState(false);
    }
    
    // 更新按钮状态和图片
    private void UpdateButtonState(bool isActive)
    {
        if (toolkitButtonImage != null)
        {
            toolkitButtonImage.sprite = isActive ? activeButtonSprite : defaultButtonSprite;
        }
    }
    
    // 显示工具包面板
    private void ShowToolkitPanel()
    {
        if (toolkitPanel == null) return;
        
        // 更新按钮状态
        UpdateButtonState(true);
        
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 激活面板
        toolkitPanel.SetActive(true);
        
        // 确保面板处于可交互状态
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        
        // 设置初始位置
        panelRectTransform.anchoredPosition = shownPosition + hiddenPosition;
        panelCanvasGroup.alpha = 0;
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡入动画
        currentAnimation.Join(panelRectTransform.DOAnchorPos(shownPosition, animationDuration).SetEase(animationEase))
                       .Join(panelCanvasGroup.DOFade(1, animationDuration * 0.7f));
        
        // 播放动画
        currentAnimation.Play();
    }
    
    // 隐藏工具包面板
    private void HideToolkitPanel(bool animated = true)
    {
        if (toolkitPanel == null) return;
        
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            panelCanvasGroup.alpha = 0;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            toolkitPanel.SetActive(false);
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡出动画
        currentAnimation.Join(panelRectTransform.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase))
                       .Join(panelCanvasGroup.DOFade(0, animationDuration * 0.7f));
        
        // 动画完成后设置交互状态
        currentAnimation.OnComplete(() => {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            toolkitPanel.SetActive(false);
        });
        
        // 播放动画
        currentAnimation.Play();
    }
} 