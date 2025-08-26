using UnityEngine;

/// <summary>
/// 可生成家具接口 - 定义所有可以通过FurnitureSpawner创建的家具物件
/// </summary>
public interface ISpawnableFurniture
{
    /// <summary>
    /// 家具唯一ID（用于存档匹配）
    /// </summary>
    string FurnitureId { get; set; }
    
    /// <summary>
    /// 家具类型（用于预制体查找）
    /// </summary>
    FurnitureType SpawnableFurnitureType { get; }
    
    /// <summary>
    /// 家具名称
    /// </summary>
    string FurnitureName { get; set; }
    
    /// <summary>
    /// 家具位置
    /// </summary>
    Vector3 Position { get; set; }
    
    /// <summary>
    /// 游戏对象引用
    /// </summary>
    GameObject GameObject { get; }
    
    /// <summary>
    /// 从存档数据初始化家具
    /// </summary>
    /// <param name="saveData">存档数据</param>
    void InitializeFromSaveData(object saveData);
    
    /// <summary>
    /// 获取存档数据
    /// </summary>
    /// <returns>存档数据对象</returns>
    object GetSaveData();
    
    /// <summary>
    /// 生成家具ID（如果需要）
    /// </summary>
    void GenerateFurnitureId();
}

/// <summary>
/// 家具类型枚举
/// </summary>
public enum FurnitureType
{
    Plant,      // 植物
    Food,       // 食物
    Speaker,    // 音响
    Decoration, // 装饰品
    // 可以继续添加其他家具类型
}