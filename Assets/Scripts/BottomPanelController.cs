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
    [SerializeField] private Button[] functionButtons; // 面板上的功能按钮数组
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private float collapsedYPosition = -200f; // 收起状态的Y位置（负值）
    [SerializeField] private float expandedYPosition = 0f; // 展开状态的Y位置
    
    // 面板当前状态
    private bool isExpanded = true;
    
    // 是否正在动画中
    private bool isAnimating = false;
    
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
        if (functionButtons == null || functionButtons.Length == 0) return;
        
        foreach (Button button in functionButtons)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
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
} 