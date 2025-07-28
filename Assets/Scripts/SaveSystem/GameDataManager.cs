using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏数据管理器 - 管理运行时的游戏数据状态和自动保存
/// </summary>
public class GameDataManager : MonoBehaviour
{
    // 单例模式
    private static GameDataManager _instance;
    public static GameDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameDataManager>();
                if (_instance == null)
                {
                    GameObject gameDataManagerObj = new GameObject("GameDataManager");
                    _instance = gameDataManagerObj.AddComponent<GameDataManager>();
                    DontDestroyOnLoad(gameDataManagerObj);
                }
            }
            return _instance;
        }
    }
    
    [Header("自动保存设置")]
    [SerializeField] private float positionSaveInterval = 1f; // 位置保存间隔（秒）- 改为每秒保存
    [SerializeField] private bool enableAutoSave = true; // 是否启用自动保存
    
    // 运行时宠物数据映射 (petId -> PetController2D)
    private Dictionary<string, PetController2D> activePets = new Dictionary<string, PetController2D>();
    
    // 定时器
    private float positionSaveTimer = 0f;
    
    // 事件
    public event System.Action OnDataChanged;
    
    private void Awake()
    {
        // 单例模式设置
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
    
    private void Start()
    {
        // 订阅PlayerManager的货币变化事件
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }
    }
    
    private void Update()
    {
        if (!enableAutoSave) return;
        
        // 定时保存宠物位置
        positionSaveTimer += Time.deltaTime;
        if (positionSaveTimer >= positionSaveInterval)
        {
            SavePetPositions();
            positionSaveTimer = 0f;
        }
    }
    
    private void OnDestroy()
    {
        // 取消事件订阅
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }
    
    #region 宠物管理
    
    /// <summary>
    /// 注册宠物到数据管理器
    /// </summary>
    public void RegisterPet(string petId, PetController2D petController)
    {
        if (string.IsNullOrEmpty(petId) || petController == null)
        {
            Debug.LogWarning("注册宠物失败：petId为空或petController为null");
            return;
        }
        
        if (activePets.ContainsKey(petId))
        {
            Debug.LogWarning($"宠物ID已存在: {petId}，将覆盖旧的引用");
        }
        
        activePets[petId] = petController;
        //Debug.Log($"宠物已注册: {petId} -> {petController.name}");
    }
    
    /// <summary>
    /// 注销宠物
    /// </summary>
    public void UnregisterPet(string petId)
    {
        if (activePets.ContainsKey(petId))
        {
            activePets.Remove(petId);
            //Debug.Log($"宠物已注销: {petId}");
        }
    }
    
    /// <summary>
    /// 清理所有注册的宠物引用
    /// </summary>
    public void ClearAllPets()
    {
        int count = activePets.Count;
        activePets.Clear();
        //Debug.Log($"GameDataManager: 已清理所有宠物引用，共清理 {count} 个");
    }
    
    /// <summary>
    /// 获取活跃的宠物
    /// </summary>
    public PetController2D GetActivePet(string petId)
    {
        activePets.TryGetValue(petId, out PetController2D pet);
        return pet;
    }
    
    /// <summary>
    /// 获取所有活跃宠物
    /// </summary>
    public Dictionary<string, PetController2D> GetAllActivePets()
    {
        return new Dictionary<string, PetController2D>(activePets);
    }
    
    #endregion
    
    #region 数据同步
    
    /// <summary>
    /// 将运行时数据同步到存档
    /// </summary>
    public async void SyncToSave(bool immediate = false)
    {
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData == null)
        {
            Debug.LogWarning("没有当前存档数据，无法同步");
            return;
        }
        
        // 同步玩家数据
        SyncPlayerData(saveData);
        
        // 同步宠物数据
        SyncPetData(saveData);
        
        // 保存到文件
        if (immediate)
        {
            SaveManager.Instance.Save();
        }
        else
        {
            await SaveManager.Instance.SaveAsync();
        }
        
        // Debug.Log("数据已同步到存档");
    }
    
    /// <summary>
    /// 同步玩家数据
    /// </summary>
    private void SyncPlayerData(SaveData saveData)
    {
        if (PlayerManager.Instance != null)
        {
            saveData.playerData.heartCurrency = PlayerManager.Instance.HeartCurrency;
            // 可以在这里添加其他玩家数据同步
        }
    }
    
    /// <summary>
    /// 同步宠物数据
    /// </summary>
    private void SyncPetData(SaveData saveData)
    {
        foreach (var kvp in activePets)
        {
            string petId = kvp.Key;
            PetController2D petController = kvp.Value;
            
            if (petController == null) continue;
            
            // 查找对应的存档数据
            PetSaveData petSaveData = saveData.petsData.Find(p => p.petId == petId);
            if (petSaveData == null)
            {
                Debug.LogWarning($"未找到宠物 {petId} 的存档数据");
                continue;
            }
            
            // 同步属性（不保存当前状态isSleeping、isEating）
            petSaveData.displayName = petController.PetDisplayName;
            petSaveData.introduction = petController.PetIntroduction;
            petSaveData.energy = petController.Energy;
            petSaveData.satiety = petController.Satiety;
            petSaveData.position = petController.transform.position;
            petSaveData.isBored = petController.IsBored;
            
            // 同步厌倦时间 - 需要反射或添加公共属性
            // petSaveData.lastBoredomTime = petController.LastBoredomTime;
        }
    }
    
    /// <summary>
    /// 只保存宠物位置（用于定时保存）
    /// </summary>
    private async void SavePetPositions()
    {
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData == null) 
        {
            Debug.LogWarning("[位置保存] 无法获取当前存档数据");
            return;
        }
        
        bool hasChanges = false;
        
        foreach (var kvp in activePets)
        {
            string petId = kvp.Key;
            PetController2D petController = kvp.Value;
            
            if (petController == null) continue;
            
            PetSaveData petSaveData = saveData.petsData.Find(p => p.petId == petId);
            if (petSaveData == null) 
            {
                Debug.LogWarning($"[位置保存] 未找到宠物 {petId} 的存档数据");
                continue;
            }
            
            Vector3 currentPos = petController.transform.position;
            float distance = Vector3.Distance(petSaveData.position, currentPos);
            
            if (distance > 0.1f)
            {
                petSaveData.position = currentPos;
                hasChanges = true;
            }
        }
        
        if (hasChanges)
        {
            await SaveManager.Instance.SaveAsync();
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 货币变化事件处理
    /// </summary>
    private void OnCurrencyChanged(int newAmount)
    {
        OnDataChanged?.Invoke();
        
        // 只有在启用自动保存时才保存
        if (enableAutoSave)
        {
            // 立即保存货币变化
            SyncToSave(true);
        }
    }
    
    /// <summary>
    /// 宠物属性变化通知
    /// </summary>
    public void OnPetDataChanged()
    {
        OnDataChanged?.Invoke();
        
        // 只有在启用自动保存时才保存
        if (enableAutoSave)
        {
            // 异步保存宠物数据变化
            SyncToSave(false);
        }
        
        // 同步数据到iOS（灵动岛和主屏幕Widget）
        if (IOSDataBridge.Instance != null)
        {
            IOSDataBridge.Instance.ForceSyncNow();
            Debug.Log("[GameDataManager] 宠物数据变化，已触发iOS数据同步");
        }
    }
    
    /// <summary>
    /// 检查自动保存是否启用
    /// </summary>
    public bool IsAutoSaveEnabled => enableAutoSave;
    
    /// <summary>
    /// 设置自动保存状态
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        enableAutoSave = enabled;
    }
    
    #endregion
    
    #region 应用程序生命周期
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 应用暂停时强制保存所有数据（包括位置）
            SyncToSave(true);
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // 应用失去焦点时强制保存所有数据（包括位置）
            SyncToSave(true);
        }
    }
    
    private void OnApplicationQuit()
    {
        // 应用退出时强制保存所有数据（包括位置）
        SyncToSave(true);
        Debug.Log("应用退出，已保存所有数据");
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("强制同步数据")]
    public void ForceSyncData()
    {
        SyncToSave(true);
    }
    
    [ContextMenu("打印活跃宠物")]
    public void DebugActivePets()
    {
        Debug.Log($"当前活跃宠物数量: {activePets.Count}");
        foreach (var kvp in activePets)
        {
            Debug.Log($"- {kvp.Key}: {kvp.Value?.name ?? "null"}");
        }
    }
    
    [ContextMenu("调试：立即保存位置")]
    public void DebugSavePositionsNow()
    {
        Debug.Log("=== 立即保存位置测试 ===");
        SavePetPositions();
    }
    
    [ContextMenu("调试：检查宠物注册状态")]
    public void DebugCheckPetRegistration()
    {
        Debug.Log($"=== 宠物注册状态检查 ===");
        Debug.Log($"活跃宠物数量: {activePets.Count}");
        
        // 检查场景中的所有PetController2D
        PetController2D[] allPets = FindObjectsOfType<PetController2D>();
        Debug.Log($"场景中的宠物数量: {allPets.Length}");
        
        foreach (var pet in allPets)
        {
            bool isRegistered = activePets.ContainsValue(pet);
            Debug.Log($"宠物 {pet.name} (ID: {pet.petId}): 注册状态={isRegistered}, 位置={pet.transform.position}");
        }
    }
    
    #endregion
} 