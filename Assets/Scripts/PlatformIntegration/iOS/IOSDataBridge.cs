using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// iOS数据桥接管理器
/// 负责Unity游戏数据与iOS灵动岛、主屏幕Widget的数据互通
/// </summary>
public class IOSDataBridge : MonoBehaviour
{
    // 单例模式
    private static IOSDataBridge _instance;
    public static IOSDataBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<IOSDataBridge>();
                if (_instance == null)
                {
                    GameObject bridgeObj = new GameObject("IOSDataBridge");
                    _instance = bridgeObj.AddComponent<IOSDataBridge>();
                    DontDestroyOnLoad(bridgeObj);
                }
            }
            return _instance;
        }
    }
    
    [Header("iOS 数据同步设置")]
    [SerializeField] private bool enableIOSSync = true;
    [SerializeField] private string appGroupIdentifier = "group.com.zher.meow";
    [SerializeField] private float syncInterval = 2f; // 数据同步间隔（秒）
    
    // iOS原生插件方法声明
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _IOSSetSharedData(string key, string value);
    
    [DllImport("__Internal")]
    private static extern void _IOSSetSharedImage(string key, string imagePath);
    
    [DllImport("__Internal")]
    private static extern void _IOSUpdateWidgetData();
    
    [DllImport("__Internal")]
    private static extern void _IOSStartLiveActivity();
    
    [DllImport("__Internal")]
    private static extern void _IOSStopLiveActivity();
    
    [DllImport("__Internal")]
    private static extern bool _IOSIsLiveActivityActive();
    

    
    [DllImport("__Internal")]
    private static extern void _IOSSetAppGroupIdentifier(string identifier);
#endif
    
    // 数据同步状态
    private bool isInitialized = false;
    private float syncTimer = 0f;
    private string lastSyncedPetId = "";
    private bool lastDynamicIslandEnabled = false;
    
    // 缓存数据
    private Dictionary<string, IOSPetData> cachedPetData = new Dictionary<string, IOSPetData>();
    
    private void Awake()
    {
        // 单例模式设置
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeIOSBridge();
    }
    
    private void Update()
    {
        if (!isInitialized || !enableIOSSync)
            return;
            
        // 定时同步数据
        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            SyncToIOS();
            syncTimer = 0f;
        }
    }
    
    /// <summary>
    /// 初始化iOS桥接系统
    /// </summary>
    private void InitializeIOSBridge()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            // 设置App Group标识符
            _IOSSetAppGroupIdentifier(appGroupIdentifier);
            
            // Debug.Log($"[IOSDataBridge] iOS桥接系统已初始化，App Group: {appGroupIdentifier}");
            isInitialized = true;
            
            // 立即进行一次数据同步
            SyncToIOS();
        }
        catch (Exception e)
        {
            Debug.LogError($"[IOSDataBridge] 初始化失败: {e.Message}");
        }
#else
        // Debug.Log($"[IOSDataBridge] 非iOS平台，跳过原生桥接初始化，App Group: {appGroupIdentifier}");
        isInitialized = true;
        
        // 在编辑器中也进行数据同步（用于测试）
        SyncToIOS();
