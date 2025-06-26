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
    /// 刷新宠物显示（强制重新加载）
    /// </summary>
    public void RefreshPetDisplay()
    {
        // Debug.Log("[StartScenePetDisplay] 强制刷新宠物显示");
        
        // 重置显示状态
        isDisplayed = false;
        
        // 清理现有宠物
        ClearDisplayedPets();
        
        // 重新显示宠物
        DisplayPetsFromSave();
    }
    
    /// <summary>
    /// 根据存档信息显示宠物
    /// </summary>
    public void DisplayPetsFromSave()
    {
        // Debug.Log($"[StartScenePetDisplay] DisplayPetsFromSave 开始，isDisplayed={isDisplayed}");
        
        if (isDisplayed) 
        {
            // Debug.Log("[StartScenePetDisplay] 宠物已显示，跳过");
            return;
        }
        
        // 清理现有宠物
        ClearDisplayedPets();
        
        // 获取存档数据
        SaveData saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData == null)
        {
            Debug.LogError("[StartScenePetDisplay] 没有找到存档数据");
            Debug.LogError($"[StartScenePetDisplay] SaveManager.Instance: {SaveManager.Instance != null}");
            return;
        }
        
        if (saveData.petsData == null)
        {
            Debug.LogError("[StartScenePetDisplay] 存档中没有宠物数据");
            return;
        }
        
        List<PetSaveData> petsData = saveData.petsData;
        int petCount = petsData.Count;
        
        // Debug.Log($"[StartScenePetDisplay] 找到 {petCount} 只宠物");
        
        if (petCount == 0)
        {
            // Debug.Log("[StartScenePetDisplay] 没有宠物数据，设置为已显示");
            isDisplayed = true;
            return;
        }
        
        // 检查PetDatabaseManager
        if (PetDatabaseManager.Instance == null)
        {
            Debug.LogError("[StartScenePetDisplay] PetDatabaseManager.Instance 为空！");
            return;
        }
        
        if (!PetDatabaseManager.Instance.IsDatabaseLoaded())
        {
            Debug.LogError("[StartScenePetDisplay] PetDatabaseManager 数据库未加载！");
            return;
        }
        
        // 检查petContainer
        if (petContainer == null)
        {
            Debug.LogError("[StartScenePetDisplay] petContainer 未配置！");
            return;
        }
        
        // Debug.Log($"[StartScenePetDisplay] 开始生成 {petCount} 只宠物");
        
        // 计算宠物位置
        Vector3[] petPositions = CalculatePetPositions(petCount);
        
        // 生成宠物
        for (int i = 0; i < petCount; i++)
        {
            // Debug.Log($"[StartScenePetDisplay] 生成第 {i + 1}/{petCount} 只宠物");
            SpawnPetAtPosition(petsData[i], petPositions[i]);
        }
        
        isDisplayed = true;
        // Debug.Log($"[StartScenePetDisplay] 宠物显示完成，共生成 {spawnedPets.Count} 只宠物");
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
        // Debug.Log($"[StartScenePetDisplay] 开始生成宠物: {petSaveData.prefabName} (ID: {petSaveData.petId})");
        
        // 检查PetDatabaseManager
        if (PetDatabaseManager.Instance == null)
        {
            Debug.LogError("[StartScenePetDisplay] PetDatabaseManager.Instance为空！");
            return;
        }
        
        // 根据预制体名称获取配置
        PetConfigData petConfig = PetDatabaseManager.Instance.GetPetByPrefabName(petSaveData.prefabName);
        if (petConfig == null)
        {
            Debug.LogError($"[StartScenePetDisplay] 无法找到宠物配置，PrefabName: {petSaveData.prefabName}");
            
            // 尝试列出所有可用的宠物配置用于调试
            var allPets = PetDatabaseManager.Instance.GetAllPets();
            // Debug.Log($"[StartScenePetDisplay] 数据库中的宠物配置: {string.Join(", ", allPets.ConvertAll(p => $"{p.petId}({p.petPrefab?.name})"))}");
            return;
        }
        
        // Debug.Log($"[StartScenePetDisplay] 找到宠物配置: {petConfig.petName}, 预览预制体: {petConfig.previewPrefab?.name}");
        
        // 只使用预览prefab
        if (petConfig.previewPrefab == null)
        {
            Debug.LogError($"[StartScenePetDisplay] 宠物配置中预览预制体为空，PetConfig: {petConfig.petName}");
            return;
        }
        
        // 生成宠物预制体
        GameObject petObj = Instantiate(petConfig.previewPrefab, position, Quaternion.identity, petContainer);
        
        if (petObj != null)
        {
            // 设置宠物显示名称为预制体名称加序号ID
            petObj.name = $"{petSaveData.prefabName}_{petSaveData.petId}";
            spawnedPets.Add(petObj);
            
            // 确保Preview宠物显示正确的默认状态
            ResetPetPreviewState(petObj, petConfig);
            
            // Debug.Log($"[StartScenePetDisplay] 成功生成宠物: {petObj.name} 在位置 {position}");
            
            // 检查生成的宠物的SpriteRenderer
            SpriteRenderer spriteRenderer = petObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Debug.Log($"[StartScenePetDisplay] 宠物Sprite: {spriteRenderer.sprite?.name}");
            }
            else
            {
                Debug.LogWarning($"[StartScenePetDisplay] 宠物预制体没有SpriteRenderer组件: {petObj.name}");
            }
        }
        else
        {
            Debug.LogError($"[StartScenePetDisplay] 宠物预制体实例化失败: {petConfig.petName}");
        }
    }
    
    /// <summary>
    /// 重置宠物预览状态，确保显示正确的默认外观
    /// </summary>
    private void ResetPetPreviewState(GameObject petObj, PetConfigData petConfig)
    {
        // 禁用可能影响显示的组件
        PetController2D petController = petObj.GetComponent<PetController2D>();
        if (petController != null)
        {
            // 禁用PetController2D，避免其逻辑影响显示
            petController.enabled = false;
            // Debug.Log($"[StartScenePetDisplay] 已禁用PetController2D组件: {petObj.name}");
        }
        
        // 禁用Animator组件，确保显示静态的默认状态
        Animator animator = petObj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            // Debug.Log($"[StartScenePetDisplay] 已禁用Animator组件: {petObj.name}");
        }
        
        // 禁用NavMeshAgent等移动相关组件
        UnityEngine.AI.NavMeshAgent navAgent = petObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.enabled = false;
            // Debug.Log($"[StartScenePetDisplay] 已禁用NavMeshAgent组件: {petObj.name}");
        }
        
        // 禁用所有Collider，避免物理交互
        Collider2D[] colliders = petObj.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
        
        Collider[] colliders3D = petObj.GetComponents<Collider>();
        foreach (var collider in colliders3D)
        {
            collider.enabled = false;
        }
        
        // 确保SpriteRenderer显示正确的默认Sprite
        SpriteRenderer spriteRenderer = petObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 如果PreviewPrefab本身的SpriteRenderer没有正确的Sprite，尝试从配置中获取
            if (spriteRenderer.sprite == null)
            {
                Debug.LogWarning($"[StartScenePetDisplay] Preview Prefab的SpriteRenderer没有Sprite，宠物: {petConfig.petName}");
            }
            
            // 确保显示层级正确 - Preview宠物应该在Pet层
            spriteRenderer.sortingLayerName = "Pet";
            spriteRenderer.sortingOrder = 0;
        }
        
        // Debug.Log($"[StartScenePetDisplay] 宠物预览状态重置完成: {petObj.name}");
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
                // 使用Destroy而不是DestroyImmediate，避免影响同GameObject上的其他组件
                Destroy(pet);
            }
        }
        spawnedPets.Clear();
        isDisplayed = false;
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
    
    [ContextMenu("诊断宠物显示问题")]
    public void DiagnosePetDisplay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("只能在运行时诊断宠物显示问题");
            return;
        }
        
        // Debug.Log("=== 宠物显示诊断开始 ===");
        
        // 1. 检查基础组件
        // Debug.Log($"petContainer: {petContainer != null}");
        // Debug.Log($"isDisplayed: {isDisplayed}");
        // Debug.Log($"spawnedPets.Count: {spawnedPets.Count}");
        
        // 2. 检查管理器
        // Debug.Log($"SaveManager.Instance: {SaveManager.Instance != null}");
        // Debug.Log($"PetDatabaseManager.Instance: {PetDatabaseManager.Instance != null}");
        
        if (PetDatabaseManager.Instance != null)
        {
            // Debug.Log($"Database loaded: {PetDatabaseManager.Instance.IsDatabaseLoaded()}");
            var allPets = PetDatabaseManager.Instance.GetAllPets();
            // Debug.Log($"Database pets count: {allPets.Count}");
            foreach (var pet in allPets)
            {
                // Debug.Log($"  - {pet.petId}: petPrefab={pet.petPrefab?.name}, previewPrefab={pet.previewPrefab?.name}");
            }
        }
        
        // 3. 检查存档数据
        if (SaveManager.Instance != null)
        {
            var saveData = SaveManager.Instance.GetCurrentSaveData();
            if (saveData != null)
            {
                // Debug.Log($"Save data pets count: {saveData.petsData?.Count ?? 0}");
                if (saveData.petsData != null)
                {
                    foreach (var petData in saveData.petsData)
                    {
                        // Debug.Log($"  - {petData.petId}: prefabName={petData.prefabName}, displayName={petData.displayName}");
                    }
                }
            }
            else
            {
                Debug.LogError("Save data is null");
            }
        }
        
        // Debug.Log("=== 宠物显示诊断结束 ===");
    }
    
    #endregion
} 