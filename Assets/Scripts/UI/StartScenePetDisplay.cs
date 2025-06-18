using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Start场景宠物形象显示管理器
/// 根据玩家存档信息动态显示拥有的宠物形象
/// </summary>
public class StartScenePetDisplay : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private Transform petContainer;                    // 宠物容器（Grid Prefab）
    [SerializeField] private float fixedYPosition = 0f;                 // 固定的Y轴坐标
    [SerializeField] private float minXPosition = -3f;                  // X轴最小值
    [SerializeField] private float maxXPosition = 3f;                   // X轴最大值
    [SerializeField] private float petSpacing = 1.5f;                   // 宠物之间的间距
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = false;              // 调试日志开关
    
    // 私有变量
    private List<GameObject> spawnedPets = new List<GameObject>();      // 已生成的宠物列表
    private bool isDisplayed = false;                                   // 是否已显示
    
    // 单例模式
    private static StartScenePetDisplay _instance;
    public static StartScenePetDisplay Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StartScenePetDisplay>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // 单例初始化
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 检查容器是否配置
        if (petContainer == null)
        {
            Debug.LogError("[StartScenePetDisplay] petContainer 未配置！请在Inspector中设置宠物容器");
            return;
        }
        
        // 订阅存档删除事件
        SubscribeToSaveEvents();
        
        // 延迟显示，确保存档系统已加载
        StartCoroutine(DelayedDisplayPets());
    }
    
    /// <summary>
    /// 订阅存档相关事件
    /// </summary>
    private void SubscribeToSaveEvents()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveDeleted += OnSaveDeleted;
        }
    }
    
    /// <summary>
    /// 取消订阅存档相关事件
    /// </summary>
    private void UnsubscribeFromSaveEvents()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveDeleted -= OnSaveDeleted;
        }
    }
    
    /// <summary>
    /// 存档删除事件处理
    /// </summary>
    private void OnSaveDeleted()
    {
        ClearDisplayedPets();
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件，避免内存泄漏
        UnsubscribeFromSaveEvents();
    }
    
    /// <summary>
    /// 延迟显示宠物（等待存档系统加载完成）
    /// </summary>
    private IEnumerator DelayedDisplayPets()
    {
        // 等待SaveManager初始化
        while (SaveManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 主动加载存档数据
        try
        {
            SaveManager.Instance.LoadSave();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartScenePetDisplay] 加载存档失败: {e.Message}");
        }
        
        // 再等待一帧确保数据完全同步
        yield return null;
        
        // 显示宠物
        DisplayPetsFromSave();
    }
    
    /// <summary>
    /// 根据存档信息显示宠物
    /// </summary>
    public void DisplayPetsFromSave()
    {
        if (isDisplayed) 
        {
            return;
        }
        
        // 清理现有宠物
        ClearDisplayedPets();
        
        // 获取存档数据
        SaveData saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData == null)
        {
            Debug.LogError("[StartScenePetDisplay] 没有找到存档数据");
            return;
        }
        
        if (saveData.petsData == null)
        {
            Debug.LogError("[StartScenePetDisplay] 存档中没有宠物数据");
            return;
        }
        
        List<PetSaveData> petsData = saveData.petsData;
        int petCount = petsData.Count;
        
        if (petCount == 0)
        {
            isDisplayed = true;
            return;
        }
        
        // 检查PetDatabaseManager
        if (PetDatabaseManager.Instance == null)
        {
            Debug.LogError("[StartScenePetDisplay] PetDatabaseManager.Instance 为空！");
            return;
        }
        
        // 计算宠物位置
        Vector3[] petPositions = CalculatePetPositions(petCount);
        
        // 生成宠物
        for (int i = 0; i < petCount; i++)
        {
            SpawnPetAtPosition(petsData[i], petPositions[i]);
        }
        
        isDisplayed = true;
    }
    
    /// <summary>
    /// 计算宠物位置数组
    /// </summary>
    /// <param name="petCount">宠物数量</param>
    /// <returns>位置数组</returns>
    private Vector3[] CalculatePetPositions(int petCount)
    {
        Vector3[] positions = new Vector3[petCount];
        
        if (petCount == 1)
        {
            // 单只宠物：居中
            positions[0] = new Vector3(0f, fixedYPosition, 0f);
        }
        else
        {
            // 多只宠物：对称排列
            float totalWidth = (petCount - 1) * petSpacing;
            float startX = -totalWidth / 2f;
            
            // 确保不超出范围
            float clampedStartX = Mathf.Max(minXPosition, startX);
            float clampedEndX = Mathf.Min(maxXPosition, startX + totalWidth);
            
            // 如果超出范围，重新计算间距
            if (clampedEndX - clampedStartX < totalWidth)
            {
                float actualSpacing = (clampedEndX - clampedStartX) / (petCount - 1);
                for (int i = 0; i < petCount; i++)
                {
                    float xPos = clampedStartX + i * actualSpacing;
                    positions[i] = new Vector3(xPos, fixedYPosition, 0f);
                }
            }
            else
            {
                // 正常间距
                for (int i = 0; i < petCount; i++)
                {
                    float xPos = startX + i * petSpacing;
                    positions[i] = new Vector3(xPos, fixedYPosition, 0f);
                }
            }
        }
        
        return positions;
    }
    
    /// <summary>
    /// 在指定位置生成宠物
    /// </summary>
    /// <param name="petSaveData">宠物存档数据</param>
    /// <param name="position">生成位置</param>
    private void SpawnPetAtPosition(PetSaveData petSaveData, Vector3 position)
    {
        // 根据预制体名称获取配置
        PetConfigData petConfig = PetDatabaseManager.Instance?.GetPetByPrefabName(petSaveData.prefabName);
        if (petConfig == null)
        {
            Debug.LogError($"[StartScenePetDisplay] 无法找到宠物配置，PrefabName: {petSaveData.prefabName}");
            return;
        }
        
        // 只使用预览prefab
        if (petConfig.previewPrefab == null)
        {
            Debug.LogError($"[StartScenePetDisplay] 宠物配置中预览预制体为空，PrefabName: {petSaveData.prefabName}");
            return;
        }
        
        // 生成宠物预制体
        GameObject petObj = Instantiate(petConfig.previewPrefab, position, Quaternion.identity, petContainer);
        
        if (petObj != null)
        {
            // 设置宠物显示名称为预制体名称加序号ID
            petObj.name = $"{petSaveData.prefabName}_{petSaveData.petId}";
            spawnedPets.Add(petObj);
        }
        else
        {
            Debug.LogError($"[StartScenePetDisplay] 宠物预制体实例化失败: {petConfig.petName}");
        }
    }
    

    
    /// <summary>
    /// 清理已显示的宠物
    /// </summary>
    public void ClearDisplayedPets()
    {
        foreach (var pet in spawnedPets)
        {
            if (pet != null)
            {
                DestroyImmediate(pet);
            }
        }
        spawnedPets.Clear();
        isDisplayed = false;
    }
    
    /// <summary>
    /// 刷新宠物显示
    /// </summary>
    public void RefreshPetDisplay()
    {
        isDisplayed = false;
        DisplayPetsFromSave();
    }
    
    /// <summary>
    /// 获取当前显示的宠物数量
    /// </summary>
    public int GetDisplayedPetCount()
    {
        return spawnedPets.Count;
    }
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[StartScenePetDisplay] {message}");
        }
    }
    
    /// <summary>
    /// 验证配置设置
    /// </summary>
    private void OnValidate()
    {
        // 确保最小值小于最大值
        if (minXPosition >= maxXPosition)
        {
            maxXPosition = minXPosition + 1f;
        }
        
        // 确保间距为正值
        if (petSpacing <= 0f)
        {
            petSpacing = 0.5f;
        }
    }
    
    #region 编辑器调试方法
    
    [ContextMenu("测试显示宠物")]
    public void TestDisplayPets()
    {
        if (Application.isPlaying)
        {
            RefreshPetDisplay();
        }
        else
        {
            Debug.LogWarning("只能在运行时测试宠物显示");
        }
    }
    
    [ContextMenu("清理宠物")]
    public void TestClearPets()
    {
        if (Application.isPlaying)
        {
            ClearDisplayedPets();
        }
        else
        {
            Debug.LogWarning("只能在运行时清理宠物");
        }
    }
    
    [ContextMenu("强制显示宠物（跳过isDisplayed检查）")]
    public void ForceDisplayPets()
    {
        if (Application.isPlaying)
        {
            isDisplayed = false;
            DisplayPetsFromSave();
        }
        else
        {
            Debug.LogWarning("只能在运行时强制显示宠物");
        }
    }
    
    #endregion
} 