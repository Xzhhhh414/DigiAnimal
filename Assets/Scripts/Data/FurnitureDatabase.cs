using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 家具数据库 - 管理所有家具预制体的配置信息
/// </summary>
[CreateAssetMenu(fileName = "FurnitureDatabase", menuName = "DigiAnimal/Furniture Database")]
public class FurnitureDatabase : ScriptableObject
{
    [Header("家具预制体配置")]
    [SerializeField] private List<FurnitureConfig> furnitureConfigs = new List<FurnitureConfig>();
    
    /// <summary>
    /// 根据配置ID获取家具配置
    /// </summary>
    public FurnitureConfig GetFurnitureConfig(string configId)
    {
        foreach (var config in furnitureConfigs)
        {
            if (config.configId == configId)
            {
                return config;
            }
        }
        
        Debug.LogWarning($"[FurnitureDatabase] 未找到家具配置: ConfigId={configId}");
        return null;
    }
    
    /// <summary>
    /// 获取所有家具配置
    /// </summary>
    public List<FurnitureConfig> GetAllConfigs()
    {
        return new List<FurnitureConfig>(furnitureConfigs);
    }
}

/// <summary>
/// 家具配置数据（简化版）
/// </summary>
[System.Serializable]
public class FurnitureConfig
{
    //[Header("配置ID")]
    public string configId = "";                           // 配置ID（使用预制体名称，确保唯一）
    
    //[Header("预制体")]
    public GameObject prefab;                               // 家具预制体（核心）
}

