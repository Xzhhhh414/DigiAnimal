using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宠物数据库管理器 - 单例模式，提供运行时访问宠物配置数据
/// </summary>
public class PetDatabaseManager : MonoBehaviour
{
    [Header("数据库配置")]
    [SerializeField] private PetDatabase petDatabase;
    
    // 单例实例
    private static PetDatabaseManager _instance;
    public static PetDatabaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetDatabaseManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PetDatabaseManager");
                    _instance = go.AddComponent<PetDatabaseManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // 单例模式设置
        if (_instance == null)
        {
            _instance = this;
            
            // 如果这个PetDatabaseManager不是在专门的GameObject上，创建一个新的
            if (gameObject.name != "PetDatabaseManager")
            {
                // 创建专门的GameObject来承载PetDatabaseManager
                GameObject dedicatedGO = new GameObject("PetDatabaseManager");
                PetDatabaseManager newManager = dedicatedGO.AddComponent<PetDatabaseManager>();
                
                // 复制配置到新的管理器
                newManager.petDatabase = this.petDatabase;
                
                DontDestroyOnLoad(dedicatedGO);
                
                // 更新单例引用
                _instance = newManager;
                
                // 让新的管理器初始化数据库
                newManager.InitializeDatabase();
                
                Debug.Log($"PetDatabaseManager: 从 {gameObject.name} 迁移到专门的GameObject");
                
                // 销毁当前组件（但不影响挂载的GameObject）
                Destroy(this);
                return;
            }
            
            // 如果已经在专门的GameObject上，直接设置DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            
            // 初始化数据库
            InitializeDatabase();
        }
        else if (_instance != this)
        {
            // 如果已经有实例，但当前实例有配置而单例实例没有配置，则更新配置
            if (_instance.petDatabase == null && this.petDatabase != null)
            {
                _instance.petDatabase = this.petDatabase;
                _instance.InitializeDatabase();
                Debug.Log("PetDatabaseManager: 更新了数据库配置");
            }
            
            Destroy(this); // 只销毁组件，不销毁整个GameObject
        }
    }
    
    /// <summary>
    /// 初始化数据库
    /// </summary>
    public void InitializeDatabase()
    {
        if (petDatabase == null)
        {
            // 尝试从Resources文件夹加载默认数据库
            petDatabase = Resources.Load<PetDatabase>("Data/PetDatabase");
            
            if (petDatabase == null)
            {
                Debug.LogError("PetDatabaseManager: 未找到宠物数据库配置文件！请在Resources/Data/文件夹中创建PetDatabase.asset");
                return;
            }
        }
        
        // 验证配置
        if (!petDatabase.ValidateConfigs())
        {
            Debug.LogError("PetDatabaseManager: 宠物数据库配置验证失败！");
        }
        else
        {
            Debug.Log($"PetDatabaseManager: 成功加载 {petDatabase.GetAllPets().Count} 个宠物配置");
        }
    }
    
    /// <summary>
    /// 获取所有宠物配置
    /// </summary>
    public List<PetConfigData> GetAllPets()
    {
        return petDatabase?.GetAllPets() ?? new List<PetConfigData>();
    }
    
    /// <summary>
    /// 获取初始宠物列表（用于初次选择）
    /// </summary>
    public List<PetConfigData> GetStarterPets()
    {
        return petDatabase?.GetStarterPets() ?? new List<PetConfigData>();
    }
    
    /// <summary>
    /// 获取商店宠物列表
    /// </summary>
    public List<PetConfigData> GetShopPets()
    {
        return petDatabase?.GetShopPets() ?? new List<PetConfigData>();
    }
    
    /// <summary>
    /// 根据ID获取宠物配置
    /// </summary>
    public PetConfigData GetPetById(string petId)
    {
        return petDatabase?.GetPetById(petId);
    }
    
    /// <summary>
    /// 根据预制体获取宠物配置
    /// </summary>
    public PetConfigData GetPetByPrefab(GameObject petPrefab)
    {
        return petDatabase?.GetPetByPrefab(petPrefab);
    }
    
    /// <summary>
    /// 根据预制体名称获取宠物配置
    /// </summary>
    public PetConfigData GetPetByPrefabName(string prefabName)
    {
        if (petDatabase == null || string.IsNullOrEmpty(prefabName))
            return null;
            
        var allPets = petDatabase.GetAllPets();
        foreach (var pet in allPets)
        {
            // 首先尝试通过petId匹配（最准确）
            if (pet.petId == prefabName)
            {
                return pet;
            }
            
            // 然后尝试通过petPrefab.name匹配
            if (pet.petPrefab != null && pet.petPrefab.name == prefabName)
            {
                return pet;
            }
            
            // 最后尝试通过previewPrefab.name匹配
            if (pet.previewPrefab != null && pet.previewPrefab.name == prefabName)
            {
                return pet;
            }
        }
        
        Debug.LogWarning($"[PetDatabaseManager] 无法找到预制体名称为 '{prefabName}' 的宠物配置");
        return null;
    }
    
    /// <summary>
    /// 根据类型获取宠物列表
    /// </summary>
    public List<PetConfigData> GetPetsByType(PetType petType)
    {
        return petDatabase?.GetPetsByType(petType) ?? new List<PetConfigData>();
    }
    
    /// <summary>
    /// 检查数据库是否已加载
    /// </summary>
    public bool IsDatabaseLoaded()
    {
        return petDatabase != null;
    }
    
    /// <summary>
    /// 重新加载数据库（编辑器用）
    /// </summary>
    [ContextMenu("重新加载数据库")]
    public void ReloadDatabase()
    {
        petDatabase = Resources.Load<PetDatabase>("Data/PetDatabase");
        InitializeDatabase();
    }
    
    /// <summary>
    /// 设置数据库（编辑器用）
    /// </summary>
    public void SetDatabase(PetDatabase database)
    {
        petDatabase = database;
        InitializeDatabase();
    }
} 