using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 工具使用面板控制器 - 控制底部工具使用提示面板的显示和隐藏
/// </summary>
public class ToolUsePanelController : MonoBehaviour
{
    [Header("面板引用")]
    [SerializeField] private Text instructionText; // 使用说明文本
    [SerializeField] private Button cancelButton;  // 取消按钮

    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 showPosition;
    private Vector2 hidePosition;
    
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
        showPosition = rectTransform.anchoredPosition;
        
        // 设置隐藏位置（向下偏移）
        hidePosition = new Vector2(showPosition.x, showPosition.y - 100f);
        
        // 设置取消按钮监听
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        }
        
        // 初始状态隐藏
        HidePanel(false);
    }
    
    /// <summary>
    /// 设置使用说明文本
    /// </summary>
    public void SetInstructionText(string text)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }
    }
    
    /// <summary>
    /// 显示面板
    /// </summary>
    public void ShowPanel(bool animated = true)
    {
        gameObject.SetActive(true);
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = showPosition;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            return;
        }
        
        // 设置初始状态
        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = hidePosition;
        
        // 创建动画
        Sequence sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(showPosition, animationDuration).SetEase(animationEase));
        sequence.Join(canvasGroup.DOFade(1, animationDuration * 0.7f));
        
        // 确保面板可交互
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// 隐藏面板
    /// </summary>
    public void HidePanel(bool animated = true)
    {
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 0;
            rectTransform.anchoredPosition = hidePosition;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            return;
        }
        
        // 创建动画
        Sequence sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(hidePosition, animationDuration).SetEase(animationEase));
        sequence.Join(canvasGroup.DOFade(0, animationDuration * 0.7f));
        
        // 动画完成后禁用面板
        sequence.OnComplete(() => {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        });
    }
    
    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelButtonClick()
    {
        if (ToolInteractionManager.Instance != null)
        {
            ToolInteractionManager.Instance.CancelToolUse();
        }
    }
    
    private void OnDestroy()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(OnCancelButtonClick);
        }
    }
} 