#endif
    }
    
    /// <summary>
    /// 同步数据到iOS
    /// </summary>
    public void SyncToIOS()
    {
        try
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData?.playerData == null)
            {
                Debug.LogWarning("[IOSDataBridge] 存档数据不可用，跳过同步");
                return;
            }
            
            bool dynamicIslandEnabled = saveData.playerData.dynamicIslandEnabled;
            string selectedPetId = saveData.playerData.selectedDynamicIslandPetId;
            
            // 检查是否需要更新
            bool needUpdate = false;
            if (lastDynamicIslandEnabled != dynamicIslandEnabled)
            {
                needUpdate = true;
                lastDynamicIslandEnabled = dynamicIslandEnabled;
            }
            
            if (lastSyncedPetId != selectedPetId)
            {
                needUpdate = true;
                lastSyncedPetId = selectedPetId;
            }
            
            // 检查宠物数据内容是否发生变化
            if (!string.IsNullOrEmpty(selectedPetId) && saveData.petsData != null)
            {
                var petSaveData = saveData.petsData.Find(p => p.petId == selectedPetId);
                if (petSaveData != null)
                {
                    var currentPetData = ConvertToPetData(petSaveData);
                    
                    // 如果缓存中有数据，比较内容是否变化
                    if (cachedPetData.ContainsKey(selectedPetId))
                    {
                        var cachedData = cachedPetData[selectedPetId];
                        if (!IsPetDataEqual(cachedData, currentPetData))
                        {
                            needUpdate = true;
                            // Debug.Log($"[IOSDataBridge] 宠物数据内容发生变化: {selectedPetId}");
                        }
                    }
                    else
                    {
                        needUpdate = true; // 缓存中没有数据，需要更新
                    }
                }
            }
            
            if (!needUpdate)
            {
                return; // 数据没有变化，跳过同步
            }
            
            // 准备同步数据
            var syncData = new IOSWidgetData
            {
                dynamicIslandEnabled = dynamicIslandEnabled,
                selectedPetId = selectedPetId,
                lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // 获取选中宠物的数据
            if (!string.IsNullOrEmpty(selectedPetId) && saveData.petsData != null)
            {
                var petSaveData = saveData.petsData.Find(p => p.petId == selectedPetId);
                if (petSaveData != null)
                {
                    var petData = ConvertToPetData(petSaveData);
                    syncData.selectedPetData = petData;
                    
                    // 缓存宠物数据
                    cachedPetData[selectedPetId] = petData;
                }
            }
            
            // 序列化数据并发送到iOS
            string jsonData = JsonUtility.ToJson(syncData, true);
            
#if UNITY_IOS && !UNITY_EDITOR
            _IOSSetSharedData("WidgetData", jsonData);
            _IOSUpdateWidgetData();
#else
            // 在编辑器中保存到PlayerPrefs用于测试
            PlayerPrefs.SetString("iOS_WidgetData", jsonData);
            PlayerPrefs.Save();
            
            // Debug.Log($"[IOSDataBridge] 数据已同步（编辑器模式）:\n{jsonData}");
#endif
            
            // Debug.Log($"[IOSDataBridge] 数据同步完成 - 灵动岛:{dynamicIslandEnabled}, 宠物:{selectedPetId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[IOSDataBridge] 数据同步失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 将PetSaveData转换为IOSPetData
    /// </summary>
    private IOSPetData ConvertToPetData(PetSaveData petSaveData)
    {
        var petConfig = PetDatabaseManager.Instance?.GetPetByPrefabName(petSaveData.prefabName);
        
        // 计算宠物年龄（天数）
        int ageInDays = 1; // 默认最小1天
        if (!string.IsNullOrEmpty(petSaveData.purchaseDate))
        {
            if (DateTime.TryParseExact(petSaveData.purchaseDate, "yyyy-MM-dd HH:mm:ss", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out DateTime purchaseDateTime))
            {
                TimeSpan age = DateTime.Now - purchaseDateTime;
                ageInDays = Mathf.Max(1, (int)age.TotalDays + 1);
            }
        }
        
        return new IOSPetData
        {
            petId = petSaveData.petId,
            petName = !string.IsNullOrEmpty(petSaveData.displayName) ? petSaveData.displayName : 
                     (petConfig?.petName ?? petSaveData.prefabName),
            prefabName = petSaveData.prefabName,
            energy = petSaveData.energy,
            satiety = petSaveData.satiety,
            isBored = petSaveData.isBored,
            purchaseDate = petSaveData.purchaseDate,
            ageInDays = ageInDays,
            introduction = !string.IsNullOrEmpty(petSaveData.introduction) ? petSaveData.introduction :
                          (petConfig?.introduction ?? "可爱的宠物"),
            lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
    
    /// <summary>
    /// 比较两个IOSPetData是否相等（不包括lastUpdateTime）
    /// </summary>
    private bool IsPetDataEqual(IOSPetData data1, IOSPetData data2)
    {
        if (data1 == null || data2 == null)
            return data1 == data2;
        
        return data1.petId == data2.petId &&
               data1.petName == data2.petName &&
               data1.prefabName == data2.prefabName &&
               data1.energy == data2.energy &&
               data1.satiety == data2.satiety &&
               data1.isBored == data2.isBored &&
               data1.purchaseDate == data2.purchaseDate &&
               data1.ageInDays == data2.ageInDays &&
               data1.introduction == data2.introduction;
    }
    

    

    
    /// <summary>
    /// 强制立即同步数据（外部调用）
    /// </summary>
    public void ForceSyncNow()
    {
        // Debug.Log("[IOSDataBridge] 强制同步数据");
        SyncToIOS();
    }
    
    /// <summary>
    /// 启动Live Activity
    /// </summary>
    public void StartLiveActivity()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _IOSStartLiveActivity();
#else
        // Debug.Log("[IOSDataBridge] 启动Live Activity（编辑器模式）");
#endif
    }
    
    /// <summary>
    /// 停止Live Activity
    /// </summary>
    public void StopLiveActivity()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _IOSStopLiveActivity();
#else
        // Debug.Log("[IOSDataBridge] 停止Live Activity（编辑器模式）");
#endif
    }
    
    /// <summary>
    /// 检查Live Activity是否活跃
    /// </summary>
    public bool IsLiveActivityActive()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return _IOSIsLiveActivityActive();
#else
        // Debug.Log("[IOSDataBridge] 检查Live Activity状态（编辑器模式）");
        return false;
#endif
    }
    

    
    /// <summary>
    /// 获取当前缓存的iOS数据（用于调试）
    /// </summary>
    [ContextMenu("显示当前iOS数据")]
    public void ShowCurrentIOSData()
    {
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData == null)
        {
            Debug.Log("[IOSDataBridge] 无存档数据");
            return;
        }
        
        Debug.Log($"[IOSDataBridge] 当前iOS数据状态:");
        Debug.Log($"  - 灵动岛开启: {saveData.playerData.dynamicIslandEnabled}");
        Debug.Log($"  - 选中宠物: {saveData.playerData.selectedDynamicIslandPetId}");
        Debug.Log($"  - 锁屏Widget开启: {saveData.playerData.lockScreenWidgetEnabled}");
        Debug.Log($"  - 锁屏Widget宠物: {saveData.playerData.selectedLockScreenPetId}");
        
        // 显示编辑器模式下的数据
