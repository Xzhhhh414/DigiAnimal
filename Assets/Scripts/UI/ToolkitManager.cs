using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 工具包管理器 - 单例类，用于协调工具包按钮和弹窗之间的通信
/// </summary>
public class ToolkitManager : MonoBehaviour
{
    // 单例实例
    public static ToolkitManager Instance { get; private set; }
    
    // 当前工具包是否打开
    public bool IsToolkitOpen { get; private set; } = false;
    
    // 工具包状态改变事件委托
    public delegate void ToolkitStateChangedHandler(bool isOpen);
    
    // 工具包状态改变事件
    public event ToolkitStateChangedHandler OnToolkitStateChanged;
    
    private void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 打开工具包
    /// </summary>
    public void OpenToolkit()
    {
        if (!IsToolkitOpen)
        {
            IsToolkitOpen = true;
            OnToolkitStateChanged?.Invoke(true);
        }
    }
    
    /// <summary>
    /// 关闭工具包
    /// </summary>
    public void CloseToolkit()
    {
        if (IsToolkitOpen)
        {
            IsToolkitOpen = false;
            OnToolkitStateChanged?.Invoke(false);
        }
    }
    
    /// <summary>
    /// 切换工具包状态
    /// </summary>
    public void ToggleToolkit()
    {
        IsToolkitOpen = !IsToolkitOpen;
        OnToolkitStateChanged?.Invoke(IsToolkitOpen);
    }
} 