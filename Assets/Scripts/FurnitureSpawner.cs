using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



/// <summary>
/// 家具生成器 - 负责根据存档数据动态创建和管理家具对象
/// </summary>
public class FurnitureSpawner : MonoBehaviour
{
    [Header("数据库")]
    [SerializeField] private FurnitureDatabase database;    // 家具数据库
    
    [Header("家具容器")]
    [SerializeField] private Transform furnitureParent;     // 家具父对象（直接拖拽场景对象）
    

    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;    // 调试日志开关
    
    // 单例实例
    public static FurnitureSpawner Instance { get; private set; }
    
    // 已生成的家具对象字典 <FurnitureId, ISpawnableFurniture>
    private Dictionary<string, ISpawnableFurniture> spawnedFurniture = new Dictionary<string, ISpawnableFurniture>();
    
    // 家具ID计数器（从存档加载）
    private static int currentIdCounter = 1;
    
    void Awake()
    {
        // 单例初始化（不使用DontDestroyOnLoad，因为挂在GameManager上）
        Instance = this;
        
        // 检查数据库配置
        if (database == null)
        {
            Debug.LogError("[FurnitureSpawner] 家具数据库未配置！");
        }
        
        // 确保有家具容器
        if (furnitureParent == null)
        {
            GameObject container = new GameObject("FurnitureContainer");
            container.transform.SetParent(transform);
            furnitureParent = container.transform;
            DebugLog("自动创建家具容器");
        }
        
        // DebugLog($"FurnitureSpawner 初始化完成，数据库包含 {database?.GetAllConfigs().Count ?? 0} 个配置");
    }
    
