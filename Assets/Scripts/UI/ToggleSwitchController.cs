using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

/// <summary>
/// 通用滑动开关控制器 - 现代风格的Toggle Switch
/// </summary>
public class ToggleSwitchController : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件引用")]
    [SerializeField] private Image backgroundImage;     // 背景图片
    [SerializeField] private RectTransform handleRect;  // 滑块的RectTransform
    [SerializeField] private Image handleImage;         // 滑块图片
    
    [Header("开关状态")]
    [SerializeField] private bool isOn = false;         // 当前开关状态
    
    [Header("背景图片设置")]
    [SerializeField] private Sprite onBackgroundSprite;    // 开启时背景图片
    [SerializeField] private Sprite offBackgroundSprite;   // 关闭时背景图片
    
    [Header("滑块图片设置")]
    [SerializeField] private Sprite onHandleSprite;        // 开启时滑块图片
    [SerializeField] private Sprite offHandleSprite;       // 关闭时滑块图片
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;    // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutQuart; // 动画缓动类型
    
    [Header("位置设置")]
    [Tooltip("滑块在开启状态时的X轴位置偏移。关闭状态会自动计算为负值。建议值：15-25")]
    [SerializeField] private float onPositionOffset = 20f;      // 开启状态的位置偏移
    
    // 计算的位置
    private float onPosition;
    private float offPosition;
    
    // 事件
    public event Action<bool> OnValueChanged;
    
    // 属性
    public bool IsOn
    {
        get => isOn;
        set => SetValue(value);
    }
    
    private void Awake()
    {
        // 计算滑块位置
        CalculateHandlePositions();
        
        // 设置初始状态（不触发事件）
        SetVisualState(isOn, false);
    }
    
    /// <summary>
    /// 计算滑块的开/关位置
    /// 新的计算方式：
    /// - 开启位置：+onPositionOffset（向右偏移）
    /// - 关闭位置：-onPositionOffset（向左偏移）
    /// - Handle在预制体中应该默认配置为开启状态的位置
    /// </summary>
    private void CalculateHandlePositions()
    {
        // 右侧位置（开启状态）
        onPosition = onPositionOffset;
        
        // 左侧位置（关闭状态）
        offPosition = -onPositionOffset;
    }
    
    /// <summary>
    /// 点击处理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }
    
    /// <summary>
    /// 切换开关状态
    /// </summary>
    public void Toggle()
    {
        SetValue(!isOn);
    }
    
    /// <summary>
    /// 设置开关值
    /// </summary>
    /// <param name="value">目标值</param>
    /// <param name="triggerEvent">是否触发事件</param>
    public void SetValue(bool value, bool triggerEvent = true)
    {
        if (isOn == value) return;
        
        isOn = value;
        SetVisualState(isOn, true);
        
        if (triggerEvent)
        {
            OnValueChanged?.Invoke(isOn);
        }
    }
    
    /// <summary>
    /// 设置视觉状态
    /// </summary>
    /// <param name="on">是否开启</param>
    /// <param name="animate">是否播放动画</param>
    private void SetVisualState(bool on, bool animate = true)
    {
        if (backgroundImage == null || handleRect == null) return;
        
        // 重新计算位置（防止UI大小变化）
        CalculateHandlePositions();
        
        // 选择目标图片和位置
        Sprite targetBackgroundSprite = on ? onBackgroundSprite : offBackgroundSprite;
        Sprite targetHandleSprite = on ? onHandleSprite : offHandleSprite;
        float targetPosition = on ? onPosition : offPosition;
        
        if (animate)
        {
            // 动画切换位置
            handleRect.DOAnchorPosX(targetPosition, animationDuration)
                .SetEase(animationEase);
        }
        else
        {
            // 立即设置位置
            handleRect.anchoredPosition = new Vector2(targetPosition, handleRect.anchoredPosition.y);
        }
        
        // 切换图片（立即切换，不需要动画）
        if (targetBackgroundSprite != null)
        {
            backgroundImage.sprite = targetBackgroundSprite;
        }
        
        if (targetHandleSprite != null && handleImage != null)
        {
            handleImage.sprite = targetHandleSprite;
        }
    }
    
    /// <summary>
    /// 在Editor中预览效果
    /// </summary>
    [ContextMenu("测试开关")]
    private void TestToggle()
    {
        Toggle();
    }
    
    /// <summary>
    /// 在Editor中设置为开启状态
    /// </summary>
    [ContextMenu("设置为开启")]
    private void SetToOn()
    {
        SetValue(true, false);
    }
    
    /// <summary>
    /// 在Editor中设置为关闭状态
    /// </summary>
    [ContextMenu("设置为关闭")]
    private void SetToOff()
    {
        SetValue(false, false);
    }
    
    private void OnValidate()
    {
        // 在Editor中实时更新
        if (Application.isPlaying) return;
        
        if (backgroundImage != null && handleRect != null)
        {
            CalculateHandlePositions();
            SetVisualState(isOn, false);
        }
    }
} 