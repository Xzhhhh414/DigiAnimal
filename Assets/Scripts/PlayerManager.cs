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
    public static PlayerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PlayerManager>();
                
                if (_instance == null)
                {
                    // 如果场景中没有PlayerManager，创建一个
                    GameObject playerManagerObj = new GameObject("PlayerManager");
                    _instance = playerManagerObj.AddComponent<PlayerManager>();
                    DontDestroyOnLoad(playerManagerObj);
                }
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
            Debug.Log($"获得爱心货币 +{amount}，当前总数: {HeartCurrency}");
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
            Debug.Log($"消费爱心货币 -{amount}，当前总数: {HeartCurrency}");
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
        
        // 初始化工具数组（如果为空）
        if (tools == null)
        {
            tools = new ToolInfo[0];
        }
    }
    
    private void Start()
    {
        // 验证工具配置
        ValidateToolConfiguration();
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
        }
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
    public string toolDescription;       // 工具描述（在工具背包面板中显示）
    public string useInstruction;        // 使用说明（在工具使用面板中显示）
    
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