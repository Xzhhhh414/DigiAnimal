using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 渐变消失并销毁对象的组件
/// 支持SpriteRenderer和CanvasGroup的渐变效果
/// </summary>
public class FadeOutDestroy : MonoBehaviour
{
    [Header("渐变设置")]
    [SerializeField] private float fadeOutDuration = 1f; // 渐变时长
    [SerializeField] private Ease fadeOutEase = Ease.InOutQuad; // 渐变曲线
    
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private CanvasGroup canvasGroup;
    private SpriteRenderer[] childSpriteRenderers; // 子对象的SpriteRenderer
    
    // 回调事件
    public System.Action OnFadeOutComplete;
    
    // 状态
    private bool isFading = false;
    
    private void Awake()
    {
        // 获取组件引用
        spriteRenderer = GetComponent<SpriteRenderer>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 获取所有子对象的SpriteRenderer（用于复杂的预制体）
        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }
    
    /// <summary>
    /// 开始渐变消失效果
    /// </summary>
    /// <param name="destroyAfterFade">渐变完成后是否销毁对象</param>
    /// <param name="onComplete">渐变完成回调</param>
    public void StartFadeOut(bool destroyAfterFade = true, System.Action onComplete = null)
    {
        if (isFading) return; // 防止重复调用
        
        isFading = true;
        
        // 合并回调
        System.Action finalCallback = () =>
        {
            OnFadeOutComplete?.Invoke();
            onComplete?.Invoke();
            
            if (destroyAfterFade)
            {
                Destroy(gameObject);
            }
        };
        
        // 根据可用组件选择渐变方式
        if (canvasGroup != null)
        {
            // 使用CanvasGroup渐变（适用于UI元素）
            FadeOutCanvasGroup(finalCallback);
        }
        else if (spriteRenderer != null || childSpriteRenderers.Length > 0)
        {
            // 使用SpriteRenderer渐变（适用于2D精灵）
            FadeOutSpriteRenderers(finalCallback);
        }
        else
        {
            // 没有可渐变的组件，直接执行回调
            Debug.LogWarning($"FadeOutDestroy: {gameObject.name} 没有找到可渐变的组件 (SpriteRenderer/CanvasGroup)");
            finalCallback?.Invoke();
        }
    }
    
    /// <summary>
    /// 使用CanvasGroup进行渐变
    /// </summary>
    private void FadeOutCanvasGroup(System.Action onComplete)
    {
        canvasGroup.DOFade(0f, fadeOutDuration)
            .SetEase(fadeOutEase)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    /// <summary>
    /// 使用SpriteRenderer进行渐变
    /// </summary>
    private void FadeOutSpriteRenderers(System.Action onComplete)
    {
        // 创建一个Sequence来同时渐变所有SpriteRenderer
        Sequence fadeSequence = DOTween.Sequence();
        
        // 主要的SpriteRenderer
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            fadeSequence.Join(spriteRenderer.DOColor(new Color(originalColor.r, originalColor.g, originalColor.b, 0f), fadeOutDuration)
                .SetEase(fadeOutEase));
        }
        
        // 所有子对象的SpriteRenderer
        foreach (var childRenderer in childSpriteRenderers)
        {
            if (childRenderer != null && childRenderer != spriteRenderer) // 避免重复处理主SpriteRenderer
            {
                Color originalColor = childRenderer.color;
                fadeSequence.Join(childRenderer.DOColor(new Color(originalColor.r, originalColor.g, originalColor.b, 0f), fadeOutDuration)
                    .SetEase(fadeOutEase));
            }
        }
        
        // 设置完成回调
        fadeSequence.OnComplete(() => onComplete?.Invoke());
    }
    
    /// <summary>
    /// 立即停止渐变并重置透明度
    /// </summary>
    public void StopFadeOut()
    {
        if (!isFading) return;
        
        // 停止所有DOTween动画
        transform.DOKill();
        
        // 重置透明度
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, 1f);
        }
        
        foreach (var childRenderer in childSpriteRenderers)
        {
            if (childRenderer != null)
            {
                Color color = childRenderer.color;
                childRenderer.color = new Color(color.r, color.g, color.b, 1f);
            }
        }
        
        isFading = false;
    }
    
    private void OnDestroy()
    {
        // 清理DOTween动画
        transform.DOKill();
    }
}