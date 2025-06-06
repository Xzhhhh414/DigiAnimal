using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 存档系统测试器 - 用于验证存档功能
/// </summary>
public class SaveSystemTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private string testPetPrefabName = "Pet_CatBrown";
    
    private void Update()
    {
        // 按键测试
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestSaveSystem();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestCreatePet();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestDeleteSave();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            TestPrintSaveInfo();
        }
    }
    
    /// <summary>
    /// 测试存档系统基本功能
    /// </summary>
    [ContextMenu("测试存档系统")]
    public async void TestSaveSystem()
    {
        DebugLog("=== 开始测试存档系统 ===");
        
        // 1. 测试存档信息获取
        SaveFileInfo info = SaveManager.Instance.GetSaveFileInfo();
        if (info != null && info.exists)
        {
            DebugLog($"存档存在: 宠物数量={info.petCount}, 爱心货币={info.heartCurrency}");
        }
        else
        {
            DebugLog("存档不存在或损坏");
        }
        
        // 2. 测试加载存档
        SaveData saveData = await SaveManager.Instance.LoadSaveAsync();
        if (saveData != null)
        {
            DebugLog($"存档加载成功: 宠物数量={saveData.petsData.Count}, 爱心货币={saveData.playerData.heartCurrency}");
        }
        else
        {
            DebugLog("存档加载失败");
        }
        
        // 3. 测试保存
        bool saveResult = await SaveManager.Instance.SaveAsync();
        DebugLog($"保存结果: {(saveResult ? "成功" : "失败")}");
        
        DebugLog("=== 存档系统测试完成 ===");
    }
    
    /// <summary>
    /// 测试创建宠物
    /// </summary>
    [ContextMenu("测试创建宠物")]
    public async void TestCreatePet()
    {
        DebugLog("=== 开始测试创建宠物 ===");
        
        if (PetSpawner.Instance == null)
        {
            DebugLog("PetSpawner未初始化");
            return;
        }
        
        try
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-5f, 5f), 
                Random.Range(-3f, 3f), 
                0f
            );
            
            PetController2D newPet = await PetSpawner.Instance.CreateNewPet(testPetPrefabName, randomPos);
            
            if (newPet != null)
            {
                newPet.PetDisplayName = $"测试宠物_{Random.Range(1000, 9999)}";
                newPet.PetIntroduction = "这是一只测试宠物";
                
                DebugLog($"宠物创建成功: {newPet.PetDisplayName} 位置: {randomPos}");
            }
            else
            {
                DebugLog("宠物创建失败");
            }
        }
        catch (System.Exception e)
        {
            DebugLog($"创建宠物异常: {e.Message}");
        }
        
        DebugLog("=== 创建宠物测试完成 ===");
    }
    
    /// <summary>
    /// 测试删除存档
    /// </summary>
    [ContextMenu("测试删除存档")]
    public void TestDeleteSave()
    {
        DebugLog("=== 开始测试删除存档 ===");
        
        bool result = SaveManager.Instance.DeleteSave();
        DebugLog($"删除存档结果: {(result ? "成功" : "失败")}");
        
        DebugLog("=== 删除存档测试完成 ===");
    }
    
    /// <summary>
    /// 打印存档信息
    /// </summary>
    [ContextMenu("打印存档信息")]
    public void TestPrintSaveInfo()
    {
        DebugLog("=== 存档信息 ===");
        
        SaveData currentSave = SaveManager.Instance.GetCurrentSaveData();
        if (currentSave != null)
        {
            DebugLog($"存档版本: {currentSave.saveVersion}");
            DebugLog($"最后保存时间: {currentSave.lastSaveTime}");
            DebugLog($"爱心货币: {currentSave.playerData.heartCurrency}");
            DebugLog($"宠物数量: {currentSave.petsData.Count}");
            
            for (int i = 0; i < currentSave.petsData.Count; i++)
            {
                var pet = currentSave.petsData[i];
                DebugLog($"  宠物{i+1}: {pet.petId} - {pet.displayName} ({pet.prefabName})");
            }
        }
        else
        {
            DebugLog("当前没有存档数据");
        }
        
        // 打印活跃宠物
        if (GameDataManager.Instance != null)
        {
            var activePets = GameDataManager.Instance.GetAllActivePets();
            DebugLog($"活跃宠物数量: {activePets.Count}");
            foreach (var kvp in activePets)
            {
                DebugLog($"  活跃宠物: {kvp.Key} - {kvp.Value?.name ?? "null"}");
            }
        }
        
        DebugLog("=== 存档信息结束 ===");
    }
    
    /// <summary>
    /// 测试货币系统
    /// </summary>
    [ContextMenu("测试货币系统")]
    public void TestCurrencySystem()
    {
        DebugLog("=== 开始测试货币系统 ===");
        
        if (PlayerManager.Instance != null)
        {
            int currentCurrency = PlayerManager.Instance.HeartCurrency;
            DebugLog($"当前爱心货币: {currentCurrency}");
            
            // 添加货币
            PlayerManager.Instance.AddHeartCurrency(10);
            DebugLog($"添加10货币后: {PlayerManager.Instance.HeartCurrency}");
            
            // 消费货币
            bool spendResult = PlayerManager.Instance.SpendHeartCurrency(5);
            DebugLog($"消费5货币: {(spendResult ? "成功" : "失败")}, 剩余: {PlayerManager.Instance.HeartCurrency}");
        }
        else
        {
            DebugLog("PlayerManager未初始化");
        }
        
        DebugLog("=== 货币系统测试完成 ===");
    }
    
    /// <summary>
    /// 强制同步数据
    /// </summary>
    [ContextMenu("强制同步数据")]
    public void TestForceSyncData()
    {
        DebugLog("=== 强制同步数据 ===");
        
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SyncToSave(true);
            DebugLog("数据同步完成");
        }
        else
        {
            DebugLog("GameDataManager未初始化");
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SaveSystemTester] {message}");
        }
    }
    
    private void OnGUI()
    {
        if (!enableDebugLog) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("存档系统测试器", GUI.skin.box);
        GUILayout.Label("F1: 测试存档系统");
        GUILayout.Label("F2: 创建测试宠物");
        GUILayout.Label("F3: 删除存档");
        GUILayout.Label("F4: 打印存档信息");
        
        if (SaveManager.Instance != null)
        {
            SaveFileInfo info = SaveManager.Instance.GetSaveFileInfo();
            if (info != null && info.exists)
            {
                GUILayout.Label($"存档: {info.petCount}只宠物, {info.heartCurrency}爱心");
            }
            else
            {
                GUILayout.Label("存档: 不存在");
            }
        }
        
        GUILayout.EndArea();
    }
} 