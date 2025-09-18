using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Android数据桥接管理器
/// 负责Unity游戏数据与Android桌面小组件的数据互通
/// </summary>
public class AndroidDataBridge : MonoBehaviour
{
    // 单例模式
    private static AndroidDataBridge _instance;
    public static AndroidDataBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AndroidDataBridge>();
                if (_instance == null)
                {
                    GameObject bridgeObj = new GameObject("AndroidDataBridge");
                    _instance = bridgeObj.AddComponent<AndroidDataBridge>();
                    DontDestroyOnLoad(bridgeObj);
                }
            }
            return _instance;
        }
    }
    
    [Header("Android 数据同步设置")]
    [SerializeField] private bool enableAndroidSync = true;
    [SerializeField] private string sharedPrefsName = "DigiAnimalWidgetData";
    [SerializeField] private float syncInterval = 2f; // 数据同步间隔（秒）
    
    // Android原生插件方法声明
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject androidPlugin;
    private AndroidJavaClass unityClass;
    private AndroidJavaObject unityActivity;
#endif
    
    // 数据同步状态
    private bool isInitialized = false;
    private float syncTimer = 0f;
    private string lastSyncedPetId = "";
    private bool lastWidgetEnabled = false;
    
    // 缓存数据
    private Dictionary<string, AndroidPetData> cachedPetData = new Dictionary<string, AndroidPetData>();
    
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
        InitializeAndroidBridge();
    }
    
    private void Update()
    {
        if (!isInitialized || !enableAndroidSync)
            return;
            
        // 定时同步数据
        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            SyncToAndroid();
            syncTimer = 0f;
        }
    }
    
    /// <summary>
    /// 初始化Android桥接系统
    /// </summary>
    private void InitializeAndroidBridge()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 获取Unity Activity
            unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            
            // 初始化Android插件
            androidPlugin = new AndroidJavaObject("com.zher.meow.widget.AndroidWidgetPlugin", unityActivity);
            
            Debug.Log("[AndroidDataBridge] Android桥接系统已初始化");
            isInitialized = true;
            
            // 立即进行一次数据同步
            SyncToAndroid();
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidDataBridge] 初始化失败: {e.Message}");
        }
#else
        Debug.Log("[AndroidDataBridge] 非Android平台，跳过原生桥接初始化");
        isInitialized = true;
        
        // 在编辑器中也进行数据同步（用于测试）
        SyncToAndroid();
