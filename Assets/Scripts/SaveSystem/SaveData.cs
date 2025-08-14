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
/// 世界存档数据（为未来扩展准备）
/// </summary>
[Serializable]
public class WorldSaveData
{
    // 玩具数据
    public List<ToySaveData> toys;
    
    // 家具数据
    public List<FurnitureSaveData> furniture;
    
    // 食物数据
    public List<FoodSaveData> foods;
    
    // 场景设置
    public string currentScene;
    
    public WorldSaveData()
    {
        toys = new List<ToySaveData>();
        furniture = new List<FurnitureSaveData>();
        foods = new List<FoodSaveData>();
        currentScene = "Gameplay";
    }
}

/// <summary>
/// 玩具存档数据（预留）
/// </summary>
[Serializable]
public class ToySaveData
{
    public string toyId;
    public string toyType;
    public Vector3 position;
    public Dictionary<string, object> properties;
    
    public ToySaveData()
    {
        toyId = "";
        toyType = "";
        position = Vector3.zero;
        properties = new Dictionary<string, object>();
    }
}

/// <summary>
/// 家具存档数据（预留）
/// </summary>
[Serializable]
public class FurnitureSaveData
{
    public string furnitureId;
    public string furnitureType;
    public Vector3 position;
    public Vector3 rotation;
    public Dictionary<string, object> properties;
    
    public FurnitureSaveData()
    {
        furnitureId = "";
        furnitureType = "";
        position = Vector3.zero;
        rotation = Vector3.zero;
        properties = new Dictionary<string, object>();
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
    public bool isEmpty;            // 是否为空盘状态
    public Vector3 position;        // 食物位置
    public int tasty;               // 美味度
    public int satietyRecoveryValue; // 饱腹度恢复值
    
    public FoodSaveData()
    {
        foodId = "";
        foodType = "";
        isEmpty = false;
        position = Vector3.zero;
        tasty = 3;
        satietyRecoveryValue = 25;
    }
    
    public FoodSaveData(string id, string type, bool empty, Vector3 pos, int tastyValue, int satietyValue)
    {
        foodId = id;
        foodType = type;
        isEmpty = empty;
        position = pos;
        tasty = tastyValue;
        satietyRecoveryValue = satietyValue;
    }
} 