using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏初始化管理器 - 负责在Gameplay场景启动时初始化存档系统
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool autoInitializeOnStart = true;
    [SerializeField] private bool createDefaultPetIfEmpty = true;
    [SerializeField] private string defaultPetPrefabName = "Pet_CatBrown";
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 初始化状态
    private bool isInitialized = false;
    
    // 事件
    public System.Action OnInitializationComplete;
    public System.Action<string> OnInitializationFailed;
    
    private void Start()
    {
        if (autoInitializeOnStart)
        {
            StartCoroutine(InitializeGameAsync());
        }
    }
    
    /// <summary>
    /// 异步初始化游戏
    /// </summary>
    public IEnumerator InitializeGameAsync()
    {
        if (isInitialized)
        {
            DebugLog("游戏已经初始化过了");
            yield break;
        }
        
        // DebugLog("开始初始化游戏...");
        
        bool initializationSuccess = false;
        string errorMessage = "";
        
        // 1. 确保核心管理器存在
        yield return EnsureCoreManagers();
        
        // 2. 清理旧的宠物引用
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ClearAllPets();
        }
        
        if (PetSpawner.Instance != null)
        {
            PetSpawner.Instance.ClearAllPets();
        }
        
        // 3. 加载存档数据
        Task<SaveData> loadTask = SaveManager.Instance.LoadSaveAsync();
        yield return new WaitUntil(() => loadTask.IsCompleted);
        
        SaveData saveData = loadTask.Result;
        if (saveData == null)
        {
            errorMessage = "加载存档失败";
        }
        else
        {
            // DebugLog($"存档加载成功，宠物数量: {saveData.petsData.Count}");
            
            // 4. 暂时禁用自动保存，避免在初始化过程中覆盖存档数据
            bool originalAutoSaveState = GameDataManager.Instance.IsAutoSaveEnabled;
            GameDataManager.Instance.SetAutoSaveEnabled(false);
            // Debug.Log("[GameInitializer] 已暂时禁用自动保存");
            
            // 5. 同步玩家数据到PlayerManager
            yield return SyncPlayerData(saveData);
            
            // 6. 生成宠物
            yield return SpawnPets(saveData);
            
            // 7. 加载食物状态
            yield return LoadFoodStates(saveData);
            
            // 8. 重新启用自动保存
            GameDataManager.Instance.SetAutoSaveEnabled(originalAutoSaveState);
            // Debug.Log("[GameInitializer] 已重新启用自动保存");
            
            // 9. 如果没有宠物且启用了默认宠物创建，创建默认宠物
            if (saveData.petsData.Count == 0 && createDefaultPetIfEmpty)
            {
                yield return CreateDefaultPet();
            }
            
            initializationSuccess = true;
        }
        
        if (initializationSuccess)
        {
            isInitialized = true;
            // DebugLog("游戏初始化完成！");
            OnInitializationComplete?.Invoke();
        }
        else
        {
            Debug.LogError($"游戏初始化失败: {errorMessage}");
            OnInitializationFailed?.Invoke(errorMessage);
        }
    }
    
    /// <summary>
    /// 确保核心管理器存在
    /// </summary>
    private IEnumerator EnsureCoreManagers()
    {
        // DebugLog("检查核心管理器...");
        
        // 确保SaveManager存在
        if (SaveManager.Instance == null)
        {
            throw new System.Exception("SaveManager初始化失败");
        }
        
        // 确保GameDataManager存在
        if (GameDataManager.Instance == null)
        {
            throw new System.Exception("GameDataManager初始化失败");
        }
        
        // 确保PetSpawner存在
        if (PetSpawner.Instance == null)
        {
            throw new System.Exception("PetSpawner初始化失败");
        }
        
        // 确保PlayerManager存在
        if (PlayerManager.Instance == null)
        {
            throw new System.Exception("PlayerManager初始化失败");
        }
        

        
        // 确保IOSDataBridge存在（用于iOS数据同步）
        if (IOSDataBridge.Instance == null)
        {
            DebugLog("警告: IOSDataBridge初始化失败，iOS数据同步将不可用");
        }
        else
        {
            DebugLog("IOSDataBridge已初始化");
        }
        
        // DebugLog("所有核心管理器检查完成");
        yield return null;
    }
    
    /// <summary>
    /// 同步玩家数据
    /// </summary>
    private IEnumerator SyncPlayerData(SaveData saveData)
    {
        // DebugLog("同步玩家数据...");
        
        if (PlayerManager.Instance != null && saveData.playerData != null)
        {
            // 设置爱心货币（不触发事件，避免立即保存）
            int targetCurrency = saveData.playerData.heartCurrency;
            PlayerManager.Instance.SetHeartCurrencyDirect(targetCurrency);
            
            // 等待一帧后触发货币变化事件，确保UI组件已经初始化
            yield return null;
            PlayerManager.Instance.TriggerCurrencyChangeEvent();
            
            // DebugLog($"玩家数据同步完成，爱心货币: {targetCurrency}");
        }
        else
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// 生成宠物
    /// </summary>
    private IEnumerator SpawnPets(SaveData saveData)
    {
        // DebugLog("开始生成宠物...");
        
        if (saveData.petsData != null && saveData.petsData.Count > 0)
        {
            Task spawnTask = PetSpawner.Instance.SpawnPetsFromSaveData(saveData);
            yield return new WaitUntil(() => spawnTask.IsCompleted);
            
            if (spawnTask.Exception != null)
            {
                throw spawnTask.Exception;
            }
            
            // DebugLog($"宠物生成完成，共生成 {saveData.petsData.Count} 只宠物");
        }
        else
        {
            // DebugLog("存档中没有宠物数据");
        }
    }
    
    /// <summary>
    /// 创建默认宠物
    /// </summary>
    private IEnumerator CreateDefaultPet()
    {
        // DebugLog($"创建默认宠物: {defaultPetPrefabName}");
        
        Task<PetController2D> createTask = PetSpawner.Instance.CreateNewPet(defaultPetPrefabName, Vector3.zero);
        yield return new WaitUntil(() => createTask.IsCompleted);
        
        if (createTask.Exception != null)
        {
            Debug.LogError($"创建默认宠物失败: {createTask.Exception.Message}");
            yield break;
        }
        
        PetController2D newPet = createTask.Result;
        if (newPet != null)
        {
            // 设置默认属性
            newPet.PetDisplayName = "小可爱";
            newPet.PetIntroduction = "你的第一只宠物！";
            
            // DebugLog($"默认宠物创建成功: {newPet.name}");
        }
        else
        {
            Debug.LogWarning("默认宠物创建失败");
        }
    }
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GameInitializer] {message}");
        }
    }
    
    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
    
    /// <summary>
    /// 手动触发初始化
    /// </summary>
    [ContextMenu("手动初始化")]
    public void ManualInitialize()
    {
        if (!isInitialized)
        {
            StartCoroutine(InitializeGameAsync());
        }
        else
        {
            DebugLog("游戏已经初始化，跳过");
        }
    }
    
    /// <summary>
    /// 重置游戏状态（清理所有宠物，重新初始化）
    /// </summary>
    [ContextMenu("重置游戏状态")]
    public void ResetGameState()
    {
        DebugLog("重置游戏状态...");
        
        // 清理现有宠物
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ClearAllPets();
        }
        
        if (PetSpawner.Instance != null)
        {
            PetSpawner.Instance.ClearAllPets();
        }
        
        // 重置初始化状态
        isInitialized = false;
        
        // 重新初始化
        StartCoroutine(InitializeGameAsync());
    }
    
    /// <summary>
    /// 加载食物状态
    /// </summary>
    private IEnumerator LoadFoodStates(SaveData saveData)
    {
        if (saveData?.worldData?.foods == null || saveData.worldData.foods.Count == 0)
        {
            // DebugLog("没有食物数据需要加载");
            yield break;
        }
        
        // 创建食物数据的副本，避免在遍历过程中被修改
        var foodDataCopy = new List<FoodSaveData>(saveData.worldData.foods);
        
        Debug.Log($"[GameInitializer] 开始加载 {foodDataCopy.Count} 个食物的状态...");
        
        // 查找场景中所有的食物对象
        FoodController[] allFoods = FindObjectsOfType<FoodController>();
        
        foreach (FoodSaveData foodSaveData in foodDataCopy)
        {
            // 根据位置和类型查找对应的食物对象
            FoodController matchedFood = FindFoodByIdOrPosition(allFoods, foodSaveData);
            
            if (matchedFood != null)
            {
                Debug.Log($"[GameInitializer] 找到匹配食物 {matchedFood.name}，加载状态: isEmpty={foodSaveData.isEmpty}");
                matchedFood.LoadFromSaveData(foodSaveData);
                Debug.Log($"[GameInitializer] 加载完成，当前食物状态: isEmpty={matchedFood.IsEmpty}");
            }
            else
            {
                Debug.LogWarning($"[GameInitializer] 未找到匹配的食物对象: {foodSaveData.foodId}");
            }
            
            // 每处理一个食物对象后让出一帧，避免阻塞
            yield return null;
        }
        
        Debug.Log("[GameInitializer] 食物状态加载完成");
        
        // 延迟1秒后再检查一次食物状态，看是否被其他地方修改了
        yield return new WaitForSeconds(1f);
        Debug.Log("[GameInitializer] 1秒后检查食物状态...");
        FoodController[] allFoodsCheck = FindObjectsOfType<FoodController>();
        foreach (var food in allFoodsCheck)
        {
            Debug.Log($"[GameInitializer] 延迟检查 - 食物 {food.name}: isEmpty={food.IsEmpty}");
        }
    }
    
    /// <summary>
    /// 根据ID或位置查找食物对象
    /// </summary>
    private FoodController FindFoodByIdOrPosition(FoodController[] allFoods, FoodSaveData saveData)
    {
        // 首先尝试通过ID匹配
        foreach (FoodController food in allFoods)
        {
            if (food.FoodId == saveData.foodId)
            {
                return food;
            }
        }
        
        // 如果ID匹配失败，尝试通过位置和类型匹配
        foreach (FoodController food in allFoods)
        {
            Vector3 foodPos = food.transform.position;
            Vector3 savePos = saveData.position;
            
            // 检查位置是否接近（误差范围0.5单位）
            if (Vector3.Distance(foodPos, savePos) < 0.5f)
            {
                string foodType = food.gameObject.name.Replace("(Clone)", "");
                if (foodType == saveData.foodType)
                {
                    return food;
                }
            }
        }
        
        return null;
    }
} 