    /// <summary>
    /// 从存档数据生成家具
    /// </summary>
    /// <param name="saveData">存档数据</param>
    /// <returns>生成的家具对象</returns>
    public ISpawnableFurniture SpawnFurnitureFromSaveData(object saveData)
    {
        if (database == null)
        {
            Debug.LogError("[FurnitureSpawner] 家具数据库未配置！");
            return null;
        }
        
        // 从存档数据中提取configId（这里需要根据实际的存档数据结构来实现）
        string configId = ExtractConfigIdFromSaveData(saveData);
        if (string.IsNullOrEmpty(configId))
        {
            Debug.LogError("[FurnitureSpawner] 无法从存档数据中提取configId");
            return null;
        }
        
        // 获取家具配置
        FurnitureConfig config = database.GetFurnitureConfig(configId);
        if (config?.prefab == null)
        {
            Debug.LogError($"[FurnitureSpawner] 无法找到家具配置: ConfigId={configId}");
            return null;
        }
        
        try
        {
            // 实例化家具预制体到指定父对象
            GameObject furnitureObj = Instantiate(config.prefab, furnitureParent);
            
            // 获取ISpawnableFurniture组件
            ISpawnableFurniture furniture = furnitureObj.GetComponent<ISpawnableFurniture>();
            if (furniture == null)
            {
                Debug.LogError($"[FurnitureSpawner] 家具预制体缺少ISpawnableFurniture组件: {config.prefab.name}");
                Destroy(furnitureObj);
                return null;
            }
            
            // 从存档数据初始化（包括ID和位置）
            furniture.InitializeFromSaveData(saveData);
            
            // 注册到管理器
            if (!string.IsNullOrEmpty(furniture.FurnitureId))
            {
                spawnedFurniture[furniture.FurnitureId] = furniture;
                // DebugLog($"家具生成成功: {furniture.FurnitureName} (ID: {furniture.FurnitureId})");
            }
            else
            {
                Debug.LogWarning($"[FurnitureSpawner] 家具ID为空: {furniture.FurnitureName}");
            }
            
            return furniture;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[FurnitureSpawner] 生成家具失败: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 从存档数据中提取configId
    /// </summary>
    private string ExtractConfigIdFromSaveData(object saveData)
    {
        // 直接从存档数据中读取ConfigId
        if (saveData is PlantSaveData plantData)
        {
            if (!string.IsNullOrEmpty(plantData.configId))
            {
                // DebugLog($"从植物存档数据中提取ConfigId: {plantData.configId}");
                return plantData.configId;
            }
            else
            {
                Debug.LogWarning($"[FurnitureSpawner] 植物存档数据中ConfigId为空: {plantData.plantId}");
                return null;
            }
        }
        else if (saveData is FoodSaveData foodData)
        {
            if (!string.IsNullOrEmpty(foodData.configId))
            {
                // DebugLog($"从食物存档数据中提取ConfigId: {foodData.configId}");
                return foodData.configId;
            }
            else
            {
                Debug.LogWarning($"[FurnitureSpawner] 食物存档数据中ConfigId为空: {foodData.foodId}");
                return null;
            }
        }
        else if (saveData is SpeakerSaveData speakerData)
        {
            if (!string.IsNullOrEmpty(speakerData.configId))
            {
                DebugLog($"从音响存档数据中提取ConfigId: {speakerData.configId}");
                return speakerData.configId;
            }
            else
            {
                Debug.LogWarning($"[FurnitureSpawner] 音响存档数据中ConfigId为空: {speakerData.speakerId}");
                return null;
            }
        }
        else if (saveData is TVSaveData tvData)
        {
            if (!string.IsNullOrEmpty(tvData.configId))
            {
                DebugLog($"从电视机存档数据中提取ConfigId: {tvData.configId}");
                return tvData.configId;
            }
            else
            {
                Debug.LogWarning($"[FurnitureSpawner] 电视机存档数据中ConfigId为空: {tvData.tvId}");
                return null;
            }
        }
        
        DebugLog($"未知的存档数据类型: {saveData?.GetType().Name}");
        return null;
    }
    

    
    /// <summary>
    /// 销毁家具
    /// </summary>
    /// <param name="furnitureId">家具ID</param>
    public void DestroyFurniture(string furnitureId)
    {
        if (spawnedFurniture.TryGetValue(furnitureId, out ISpawnableFurniture furniture))
        {
            // DebugLog($"销毁家具: {furniture.FurnitureName} (ID: {furnitureId})");
            
            if (furniture.GameObject != null)
            {
                Destroy(furniture.GameObject);
            }
            
            spawnedFurniture.Remove(furnitureId);
        }
        else
        {
            Debug.LogWarning($"[FurnitureSpawner] 未找到要销毁的家具: {furnitureId}");
        }
    }
    
    /// <summary>
    /// 清理所有家具
    /// </summary>
    public void ClearAllFurniture()
    {
        // DebugLog($"清理所有家具，共 {spawnedFurniture.Count} 个");
        
        List<string> furnitureIds = new List<string>(spawnedFurniture.Keys);
        foreach (string id in furnitureIds)
        {
            DestroyFurniture(id);
        }
        
        spawnedFurniture.Clear();
    }
    
    /// <summary>
    /// 获取已生成的家具
    /// </summary>
    /// <param name="furnitureId">家具ID</param>
    /// <returns>家具对象</returns>
    public ISpawnableFurniture GetFurniture(string furnitureId)
    {
        spawnedFurniture.TryGetValue(furnitureId, out ISpawnableFurniture furniture);
        return furniture;
    }
    
    /// <summary>
    /// 获取指定类型的所有家具
    /// </summary>
    /// <param name="furnitureType">家具类型</param>
    /// <returns>家具列表</returns>
    public List<ISpawnableFurniture> GetFurnitureByType(FurnitureType furnitureType)
    {
        List<ISpawnableFurniture> result = new List<ISpawnableFurniture>();
        
        foreach (var furniture in spawnedFurniture.Values)
        {
            if (furniture.SpawnableFurnitureType == furnitureType)
            {
                result.Add(furniture);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取指定配置ID的所有家具
    /// </summary>
    /// <param name="configId">配置ID</param>
    /// <returns>家具列表</returns>
    public List<ISpawnableFurniture> GetFurnitureByConfigId(string configId)
    {
        List<ISpawnableFurniture> result = new List<ISpawnableFurniture>();
        
        foreach (var furniture in spawnedFurniture.Values)
        {
            // 这里需要根据实际情况判断家具的configId
            // 可能需要在ISpawnableFurniture接口中添加ConfigId属性
            if (furniture.FurnitureId.Contains(configId))
            {
                result.Add(furniture);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取所有已生成的家具
    /// </summary>
    /// <returns>家具字典</returns>
    public Dictionary<string, ISpawnableFurniture> GetAllFurniture()
    {
        return new Dictionary<string, ISpawnableFurniture>(spawnedFurniture);
    }
    
    #region ID管理相关
    
    /// <summary>
    /// 生成唯一家具ID（带冲突检测）
    /// </summary>
    public string GenerateUniqueFurnitureId()
    {
        string id;
        int attempts = 0;
        
        do 
        {
            id = $"furniture_{currentIdCounter}";
            currentIdCounter++;
            attempts++;
            
            // 防止无限循环
            if (attempts > 10000) 
            {
                Debug.LogError("[FurnitureSpawner] ID生成失败，可能存在系统错误");
                break;
            }
        } 
        while (spawnedFurniture.ContainsKey(id)); // 检查是否已存在
        
        // 立即更新存档计数器
        UpdateSavedIdCounter();
        
        // DebugLog($"生成家具ID: {id}");
        return id;
    }
    
    /// <summary>
    /// 从存档加载ID计数器
    /// </summary>
    public void LoadIdCounter(int savedCounter)
    {
        currentIdCounter = Mathf.Max(savedCounter, currentIdCounter);
        // DebugLog($"加载家具ID计数器: {currentIdCounter}");
    }
    
    /// <summary>
    /// 更新存档中的ID计数器
    /// </summary>
    private void UpdateSavedIdCounter()
    {
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData != null)
        {
            saveData.nextFurnitureIdCounter = currentIdCounter;
            // DebugLog($"更新存档ID计数器: {currentIdCounter}");
        }
    }
    
    #endregion
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[FurnitureSpawner] {message}");
        }
    }
    
    void OnDestroy()
    {
        // 清理单例引用
        if (Instance == this)
        {
            Instance = null;
        }
        
        // DebugLog("FurnitureSpawner 被销毁");
    }
}