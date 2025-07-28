using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// iOS集成检查器
/// 用于验证iOS数据桥接系统的配置和运行状态
/// </summary>
public class IOSIntegrationChecker : MonoBehaviour
{
    [Header("检查设置")]
    [SerializeField] private bool autoCheckOnStart = true;
    
    private void Start()
    {
        if (autoCheckOnStart)
        {
            CheckIntegrationStatus();
        }
    }
    
    /// <summary>
    /// 检查集成状态
    /// </summary>
    [ContextMenu("检查iOS集成状态")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("=== iOS集成状态检查 ===");
        
        CheckCoreComponents();
        CheckDataBridge();
        CheckSaveSystem();
        CheckPetDatabase();
        
        Debug.Log("=== 检查完成 ===");
    }
    
    /// <summary>
    /// 检查核心组件
    /// </summary>
    private void CheckCoreComponents()
    {
        Debug.Log("\n--- 核心组件检查 ---");
        
        bool allGood = true;
        
        // IOSDataBridge
        if (IOSDataBridge.Instance != null)
        {
            Debug.Log("✓ IOSDataBridge: 已初始化");
        }
        else
        {
            Debug.LogWarning("✗ IOSDataBridge: 未初始化");
            allGood = false;
        }
        

        
        // SaveManager
        if (SaveManager.Instance != null)
        {
            Debug.Log("✓ SaveManager: 已初始化");
        }
        else
        {
            Debug.LogWarning("✗ SaveManager: 未初始化");
            allGood = false;
        }
        
        // PetDatabaseManager
        if (PetDatabaseManager.Instance != null)
        {
            Debug.Log("✓ PetDatabaseManager: 已初始化");
        }
        else
        {
            Debug.LogWarning("✗ PetDatabaseManager: 未初始化");
            allGood = false;
        }
        
        if (allGood)
        {
            Debug.Log("✓ 所有核心组件正常");
        }
    }
    
    /// <summary>
    /// 检查数据桥接状态
    /// </summary>
    private void CheckDataBridge()
    {
        Debug.Log("\n--- 数据桥接检查 ---");
        
        if (IOSDataBridge.Instance == null)
        {
            Debug.LogWarning("✗ IOSDataBridge未初始化，跳过检查");
            return;
        }
        
        // 显示当前iOS数据
        IOSDataBridge.Instance.ShowCurrentIOSData();
        
        // 检查Live Activity状态
        bool liveActivityActive = IOSDataBridge.Instance.IsLiveActivityActive();
        Debug.Log($"Live Activity状态: {(liveActivityActive ? "活跃" : "未活跃")}");
        
        // 检查编辑器模式数据
#if UNITY_EDITOR
        string editorData = PlayerPrefs.GetString("iOS_WidgetData", "");
        if (!string.IsNullOrEmpty(editorData))
        {
            Debug.Log("✓ 编辑器模式数据同步正常");
            Debug.Log($"数据内容: {editorData}");
        }
        else
        {
            Debug.LogWarning("✗ 编辑器模式数据为空");
        }
#endif
    }
    

    
    /// <summary>
    /// 检查存档系统
    /// </summary>
    private void CheckSaveSystem()
    {
        Debug.Log("\n--- 存档系统检查 ---");
        
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData == null)
        {
            Debug.LogWarning("✗ 无存档数据");
            return;
        }
        
        if (saveData.playerData == null)
        {
            Debug.LogWarning("✗ 无玩家数据");
            return;
        }
        
        Debug.Log($"✓ 存档版本: {saveData.saveVersion}");
        Debug.Log($"✓ 宠物数量: {saveData.petsData?.Count ?? 0}");
        Debug.Log($"✓ 灵动岛开启: {saveData.playerData.dynamicIslandEnabled}");
        Debug.Log($"✓ 选中宠物: {saveData.playerData.selectedDynamicIslandPetId}");
        
