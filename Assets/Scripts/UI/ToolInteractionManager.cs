using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 工具交互管理器 - 管理工具使用状态和交互
/// </summary>
public class ToolInteractionManager : MonoBehaviour
{
    [Header("工具信息")]
    [SerializeField] private ToolInfo[] tools;
    
    [Header("UI引用")]
    [SerializeField] private ToolUsePanelController toolUsePanel;
    
    [Header("设置")]
    [SerializeField] private float interactionDistance = 2f; // 与宠物交互的最大距离
    
    // 单例模式
    private static ToolInteractionManager _instance;
    public static ToolInteractionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ToolInteractionManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("场景中未找到ToolInteractionManager实例！");
                }
            }
            return _instance;
        }
    }
    
    // 当前使用的工具索引(-1表示未使用工具)
    private int currentToolIndex = -1;
    
    // 当前选中的工具信息
    private ToolInfo CurrentTool => (currentToolIndex >= 0 && currentToolIndex < tools.Length) ? tools[currentToolIndex] : null;
    
    // 工具使用状态
    private bool isUsingTool = false;
    
    private void Awake()
    {
        // 单例初始化
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Update()
    {
        // 在工具使用状态下检测点击
        if (isUsingTool && Input.GetMouseButtonDown(0))
        {
            // 检查是否点击在UI上
            if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            {
                return;
            }
            
            // 尝试与宠物交互
            TryInteractWithPet();
        }
    }
    
    private void TryInteractWithPet()
    {
        // 从摄像机发射射线到点击位置
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // 检查是否点击到宠物
        if (hit.collider != null)
        {
            CharacterController2D pet = hit.collider.GetComponent<CharacterController2D>();
            
            if (pet != null)
            {
                // 与宠物交互
                InteractWithPet(pet);
            }
        }
    }
    
    /// <summary>
    /// 启动工具使用
    /// </summary>
    public void StartToolUse(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= tools.Length)
        {
            Debug.LogError($"工具索引无效: {toolIndex}");
            return;
        }
        
        // 设置当前工具
        currentToolIndex = toolIndex;
        isUsingTool = true;
        
        // 显示工具使用面板
        if (toolUsePanel != null)
        {
            // 设置使用说明文本
            if (CurrentTool != null)
            {
                toolUsePanel.SetInstructionText(CurrentTool.useInstruction);
            }
            
            // 显示面板
            toolUsePanel.ShowPanel();
        }
        
        // 进入工具使用模式，隐藏其他UI界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.EnterToolUseMode();
        }
    }
    
    /// <summary>
    /// 取消工具使用
    /// </summary>
    public void CancelToolUse()
    {
        isUsingTool = false;
        currentToolIndex = -1;
        
        // 隐藏工具使用面板
        if (toolUsePanel != null)
        {
            toolUsePanel.HidePanel();
        }
        
        // 退出工具使用模式，恢复其他UI界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ExitToolUseMode();
        }
    }
    
    /// <summary>
    /// 与宠物交互
    /// </summary>
    private void InteractWithPet(CharacterController2D pet)
    {
        if (pet == null || CurrentTool == null) return;
        
        // 获取宠物对工具的喜好值
        float affectionValue = CurrentTool.GetPetAffection(pet.Preference);
        
        // 执行交互效果（可以在这里添加特效或动画）
        Debug.Log($"与宠物 {pet.PetDisplayName} 使用工具 {CurrentTool.toolName}，喜好值: {affectionValue}");
        
        // TODO: 根据宠物对工具的喜好更新宠物状态
        // 例如：增加亲密度，恢复饱腹度等
    }
}

/// <summary>
/// 定义工具信息类
/// </summary>
[System.Serializable]
public class ToolInfo
{
    public string toolName;              // 工具名称
    public Sprite toolIcon;              // 工具图标
    public string useInstruction;        // 使用说明
    
    [Header("交互设置")]
    public ToolPreference[] preferences; // 不同宠物种类对此工具的喜好配置
    
    /// <summary>
    /// 获取指定宠物类型对这个工具的喜好值
    /// </summary>
    public float GetPetAffection(string petPreference)
    {
        if (string.IsNullOrEmpty(petPreference) || preferences == null)
            return 0f;
            
        // 遍历喜好配置，找到匹配的宠物类型
        foreach (var pref in preferences)
        {
            if (pref.petPreference.Equals(petPreference, System.StringComparison.OrdinalIgnoreCase))
            {
                return pref.affectionValue;
            }
        }
        
        // 默认情况下返回中性值
        return 0f;
    }
}

/// <summary>
/// 定义宠物对工具的喜好配置
/// </summary>
[System.Serializable]
public class ToolPreference
{
    public string petPreference;  // 宠物偏好类型
    
    [Range(-1f, 1f)]
    public float affectionValue;  // 喜好值：-1(厌恶) 到 1(喜爱)
} 