#endif
    }
    
    /// <summary>
    /// 同步数据到Android
    /// </summary>
    public void SyncToAndroid()
    {
        try
        {
            var saveData = SaveManager.Instance?.GetCurrentSaveData();
            if (saveData?.playerData == null)
            {
                Debug.LogWarning("[AndroidDataBridge] 存档数据不可用，跳过同步");
                return;
            }
            
            // 获取当前选中的宠物ID（可以复用iOS的逻辑，或者单独设置Android的选中宠物）
            string selectedPetId = saveData.playerData.selectedDynamicIslandPetId; // 暂时复用iOS的设置
            bool widgetEnabled = true; // Android小组件默认启用
            
            // 检查是否需要更新
            bool needUpdate = false;
            if (lastWidgetEnabled != widgetEnabled)
            {
                needUpdate = true;
                lastWidgetEnabled = widgetEnabled;
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
                    var currentPetData = ConvertToAndroidPetData(petSaveData);
                    
                    // 如果缓存中有数据，比较内容是否变化
                    if (cachedPetData.ContainsKey(selectedPetId))
                    {
                        var cachedData = cachedPetData[selectedPetId];
                        if (!IsPetDataEqual(cachedData, currentPetData))
                        {
                            needUpdate = true;
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
            var syncData = new AndroidWidgetData
            {
                widgetEnabled = widgetEnabled,
                selectedPetId = selectedPetId,
                lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // 获取选中宠物的数据
            if (!string.IsNullOrEmpty(selectedPetId) && saveData.petsData != null)
            {
                var petSaveData = saveData.petsData.Find(p => p.petId == selectedPetId);
                if (petSaveData != null)
                {
                    var petData = ConvertToAndroidPetData(petSaveData);
                    syncData.selectedPetData = petData;
                    
                    // 缓存宠物数据
                    cachedPetData[selectedPetId] = petData;
                }
            }
            
            // 序列化数据并发送到Android
            string jsonData = JsonUtility.ToJson(syncData, true);
            
#if UNITY_ANDROID && !UNITY_EDITOR
            // 使用AndroidWidgetPlugin更新数据
            AndroidWidgetPlugin.UpdateWidgetData(jsonData);
            Debug.Log($"[AndroidDataBridge] 数据已同步到Android小组件");
#else
            // 在编辑器中保存到PlayerPrefs用于测试
            PlayerPrefs.SetString("Android_WidgetData", jsonData);
            PlayerPrefs.Save();
            
            Debug.Log($"[AndroidDataBridge] 数据已同步（编辑器模式）:\n{jsonData}");
#endif
            
            Debug.Log($"[AndroidDataBridge] 数据同步完成 - 小组件:{widgetEnabled}, 宠物:{selectedPetId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidDataBridge] 数据同步失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 将PetSaveData转换为AndroidPetData
    /// </summary>
    private AndroidPetData ConvertToAndroidPetData(PetSaveData petSaveData)
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
        
        return new AndroidPetData
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
    /// 比较两个AndroidPetData是否相等（不包括lastUpdateTime）
    /// </summary>
    private bool IsPetDataEqual(AndroidPetData data1, AndroidPetData data2)
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
        Debug.Log("[AndroidDataBridge] 强制同步数据");
        SyncToAndroid();
    }
    
    /// <summary>
    /// 播放宠物动画（从Android小组件调用）
    /// </summary>
    public void PlayPetAnimation(string animationType)
    {
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        string petPrefabName = "Pet_CatBrown"; // 默认值
        
        if (saveData?.playerData != null && !string.IsNullOrEmpty(saveData.playerData.selectedDynamicIslandPetId))
        {
            var petSaveData = saveData.petsData?.Find(p => p.petId == saveData.playerData.selectedDynamicIslandPetId);
            if (petSaveData != null)
            {
                petPrefabName = petSaveData.prefabName;
            }
        }
        
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidWidgetPlugin.PlayPetAnimation(petPrefabName, animationType);
        Debug.Log($"[AndroidDataBridge] 播放动画: {petPrefabName} - {animationType}");
#else
        Debug.Log($"[AndroidDataBridge] 播放动画（编辑器模式）: {petPrefabName} - {animationType}");
#endif
    }
    
    /// <summary>
    /// 获取当前缓存的Android数据（用于调试）
    /// </summary>
    [ContextMenu("显示当前Android数据")]
    public void ShowCurrentAndroidData()
    {
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData == null)
        {
            Debug.Log("[AndroidDataBridge] 无存档数据");
            return;
        }
        
        Debug.Log($"[AndroidDataBridge] 当前Android数据状态:");
        Debug.Log($"  - 小组件开启: {lastWidgetEnabled}");
        Debug.Log($"  - 选中宠物: {lastSyncedPetId}");
        
        // 显示编辑器模式下的数据
#if UNITY_EDITOR
        string editorData = PlayerPrefs.GetString("Android_WidgetData", "无数据");
        Debug.Log($"  - 编辑器模式数据: {editorData}");
#endif
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
        Debug.Log($"[AndroidDataBridge] 已清理宠物数据缓存，共清理 {count} 个缓存项");
    }
    
    private void OnDestroy()
    {
        // 清理资源
        cachedPetData.Clear();
        
#if UNITY_ANDROID && !UNITY_EDITOR
        androidPlugin?.Dispose();
        unityActivity?.Dispose();
        unityClass?.Dispose();
#endif
    }
}

/// <summary>
/// Android Widget数据结构
/// </summary>
[Serializable]
public class AndroidWidgetData
{
    public bool widgetEnabled;
    public string selectedPetId;
    public AndroidPetData selectedPetData;
    public string lastUpdateTime;
}

/// <summary>
/// Android宠物数据结构
/// </summary>
[Serializable]
public class AndroidPetData
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
