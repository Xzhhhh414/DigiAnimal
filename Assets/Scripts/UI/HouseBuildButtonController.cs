using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 房屋建造按钮控制器 - 房屋建造功能入口
/// </summary>
public class HouseBuildButtonController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button buildButton;                    // 建造按钮
    
    [Header("Toast提示文本")]
    [SerializeField] private string toastMessage = "房屋建造功能正在施工中，即将开放！";
    
    private void Awake()
    {
        // 自动查找组件（如果没有在Inspector中设置）
        if (buildButton == null)
            buildButton = GetComponent<Button>();
    }
    
    private void Start()
    {
        // 设置按钮点击事件
        if (buildButton != null)
        {
            buildButton.onClick.AddListener(OnBuildButtonClick);
        }
        else
        {
            Debug.LogWarning("HouseBuildButtonController: 未找到Button组件！");
        }
    }
    
    /// <summary>
    /// 建造按钮点击事件
    /// </summary>
    private void OnBuildButtonClick()
    {
        Debug.Log("房屋建造按钮被点击！");
        
        // 显示Toast提示
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(toastMessage);
        }
        else
        {
            Debug.LogWarning("未找到ToastManager！");
            Debug.Log("房屋建造功能开发中...");
        }
    }
    
    /// <summary>
    /// 设置按钮可交互性
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (buildButton != null)
        {
            buildButton.interactable = interactable;
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