using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主存档数据结构
/// </summary>
[Serializable]
public class SaveData
{
    public PlayerSaveData playerData;
    public List<PetSaveData> petsData;
    public WorldSaveData worldData;
    
    // 存档版本号，用于后续升级兼容
    public int saveVersion = 1;
    
    // 最后保存时间
    public string lastSaveTime;
    
    // 家具ID计数器（用于生成唯一的家具ID）
    public int nextFurnitureIdCounter = 1;
    
    public SaveData()
    {
        playerData = new PlayerSaveData();
        petsData = new List<PetSaveData>();
        worldData = new WorldSaveData();
        lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

/// <summary>
/// 玩家存档数据
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public int heartCurrency = 0;
    
    // 预留其他玩家数据扩展
    public int playerLevel = 1;
    public float playTime = 0f;
    
    // 系统设置相关
    public bool dynamicIslandEnabled = true;        // 灵动岛是否开启（默认开启）
    public string selectedDynamicIslandPetId = "";  // 选中的灵动岛宠物ID
    public bool lockScreenWidgetEnabled = false;    // 锁屏小组件是否开启
    public string selectedLockScreenPetId = "";     // 选中的锁屏小组件宠物ID
    
    public PlayerSaveData()
    {
        heartCurrency = 0;
        playerLevel = 1;
        playTime = 0f;
        dynamicIslandEnabled = true;
        selectedDynamicIslandPetId = "";
        lockScreenWidgetEnabled = false;
        selectedLockScreenPetId = "";
    }
}

/// <summary>
/// 宠物存档数据
/// </summary>
[Serializable]
public class PetSaveData
{
    // 唯一标识
    public string petId;
    
    // 预制体信息
    public string prefabName;       // 例如："Pet_Cat", "Pet_Dog"
    
    // 动态属性
    public string displayName;
    public string introduction;
    public int energy;
    public int satiety;
    
    // 位置信息
    public Vector3 position;
    
    // 年龄系统
    public string purchaseDate; // 购买日期，格式："yyyy-MM-dd HH:mm:ss"
    
    // 注意：不保存当前状态（isSleeping、isEating）
    // 重新登录后这些状态会重置为默认值
    
    // 厌倦系统相关
    public bool isBored;
    public float lastBoredomTime;
    
    // 离线时间计算相关
    public string lastEnergyUpdateTime;    // 上次精力更新时间
    public string lastSatietyUpdateTime;   // 上次饱腹度更新时间
    public string lastBoredomCheckTime;    // 上次厌倦检查时间
    
    // 预留扩展字段
    public Dictionary<string, object> customProperties;
    
    public PetSaveData()
    {
        petId = GenerateNewPetId();
        prefabName = "";
        displayName = "";
        introduction = "";
        energy = 100;
        satiety = 100;
        position = Vector3.zero;
        purchaseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 默认设置为当前时间
        isBored = false;
        lastBoredomTime = -1f;
        
        // 初始化离线时间字段
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastEnergyUpdateTime = currentTime;
        lastSatietyUpdateTime = currentTime;
        lastBoredomCheckTime = currentTime;
        
        customProperties = new Dictionary<string, object>();
    }
    
    public PetSaveData(string prefab)
    {
        petId = GenerateNewPetId();
        prefabName = prefab;
        displayName = "";
        introduction = "";
        energy = 100;
        satiety = 100;
        position = Vector3.zero;
        purchaseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 默认设置为当前时间
        isBored = false;
        lastBoredomTime = -1f;
        
        // 初始化离线时间字段
        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        lastEnergyUpdateTime = currentTime;
        lastSatietyUpdateTime = currentTime;
        lastBoredomCheckTime = currentTime;
        
        customProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// 生成新的宠物ID
    /// </summary>
    private static string GenerateNewPetId()
    {
        // 获取当前最大ID并递增
        int nextId = SaveManager.GetNextPetId();
        return $"pet_{nextId:D3}";
    }
}

/// <summary>
/// 世界存档数据
/// </summary>
[Serializable]
public class WorldSaveData
{
    // 食物数据
    public List<FoodSaveData> foods;
    
    // 植物数据
    public List<PlantSaveData> plants;
    
    // 音响数据
    public List<SpeakerSaveData> speakers;
    
    // 电视机数据
    public List<TVSaveData> tvs;
    
    // 场景设置
    public string currentScene;
    
    public WorldSaveData()
    {
        foods = new List<FoodSaveData>();
        plants = new List<PlantSaveData>();
        speakers = new List<SpeakerSaveData>();
        tvs = new List<TVSaveData>();
        currentScene = "Gameplay";
    }
}



/// <summary>
/// 食物存档数据
/// </summary>
[Serializable]
public class FoodSaveData
{
    public string foodId;           // 食物唯一标识（基于场景中的GameObject名称或位置）
    public string foodType;         // 食物类型（如"CatFood"）
    public string configId;         // 家具数据库中的ConfigId
    public string saveDataId;        // 默认家具标识符（如果是默认创建的家具）
    public bool isEmpty;            // 是否为空盘状态
    public Vector3 position;        // 食物位置
    public int tasty;               // 美味度
    public int satietyRecoveryValue; // 饱腹度恢复值
    
