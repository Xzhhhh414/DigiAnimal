using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // 添加DOTween支持

public enum PetNeedType
{
    None = 0,
    Hungry = 1,
    Tired = 2,
    Happy = 3,      // 开心状态
    Indifferent = 4, // 无感状态
    Curious = 5     // 好奇状态
    // 后续可添加更多需求类型
}

public class NeedBubbleController : MonoBehaviour
{
    [Header("气泡引用")]
    [SerializeField] private GameObject bubbleObject;
    [SerializeField] private SpriteRenderer statusIconRenderer;
    
    [Header("状态图标")]
    [SerializeField] private Sprite hungryIcon;
    [SerializeField] private Sprite tiredIcon;
    [SerializeField] private Sprite happyIcon;        // 开心图标
    [SerializeField] private Sprite indifferentIcon;  // 无感图标
    [SerializeField] private Sprite curiousIcon;      // 好奇图标
    // 其他状态图标...
    
    [Header("浮动效果设置")]
    [SerializeField] private float floatSpeed = 1.0f;
    [SerializeField] private float floatAmount = 0.1f;
    
    [Header("动画设置")]
    [SerializeField] private float fadeInDuration = 0.4f; // 淡入时长
    [SerializeField] private float fadeOutDuration = 0.4f; // 淡出时长
    [SerializeField] private float scaleInDuration = 0.3f; // 缩放进入时长
    [SerializeField] private float scaleOutDuration = 0.4f; // 缩放退出时长（与淡出同步）
    
    private PetNeedType currentNeed = PetNeedType.None;
    private Vector3 startPosition;
    private Vector3 originalScale; // 原始缩放
    private CanvasGroup canvasGroup; // 用于淡入淡出控制
    private bool isAnimating = false; // 是否正在播放动画
    
    // 优先级配置，数值越高优先级越高
    private Dictionary<PetNeedType, int> needPriorities = new Dictionary<PetNeedType, int>
    {
        { PetNeedType.None, 0 },
        { PetNeedType.Hungry, 100 },
        { PetNeedType.Tired, 90 },
        { PetNeedType.Happy, 80 },        // 开心状态优先级
        { PetNeedType.Curious, 75 },      // 好奇状态优先级
        { PetNeedType.Indifferent, 70 },  // 无感状态优先级
        // 可添加更多需求类型及其优先级
    };
    
    private void Start()
    {
        // 初始时隐藏气泡并保存初始位置
        if (bubbleObject != null)
        {
            startPosition = bubbleObject.transform.localPosition;
            originalScale = bubbleObject.transform.localScale;
            
            // 获取或添加CanvasGroup组件
            canvasGroup = bubbleObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bubbleObject.AddComponent<CanvasGroup>();
            }
            
            // 确保气泡初始隐藏
            currentNeed = PetNeedType.None;
            bubbleObject.SetActive(false);
            canvasGroup.alpha = 0f;
            bubbleObject.transform.localScale = Vector3.zero;
        }
    }
    
    private void Update()
    {
        // 如果气泡可见且不在播放动画，应用浮动效果
        if (bubbleObject != null && bubbleObject.activeSelf && !isAnimating && currentNeed != PetNeedType.None)
        {
            // 使用正弦函数创建上下浮动效果
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            bubbleObject.transform.localPosition = new Vector3(
                startPosition.x, 
                newY, 
                startPosition.z);
        }
    }
    
    /// <summary>
    /// 显示指定类型的需求气泡
    /// </summary>
    /// <param name="needType">需求类型</param>
    public void ShowNeed(PetNeedType needType)
    {
        // 安全检查
        if (needType == PetNeedType.None) return;
        
        // 如果当前没有需求显示，或者新需求优先级更高
        if (currentNeed == PetNeedType.None || 
            needPriorities[needType] > needPriorities[currentNeed])
        {
            // 如果正在播放动画，先停止
            if (isAnimating)
        {
                DOTween.Kill(bubbleObject.transform);
                DOTween.Kill(canvasGroup);
            }
            
            // 更新当前需求
            currentNeed = needType;
            
            // 根据需求类型设置相应的图标
            if (statusIconRenderer != null)
            {
            switch (needType)
            {
                case PetNeedType.Hungry:
                    statusIconRenderer.sprite = hungryIcon;
                    break;
                case PetNeedType.Tired:
                    statusIconRenderer.sprite = tiredIcon;
                    break;
                case PetNeedType.Happy:
                    statusIconRenderer.sprite = happyIcon;
                    break;
                case PetNeedType.Curious:
                    statusIconRenderer.sprite = curiousIcon;
                    break;
                case PetNeedType.Indifferent:
                    statusIconRenderer.sprite = indifferentIcon;
                    break;
                // 其他需求类型...
                }
            }
            
            // 显示气泡并播放淡入动画
            if (bubbleObject != null && canvasGroup != null)
            {
                bubbleObject.SetActive(true);
                
                // 设置初始状态
                canvasGroup.alpha = 0f;
                bubbleObject.transform.localScale = Vector3.zero;
                
                isAnimating = true;
                
                // 创建动画序列
                Sequence sequence = DOTween.Sequence();
                
                // 同时进行淡入和缩放
                sequence.Append(canvasGroup.DOFade(1f, fadeInDuration));
                sequence.Join(bubbleObject.transform.DOScale(originalScale, scaleInDuration).SetEase(Ease.OutBack));
                
                // 动画完成回调
                sequence.OnComplete(() => {
                    isAnimating = false;
                });
            }
        }
    }
    
    /// <summary>
    /// 隐藏指定类型的需求气泡
    /// </summary>
    /// <param name="needType">需求类型</param>
    public void HideNeed(PetNeedType needType)
    {
        // 如果要隐藏的正是当前显示的需求
        if (needType == currentNeed)
        {
            // 播放淡出动画
            HideWithAnimation();
        }
    }
    
    /// <summary>
    /// 隐藏所有需求气泡
    /// </summary>
    public void HideAllNeeds()
    {
        // 播放淡出动画
        HideWithAnimation();
    }
    
    /// <summary>
    /// 播放隐藏动画
    /// </summary>
    private void HideWithAnimation()
    {
        if (bubbleObject != null && canvasGroup != null && currentNeed != PetNeedType.None)
        {
            // 如果正在播放动画，先停止
            if (isAnimating)
            {
                DOTween.Kill(bubbleObject.transform);
                DOTween.Kill(canvasGroup);
            }
            
            isAnimating = true;
            
            // 创建隐藏动画序列
            Sequence sequence = DOTween.Sequence();
            
            // 同时进行淡出和缩放
            sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration));
            sequence.Join(bubbleObject.transform.DOScale(Vector3.zero, scaleOutDuration).SetEase(Ease.InBack));
            
            // 动画完成后隐藏对象
            sequence.OnComplete(() => {
            bubbleObject.SetActive(false);
                currentNeed = PetNeedType.None;
                isAnimating = false;
            });
        }
    }
    
    /// <summary>
    /// 获取当前显示的需求类型
    /// </summary>
    public PetNeedType GetCurrentNeed()
    {
        return currentNeed;
    }
    
    /// <summary>
    /// 立即隐藏气泡（无动画）
    /// </summary>
    public void HideImmediately()
    {
        if (bubbleObject != null)
        {
            // 停止所有动画
            DOTween.Kill(bubbleObject.transform);
            DOTween.Kill(canvasGroup);
            
            // 立即隐藏
            bubbleObject.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            bubbleObject.transform.localScale = Vector3.zero;
            
            // 重置状态
            currentNeed = PetNeedType.None;
            isAnimating = false;
        }
    }
} 