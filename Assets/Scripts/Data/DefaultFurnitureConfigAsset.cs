using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 默认家具配置项
/// </summary>
[System.Serializable]
public class DefaultFurnitureConfig
{
    //[Header("家具配置")]
    public string saveDataId = "";           // 默认家具的唯一标识符（用于检查是否已创建）
    public string furnitureConfigId = "";   // 对应FurnitureDatabase中的configId
    
    //[Header("位置设置")]
    public Vector3 position = Vector3.zero; // 家具生成位置
}

/// <summary>
/// 默认家具配置 ScriptableObject
/// 用于在不同场景间共享默认家具配置
/// </summary>
[CreateAssetMenu(fileName = "DefaultFurnitureConfig", menuName = "DigiAnimal/Default Furniture Config")]
public class DefaultFurnitureConfigAsset : ScriptableObject
{
    [Header("默认家具列表")]
    [SerializeField] private List<DefaultFurnitureConfig> defaultFurnitureItems = new List<DefaultFurnitureConfig>();
    
    /// <summary>
    /// 获取默认家具配置列表
    /// </summary>
    public List<DefaultFurnitureConfig> GetDefaultFurnitureItems()
    {
        return defaultFurnitureItems ?? new List<DefaultFurnitureConfig>();
    }
    
    /// <summary>
    /// 获取默认家具数量
    /// </summary>
    public int Count => defaultFurnitureItems?.Count ?? 0;
    

}
