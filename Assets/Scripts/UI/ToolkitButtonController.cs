using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 工具包入口按钮控制器
/// </summary>
public class ToolkitButtonController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button toolkitButton;
    [SerializeField] private Image buttonImage;
    
    [Header("按钮图片")]
    [SerializeField] private Sprite defaultSprite; // 默认状态图片
    [SerializeField] private Sprite activeSprite;  // 激活状态图片
    
    private void Awake()
    {
        // 如果没有指定Button组件，尝试获取
        if (toolkitButton == null)
        {
            toolkitButton = GetComponent<Button>();
        }
        
        // 如果没有指定Image组件，尝试获取
        if (buttonImage == null && toolkitButton != null)
        {
            buttonImage = toolkitButton.GetComponent<Image>();
        }
        
        // 添加按钮点击事件监听
        if (toolkitButton != null)
        {
            toolkitButton.onClick.AddListener(OnButtonClick);
        }
        
        // 设置初始状态
        UpdateButtonState(false);
    }
    
    private void OnEnable()
    {
        // 订阅工具包状态改变事件
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged += UpdateButtonState;
            
            // 同步初始状态
            UpdateButtonState(UIManager.Instance.IsToolkitOpen);
        }
    }
    
    private void OnDisable()
    {
        // 取消订阅工具包状态改变事件
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnToolkitStateChanged -= UpdateButtonState;
        }
    }
    
    private void OnDestroy()
    {
        // 移除按钮点击事件监听
        if (toolkitButton != null)
        {
            toolkitButton.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    private void OnButtonClick()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleToolkit();
        }
    }
    
    /// <summary>
    /// 更新按钮状态
    /// </summary>
    /// <param name="isActive">是否激活状态</param>
    private void UpdateButtonState(bool isActive)
    {
        if (buttonImage != null && defaultSprite != null && activeSprite != null)
        {
            buttonImage.sprite = isActive ? activeSprite : defaultSprite;
        }
    }
} 