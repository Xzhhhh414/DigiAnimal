using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宠物配置数据
/// </summary>
[System.Serializable]
public class PetConfigData
{
    [Header("基础信息")]
    public string petId;                    // 宠物唯一ID
    public string petName;                  // 宠物名称
    public GameObject petPrefab;            // 宠物预制体直接引用（游戏内使用）
    public GameObject previewPrefab;        // 预览预制体（Start场景等预览使用）
    [Tooltip("宠物介绍文本")]
    public string introduction;             // 宠物介绍
    
    [Header("UI显示")]
    public Sprite headIconImage;            // 头像图标
    
    [Header("分类")]
    public PetType petType;                 // 宠物类型
    public bool isStarterPet;               // 是否为初始宠物
    public bool isShopPet;                  // 是否在商店出售
    
    [Header("商店信息")]
    public int heartCost;                   // 爱心货币价格
    
    [Header("属性")]
    public float baseEnergy = 80f;          // 基础精力值
    public float baseSatiety = 70f;         // 基础饱腹度
}

/// <summary>
/// 宠物类型枚举
/// </summary>
public enum PetType
{
    Cat,        // 猫咪
    Dog         // 狗狗
}

/// <summary>
/// 宠物数据库 - ScriptableObject配置文件
/// </summary>
[CreateAssetMenu(fileName = "PetDatabase", menuName = "DigiAnimal/Pet Database")]
public class PetDatabase : ScriptableObject
{
    [Header("宠物配置列表")]
    [SerializeField] private List<PetConfigData> petConfigs = new List<PetConfigData>();
    
    /// <summary>
    /// 获取所有宠物配置
    /// </summary>
    public List<PetConfigData> GetAllPets()
    {
        return new List<PetConfigData>(petConfigs);
    }
    
    /// <summary>
    /// 获取初始宠物列表（用于初次选择）
    /// </summary>
    public List<PetConfigData> GetStarterPets()
    {
        return petConfigs.FindAll(pet => pet.isStarterPet);
    }
    
    /// <summary>
    /// 获取商店宠物列表
    /// </summary>
    public List<PetConfigData> GetShopPets()
    {
        return petConfigs.FindAll(pet => pet.isShopPet);
    }
    
    /// <summary>
    /// 根据ID获取宠物配置
    /// </summary>
    public PetConfigData GetPetById(string petId)
    {
        return petConfigs.Find(pet => pet.petId == petId);
    }
    
    /// <summary>
    /// 根据预制体获取宠物配置
    /// </summary>
    public PetConfigData GetPetByPrefab(GameObject petPrefab)
    {
        return petConfigs.Find(pet => pet.petPrefab == petPrefab);
    }
    
    /// <summary>
    /// 根据类型获取宠物列表
    /// </summary>
    public List<PetConfigData> GetPetsByType(PetType petType)
    {
        return petConfigs.FindAll(pet => pet.petType == petType);
    }
    
    /// <summary>
    /// 添加宠物配置（编辑器用）
    /// </summary>
    public void AddPetConfig(PetConfigData petConfig)
    {
        if (petConfig != null && !petConfigs.Contains(petConfig))
        {
            petConfigs.Add(petConfig);
        }
    }
    
    /// <summary>
    /// 移除宠物配置（编辑器用）
    /// </summary>
    public void RemovePetConfig(PetConfigData petConfig)
    {
        if (petConfig != null)
        {
            petConfigs.Remove(petConfig);
        }
    }
    
    /// <summary>
    /// 验证配置完整性
    /// </summary>
    public bool ValidateConfigs()
    {
        foreach (var pet in petConfigs)
        {
            if (string.IsNullOrEmpty(pet.petId) || 
                string.IsNullOrEmpty(pet.petName) || 
                pet.petPrefab == null)
            {
                Debug.LogError($"宠物配置不完整: {pet.petName}");
                return false;
            }
        }
        return true;
    }
}