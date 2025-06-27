using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 宠物生成器 - 负责根据存档数据创建宠物对象
/// </summary>
public class PetSpawner : MonoBehaviour
{
    // 单例模式
    private static PetSpawner _instance;
    public static PetSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetSpawner>();
                if (_instance == null)
                {
                    GameObject spawnerObj = new GameObject("PetSpawner");
                    _instance = spawnerObj.AddComponent<PetSpawner>();
                }
            }
            return _instance;
        }
    }
    
    [Header("生成设置")]
    [SerializeField] private Transform petContainer; // 宠物容器（用于组织层级）
    
    // 默认生成位置（固定坐标，不需要在Inspector中配置）
    private readonly Vector3 defaultSpawnPosition = new Vector3(2.5f, -2f, 0f);
    
    // 已加载的预制体缓存 (resourcePath -> GameObject)
    private Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();
    
    // 生成的宠物实例 (petId -> PetController2D)
    private Dictionary<string, PetController2D> spawnedPets = new Dictionary<string, PetController2D>();
    
    private void Awake()
    {
        // 单例模式设置
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // 如果没有设置容器，创建一个
        if (petContainer == null)
        {
            GameObject container = new GameObject("Pets");
            petContainer = container.transform;
        }
    }
    
    private void OnDestroy()
    {
        // 清理预制体缓存（Resources预制体不需要手动释放）
        loadedPrefabs.Clear();
    }
    
    #region 公共API
    
    /// <summary>
    /// 根据存档数据生成所有宠物
    /// </summary>
    public async Task SpawnPetsFromSaveData(SaveData saveData)
    {
        if (saveData?.petsData == null || saveData.petsData.Count == 0)
        {
            // Debug.Log("存档中没有宠物数据");
            return;
        }
        
        // Debug.Log($"开始生成 {saveData.petsData.Count} 只宠物");
        
        List<Task> spawnTasks = new List<Task>();
        
        foreach (var petData in saveData.petsData)
        {
            // 并行生成所有宠物
            spawnTasks.Add(SpawnPetFromData(petData));
        }
        
        await Task.WhenAll(spawnTasks);
        
        // Debug.Log($"所有宠物生成完成，成功生成 {spawnedPets.Count} 只宠物");
    }
    
    /// <summary>
    /// 根据单个宠物数据生成宠物
    /// </summary>
    public async Task<PetController2D> SpawnPetFromData(PetSaveData petData)
    {
        if (petData == null)
        {
            Debug.LogError("宠物数据为空");
            return null;
        }
        
        try
        {
            // 构建Resources路径
            string resourcePath = GetResourcePath(petData.prefabName);
            
            // 加载预制体
            GameObject prefab = await LoadPetPrefab(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"无法加载宠物预制体: {resourcePath}");
                return null;
            }
            
            // 实例化宠物
            GameObject petInstance = Instantiate(prefab, petContainer);
            petInstance.name = $"{petData.prefabName}_{petData.petId}";
            
            // 获取PetController2D组件
            PetController2D petController = petInstance.GetComponent<PetController2D>();
            if (petController == null)
            {
                Debug.LogError($"宠物预制体 {petData.prefabName} 缺少PetController2D组件");
                Destroy(petInstance);
                return null;
            }
            
            // 应用存档数据
            ApplyPetData(petController, petData);
            
            // 注册到系统
            spawnedPets[petData.petId] = petController;
            GameDataManager.Instance.RegisterPet(petData.petId, petController);
            
            // Debug.Log($"[宠物生成] 宠物 {petData.petId} 生成完成，最终位置: {petController.transform.position}");
            // Debug.Log($"宠物生成成功: {petData.petId} ({petData.displayName})");
            return petController;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成宠物失败 {petData.petId}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 创建新宠物（不基于存档数据）
    /// </summary>
    public async Task<PetController2D> CreateNewPet(string prefabName, Vector3 position = default)
    {
        // 创建新的宠物存档数据
        PetSaveData newPetData = new PetSaveData(prefabName);
        
        if (position == default)
        {
            position = defaultSpawnPosition;
        }
        newPetData.position = position;
        
        // 生成宠物
        PetController2D newPet = await SpawnPetFromData(newPetData);
        
        if (newPet != null)
        {
            // 添加到当前存档
            SaveData currentSave = SaveManager.Instance.GetCurrentSaveData();
            if (currentSave != null)
            {
                currentSave.petsData.Add(newPetData);
                await SaveManager.Instance.SaveAsync();
                Debug.Log($"新宠物已添加到存档: {newPetData.petId}");
                
                // 通知GameDataManager宠物数据发生变化
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.OnPetDataChanged();
                    // Debug.Log($"[PetSpawner] 新宠物创建完成，已通知数据变化: {newPetData.petId}");
                }
            }
        }
        
        return newPet;
    }
    
    /// <summary>
    /// 移除宠物
    /// </summary>
    public async Task<bool> RemovePet(string petId)
    {
        if (!spawnedPets.ContainsKey(petId))
        {
            Debug.LogWarning($"未找到要移除的宠物: {petId}");
            return false;
        }
        
        // 从游戏世界中移除
        PetController2D petController = spawnedPets[petId];
        if (petController != null)
        {
            Destroy(petController.gameObject);
        }
        
        // 从系统中注销
        spawnedPets.Remove(petId);
        GameDataManager.Instance.UnregisterPet(petId);
        
        // 从存档中移除
        SaveData currentSave = SaveManager.Instance.GetCurrentSaveData();
        if (currentSave != null)
        {
            currentSave.petsData.RemoveAll(p => p.petId == petId);
            await SaveManager.Instance.SaveAsync();
            
            // 通知GameDataManager宠物数据发生变化
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.OnPetDataChanged();
                // Debug.Log($"[PetSpawner] 宠物移除完成，已通知数据变化: {petId}");
            }
        }
        
        //Debug.Log($"宠物已移除: {petId}");
        return true;
    }
    
    /// <summary>
    /// 清理所有生成的宠物
    /// </summary>
    public void ClearAllPets()
    {
        foreach (var kvp in spawnedPets)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
            GameDataManager.Instance.UnregisterPet(kvp.Key);
        }
        
        spawnedPets.Clear();
        //Debug.Log("所有宠物已清理");
    }
    
    /// <summary>
    /// 获取生成的宠物
    /// </summary>
    public PetController2D GetSpawnedPet(string petId)
    {
        spawnedPets.TryGetValue(petId, out PetController2D pet);
        return pet;
    }
    
    /// <summary>
    /// 获取所有生成的宠物
    /// </summary>
    public Dictionary<string, PetController2D> GetAllSpawnedPets()
    {
        return new Dictionary<string, PetController2D>(spawnedPets);
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 构建Resources路径
    /// </summary>
    private string GetResourcePath(string prefabName)
    {
        // 根据约定构建路径: Prefab/Pet/Pet_宠物名字
        return $"Prefab/Pet/{prefabName}";
    }
    
    /// <summary>
    /// 加载宠物预制体
    /// </summary>
    private async Task<GameObject> LoadPetPrefab(string resourcePath)
    {
        // 检查缓存
        if (loadedPrefabs.ContainsKey(resourcePath))
        {
            return loadedPrefabs[resourcePath];
        }
        
        try
        {
            // Debug.Log($"开始加载宠物预制体: {resourcePath}");
            
            // Resources.Load必须在主线程中调用，不能使用Task.Run
            // 使用Task.Yield()来保持异步方法的结构，但实际上是同步加载
            await Task.Yield();
            
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            
            if (prefab != null)
            {
                loadedPrefabs[resourcePath] = prefab;
                // Debug.Log($"宠物预制体加载成功: {resourcePath}");
            }
            else
            {
                Debug.LogError($"宠物预制体加载失败: {resourcePath}");
            }
            
            return prefab;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载宠物预制体异常 {resourcePath}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 将存档数据应用到宠物控制器
    /// </summary>
    private void ApplyPetData(PetController2D petController, PetSaveData petData)
    {
        // 临时禁用自动保存，避免在加载过程中触发保存冲突
        bool wasAutoSaveEnabled = GameDataManager.Instance.IsAutoSaveEnabled;
        GameDataManager.Instance.SetAutoSaveEnabled(false);
        
        try
        {
            // 设置基本属性
            petController.PetDisplayName = petData.displayName;
            petController.PetIntroduction = petData.introduction;
            petController.Energy = petData.energy;
            petController.Satiety = petData.satiety;
            
            // 调试：显示存档中的位置信息
            // Debug.Log($"[位置调试] 宠物 {petData.petId} 存档位置: {petData.position}");
            
            // 设置位置 - 需要同时设置transform和NavMeshAgent
            Vector3 targetPosition = petData.position;
            
            // 如果存档中的位置是零向量，使用默认生成位置
            if (targetPosition == Vector3.zero)
            {
                targetPosition = defaultSpawnPosition;
            }
            
            petController.transform.position = targetPosition;
            
            // 获取NavMeshAgent并设置位置
            UnityEngine.AI.NavMeshAgent agent = petController.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                // 确保NavMeshAgent也移动到正确位置
                agent.Warp(targetPosition);
                // Debug.Log($"[位置调试] NavMeshAgent已设置到位置: {targetPosition}");
            }
            
            // 设置Rigidbody2D位置
            Rigidbody2D rb = petController.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = targetPosition;
                // Debug.Log($"[位置调试] Rigidbody2D已设置到位置: {targetPosition}");
            }
            
            // 调试：显示实际设置后的位置
            // Debug.Log($"[位置调试] 宠物 {petData.petId} 实际位置: {petController.transform.position}");
            
            // 注意：不再从存档恢复当前状态（isSleeping、isEating）
            // 重新登录后，宠物会以默认状态（清醒、未进食）开始
            
            // 设置宠物ID（用于存档系统）
            petController.petId = petData.petId;
            
            // 如果需要恢复厌倦状态，设置厌倦时间
            if (petData.isBored && petData.lastBoredomTime >= 0)
            {
                petController.LastBoredomTime = petData.lastBoredomTime;
            }
            
            // Debug.Log($"宠物数据应用完成: {petData.petId} - {petData.displayName}");
        }
        finally
        {
            // 恢复自动保存设置
            GameDataManager.Instance.SetAutoSaveEnabled(wasAutoSaveEnabled);
        }
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("打印生成的宠物")]
    public void DebugSpawnedPets()
    {
        Debug.Log($"当前生成的宠物数量: {spawnedPets.Count}");
        foreach (var kvp in spawnedPets)
        {
            var pet = kvp.Value;
            Debug.Log($"- {kvp.Key}: {pet?.name ?? "null"} (位置: {pet?.transform.position ?? Vector3.zero})");
        }
    }
    
    [ContextMenu("打印预制体缓存")]
    public void DebugPrefabCache()
    {
        Debug.Log($"预制体缓存数量: {loadedPrefabs.Count}");
        foreach (var kvp in loadedPrefabs)
        {
            Debug.Log($"- {kvp.Key}: {kvp.Value?.name ?? "null"}");
        }
    }
    
    #endregion
} 