        // 检查选中的宠物是否存在
        if (!string.IsNullOrEmpty(saveData.playerData.selectedDynamicIslandPetId))
        {
            var selectedPet = saveData.petsData?.Find(p => p.petId == saveData.playerData.selectedDynamicIslandPetId);
            if (selectedPet != null)
            {
                Debug.Log($"✓ 选中宠物有效: {selectedPet.displayName} ({selectedPet.prefabName})");
            }
            else
            {
                Debug.LogWarning("✗ 选中宠物无效或不存在");
            }
        }
    }
    
    /// <summary>
    /// 检查宠物数据库
    /// </summary>
    private void CheckPetDatabase()
    {
        Debug.Log("\n--- 宠物数据库检查 ---");
        
        if (PetDatabaseManager.Instance == null)
        {
            Debug.LogWarning("✗ PetDatabaseManager未初始化");
            return;
        }
        
        var allPets = PetDatabaseManager.Instance.GetAllPets();
        Debug.Log($"✓ 数据库宠物总数: {allPets.Count}");
        
        int missingIcons = 0;
        foreach (var pet in allPets)
        {
            if (pet.headIconImage == null)
            {
                Debug.LogWarning($"✗ 宠物 {pet.petId} 缺少头像图片");
                missingIcons++;
            }
        }
        
        if (missingIcons == 0)
        {
            Debug.Log("✓ 所有宠物都有头像图片");
        }
        else
        {
            Debug.LogWarning($"✗ {missingIcons} 个宠物缺少头像图片");
        }
    }
    

    
    /// <summary>
    /// 强制重新初始化所有组件
    /// </summary>
    [ContextMenu("重新初始化iOS集成")]
    public void ReinitializeIntegration()
    {
        Debug.Log("=== 重新初始化iOS集成 ===");
        
        // 强制同步数据
        if (IOSDataBridge.Instance != null)
        {
            IOSDataBridge.Instance.ForceSyncNow();
        }
        
        Debug.Log("重新初始化完成");
    }
    
    /// <summary>
    /// 测试数据同步
    /// </summary>
    [ContextMenu("测试数据同步")]
    public void TestDataSync()
    {
        Debug.Log("=== 测试数据同步 ===");
        
        if (IOSDataBridge.Instance == null)
        {
            Debug.LogWarning("IOSDataBridge未初始化");
            return;
        }
        
        // 强制同步
        IOSDataBridge.Instance.ForceSyncNow();
        
        // 等待一帧后检查结果
        StartCoroutine(CheckSyncResult());
    }
    
    private System.Collections.IEnumerator CheckSyncResult()
    {
        yield return null;
        
#if UNITY_EDITOR
        string syncedData = PlayerPrefs.GetString("iOS_WidgetData", "");
        if (!string.IsNullOrEmpty(syncedData))
        {
            Debug.Log("✓ 数据同步成功");
            Debug.Log($"同步的数据: {syncedData}");
        }
        else
        {
            Debug.LogWarning("✗ 数据同步失败");
        }
#endif
    }
    
    /// <summary>
    /// 生成配置报告
    /// </summary>
    [ContextMenu("生成配置报告")]
    public void GenerateConfigReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== DigiAnimal iOS集成配置报告 ===");
        report.AppendLine($"生成时间: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();
        
        // 平台信息
        report.AppendLine("平台信息:");
        report.AppendLine($"  Unity版本: {Application.unityVersion}");
        report.AppendLine($"  平台: {Application.platform}");
        report.AppendLine($"  数据路径: {Application.persistentDataPath}");
        report.AppendLine();
        
        // 组件状态
        report.AppendLine("组件状态:");
        report.AppendLine($"  IOSDataBridge: {(IOSDataBridge.Instance != null ? "已初始化" : "未初始化")}");
        report.AppendLine($"  SaveManager: {(SaveManager.Instance != null ? "已初始化" : "未初始化")}");
        report.AppendLine($"  PetDatabaseManager: {(PetDatabaseManager.Instance != null ? "已初始化" : "未初始化")}");
        report.AppendLine();
        
        // 存档信息
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData != null)
        {
            report.AppendLine("存档信息:");
            report.AppendLine($"  灵动岛开启: {saveData.playerData.dynamicIslandEnabled}");
            report.AppendLine($"  选中宠物: {saveData.playerData.selectedDynamicIslandPetId}");
            report.AppendLine($"  宠物总数: {saveData.petsData?.Count ?? 0}");
        }
        
        Debug.Log(report.ToString());
        
        // 保存报告到文件
        string reportPath = Path.Combine(Application.persistentDataPath, "ios_integration_report.txt");
        File.WriteAllText(reportPath, report.ToString());
        Debug.Log($"配置报告已保存到: {reportPath}");
    }
} 