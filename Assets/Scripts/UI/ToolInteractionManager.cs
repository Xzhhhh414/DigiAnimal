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
    
    [Header("Toast消息配置")]
    [SerializeField] private string happyInteractionMessage = "{PetName} 很开心地玩着玩具！\n获得爱心货币 +{HeartReward}";
    [SerializeField] private string boredInteractionMessage = "{PetName} 获得了爱心货币 +{HeartReward}，但对玩具感到厌倦了";
    [SerializeField] private string cannotInteractMessage = "{PetName} 对玩具感到厌倦，需要休息一下";
    [SerializeField] private string sleepingInteractionMessage = "{PetName} 正在睡觉呢，别打扰ta的美梦~";
    [SerializeField] private string eatingInteractionMessage = "{PetName} 正在专心吃饭，现在可不想玩玩具";
    [SerializeField] private string otherFailureMessage = "{PetName} 现在无法进行玩具互动";
    
    //[Header("文本替换符号说明")]
    //[SerializeField] [TextArea(2, 3)] private string symbolHelp = "可用符号:\n{PetName} - 宠物名字\n{ToolName} - 工具名字\n{HeartReward} - 爱心奖励数量";
    
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
    
    // 交互间隔控制（防止过快连续点击）
    private float lastInteractionTime = 0f;
    private float interactionCooldown = 0.3f; // 0.3秒交互间隔
    
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
        //Debug.Log($"[ToolInteractionManager] Awake开始执行 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 场景特定的单例初始化 - 移除DontDestroyOnLoad
        if (_instance == null)
        {
            _instance = this;
            //Debug.Log($"[ToolInteractionManager] 单例初始化完成（场景特定） - Scene: {gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // 检查现有实例是否来自不同场景
            if (_instance.gameObject.scene != this.gameObject.scene)
            {
                //Debug.Log($"[ToolInteractionManager] 检测到跨场景实例冲突，替换为当前场景实例 - 旧场景: {_instance.gameObject.scene.name}, 新场景: {gameObject.scene.name}");
                // 清理旧实例的引用
                var oldInstance = _instance;
                _instance = this;
                // 销毁旧实例
                if (oldInstance != null)
                {
                    Destroy(oldInstance.gameObject);
                }
            }
            else
            {
                //Debug.Log($"[ToolInteractionManager] 销毁同场景重复实例 - Scene: {gameObject.scene.name}");
                Destroy(gameObject);
                return;
            }
        }
        
        //Debug.Log($"[ToolInteractionManager] Awake执行完成 - Scene: {gameObject.scene.name}");
    }
    
    private void OnDestroy()
    {
        //Debug.Log($"[ToolInteractionManager] OnDestroy被调用 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 只有当前实例是静态引用时才清除
        if (_instance == this)
        {
            //Debug.Log($"[ToolInteractionManager] 清除静态引用 - Scene: {gameObject.scene.name}");
            _instance = null;
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
        
        // 关闭工具包选择界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseToolkit();
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
        
        // 检查交互间隔
        if (Time.time - lastInteractionTime < interactionCooldown)
        {
            return; // 还在冷却期内，忽略这次交互
        }
        
        // 更新最后交互时间
        lastInteractionTime = Time.time;
        
        // 获取当前工具的爱心奖励数量
        int heartReward = CurrentTool.heartReward;
        
        // 记录交互前的厌倦状态
        bool wasBoredBefore = pet.IsBored;
        
        // 尝试玩具互动（传入奖励值而不是固定的1.0f）
        int interactionResult = pet.TryToyInteraction(heartReward);
        
        switch (interactionResult)
        {
            case 0: // 成功
            // 检查本次交互是否让宠物进入厌倦状态
            bool becameBored = !wasBoredBefore && pet.IsBored;
            
            // 显示爱心获得提示
            var heartManager = FindObjectOfType<HeartMessageManager>();
            if (heartManager != null)
            {
                heartManager.ShowHeartGainMessage(pet, heartReward);
            }
            else
            {
                Debug.LogWarning("未找到HeartMessageManager组件，爱心提示将不会显示");
            }
            
            if (becameBored)
            {
                // 本次交互让宠物进入厌倦状态，但仍然显示成功的消息
                string message = ReplaceTextSymbols(boredInteractionMessage, pet, heartReward);
                ShowPetMessage(pet, message, PetNeedType.Happy);
            }
            else
            {
                // 宠物没有因本次交互进入厌倦状态，很开心
                string message = ReplaceTextSymbols(happyInteractionMessage, pet, heartReward);
                ShowPetMessage(pet, message, PetNeedType.Happy);
            }
                break;
                
            case 1: // 厌倦状态
                string boredMessage = ReplaceTextSymbols(cannotInteractMessage, pet, 0);
                ShowPetMessage(pet, boredMessage, PetNeedType.Indifferent);
                break;
                
            case 2: // 正在睡觉
                string sleepingMessage = ReplaceTextSymbols(sleepingInteractionMessage, pet, 0);
                ShowPetMessage(pet, sleepingMessage, PetNeedType.None);
                break;
                
            case 3: // 正在吃饭
                string eatingMessage = ReplaceTextSymbols(eatingInteractionMessage, pet, 0);
                ShowPetMessage(pet, eatingMessage, PetNeedType.None);
                break;
                
            default: // 其他失败情况
                string failureMessage = ReplaceTextSymbols(otherFailureMessage, pet, 0);
                ShowPetMessage(pet, failureMessage, PetNeedType.Indifferent);
                break;
        }
        
        // 不再自动关闭工具使用界面，保持在玩具使用状态
        // CancelToolUse(); // 注释掉这行
    }
    
    /// <summary>
    /// 显示宠物消息
    /// </summary>
    private void ShowPetMessage(PetController2D pet, string message)
    {
        var petMessageManager = FindObjectOfType<PetMessageManager>();
        if (petMessageManager != null)
        {
            petMessageManager.ShowPetMessage(pet, message);
        }
        else
        {
            Debug.LogWarning("未找到PetMessageManager！请在场景中添加PetMessageManager组件以显示宠物消息。");
        }
    }
    
    /// <summary>
    /// 显示宠物消息（指定气泡类型）
    /// </summary>
    private void ShowPetMessage(PetController2D pet, string message, PetNeedType bubbleType)
    {
        var petMessageManager = FindObjectOfType<PetMessageManager>();
        if (petMessageManager != null)
        {
            petMessageManager.ShowPetMessage(pet, message, bubbleType);
        }
        else
        {
            Debug.LogWarning("未找到PetMessageManager！请在场景中添加PetMessageManager组件以显示宠物消息。");
        }
    }
    
    /// <summary>
    /// 替换文本中的符号
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="pet">宠物对象</param>
    /// <param name="heartReward">爱心奖励数量</param>
    /// <returns>替换后的文本</returns>
    private string ReplaceTextSymbols(string text, PetController2D pet, int heartReward)
    {
        string result = text;
        
        // 替换宠物名字
        if (pet != null)
        {
            result = result.Replace("{PetName}", pet.PetDisplayName);
        }
        
        // 替换工具名字
        if (CurrentTool != null)
        {
            result = result.Replace("{ToolName}", CurrentTool.toolName);
        }
        
        // 替换爱心奖励数量
        result = result.Replace("{HeartReward}", heartReward.ToString());
        
        return result;
    }
} 