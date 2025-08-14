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
    [SerializeField] private float foodSaveInterval = 5f; // 食物状态保存间隔（秒）- 每5秒保存一次
    [SerializeField] private bool enableAutoSave = true; // 是否启用自动保存
    
    // 运行时宠物数据映射 (petId -> PetController2D)
    private Dictionary<string, PetController2D> activePets = new Dictionary<string, PetController2D>();
    
    // 定时器
    private float positionSaveTimer = 0f;
    private float foodSaveTimer = 0f;
    
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
        
        // 定时保存食物状态
        foodSaveTimer += Time.deltaTime;
        if (foodSaveTimer >= foodSaveInterval)
        {
            SaveFoodStates();
            foodSaveTimer = 0f;
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
        // Debug.Log($"[GameDataManager] SyncToSave 被调用，immediate={immediate}");
        
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
        
        // 同步食物数据
        SyncFoodData(saveData);
        
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
            
            // 同步年龄数据
            petSaveData.purchaseDate = petController.PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss");
            
            // 同步厌倦时间
            petSaveData.lastBoredomTime = petController.LastBoredomTime;
            
            // Debug.Log($"[GameDataManager] 同步宠物 {petId} 厌倦状态: isBored={petSaveData.isBored}, lastBoredomTime={petSaveData.lastBoredomTime}");
        }
    }
    
    /// <summary>
    /// 同步食物数据
    /// </summary>
    private void SyncFoodData(SaveData saveData)
    {
        // 清空旧的食物数据
        saveData.worldData.foods.Clear();
        
        // 查找场景中所有的食物对象
        FoodController[] allFoods = FindObjectsOfType<FoodController>();
        
        foreach (FoodController food in allFoods)
        {
            if (food != null)
            {
                FoodSaveData foodSaveData = food.GetSaveData();
                saveData.worldData.foods.Add(foodSaveData);
                // Debug.Log($"[GameDataManager] 同步食物: {foodSaveData.foodId}, isEmpty={foodSaveData.isEmpty}");
            }
        }
        
        // Debug.Log($"[GameDataManager] 同步了 {saveData.worldData.foods.Count} 个食物的存档数据");
    }
    
    /// <summary>
    /// 只保存宠物位置（用于定时保存）
    /// </summary>
    private async void SavePetPositions()
    {
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData == null) 
        {
            // Debug.LogWarning("[位置保存] 无法获取当前存档数据");
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
                // Debug.LogWarning($"[位置保存] 未找到宠物 {petId} 的存档数据");
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
    
    /// <summary>
    /// 定时保存食物状态
    /// </summary>
    private async void SaveFoodStates()
    {
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData == null) 
        {
            // Debug.LogWarning("[食物保存] 无法获取当前存档数据");
            return;
        }
        
        // 获取当前场景中的所有食物
        FoodController[] allFoods = FindObjectsOfType<FoodController>();
        if (allFoods.Length == 0)
        {
            // Debug.Log("[食物保存] 场景中没有食物对象");
            return;
        }
        
        // 检查是否有食物状态变化
        bool hasChanges = false;
        var currentFoodData = new List<FoodSaveData>();
        
        foreach (FoodController food in allFoods)
        {
            if (food != null)
            {
                FoodSaveData currentData = food.GetSaveData();
                currentFoodData.Add(currentData);
                
                // 检查是否与存档中的数据不同
                var existingData = saveData.worldData.foods.Find(f => f.foodId == currentData.foodId);
                if (existingData == null || existingData.isEmpty != currentData.isEmpty)
                {
                    hasChanges = true;
                }
            }
        }
        
        if (hasChanges)
        {
            // 更新存档中的食物数据
            saveData.worldData.foods.Clear();
            saveData.worldData.foods.AddRange(currentFoodData);
            
            await SaveManager.Instance.SaveAsync();
            Debug.Log($"[食物保存] 定时保存了 {currentFoodData.Count} 个食物的状态");
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
            // Debug.Log("[GameDataManager] 宠物数据变化，已触发iOS数据同步");
        }
    }
    
    /// <summary>
    /// 食物状态变化通知
    /// </summary>
    public void OnFoodDataChanged()
    {
        OnDataChanged?.Invoke();
        
        // 只有在启用自动保存时才保存
        if (enableAutoSave)
        {
            // 立即保存食物数据变化（食物状态变化比较重要，立即保存）
            // Debug.Log("[GameDataManager] 食物状态发生变化，触发立即保存");
            SyncToSave(true);
        }
        else
        {
            // Debug.Log("[GameDataManager] 食物状态发生变化，但自动保存已禁用");
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
        // Debug.Log("应用退出，已保存所有数据");
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
    
    [ContextMenu("调试：检查食物状态")]
    public void DebugCheckFoodStates()
    {
        Debug.Log($"=== 食物状态检查 ===");
        
        // 检查场景中的所有食物
        FoodController[] allFoods = FindObjectsOfType<FoodController>();
        Debug.Log($"场景中的食物数量: {allFoods.Length}");
        
        foreach (var food in allFoods)
        {
            Debug.Log($"食物 {food.name} (ID: {food.FoodId}): 空盘状态={food.IsEmpty}, 位置={food.transform.position}, 爱心消耗={food.RefillHeartCost}");
        }
        
        // 检查存档中的食物数据
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData?.worldData?.foods != null)
        {
            Debug.Log($"存档中的食物数量: {saveData.worldData.foods.Count}");
            foreach (var foodData in saveData.worldData.foods)
            {
                Debug.Log($"存档食物 {foodData.foodId}: 空盘状态={foodData.isEmpty}, 位置={foodData.position}");
            }
        }
        else
        {
            Debug.Log("存档中没有食物数据");
        }
    }
    
    [ContextMenu("调试：强制保存食物状态")]
    public void DebugSaveFoodStates()
    {
        Debug.Log($"=== 强制保存食物状态 ===");
        OnFoodDataChanged();
        Debug.Log("食物状态保存完成");
    }
    
    [ContextMenu("调试：立即执行定时食物保存")]
    public void DebugSaveFoodStatesTimer()
    {
        Debug.Log($"=== 立即执行定时食物保存 ===");
        SaveFoodStates();
        Debug.Log("定时食物保存完成");
    }
    
    [ContextMenu("调试：检查宠物厌倦状态")]
    public void DebugCheckPetBoredomStates()
    {
        Debug.Log($"=== 宠物厌倦状态检查 ===");
        Debug.Log($"当前游戏时间: {Time.time:F1}秒");
        
        foreach (var kvp in activePets)
        {
            string petId = kvp.Key;
            PetController2D pet = kvp.Value;
            
            if (pet != null)
            {
                float timeSinceBoredom = pet.LastBoredomTime >= 0 ? Time.time - pet.LastBoredomTime : -1f;
                Debug.Log($"宠物 {petId} ({pet.PetDisplayName}):");
                Debug.Log($"  - IsBored: {pet.IsBored}");
                Debug.Log($"  - LastBoredomTime: {pet.LastBoredomTime:F1}秒");
                Debug.Log($"  - 厌倦已持续: {timeSinceBoredom:F1}秒");
                Debug.Log($"  - 剩余恢复时间: {pet.BoredomRecoveryRemaining:F1}分钟");
            }
        }
        
        // 检查存档中的厌倦数据
        SaveData saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData?.petsData != null)
        {
            Debug.Log($"存档中的宠物厌倦数据:");
            foreach (var petData in saveData.petsData)
            {
                Debug.Log($"存档宠物 {petData.petId}: isBored={petData.isBored}, lastBoredomTime={petData.lastBoredomTime:F1}");
            }
        }
    }
    
    #endregion
} 