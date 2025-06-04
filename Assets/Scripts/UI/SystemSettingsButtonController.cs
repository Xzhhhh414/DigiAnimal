using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 系统设置按钮控制器 - 系统设置功能入口
/// </summary>
public class SystemSettingsButtonController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button settingsButton;                 // 设置按钮
    
    [Header("Toast提示文本")]
    [SerializeField] private string toastMessage = "系统设置功能开发中，敬请期待！";
    
    private void Awake()
    {
        // 自动查找组件（如果没有在Inspector中设置）
        if (settingsButton == null)
            settingsButton = GetComponent<Button>();
    }
    
    private void Start()
    {
        // 设置按钮点击事件
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClick);
        }
        else
        {
            Debug.LogWarning("SystemSettingsButtonController: 未找到Button组件！");
        }
    }
    
    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void OnSettingsButtonClick()
    {
        Debug.Log("系统设置按钮被点击！");
        
        // 显示Toast提示
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(toastMessage);
        }
        else
        {
            Debug.LogWarning("未找到ToastManager！");
            Debug.Log("系统设置功能开发中...");
        }
    }
    
    /// <summary>
    /// 设置按钮可交互性
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (settingsButton != null)
        {
            settingsButton.interactable = interactable;
        }
    }
    
    /// <summary>
    /// 设置Toast提示文本
    /// </summary>
    /// <param name="message">提示文本</param>
    public void SetToastMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            toastMessage = message;
        }
    }
} 