    public FoodSaveData()
    {
        foodId = "";
        foodType = "";
        configId = "";
        saveDataId = "";
        isEmpty = false;
        position = Vector3.zero;
        tasty = 3;
        satietyRecoveryValue = 25;
    }
    
    public FoodSaveData(string id, string type, string config, bool empty, Vector3 pos, int tastyValue, int satietyValue)
    {
        foodId = id;
        foodType = type;
        configId = config;
        saveDataId = ""; // 默认为空，只有默认家具才会设置
        isEmpty = empty;
        position = pos;
        tasty = tastyValue;
        satietyRecoveryValue = satietyValue;
        

    }
    
    public FoodSaveData(string id, string type, string config, string defId, bool empty, Vector3 pos, int tastyValue, int satietyValue)
    {
        foodId = id;
        foodType = type;
        configId = config;
        saveDataId = defId;
        isEmpty = empty;
        position = pos;
        tasty = tastyValue;
        satietyRecoveryValue = satietyValue;
        

    }
}

/// <summary>
/// 植物存档数据
/// </summary>
[Serializable]
public class PlantSaveData
{
    public string plantId;                  // 植物唯一ID
    public string configId;                 // 家具数据库中的ConfigId
    public string saveDataId;                // 默认家具标识符（如果是默认创建的家具）
    public int healthLevel;                 // 健康度 (0-100)
    public Vector3 position;                // 植物位置
    public int wateringHeartCost;           // 浇水消耗的爱心
    public int healthRecoveryValue;         // 每次浇水恢复的健康度
    
    // 离线时间计算相关
    public string lastHealthUpdateTime;     // 上次健康度更新时间
    
    public PlantSaveData()
    {
        plantId = "";
        configId = "";
        saveDataId = "";
        healthLevel = 100;
        position = Vector3.zero;
        wateringHeartCost = 3;
        healthRecoveryValue = 25;
        
        // 初始化离线时间字段
        lastHealthUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public PlantSaveData(string id, string config, int health, Vector3 pos, int cost, int recovery)
    {
        plantId = id;
        configId = config;
        saveDataId = ""; // 默认为空，只有默认家具才会设置
        healthLevel = health;
        position = pos;
        wateringHeartCost = cost;
        healthRecoveryValue = recovery;
        
        // 初始化离线时间字段
        lastHealthUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public PlantSaveData(string id, string config, string defId, int health, Vector3 pos, int cost, int recovery)
    {
        plantId = id;
        configId = config;
        saveDataId = defId;
        healthLevel = health;
        position = pos;
        wateringHeartCost = cost;
        healthRecoveryValue = recovery;
        
        // 初始化离线时间字段
        lastHealthUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        

    }
}

/// <summary>
/// 音响存档数据
/// </summary>
[Serializable]
public class SpeakerSaveData
{
    public string speakerId;           // 音响唯一ID
    public string configId;            // 配置ID，对应FurnitureDatabase中的配置
    public string saveDataId;           // 默认家具标识符（如果是默认创建的家具）
    public Vector3 position;           // 音响位置
    public int currentTrackIndex;      // 当前曲目索引
    public float pausedTime;           // 暂停时的播放位置
    public bool wasPlaying;            // 保存时是否在播放
    
    public SpeakerSaveData()
    {
        speakerId = "";
        configId = "";
        saveDataId = "";
        position = Vector3.zero;
        currentTrackIndex = 0;
        pausedTime = 0f;
        wasPlaying = false;
    }
    
    public SpeakerSaveData(string id, string config, Vector3 pos, int trackIndex, float pauseTime, bool playing)
    {
        speakerId = id;
        configId = config;
        saveDataId = ""; // 默认为空，只有默认家具才会设置
        position = pos;
        currentTrackIndex = trackIndex;
        pausedTime = pauseTime;
        wasPlaying = playing;
    }
    
    public SpeakerSaveData(string id, string config, string defId, Vector3 pos, int trackIndex, float pauseTime, bool playing)
    {
        speakerId = id;
        configId = config;
        saveDataId = defId;
        position = pos;
        currentTrackIndex = trackIndex;
        pausedTime = pauseTime;
        wasPlaying = playing;
    }
}

/// <summary>
/// 电视机存档数据
/// </summary>
[System.Serializable]
public class TVSaveData
{
    public string tvId;           // 电视机唯一ID
    public string configId;       // 配置ID（对应FurnitureDatabase中的configId）
    public string saveDataId;     // 默认家具标识符
    public Vector3 position;      // 位置
    public bool isOn;             // 开关状态
    
    public TVSaveData()
    {
        tvId = "";
        configId = "";
        saveDataId = "";
        position = Vector3.zero;
        isOn = false;
    }
    
    public TVSaveData(string id, string config, string defId, Vector3 pos, bool powerState)
    {
        tvId = id;
        configId = config;
        saveDataId = defId;
        position = pos;
        isOn = powerState;
    }
} 