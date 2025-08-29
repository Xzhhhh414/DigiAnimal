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
    
    [Header("默认家具配置")]
    [SerializeField] private DefaultFurnitureConfigAsset defaultFurnitureConfig; // ScriptableObject配置文件
    
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
            // DebugLog("游戏已经初始化过了");
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
            
            // 4. 为老账号补充缺失的默认家具（如果有新增的默认家具）
            //Debug.Log("[GameInitializer] 开始检查并补充默认家具...");
            SupplementMissingDefaultFurniture(saveData);
            //Debug.Log("[GameInitializer] 默认家具补充检查完成");
            
            // 5. 暂时禁用自动保存，避免在初始化过程中覆盖存档数据
            bool originalAutoSaveState = GameDataManager.Instance.IsAutoSaveEnabled;
            GameDataManager.Instance.SetAutoSaveEnabled(false);
            // Debug.Log("[GameInitializer] 已暂时禁用自动保存");
            
            // 5. 同步玩家数据到PlayerManager
            yield return SyncPlayerData(saveData);
            
            // 6. 生成宠物
            yield return SpawnPets(saveData);
            
            // 7. 生成家具（植物和食物等）
            yield return SpawnFurniture(saveData);
            
            // 9. 重新启用自动保存
            GameDataManager.Instance.SetAutoSaveEnabled(originalAutoSaveState);
            // Debug.Log("[GameInitializer] 已重新启用自动保存");
            
            // 9. 如果是新账号，创建默认内容（现在只处理宠物，家具在Start场景已创建）
            bool isNewAccount = saveData.petsData.Count == 0;
            
                    // Debug.Log($"[GameInitializer] 新账号检查 - 宠物:{saveData.petsData.Count}, 植物:{saveData.worldData.plants?.Count ?? 0}, 食物:{saveData.worldData.foods?.Count ?? 0}");
        // Debug.Log($"[GameInitializer] 是否新账号:{isNewAccount}");
            
            if (isNewAccount)
            {
                // 创建默认宠物（只在直接进入Gameplay场景时需要）
                if (createDefaultPetIfEmpty)
                {
                    yield return CreateDefaultPet();
                }
            }
            
            // 家具数据现在在Start场景创建，这里不再需要额外创建
            
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
            //DebugLog("警告: IOSDataBridge初始化失败，iOS数据同步将不可用");
        }
        else
        {
            //DebugLog("IOSDataBridge已初始化");
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
            // Debug.Log($"[GameInitializer] {message}");
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
            // DebugLog("游戏已经初始化，跳过");
        }
    }
    
    /// <summary>
    /// 重置游戏状态（清理所有宠物，重新初始化）
    /// </summary>
    [ContextMenu("重置游戏状态")]
    public void ResetGameState()
    {
        // DebugLog("重置游戏状态...");
        
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
        
        // Debug.Log($"[GameInitializer] 开始加载 {foodDataCopy.Count} 个食物的状态...");
        
        // 查找场景中所有的食物对象
        FoodController[] allFoods = FindObjectsOfType<FoodController>();
        
        foreach (FoodSaveData foodSaveData in foodDataCopy)
        {
            // 根据位置和类型查找对应的食物对象
            FoodController matchedFood = FindFoodByIdOrPosition(allFoods, foodSaveData);
            
            if (matchedFood != null)
            {
                // Debug.Log($"[GameInitializer] 找到匹配食物 {matchedFood.name}，加载状态: isEmpty={foodSaveData.isEmpty}");
                matchedFood.LoadFromSaveData(foodSaveData);
                // Debug.Log($"[GameInitializer] 加载完成，当前食物状态: isEmpty={matchedFood.IsEmpty}");
            }
            else
            {
                Debug.LogWarning($"[GameInitializer] 未找到匹配的食物对象: {foodSaveData.foodId}");
            }
            
            // 每处理一个食物对象后让出一帧，避免阻塞
            yield return null;
        }
        
        // Debug.Log("[GameInitializer] 食物状态加载完成");
        
        // 延迟1秒后再检查一次食物状态，看是否被其他地方修改了
        yield return new WaitForSeconds(1f);
        // Debug.Log("[GameInitializer] 1秒后检查食物状态...");
        FoodController[] allFoodsCheck = FindObjectsOfType<FoodController>();
        foreach (var food in allFoodsCheck)
        {
            // Debug.Log($"[GameInitializer] 延迟检查 - 食物 {food.name}: isEmpty={food.IsEmpty}");
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
    

    
    /// <summary>
    /// 生成家具（新的统一家具生成系统）
    /// </summary>
    private IEnumerator SpawnFurniture(SaveData saveData)
    {
        if (FurnitureSpawner.Instance == null)
        {
            Debug.LogError("[GameInitializer] FurnitureSpawner未初始化！");
            yield break;
        }
        
        // 1. 先加载ID计数器
        int savedCounter = saveData.nextFurnitureIdCounter;
        FurnitureSpawner.Instance.LoadIdCounter(savedCounter);
        // Debug.Log($"[GameInitializer] 加载家具ID计数器: {savedCounter}");
        
        // 2. 清理现有的场景预置家具（如果有的话）
        yield return ClearSceneFurniture();
        
        // 3. 生成植物
        // Debug.Log($"[GameInitializer] 植物数据检查 - worldData: {saveData?.worldData != null}, plants: {saveData?.worldData?.plants != null}, count: {saveData?.worldData?.plants?.Count ?? -1}");
        
        if (saveData?.worldData?.plants != null && saveData.worldData.plants.Count > 0)
        {
            // Debug.Log($"[GameInitializer] 开始生成 {saveData.worldData.plants.Count} 个植物...");
            
            foreach (var plantData in saveData.worldData.plants)
            {
                // Debug.Log($"[GameInitializer] 正在生成植物: ID={plantData.plantId}, ConfigId={plantData.configId}, Position={plantData.position}");
                var spawnedFurniture = FurnitureSpawner.Instance.SpawnFurnitureFromSaveData(plantData);
                
                if (spawnedFurniture != null)
                {
                    // Debug.Log($"[GameInitializer] 植物生成成功: {spawnedFurniture.FurnitureName}");
                }
                else
                {
                    Debug.LogWarning($"[GameInitializer] 植物生成失败: {plantData.plantId}");
                }
                
                yield return null; // 每生成一个家具后让出一帧
            }
        }
        else
        {
            // Debug.Log("[GameInitializer] 跳过植物生成 - 没有植物数据");
        }
        
        // 4. 生成食物
        // Debug.Log($"[GameInitializer] 食物数据检查 - worldData: {saveData?.worldData != null}, foods: {saveData?.worldData?.foods != null}, count: {saveData?.worldData?.foods?.Count ?? -1}");
        
        if (saveData?.worldData?.foods != null && saveData.worldData.foods.Count > 0)
        {
            // Debug.Log($"[GameInitializer] 开始生成 {saveData.worldData.foods.Count} 个食物...");
            
            foreach (var foodData in saveData.worldData.foods)
            {
                // Debug.Log($"[GameInitializer] 正在生成食物: ID={foodData.foodId}, ConfigId={foodData.configId}, Position={foodData.position}");
                var spawnedFurniture = FurnitureSpawner.Instance.SpawnFurnitureFromSaveData(foodData);
                
                if (spawnedFurniture != null)
                {
                    // Debug.Log($"[GameInitializer] 食物生成成功: {spawnedFurniture.FurnitureName}");
                }
                else
                {
                    Debug.LogWarning($"[GameInitializer] 食物生成失败: {foodData.foodId}");
                }
                
                yield return null; // 每生成一个家具后让出一帧
            }
        }
        else
        {
            // Debug.Log("[GameInitializer] 跳过食物生成 - 没有食物数据");
        }
        
        // 5. 生成音响
        //Debug.Log($"[GameInitializer] 音响数据检查 - worldData: {saveData?.worldData != null}, speakers: {saveData?.worldData?.speakers != null}, count: {saveData?.worldData?.speakers?.Count ?? -1}");
        
        if (saveData?.worldData?.speakers != null && saveData.worldData.speakers.Count > 0)
        {
            //Debug.Log($"[GameInitializer] 开始生成 {saveData.worldData.speakers.Count} 个音响...");
            
            foreach (var speakerData in saveData.worldData.speakers)
            {
                //Debug.Log($"[GameInitializer] 正在生成音响: ID={speakerData.speakerId}, ConfigId={speakerData.configId}, Position={speakerData.position}");
                var spawnedFurniture = FurnitureSpawner.Instance.SpawnFurnitureFromSaveData(speakerData);
                
                if (spawnedFurniture != null)
                {
                    //Debug.Log($"[GameInitializer] 音响生成成功: {spawnedFurniture.FurnitureName}");
                }
                else
                {
                    Debug.LogWarning($"[GameInitializer] 音响生成失败: {speakerData.speakerId}");
                }
                
                yield return null; // 每生成一个家具后让出一帧
            }
        }
        else
        {
            //Debug.Log("[GameInitializer] 跳过音响生成 - 没有音响数据");
        }
        
        // 6. 生成电视机
        //Debug.Log($"[GameInitializer] 电视机数据检查 - worldData: {saveData?.worldData != null}, tvs: {saveData?.worldData?.tvs != null}, count: {saveData?.worldData?.tvs?.Count ?? -1}");
        
        if (saveData?.worldData?.tvs != null && saveData.worldData.tvs.Count > 0)
        {
            //Debug.Log($"[GameInitializer] 开始生成 {saveData.worldData.tvs.Count} 个电视机...");
            
            foreach (var tvData in saveData.worldData.tvs)
            {
                //Debug.Log($"[GameInitializer] 正在生成电视机: ID={tvData.tvId}, ConfigId={tvData.configId}, Position={tvData.position}");
                var spawnedFurniture = FurnitureSpawner.Instance.SpawnFurnitureFromSaveData(tvData);
                
                if (spawnedFurniture != null)
                {
                    //Debug.Log($"[GameInitializer] 电视机生成成功: {spawnedFurniture.FurnitureName}");
                }
                else
                {
                    Debug.LogWarning($"[GameInitializer] 电视机生成失败: {tvData.tvId}");
                }
            }
        }
        else
        {
            //Debug.Log("[GameInitializer] 跳过电视机生成 - 没有电视机数据");
        }
        
        // TODO: 在这里添加其他类型家具的生成逻辑
        // 例如：装饰品等
        
        // Debug.Log("[GameInitializer] 家具生成完成");
    }
    

    
    /// <summary>
    /// 清理场景中预置的家具对象
    /// </summary>
    private IEnumerator ClearSceneFurniture()
    {
        // 清理场景中预置的植物对象
        PlantController[] existingPlants = FindObjectsOfType<PlantController>();
        if (existingPlants.Length > 0)
        {
            //Debug.Log($"[GameInitializer] 清理 {existingPlants.Length} 个场景预置植物");
            
            foreach (var plant in existingPlants)
            {
                if (plant != null)
                {
                    DestroyImmediate(plant.gameObject);
                }
            }
        }
        
        // 清理场景中预置的食物对象
        FoodController[] existingFoods = FindObjectsOfType<FoodController>();
        if (existingFoods.Length > 0)
        {
           //Debug.Log($"[GameInitializer] 清理 {existingFoods.Length} 个场景预置食物");
            
            foreach (var food in existingFoods)
            {
                if (food != null)
                {
                    DestroyImmediate(food.gameObject);
                }
            }
        }
        
        // 清理场景中预置的音响对象
        SpeakerController[] existingSpeakers = FindObjectsOfType<SpeakerController>();
        if (existingSpeakers.Length > 0)
        {
            //Debug.Log($"[GameInitializer] 清理 {existingSpeakers.Length} 个场景预置音响");
            
            foreach (var speaker in existingSpeakers)
            {
                if (speaker != null)
                {
                    DestroyImmediate(speaker.gameObject);
                }
            }
        }
        
        // 清理场景中预置的电视机对象
        TVController[] existingTVs = FindObjectsOfType<TVController>();
        if (existingTVs.Length > 0)
        {
            //Debug.Log($"[GameInitializer] 清理 {existingTVs.Length} 个场景预置电视机");
            
            foreach (var tv in existingTVs)
            {
                if (tv != null)
                {
                    DestroyImmediate(tv.gameObject);
                }
            }
        }
        
        // TODO: 清理其他类型的预置家具
        
        yield return null;
    }
    
    /// <summary>
    /// 为老账号补充缺失的默认家具
    /// </summary>
    private void SupplementMissingDefaultFurniture(SaveData saveData)
    {
        List<DefaultFurnitureConfig> furnitureConfigList = GetDefaultFurnitureList();
        
        if (furnitureConfigList == null || furnitureConfigList.Count == 0)
        {
           //Debug.Log("[GameInitializer] 没有找到默认家具配置，跳过补充");
            return;
        }
        
        // 确保worldData和相关列表存在
        EnsureWorldDataExists(saveData);
        
        int addedCount = 0;
        
        // 遍历每个默认家具配置
        foreach (var defaultConfig in furnitureConfigList)
        {
            if (string.IsNullOrEmpty(defaultConfig.furnitureConfigId))
                continue;
                
            // 检查是否已存在该saveDataId的默认家具
            if (!HasFurnitureWithDefaultId(saveData, defaultConfig.saveDataId))
            {
                // 缺失该默认家具，需要补充
                CreateFurnitureByConfigId(saveData, defaultConfig);
                addedCount++;
                
                //Debug.Log($"[GameInitializer] 为老账号补充家具: {defaultConfig.saveDataId} (ConfigId: {defaultConfig.furnitureConfigId}) at {defaultConfig.position}");
            }
        }
        
        if (addedCount > 0)
        {
            //Debug.Log($"[GameInitializer] 老账号补充完成，共添加 {addedCount} 个家具");
            
            // 保存修改后的存档
            SaveManager.Instance.SetCurrentSaveData(saveData);
            bool saveSuccess = SaveManager.Instance.Save();
            
            if (saveSuccess)
            {
                //Debug.Log("[GameInitializer] 默认家具补充保存成功");
            }
            else
            {
                Debug.LogError("[GameInitializer] 默认家具补充保存失败");
            }
        }
        else
        {
            //Debug.Log("[GameInitializer] 老账号无需补充家具");
        }
    }
    
    /// <summary>
    /// 检查存档中是否已存在指定saveDataId的默认家具
    /// </summary>
    private bool HasFurnitureWithDefaultId(SaveData saveData, string saveDataId)
    {
        if (string.IsNullOrEmpty(saveDataId))
            return false;
            
        // 检查植物
        if (saveData.worldData?.plants != null)
        {
            foreach (var plant in saveData.worldData.plants)
            {
                if (!string.IsNullOrEmpty(plant.saveDataId) && plant.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查食物
        if (saveData.worldData?.foods != null)
        {
            foreach (var food in saveData.worldData.foods)
            {
                if (!string.IsNullOrEmpty(food.saveDataId) && food.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查音响
        if (saveData.worldData?.speakers != null)
        {
            foreach (var speaker in saveData.worldData.speakers)
            {
                if (!string.IsNullOrEmpty(speaker.saveDataId) && speaker.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查电视机
        if (saveData.worldData?.tvs != null)
        {
            foreach (var tv in saveData.worldData.tvs)
            {
                if (!string.IsNullOrEmpty(tv.saveDataId) && tv.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 根据ConfigId创建对应的家具数据
    /// </summary>
    private void CreateFurnitureByConfigId(SaveData saveData, DefaultFurnitureConfig config)
    {
        if (string.IsNullOrEmpty(config.furnitureConfigId))
        {
            Debug.LogWarning("[GameInitializer] 家具ConfigId为空，跳过创建");
            return;
        }
        
        //Debug.Log($"[GameInitializer] 正在创建家具: DefaultId='{config.saveDataId}', ConfigId='{config.furnitureConfigId}', Position={config.position}");
        
        string furnitureId = GenerateUniqueFurnitureId(saveData);
        
        // 根据ConfigId判断家具类型并创建对应的存档数据
        if (config.furnitureConfigId.ToLower().Contains("plant"))
        {
            // 创建植物数据
            PlantSaveData plantData = new PlantSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, 100, config.position, 0, 25);
            plantData.lastHealthUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.worldData.plants.Add(plantData);
            //Debug.Log($"[GameInitializer] 创建植物数据: {config.saveDataId} (ConfigId: {config.furnitureConfigId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("food"))
        {
            // 创建食物数据
            FoodSaveData foodData = new FoodSaveData(furnitureId, "猫粮", config.furnitureConfigId, config.saveDataId, false, config.position, 3, 25);
            saveData.worldData.foods.Add(foodData);
            //Debug.Log($"[GameInitializer] 创建食物数据: {config.saveDataId} (ConfigId: {config.furnitureConfigId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("speaker"))
        {
            // 创建音响数据
            SpeakerSaveData speakerData = new SpeakerSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, config.position, 0, 0f, false);
            saveData.worldData.speakers.Add(speakerData);
            //Debug.Log($"[GameInitializer] 创建音响数据: {config.saveDataId} (ConfigId: {config.furnitureConfigId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("tv"))
        {
            // 创建电视机数据
            TVSaveData tvData = new TVSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, config.position, false);
            saveData.worldData.tvs.Add(tvData);
            //Debug.Log($"[GameInitializer] 创建电视机数据: {config.saveDataId} (ConfigId: {config.furnitureConfigId}) at {config.position}");
        }
        else
        {
            Debug.LogWarning($"[GameInitializer] 未知的家具类型: {config.furnitureConfigId}");
        }
    }
    
    /// <summary>
    /// 确保WorldData和相关列表存在
    /// </summary>
    private void EnsureWorldDataExists(SaveData saveData)
    {
        if (saveData.worldData == null)
        {
            saveData.worldData = new WorldSaveData();
        }
        
        if (saveData.worldData.plants == null)
        {
            saveData.worldData.plants = new List<PlantSaveData>();
        }
        
        if (saveData.worldData.foods == null)
        {
            saveData.worldData.foods = new List<FoodSaveData>();
        }
        
        if (saveData.worldData.speakers == null)
        {
            saveData.worldData.speakers = new List<SpeakerSaveData>();
        }
        
        if (saveData.worldData.tvs == null)
        {
            saveData.worldData.tvs = new List<TVSaveData>();
        }
    }
    
    /// <summary>
    /// 获取默认家具配置列表
    /// </summary>
    private List<DefaultFurnitureConfig> GetDefaultFurnitureList()
    {
        if (defaultFurnitureConfig != null)
        {
            var configItems = defaultFurnitureConfig.GetDefaultFurnitureItems();
            if (configItems != null && configItems.Count > 0)
            {
                //Debug.Log($"[GameInitializer] 从ScriptableObject读取到 {configItems.Count} 个默认家具配置");
                return configItems;
            }
            else
            {
                Debug.LogWarning("[GameInitializer] ScriptableObject配置为空");
            }
        }
        else
        {
            Debug.LogError("[GameInitializer] 未配置DefaultFurnitureConfigAsset！请在Inspector中设置");
        }
        
        return null;
    }

    /// <summary>
    /// 生成唯一的家具ID
    /// </summary>
    private string GenerateUniqueFurnitureId(SaveData saveData)
    {
        string id = $"furniture_{saveData.nextFurnitureIdCounter}";
        saveData.nextFurnitureIdCounter++;
        return id;
    }

} 