#if UNITY_EDITOR
        string editorData = PlayerPrefs.GetString("iOS_WidgetData", "无数据");
        Debug.Log($"  - 编辑器模式数据: {editorData}");
#endif

        // 显示Live Activity状态
        bool isActive = IsLiveActivityActive();
        Debug.Log($"  - Live Activity状态: {(isActive ? "活跃" : "未活跃")}");
    }
    

    

    

    
    /// <summary>
    /// 监控宠物数据变化（调试用）
    /// </summary>
    [ContextMenu("监控宠物数据变化")]
    public void MonitorPetDataChanges()
    {
        Debug.Log("=== 开始监控宠物数据变化 ===");
        
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData == null)
        {
            Debug.LogWarning("无法获取存档数据");
            return;
        }
        
        string selectedPetId = saveData.playerData.selectedDynamicIslandPetId;
        if (string.IsNullOrEmpty(selectedPetId))
        {
            Debug.LogWarning("未选择宠物用于灵动岛显示");
            return;
        }
        
        var petSaveData = saveData.petsData.Find(p => p.petId == selectedPetId);
        if (petSaveData == null)
        {
            Debug.LogWarning($"未找到宠物数据: {selectedPetId}");
            return;
        }
        
        var currentData = ConvertToPetData(petSaveData);
        Debug.Log($"当前选中宠物数据:");
        Debug.Log($"  - ID: {currentData.petId}");
        Debug.Log($"  - 名字: {currentData.petName}");
        Debug.Log($"  - 预制体: {currentData.prefabName}");
        Debug.Log($"  - 能量: {currentData.energy}");
        Debug.Log($"  - 饱腹度: {currentData.satiety}");
        Debug.Log($"  - 厌倦状态: {currentData.isBored}");
        Debug.Log($"  - 创建时间: {currentData.purchaseDate}");
        Debug.Log($"  - 养成天数: {currentData.ageInDays}天");
        Debug.Log($"  - 介绍: {currentData.introduction}");
        
        if (cachedPetData.ContainsKey(selectedPetId))
        {
            var cachedData = cachedPetData[selectedPetId];
            bool isEqual = IsPetDataEqual(cachedData, currentData);
            Debug.Log($"与缓存数据比较: {(isEqual ? "相同" : "不同")}");
            
            if (!isEqual)
            {
                Debug.Log("差异详情:");
                if (cachedData.petName != currentData.petName)
                    Debug.Log($"  - 名字: {cachedData.petName} -> {currentData.petName}");
                if (cachedData.energy != currentData.energy)
                    Debug.Log($"  - 能量: {cachedData.energy} -> {currentData.energy}");
                if (cachedData.satiety != currentData.satiety)
                    Debug.Log($"  - 饱腹度: {cachedData.satiety} -> {currentData.satiety}");
                if (cachedData.isBored != currentData.isBored)
                    Debug.Log($"  - 厌倦状态: {cachedData.isBored} -> {currentData.isBored}");
                if (cachedData.purchaseDate != currentData.purchaseDate)
                    Debug.Log($"  - 创建时间: {cachedData.purchaseDate} -> {currentData.purchaseDate}");
                if (cachedData.ageInDays != currentData.ageInDays)
                    Debug.Log($"  - 养成天数: {cachedData.ageInDays} -> {currentData.ageInDays}天");
                if (cachedData.introduction != currentData.introduction)
                    Debug.Log($"  - 介绍: {cachedData.introduction} -> {currentData.introduction}");
            }
        }
        else
        {
            Debug.Log("缓存中没有该宠物数据");
        }
    }
    
    /// <summary>
    /// 清理宠物数据缓存（调试用）
    /// </summary>
    [ContextMenu("清理宠物数据缓存")]
    public void ClearPetDataCache()
    {
        int count = cachedPetData.Count;
        cachedPetData.Clear();
        lastSyncedPetId = "";
        // Debug.Log($"[IOSDataBridge] 已清理宠物数据缓存，共清理 {count} 个缓存项");
        // Debug.Log("[IOSDataBridge] 下次调用SyncToIOS时将强制更新所有数据");
    }
    
    private void OnDestroy()
    {
        // 清理资源
        cachedPetData.Clear();
    }
}

/// <summary>
/// iOS Widget数据结构
/// </summary>
[Serializable]
public class IOSWidgetData
{
    public bool dynamicIslandEnabled;
    public string selectedPetId;
    public IOSPetData selectedPetData;
    public string lastUpdateTime;
}

/// <summary>
/// iOS宠物数据结构
/// </summary>
[Serializable]
public class IOSPetData
{
    public string petId;
    public string petName;
    public string prefabName;
    public int energy;
    public int satiety;
    public bool isBored;
    public string purchaseDate;
    public int ageInDays;
    public string introduction;
    public string lastUpdateTime;
} 