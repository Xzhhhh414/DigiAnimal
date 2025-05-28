using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.EventSystems;

// 添加DefaultExecutionOrder属性，确保UIManager最先初始化(-100表示比默认顺序更早执行)
[DefaultExecutionOrder(-100)]
public class UIManager : MonoBehaviour
{
    [Header("UI引用")]
    public Canvas gameCanvas;
    
    [Header("UI面板")]
    [SerializeField] private SelectedPetInfo selectedPetInfoPanel;
    [SerializeField] private SelectedFoodInfo selectedFoodInfoPanel;
    
    // 单例模式 - 添加静态引用初始化
    private static UIManager _instance;
    public static UIManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
            }
            return _instance;
        }
        private set { _instance = value; }
    }
    
    #region 工具包管理
    // 当前工具包是否打开
    public bool IsToolkitOpen { get; private set; } = false;
    
    // 是否处于工具使用模式
    public bool IsInToolUseMode { get; private set; } = false;
    
    // 工具包状态改变事件委托
    public delegate void ToolkitStateChangedHandler(bool isOpen);
    
    // 工具使用模式状态改变事件委托
    public delegate void ToolUseModeChangedHandler(bool isInToolUseMode);
    
    // 工具包状态改变事件
    public event ToolkitStateChangedHandler OnToolkitStateChanged;
    
    // 工具使用模式状态改变事件
    public event ToolUseModeChangedHandler OnToolUseModeChanged;
    
    /// <summary>
    /// 打开工具包
    /// </summary>
    public void OpenToolkit()
    {
        if (!IsToolkitOpen)
        {
            IsToolkitOpen = true;
            //Debug.Log("UIManager: 打开工具包");
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
            //Debug.Log("UIManager: 关闭工具包");
            OnToolkitStateChanged?.Invoke(false);
        }
    }
    
    /// <summary>
    /// 切换工具包状态
    /// </summary>
    public void ToggleToolkit()
    {
        IsToolkitOpen = !IsToolkitOpen;
        //Debug.Log($"UIManager: 工具包状态切换为: {IsToolkitOpen}");
        OnToolkitStateChanged?.Invoke(IsToolkitOpen);
    }
    
    /// <summary>
    /// 进入工具使用模式
    /// </summary>
    public void EnterToolUseMode()
    {
        if (!IsInToolUseMode)
        {
            IsInToolUseMode = true;
            OnToolUseModeChanged?.Invoke(true);
            
            // 隐藏所有可能的UI
            HideAllUI();
        }
    }
    
    /// <summary>
    /// 退出工具使用模式
    /// </summary>
    public void ExitToolUseMode()
    {
        if (IsInToolUseMode)
        {
            IsInToolUseMode = false;
            OnToolUseModeChanged?.Invoke(false);
            
            // 恢复原有UI
            RestoreUI();
        }
    }
    
    // 存储隐藏前的UI状态
    private Dictionary<GameObject, bool> hiddenUIStates = new Dictionary<GameObject, bool>();
    
    // 隐藏所有UI（但保留工具使用面板）
    private void HideAllUI()
    {
        // 清空之前的状态记录
        hiddenUIStates.Clear();
        
        // 隐藏底部面板
        var bottomPanel = FindObjectOfType<BottomPanelController>();
        if (bottomPanel != null)
        {
            hiddenUIStates[bottomPanel.gameObject] = bottomPanel.gameObject.activeSelf;
            bottomPanel.gameObject.SetActive(false);
        }
        
        // 隐藏工具包面板（但不改变工具包的打开状态）
        var toolkitPanel = FindObjectOfType<ToolkitPanelController>();
        if (toolkitPanel != null)
        {
            hiddenUIStates[toolkitPanel.gameObject] = toolkitPanel.gameObject.activeSelf;
            toolkitPanel.gameObject.SetActive(false);
        }
        
        // 隐藏选中宠物信息面板
        if (selectedPetInfoPanel != null && selectedPetInfoPanel.gameObject.activeSelf)
        {
            hiddenUIStates[selectedPetInfoPanel.gameObject] = true;
            selectedPetInfoPanel.gameObject.SetActive(false);
        }
        
        // 隐藏选中食物信息面板
        if (selectedFoodInfoPanel != null && selectedFoodInfoPanel.gameObject.activeSelf)
        {
            hiddenUIStates[selectedFoodInfoPanel.gameObject] = true;
            selectedFoodInfoPanel.gameObject.SetActive(false);
        }
        
        // 隐藏工具包按钮
        var toolkitButton = FindObjectOfType<ToolkitButtonController>();
        if (toolkitButton != null)
        {
            hiddenUIStates[toolkitButton.gameObject] = toolkitButton.gameObject.activeSelf;
            toolkitButton.gameObject.SetActive(false);
        }
        
        // 可以在这里添加更多需要隐藏的UI组件
        // 例如：顶部状态栏、侧边栏等
    }
    
    // 恢复原有UI
    private void RestoreUI()
    {
        // 恢复所有之前隐藏的UI组件
        foreach (var kvp in hiddenUIStates)
        {
            if (kvp.Key != null && kvp.Value)
            {
                kvp.Key.SetActive(true);
            }
        }
        
        // 清空状态记录
        hiddenUIStates.Clear();
    }
    #endregion
    
    private void Awake()
    {
        //Debug.Log("UIManager: Awake开始执行");
        
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //Debug.Log("UIManager: 单例初始化完成");
        }
        else if (Instance != this)
        {
            //Debug.Log("UIManager: 销毁重复实例");
            Destroy(gameObject);
            return;
        }
        
        // 初始化UI组件
        InitializeUI();
        
        //Debug.Log("UIManager: Awake执行完成");
    }
    
    // 在编辑器中验证组件设置
    void OnValidate()
    {
        // 确保在编辑器中有正确的设置
        // 移除了警告日志
    }
    
    public void Start()
    {
        //Debug.Log("UIManager: Start开始执行");
        
        // 确保GameCanvas存在
        if (gameCanvas == null)
        {
            // 自动查找Canvas，不再输出错误日志
            gameCanvas = FindObjectOfType<Canvas>();
        }
        
        //Debug.Log("UIManager: Start执行完成");
    }
    
    // 初始化UI组件
    private void InitializeUI()
    {
        // 查找SelectedPetInfo面板（如果没有通过Inspector设置）
        if (selectedPetInfoPanel == null)
        {
            selectedPetInfoPanel = FindObjectOfType<SelectedPetInfo>();
            
            if (selectedPetInfoPanel == null)
            {
                Debug.LogWarning("UIManager: 未找到SelectedPetInfo面板，某些功能可能无法正常工作。");
            }
        }
        
        // 查找SelectedFoodInfo面板（如果没有通过Inspector设置）
        if (selectedFoodInfoPanel == null)
        {
            selectedFoodInfoPanel = FindObjectOfType<SelectedFoodInfo>();
            
            if (selectedFoodInfoPanel == null)
            {
                Debug.LogWarning("UIManager: 未找到SelectedFoodInfo面板，某些功能可能无法正常工作。");
            }
        }
    }
    
    // 显示选中宠物信息面板
    public void ShowSelectedPetInfo(PetController2D pet)
    {
        if (selectedPetInfoPanel != null)
        {
            selectedPetInfoPanel.gameObject.SetActive(true);
        }
    }
    
    // 隐藏选中宠物信息面板
    public void HideSelectedPetInfo()
    {
        if (selectedPetInfoPanel != null)
        {
            selectedPetInfoPanel.gameObject.SetActive(false);
        }
    }
    
    // 显示选中食物信息面板
    public void ShowSelectedFoodInfo(FoodController food)
    {
        if (selectedFoodInfoPanel != null)
        {
            selectedFoodInfoPanel.gameObject.SetActive(true);
        }
    }
    
    // 隐藏选中食物信息面板
    public void HideSelectedFoodInfo()
    {
        if (selectedFoodInfoPanel != null)
        {
            selectedFoodInfoPanel.gameObject.SetActive(false);
        }
    }
    
    // 检查当前鼠标/触摸点是否在UI元素上
    public bool IsPointerOverUI()
    {
        // 检查是否点击在UI元素上
        if (EventSystem.current == null)
            return false;
            
        // 使用Unity内置的IsPointerOverGameObject来检测
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
