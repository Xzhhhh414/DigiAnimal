using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 宠物数据库测试器 - 用于测试宠物数据库系统
/// </summary>
public class PetDatabaseTester : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private bool testOnStart = true;
    
    private void Start()
    {
        if (testOnStart)
        {
            TestPetDatabase();
        }
    }
    
    [ContextMenu("测试宠物数据库")]
    public void TestPetDatabase()
    {
        Debug.Log("=== 宠物数据库测试开始 ===");
        
        // 测试数据库管理器
        if (PetDatabaseManager.Instance == null)
        {
            Debug.LogError("PetDatabaseManager未找到！");
            return;
        }
        
        Debug.Log("PetDatabaseManager已找到");
        
        if (!PetDatabaseManager.Instance.IsDatabaseLoaded())
        {
            Debug.LogError("宠物数据库未加载！请确保Resources/Data/PetDatabase.asset存在");
            return;
        }
        
        Debug.Log("宠物数据库已加载");
        
        // 测试获取所有宠物
        var allPets = PetDatabaseManager.Instance.GetAllPets();
        Debug.Log($"总宠物数量: {allPets.Count}");
        
        // 测试获取初始宠物
        var starterPets = PetDatabaseManager.Instance.GetStarterPets();
        Debug.Log($"初始宠物数量: {starterPets.Count}");
        
        foreach (var pet in starterPets)
        {
            Debug.Log($"初始宠物: {pet.petName} (ID: {pet.petId}, 预制体: {pet.petPrefab?.name})");
        }
        
        // 测试获取商店宠物
        var shopPets = PetDatabaseManager.Instance.GetShopPets();
        Debug.Log($"商店宠物数量: {shopPets.Count}");
        
        Debug.Log("=== 宠物数据库测试完成 ===");
    }
} 