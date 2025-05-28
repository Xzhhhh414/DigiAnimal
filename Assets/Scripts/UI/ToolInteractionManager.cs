using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 工具交互管理器 - 管理工具使用状态和交互
/// </summary>
public class ToolInteractionManager : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private ToolUsePanelController toolUsePanel;
    
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
    private ToolInfo CurrentTool => GetCurrentTool();
    
    // 工具使用状态
    private bool isUsingTool = false;
    
    /// <summary>
    /// 获取工具信息数组（供其他脚本访问）
    /// </summary>
    public ToolInfo[] GetTools()
    {
        if (PlayerManager.Instance != null)
        {
            return PlayerManager.Instance.GetTools();
        }
        
        Debug.LogError("ToolInteractionManager: PlayerManager.Instance为空！");
        return new ToolInfo[0];
    }
    
    /// <summary>
    /// 获取当前选中的工具信息
    /// </summary>
    private ToolInfo GetCurrentTool()
    {
        if (PlayerManager.Instance != null && currentToolIndex >= 0)
        {
            return PlayerManager.Instance.GetTool(currentToolIndex);
        }
        return null;
    }
    
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
            PetController2D pet = hit.collider.GetComponent<PetController2D>();
            
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
        // 验证PlayerManager
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("ToolInteractionManager: PlayerManager.Instance为空！");
            return;
        }
        
        // 验证工具索引
        if (toolIndex < 0 || toolIndex >= PlayerManager.Instance.GetToolCount())
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
        else
        {
            Debug.LogError("ToolInteractionManager: toolUsePanel引用为空！请在Inspector中设置ToolUsePanelController引用");
        }
        
        // 进入工具使用模式，隐藏其他UI界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.EnterToolUseMode();
        }
        else
        {
            Debug.LogWarning("ToolInteractionManager: UIManager.Instance为空");
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
    private void InteractWithPet(PetController2D pet)
    {
        if (pet == null || CurrentTool == null) return;
        
        // 获取宠物对工具的喜好值
        float affectionValue = CurrentTool.GetPetAffection(pet.Preference);
        
        // 尝试逗乐宠物
        bool wasAmused = pet.TryAmuse(affectionValue);
        
        if (wasAmused)
        {
            Debug.Log($"宠物 {pet.PetDisplayName} 被 {CurrentTool.toolName} 逗乐了！获得爱心货币 +1");
        }
        else
        {
            if (!pet.CanBeAmused)
            {
                Debug.Log($"宠物 {pet.PetDisplayName} 还在逗乐CD中，剩余时间: {pet.AmusementCooldownRemaining:F1}秒");
            }
            else
            {
                Debug.Log($"宠物 {pet.PetDisplayName} 对 {CurrentTool.toolName} 没有兴趣");
            }
        }
        
        // 交互完成后，自动退出工具使用界面
        CancelToolUse();
    }
} 