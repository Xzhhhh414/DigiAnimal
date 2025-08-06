using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家数据管理器 - 管理玩家的所有数据，包括货币、工具等
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("货币设置")]
    [SerializeField] private int heartCurrency = 0; // 爱心货币数量
    
    [Header("工具信息")]
    [SerializeField] private ToolInfo[] tools; // 玩家拥有的工具
    
    // 单例模式
    private static PlayerManager _instance;
    private static bool _applicationIsQuitting = false;
    
    public static PlayerManager Instance
    {
        get
        {
            // 如果应用程序正在退出，不返回实例
            if (_applicationIsQuitting)
            {
                return null;
            }
            
            // 场景特定实现：只查找现有实例，不自动创建
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerManager>();
            }
            
            return _instance;
        }
    }
    
    // 货币变化事件
    public delegate void CurrencyChangedHandler(int newAmount);
    public event CurrencyChangedHandler OnCurrencyChanged;
    
    // 工具数据变化事件
    public delegate void ToolsChangedHandler();
    public event ToolsChangedHandler OnToolsChanged;
    
    #region 货币管理
    
    /// <summary>
    /// 获取当前爱心货币数量
    /// </summary>
    public int HeartCurrency
    {
        get { return heartCurrency; }
        private set
        {
            heartCurrency = value;
            OnCurrencyChanged?.Invoke(heartCurrency);
        }
    }
    
    /// <summary>
    /// 增加爱心货币
    /// </summary>
    /// <param name="amount">增加的数量</param>
    public void AddHeartCurrency(int amount)
    {
        if (amount > 0)
        {
            HeartCurrency += amount;
            // Debug.Log($"获得爱心货币 +{amount}，当前总数: {HeartCurrency}");
        }
    }
    
    /// <summary>
    /// 消费爱心货币
    /// </summary>
    /// <param name="amount">消费的数量</param>
    /// <returns>是否消费成功</returns>
    public bool SpendHeartCurrency(int amount)
    {
        if (amount > 0 && HeartCurrency >= amount)
        {
            HeartCurrency -= amount;
            // Debug.Log($"消费爱心货币 -{amount}，当前总数: {HeartCurrency}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 检查是否有足够的爱心货币
    /// </summary>
    /// <param name="amount">需要检查的数量</param>
    /// <returns>是否有足够的货币</returns>
    public bool HasEnoughCurrency(int amount)
    {
        return HeartCurrency >= amount;
    }
    
    /// <summary>
    /// 直接设置爱心货币（用于存档加载，不触发事件）
    /// </summary>
    /// <param name="amount">要设置的数量</param>
    public void SetHeartCurrencyDirect(int amount)
    {
        heartCurrency = Mathf.Max(0, amount);
        // 不触发OnCurrencyChanged事件，避免立即保存
    }
    
    /// <summary>
    /// 触发货币变化事件（用于存档加载后同步UI）
    /// </summary>
    public void TriggerCurrencyChangeEvent()
    {
        OnCurrencyChanged?.Invoke(heartCurrency);
    }
    
    #endregion
    
    #region 工具管理
    
    /// <summary>
    /// 获取工具信息数组（供其他脚本访问）
    /// </summary>
    public ToolInfo[] GetTools()
    {
        return tools;
    }
    
    /// <summary>
    /// 获取指定索引的工具信息
    /// </summary>
    /// <param name="index">工具索引</param>
    /// <returns>工具信息，如果索引无效返回null</returns>
    public ToolInfo GetTool(int index)
    {
        if (index >= 0 && index < tools.Length)
        {
            return tools[index];
        }
        return null;
    }
    
    /// <summary>
    /// 获取工具数量
    /// </summary>
    public int GetToolCount()
    {
        return tools != null ? tools.Length : 0;
    }
    
    /// <summary>
    /// 添加新工具（用于后续扩展，比如商店购买工具）
    /// </summary>
    /// <param name="newTool">新工具信息</param>
    public void AddTool(ToolInfo newTool)
    {
        if (newTool == null) return;
        
        // 创建新的工具数组
        ToolInfo[] newTools = new ToolInfo[tools.Length + 1];
        
        // 复制现有工具
        for (int i = 0; i < tools.Length; i++)
        {
            newTools[i] = tools[i];
        }
        
        // 添加新工具
        newTools[tools.Length] = newTool;
        
        // 更新工具数组
        tools = newTools;
        
        // 触发工具变化事件
        OnToolsChanged?.Invoke();
        
        Debug.Log($"添加新工具: {newTool.toolName}");
    }
    
    /// <summary>
    /// 移除工具（用于后续扩展）
    /// </summary>
    /// <param name="toolIndex">要移除的工具索引</param>
    public bool RemoveTool(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= tools.Length) return false;
        
        string removedToolName = tools[toolIndex].toolName;
        
        // 创建新的工具数组
        ToolInfo[] newTools = new ToolInfo[tools.Length - 1];
        
        // 复制工具（跳过要移除的工具）
        int newIndex = 0;
        for (int i = 0; i < tools.Length; i++)
        {
            if (i != toolIndex)
            {
                newTools[newIndex] = tools[i];
                newIndex++;
            }
        }
        
        // 更新工具数组
        tools = newTools;
        
        // 触发工具变化事件
        OnToolsChanged?.Invoke();
        
        Debug.Log($"移除工具: {removedToolName}");
        return true;
    }
    
    #endregion
    
    private void Awake()
    {
        //Debug.Log($"[PlayerManager] Awake开始执行 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 强制场景隔离的单例模式
        if (_instance == null)
        {
            _instance = this;
            //Debug.Log($"[PlayerManager] 单例初始化完成（场景特定） - Scene: {gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // 检查现有实例是否来自不同场景
            if (_instance.gameObject.scene != this.gameObject.scene)
            {
                //Debug.Log($"[PlayerManager] 检测到跨场景实例冲突，替换为当前场景实例 - 旧场景: {_instance.gameObject.scene.name}, 新场景: {gameObject.scene.name}");
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
                //Debug.Log($"[PlayerManager] 销毁同场景重复实例 - Scene: {gameObject.scene.name}");
                Destroy(gameObject);
                return;
            }
        }
        
        // 初始化工具数组（如果为空）
        if (tools == null)
        {
            tools = new ToolInfo[0];
        }
        
        //Debug.Log($"[PlayerManager] Awake执行完成 - Scene: {gameObject.scene.name}");
    }
    
    private void Start()
    {
        // 从存档系统加载玩家数据
        LoadPlayerDataFromSave();
        
        // 验证工具配置
        ValidateToolConfiguration();
    }
    
    /// <summary>
    /// 从存档系统加载玩家数据
    /// </summary>
    private void LoadPlayerDataFromSave()
    {
        if (SaveManager.Instance != null)
        {
            var saveData = SaveManager.Instance.GetCurrentSaveData();
            if (saveData != null && saveData.playerData != null)
            {
                // 加载货币数据（不触发事件）
                SetHeartCurrencyDirect(saveData.playerData.heartCurrency);
                
                // 触发UI更新事件
                TriggerCurrencyChangeEvent();
                
                //Debug.Log($"PlayerManager: 从存档加载玩家数据，爱心货币: {heartCurrency}");
            }
            else
            {
                Debug.LogWarning("PlayerManager: 没有找到存档数据，使用默认值");
            }
        }
        else
        {
            Debug.LogWarning("PlayerManager: SaveManager未初始化，无法加载玩家数据");
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // 当应用程序暂停时的处理（移动平台）
        if (pauseStatus)
        {
            // 触发数据同步到存档系统
            SyncDataToSave();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // 当应用程序失去/获得焦点时的处理
        if (!hasFocus)
        {
            // 触发数据同步到存档系统
            SyncDataToSave();
        }
    }
    
    private void OnApplicationQuit()
    {
        // 应用程序退出时清理单例引用
        _instance = null;
        _applicationIsQuitting = true;
    }
    
    private void OnDestroy()
    {
        //Debug.Log($"[PlayerManager] OnDestroy被调用 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 只有当前实例是静态引用时才清除
        if (_instance == this)
        {
            //Debug.Log($"[PlayerManager] 清除静态引用 - Scene: {gameObject.scene.name}");
            _instance = null;
        }
        
        // 同步数据到存档系统
        SyncDataToSave();
    }
    
    /// <summary>
    /// 同步数据到存档系统
    /// </summary>
    private void SyncDataToSave()
    {
        if (SaveManager.Instance != null && !_applicationIsQuitting)
        {
            var saveData = SaveManager.Instance.GetCurrentSaveData();
            if (saveData != null && saveData.playerData != null)
            {
                saveData.playerData.heartCurrency = heartCurrency;
                // 注意：这里不直接调用Save()，让GameDataManager统一管理保存时机
            }
        }
    }
    
    /// <summary>
    /// 验证工具配置
    /// </summary>
    private void ValidateToolConfiguration()
    {
        if (tools == null || tools.Length == 0)
        {
            Debug.LogWarning("PlayerManager: 没有配置任何工具！请在Inspector中设置工具信息。");
            return;
        }
        
        for (int i = 0; i < tools.Length; i++)
        {
            if (tools[i] == null)
            {
                Debug.LogError($"PlayerManager: 工具索引 {i} 为空！请检查工具配置。");
                continue;
            }
            
            if (string.IsNullOrEmpty(tools[i].toolName))
            {
                Debug.LogWarning($"PlayerManager: 工具索引 {i} 没有设置名称。");
            }
            
            if (tools[i].toolIcon == null)
            {
                Debug.LogWarning($"PlayerManager: 工具 '{tools[i].toolName}' 没有设置图标。");
            }
            
            // 检查直接交互类型的工具是否配置了交互成功后的提示文本
            if (tools[i].toolType == ToolType.DirectInteraction && string.IsNullOrEmpty(tools[i].interactedInstruction))
            {
                Debug.LogWarning($"PlayerManager: 直接交互类型的工具 '{tools[i].toolName}' 没有设置交互成功后的提示文本。建议在interactedInstruction字段中配置相关说明。");
            }
            
            // 检查可放置物体类型的工具是否配置了预制体
            if (tools[i].toolType == ToolType.PlaceableObject && tools[i].toolPrefab == null)
            {
                Debug.LogError($"PlayerManager: 可放置物体类型的工具 '{tools[i].toolName}' 没有设置预制体！请在toolPrefab字段中配置预制体。");
            }
            
            // 检查可放置物体类型的工具是否配置了放置后的提示文本
            if (tools[i].toolType == ToolType.PlaceableObject && string.IsNullOrEmpty(tools[i].placedInstruction))
            {
                Debug.LogWarning($"PlayerManager: 可放置物体类型的工具 '{tools[i].toolName}' 没有设置放置后的提示文本。建议在placedInstruction字段中配置相关说明。");
            }
            
            // 检查可放置物体类型的工具是否配置了互动时的提示文本
            if (tools[i].toolType == ToolType.PlaceableObject && string.IsNullOrEmpty(tools[i].interactingInstruction))
            {
                Debug.LogWarning($"PlayerManager: 可放置物体类型的工具 '{tools[i].toolName}' 没有设置互动时的提示文本。建议在interactingInstruction字段中配置相关说明。");
            }
            
            // 检查可放置物体类型的工具是否配置了无互动时的提示文本
            if (tools[i].toolType == ToolType.PlaceableObject && string.IsNullOrEmpty(tools[i].noInteractionInstruction))
            {
                Debug.LogWarning($"PlayerManager: 可放置物体类型的工具 '{tools[i].toolName}' 没有设置无互动时的提示文本。建议在noInteractionInstruction字段中配置相关说明。");
            }
        }
    }
}

/// <summary>
/// 定义工具信息类
/// </summary>
[System.Serializable]
public class ToolInfo
{
    [Header("工具设置")]
    public ToolType toolType = ToolType.DirectInteraction; // 工具类型
    public GameObject toolPrefab;        // 工具预制体（用于需要生成游戏物体的工具，如逗猫棒）
    public string toolName;              // 工具名称
    public Sprite toolIcon;              // 工具图标
    public string toolDescription;       // 工具描述（在工具背包面板中显示）
    public string useInstruction;        // 使用说明（在工具使用面板中显示）
    
    [Header("直接交互类工具专用")]
    public string interactedInstruction; // 直接交互成功后的提示文本（仅用于DirectInteraction类型工具）
                                        // 支持占位符：{PetName} - 宠物名称, {ToolName} - 工具名称, {HeartReward} - 爱心奖励数量
    
    [Header("放置类工具专用")]
    public string placedInstruction;     // 放置成功后的提示文本（仅用于PlaceableObject类型工具）
    public string interactingInstruction; // 宠物开始互动后的提示文本（仅用于PlaceableObject类型工具）
                                          // 支持占位符：{PetName} - 宠物名称, {ToolName} - 工具名称, {HeartReward} - 爱心奖励数量
    public string noInteractionInstruction = "没有宠物对{ToolName}感兴趣，{ToolName}消失了"; // 无宠物互动时的提示文本（仅用于PlaceableObject类型工具）
                                                                                           // 支持占位符：{ToolName} - 工具名称
    
    [Header("交互奖励")]
    public int heartReward = 1;          // 交互成功时获得的爱心数量
    
    [Header("通用结束阶段")]
    public string endingInstruction = "互动完成！获得了 {HeartReward} 个爱心！"; // 互动结束时的通用提示文本
    


}

/// <summary>
/// 工具类型枚举
/// </summary>
[System.Serializable]
public enum ToolType
{
    DirectInteraction,  // 直接交互类型（如摸摸）
    PlaceableObject     // 可放置物体类型（如逗猫棒、玩具老鼠）
} 