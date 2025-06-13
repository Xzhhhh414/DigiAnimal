using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 初次游戏宠物选择功能测试器 - 用于在编辑器中测试初次游戏时的宠物选择功能
/// </summary>
public class PetSelectionTester : MonoBehaviour
{
    [Header("测试按钮")]
    [SerializeField] private Button testShowSelectionButton;
    [SerializeField] private Button testClearSaveButton;
    
    [Header("调试信息")]
    [SerializeField] private Text debugText;
    
    private void Start()
    {
        // 设置测试按钮事件
        if (testShowSelectionButton != null)
        {
            testShowSelectionButton.onClick.AddListener(TestShowPetSelection);
        }
        
        if (testClearSaveButton != null)
        {
            testClearSaveButton.onClick.AddListener(TestClearSave);
        }
        
        // 显示当前存档状态
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 测试显示宠物选择界面
    /// </summary>
    [ContextMenu("测试显示宠物选择")]
    public void TestShowPetSelection()
    {
        var petSelectionManager = FindObjectOfType<FirstTimePetSelectionManager>();
        if (petSelectionManager != null)
        {
            petSelectionManager.ForceShowPetSelection();
            Debug.Log("已强制显示宠物选择界面");
        }
        else
        {
            Debug.LogError("未找到FirstTimePetSelectionManager组件");
        }
        
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 测试清空存档
    /// </summary>
    [ContextMenu("测试清空存档")]
    public void TestClearSave()
    {
        if (SaveManager.Instance != null)
        {
            bool success = SaveManager.Instance.DeleteSave();
            Debug.Log($"清空存档 {(success ? "成功" : "失败")}");
        }
        else
        {
            Debug.LogError("SaveManager未找到");
        }
        
        UpdateDebugInfo();
    }
    
    /// <summary>
    /// 更新调试信息
    /// </summary>
    [ContextMenu("更新调试信息")]
    public void UpdateDebugInfo()
    {
        if (debugText == null) return;
        
        string info = "=== 存档状态 ===\n";
        
        try
        {
            if (SaveManager.Instance != null)
            {
                SaveFileInfo saveInfo = SaveManager.Instance.GetSaveFileInfo();
                if (saveInfo != null && saveInfo.exists)
                {
                    info += $"存档存在: 是\n";
                    info += $"宠物数量: {saveInfo.petCount}\n";
                    info += $"爱心货币: {saveInfo.heartCurrency}\n";
                    info += $"保存时间: {saveInfo.saveTime}\n";
                }
                else
                {
                    info += "存档存在: 否\n";
                }
            }
            else
            {
                info += "SaveManager: 未初始化\n";
            }
        }
        catch (System.Exception e)
        {
            info += $"获取存档信息失败: {e.Message}\n";
        }
        
        info += "\n=== 宠物选择状态 ===\n";
        try
        {
            var petSelectionManager = FindObjectOfType<FirstTimePetSelectionManager>();
            if (petSelectionManager != null)
            {
                info += $"FirstTimePetSelectionManager: 已找到\n";
                info += $"应显示选择界面: {petSelectionManager.ShouldShowPetSelection()}\n";
            }
            else
            {
                info += "FirstTimePetSelectionManager: 未找到\n";
            }
        }
        catch (System.Exception e)
        {
            info += $"获取宠物选择状态失败: {e.Message}\n";
        }
        
        info += "\n=== 宠物数据库状态 ===\n";
        try
        {
            // 暂时注释掉，等待Unity重新编译
            // TODO: 取消注释当PetDatabaseManager编译完成后
            /*
            if (PetDatabaseManager.Instance != null)
            {
                info += $"PetDatabaseManager: 已找到\n";
                info += $"数据库已加载: {PetDatabaseManager.Instance.IsDatabaseLoaded()}\n";
                
                if (PetDatabaseManager.Instance.IsDatabaseLoaded())
                {
                    var starterPets = PetDatabaseManager.Instance.GetStarterPets();
                    info += $"初始宠物数量: {starterPets.Count}\n";
                    
                    foreach (var pet in starterPets)
                    {
                        info += $"  - {pet.petName} ({pet.petPrefab?.name})\n";
                    }
                }
            }
            else
            {
                info += "PetDatabaseManager: 未找到\n";
            }
            */
            info += "宠物数据库功能暂时禁用，等待编译完成\n";
        }
        catch (System.Exception e)
        {
            info += $"获取宠物选择状态失败: {e.Message}\n";
        }
        
        debugText.text = info;
    }
    
    /// <summary>
    /// 测试创建宠物（仅在Gameplay场景中有效）
    /// </summary>
    [ContextMenu("测试创建宠物")]
    public void TestCreatePet()
    {
        if (PetSpawner.Instance != null)
        {
            // 异步创建宠物
            StartCoroutine(CreateTestPet());
        }
        else
        {
            Debug.LogError("PetSpawner未找到，此功能仅在Gameplay场景中可用");
        }
    }
    
    /// <summary>
    /// 创建测试宠物的协程
    /// </summary>
    private System.Collections.IEnumerator CreateTestPet()
    {
        Debug.Log("开始创建测试宠物...");
        
        var createTask = PetSpawner.Instance.CreateNewPet("Pet_CatBrown", Vector3.zero);
        
        // 等待任务完成
        while (!createTask.IsCompleted)
        {
            yield return null;
        }
        
        if (createTask.Result != null)
        {
            Debug.Log($"测试宠物创建成功: {createTask.Result.name}");
        }
        else
        {
            Debug.LogError("测试宠物创建失败");
        }
        
        UpdateDebugInfo();
    }
} 