using UnityEngine;
using System.Collections;

public class PlantController : MonoBehaviour, ISelectableFurniture, ISpawnableFurniture
{
    // 植物健康度 (0-100)
    [SerializeField]
    [Range(0, 100)]
    private int _healthLevel = 100; // 默认值，仅在新建植物时使用
    
    // 标记是否已从存档加载，避免默认值覆盖存档数据
    private bool hasLoadedFromSave = false;
    
    // 浇水时每秒恢复的健康度
    [SerializeField]
    private int healthRecoveryValue = 10;
    
    // 浇水视觉效果设置
    [Header("浇水视觉效果设置")]
    [SerializeField]
    private GameObject wateringCanPrefab;        // 水壶预制体
    [SerializeField]
    private Vector3 wateringCanOffset = new Vector3(0, 1f, 0); // 水壶相对植物的偏移位置
    [SerializeField]
    private float wateringDuration = 2f;         // 浇水持续时间
    [SerializeField]
    private GameObject sparkleEffectPrefab;      // 闪亮特效预制体
    [SerializeField]
    private Vector3 sparkleEffectOffset = Vector3.zero; // 闪亮特效相对植物的偏移位置
    [SerializeField]
    private float sparkleEffectDuration = 1f;    // 闪亮特效持续时间
    
    // 植物状态图片
    [Header("植物外观设置")]
    [SerializeField]
    private Sprite healthySprite;     // 健康状态图片 (>=60)
    
    [SerializeField]
    private Sprite thirstySprite;     // 缺水状态图片 (10-59)
    
    [SerializeField]
    private Sprite witheredSprite;    // 枯萎状态图片 (<10)
    
    // 植物基本信息
    [Header("植物基本信息")]
    [SerializeField]
    private string plantName = "盆栽植物";  // 植物名称
    
    [SerializeField]
    private Sprite plantIcon;         // 植物图标，用于信息面板显示
    
    // 植物是否被选中
    [SerializeField]
    public bool isSelected = false;
    
    // 植物唯一标识符（用于存档）
    [Header("存档设置")]
    [SerializeField]
    private string plantId = "";
    
    // 家具配置ID（来自FurnitureDatabase）
    [SerializeField]
    private string configId = "";
    
    // 默认家具标识符（如果是默认创建的家具）
    [SerializeField]
    private string saveDataId = "";
    
    // 健康度下降设置
    [Header("健康度下降设置")]
    [SerializeField]
    private float minDecreaseInterval = 180f; // 最小间隔（3分钟）
    [SerializeField]
    private float maxDecreaseInterval = 300f; // 最大间隔（5分钟）
    [SerializeField]
    private int healthDecreaseValue = 1; // 每次下降的健康度
    
    // 是否正在加载存档数据（用于避免在加载时触发自动保存）
    private bool isLoadingFromSave = false;
    
    // 离线时间计算相关
    private System.DateTime lastHealthUpdateTime = System.DateTime.Now;
    private System.DateTime pauseTime = System.DateTime.MinValue;
    private bool wasApplicationPaused = false;
    private float offlineTimeStampUpdateTimer = 0f;
    
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    // SpriteRenderer组件
    private SpriteRenderer spriteRenderer;
    
    // 健康度下降协程
    private Coroutine healthDecreaseCoroutine;
    
    // 植物状态枚举
    public enum PlantState
    {
        Healthy,   // 健康 (>=60)
        Thirsty,   // 缺水 (10-59)
        Withered   // 枯萎 (<10)
    }
    
    void Awake()
    {
        // 获取组件
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 生成植物ID（如果为空）
        if (string.IsNullOrEmpty(plantId))
        {
            GeneratePlantId();
        }
        else
        {
            Debug.Log($"[PlantController] 使用现有植物ID: {plantId}");
        }
    }
    
    void Start()
    {
        // 等待一帧，确保GameInitializer有机会加载存档数据
        StartCoroutine(DelayedInitialization());
    }
    
    /// <summary>
    /// 延迟初始化，确保存档加载优先级
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待一帧，让GameInitializer有机会执行
        yield return null;
        
        // 如果没有从存档加载，则使用默认值
        if (!hasLoadedFromSave)
        {
            // 新植物，使用默认健康度100
            Debug.Log($"[PlantController] ⚠️ 植物 {PlantName} 未从存档加载，使用默认健康度: {_healthLevel}");
        }
        
        // 初始化植物外观
        UpdatePlantAppearance();
        
        // 开始健康度下降协程
        StartHealthDecreaseCoroutine();
    }
    
    void Update()
    {
        // 更新离线时间戳（每秒更新一次）
        UpdateOfflineTimeStamps();
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 编辑器模式下验证健康度变化（用于Inspector修改）
    /// </summary>
    void OnValidate()
    {
        // 确保健康度在有效范围内
        _healthLevel = Mathf.Clamp(_healthLevel, 0, 100);
        
        // 在编辑器中修改健康度时也要更新外观
        if (Application.isPlaying && spriteRenderer != null)
        {
            UpdatePlantAppearance();
        }
    }
    #endif
    
    void OnDestroy()
    {
        // 停止健康度下降协程
        if (healthDecreaseCoroutine != null)
        {
            StopCoroutine(healthDecreaseCoroutine);
        }
    }
    
    /// <summary>
    /// 植物健康度属性
    /// </summary>
    public int HealthLevel
    {
        get { return _healthLevel; }
        set
        {
            int oldValue = _healthLevel;
            // 限制健康度在0-100范围内
            _healthLevel = Mathf.Clamp(value, 0, 100);
            
            // 如果值发生变化，更新外观和通知存档系统
            if (oldValue != _healthLevel)
            {
                UpdatePlantAppearance();
                
                if (!isLoadingFromSave && GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.OnPlantDataChanged();
                }
                
                //Debug.Log($"[PlantController] 植物 {PlantName} 健康度变化: {oldValue} -> {_healthLevel} (isLoadingFromSave: {isLoadingFromSave}, hasLoadedFromSave: {hasLoadedFromSave})");
            }
        }
    }
    
    /// <summary>
    /// 植物名称
    /// </summary>
    public string PlantName
    {
        get { return plantName; }
        set { plantName = value; }
    }
    
    /// <summary>
    /// 植物唯一ID
    /// </summary>
    public string PlantId
    {
        get { return plantId; }
        set { plantId = value; }
    }
    
    /// <summary>
    /// 植物图标
    /// </summary>
    public Sprite PlantIcon
    {
        get { return plantIcon; }
    }
    
    #region ISpawnableFurniture 接口实现
    
    /// <summary>
    /// 家具唯一ID（ISpawnableFurniture接口）
    /// </summary>
    public string FurnitureId
    {
        get { return plantId; }
        set { plantId = value; }
    }
    
    /// <summary>
    /// 家具类型（ISpawnableFurniture接口）
    /// </summary>
    public FurnitureType SpawnableFurnitureType => global::FurnitureType.Plant;
    
    /// <summary>
    /// 家具名称（ISpawnableFurniture接口）
    /// </summary>
    string ISpawnableFurniture.FurnitureName
    {
        get { return plantName; }
        set { plantName = value; }
    }
    
    /// <summary>
    /// 家具位置（ISpawnableFurniture接口）
    /// </summary>
    public Vector3 Position
    {
        get { return transform.position; }
        set { transform.position = value; }
    }
    

    
    /// <summary>
    /// 从存档数据初始化家具（ISpawnableFurniture接口）
    /// </summary>
    public void InitializeFromSaveData(object saveData)
    {
        if (saveData is PlantSaveData plantSaveData)
        {
            LoadFromSaveData(plantSaveData);
        }
        else
        {
            Debug.LogError($"[PlantController] 无效的存档数据类型: {saveData?.GetType()}");
        }
    }
    
    /// <summary>
    /// 获取存档数据（ISpawnableFurniture接口）
    /// </summary>
    object ISpawnableFurniture.GetSaveData()
    {
        return GetSaveData();
    }
    
    /// <summary>
    /// 生成家具ID（ISpawnableFurniture接口）
    /// </summary>
    public void GenerateFurnitureId()
    {
        GeneratePlantId();
    }
    
    #endregion
    
    /// <summary>
    /// 获取当前植物状态
    /// </summary>
    public PlantState CurrentState
    {
        get
        {
            if (_healthLevel >= 60) return PlantState.Healthy;
            if (_healthLevel >= 10) return PlantState.Thirsty;
            return PlantState.Withered;
        }
    }
    
    /// <summary>
    /// 获取状态描述文本
    /// </summary>
    public string GetStateDescription()
    {
        switch (CurrentState)
        {
            case PlantState.Healthy:
                return "生机盎然";
            case PlantState.Thirsty:
                return "需要浇水";
            case PlantState.Withered:
                return "快要枯萎";
            default:
                return "未知状态";
        }
    }
    
    /// <summary>
    /// 浇水操作（同步版本，用于向后兼容）
    /// </summary>
    /// <returns>是否浇水成功</returns>
    public bool TryWatering()
    {
        // 直接恢复健康度（无视觉效果）
        HealthLevel += healthRecoveryValue;
        return true;
    }
    
    /// <summary>
    /// 浇水操作（带视觉效果的异步版本）
    /// </summary>
    /// <param name="onWateringStart">浇水开始回调</param>
    /// <param name="onWateringComplete">浇水完成回调</param>
    /// <returns>是否可以开始浇水</returns>
    public bool TryWateringWithEffects(System.Action onWateringStart = null, System.Action onWateringComplete = null)
    {
        // 检查是否可以浇水
        if (_healthLevel >= 100)
        {
            return false;
        }
        
        // 启动浇水协程
        StartCoroutine(WateringSequence(onWateringStart, onWateringComplete));
        return true;
    }
    
    /// <summary>
    /// 浇水序列协程
    /// </summary>
    private IEnumerator WateringSequence(System.Action onWateringStart, System.Action onWateringComplete)
    {
        // 1. 浇水开始回调
        onWateringStart?.Invoke();
        
        // 2. 生成水壶动画（作为植物的子对象）
        GameObject wateringCanInstance = null;
        if (wateringCanPrefab != null)
        {
            Vector3 wateringPosition = transform.position + wateringCanOffset;
            wateringCanInstance = Instantiate(wateringCanPrefab, wateringPosition, Quaternion.identity, transform);
            
            // 确保水壶在正确的层级显示
            SpriteRenderer wateringCanRenderer = wateringCanInstance.GetComponent<SpriteRenderer>();
            if (wateringCanRenderer != null)
            {
                wateringCanRenderer.sortingOrder = 10;
            }
        }
        
        // 3. 渐进式恢复健康度
        yield return StartCoroutine(GradualHealthRecovery());
        
        // 4. 销毁水壶
        if (wateringCanInstance != null)
        {
            Destroy(wateringCanInstance);
        }
        
        // 5. 播放闪亮特效（作为植物的子对象）
        if (sparkleEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position + sparkleEffectOffset;
            GameObject effectInstance = Instantiate(sparkleEffectPrefab, effectPosition, Quaternion.identity, transform);
            
            // 确保特效在正确的层级显示
            SpriteRenderer effectRenderer = effectInstance.GetComponent<SpriteRenderer>();
            if (effectRenderer != null)
            {
                effectRenderer.sortingOrder = 15;
            }
            
            // 特效播放完后自动销毁
            float sparkleEffectDuration = GetSparkleEffectDuration();
            Destroy(effectInstance, sparkleEffectDuration);
        }
        
        // 6. 浇水完成回调
        onWateringComplete?.Invoke();
        
        // Debug.Log($"植物 {PlantName} 浇水完成，健康度恢复 {healthRecoveryValue} 点");
    }
    
    /// <summary>
    /// 渐进式健康度恢复协程
    /// </summary>
    private IEnumerator GradualHealthRecovery()
    {
        int startingHealth = _healthLevel;
        
        // 计算总恢复量：每秒恢复量 × 浇水持续时间
        int totalRecovery = Mathf.RoundToInt(healthRecoveryValue * wateringDuration);
        int targetHealth = Mathf.Min(100, _healthLevel + totalRecovery);
        
        // 如果已经满血，直接等待浇水时间
        if (startingHealth >= 100)
        {
            yield return new WaitForSeconds(wateringDuration);
            yield break;
        }
        
        // 实际恢复量（考虑100的上限）
        int actualRecovery = targetHealth - startingHealth;
        
        if (actualRecovery <= 0)
        {
            yield return new WaitForSeconds(wateringDuration);
            yield break;
        }
        
        float elapsedTime = 0f;
        float recoveryTimer = 0f;
        int recoveredAmount = 0;
        
        while (elapsedTime < wateringDuration && _healthLevel < 100)
        {
            elapsedTime += Time.deltaTime;
            recoveryTimer += Time.deltaTime;
            
            // 每秒恢复一次
            if (recoveryTimer >= 1f)
            {
                recoveryTimer = 0f;
                recoveredAmount += healthRecoveryValue;
                
                // 确保不超过目标健康度和100的上限
                int newHealth = Mathf.Min(100, startingHealth + recoveredAmount);
                newHealth = Mathf.Min(newHealth, targetHealth);
                
                if (newHealth != _healthLevel)
                {
                    HealthLevel = newHealth;
                }
                
                // 如果达到满血，提前结束
                if (_healthLevel >= 100)
                {
                    break;
                }
            }
            
            yield return null; // 等待下一帧
        }
        
        // 确保最终健康度正确
        HealthLevel = targetHealth;
    }
    
    /// <summary>
    /// 获取闪亮特效持续时间
    /// </summary>
    private float GetSparkleEffectDuration()
    {
        return sparkleEffectDuration;
    }
    
    /// <summary>
    /// 更新植物外观
    /// </summary>
    private void UpdatePlantAppearance()
    {
        if (spriteRenderer == null) return;
        
        // 根据健康度状态切换对应的图片
        switch (CurrentState)
        {
            case PlantState.Healthy:
                if (healthySprite != null)
                    spriteRenderer.sprite = healthySprite;
                break;
            case PlantState.Thirsty:
                if (thirstySprite != null)
                    spriteRenderer.sprite = thirstySprite;
                break;
            case PlantState.Withered:
                if (witheredSprite != null)
                    spriteRenderer.sprite = witheredSprite;
                break;
        }
    }
    
    /// <summary>
    /// 开始健康度下降协程
    /// </summary>
    private void StartHealthDecreaseCoroutine()
    {
        if (healthDecreaseCoroutine != null)
        {
            StopCoroutine(healthDecreaseCoroutine);
        }
        
        healthDecreaseCoroutine = StartCoroutine(HealthDecreaseRoutine());
    }
    
    /// <summary>
    /// 健康度下降协程
    /// </summary>
    private IEnumerator HealthDecreaseRoutine()
    {
        while (true)
        {
            // 随机等待时间（3-5分钟）
            float waitTime = Random.Range(minDecreaseInterval, maxDecreaseInterval);
            yield return new WaitForSeconds(waitTime);
            
            // 下降健康度
            if (_healthLevel > 0)
            {
                HealthLevel -= healthDecreaseValue;
                // Debug.Log($"植物 {PlantName} 健康度自然下降，当前健康度: {_healthLevel}");
            }
        }
    }
    
    /// <summary>
    /// 生成植物唯一ID
    /// </summary>
    private void GeneratePlantId()
    {
        Vector3 pos = transform.position;
        plantId = $"{plantName}_{pos.x:F2}_{pos.y:F2}";
        //Debug.Log($"[PlantController] 生成植物ID: {plantId} (名称: {plantName}, 位置: {pos})");
    }
    
    /// <summary>
    /// 获取存档数据
    /// </summary>
    public PlantSaveData GetSaveData()
    {
        return new PlantSaveData
        {
            plantId = this.plantId,
            configId = this.configId,
            saveDataId = this.saveDataId,  // 添加 saveDataId
            healthLevel = this._healthLevel,
            position = transform.position,
            wateringHeartCost = 0, // 浇水免费，但保持存档兼容性
            healthRecoveryValue = this.healthRecoveryValue,
            lastHealthUpdateTime = lastHealthUpdateTime.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }
    
    /// <summary>
    /// 从存档数据加载
    /// </summary>
    public void LoadFromSaveData(PlantSaveData saveData)
    {
        if (saveData == null) return;
        
        try
        {
            isLoadingFromSave = true;
            hasLoadedFromSave = true; // 标记已从存档加载
            
            plantId = saveData.plantId;
            configId = saveData.configId;
            saveDataId = saveData.saveDataId;  // 加载 saveDataId
            HealthLevel = saveData.healthLevel; // 这会触发外观更新
            // wateringHeartCost 已移除，浇水免费
            healthRecoveryValue = saveData.healthRecoveryValue;
            
            // 设置位置
            transform.position = saveData.position;
            
            // 确保植物ID与存档一致（避免下次匹配失败）
            if (plantId != saveData.plantId)
            {
                //Debug.Log($"[PlantController] 更新植物ID: {plantId} -> {saveData.plantId}");
                plantId = saveData.plantId;
            }
            
            // 应用离线时间变化
            System.DateTime lastHealthUpdate = ParseDateTime(saveData.lastHealthUpdateTime);
            ApplyOfflineTimeChanges(lastHealthUpdate);
            
            //Debug.Log($"[PlantController] 植物 {PlantName} 加载存档数据完成，健康度: {_healthLevel} (存档值: {saveData.healthLevel})");
        }
        finally
        {
            isLoadingFromSave = false;
        }
    }
    
    /// <summary>
    /// 解析日期时间字符串，失败时返回MinValue
    /// </summary>
    private System.DateTime ParseDateTime(string dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return System.DateTime.MinValue;
            
        if (System.DateTime.TryParseExact(dateTimeString, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out System.DateTime result))
        {
            return result;
        }
        
        // Debug.LogWarning($"[PlantController] 无法解析时间字符串: {dateTimeString}，使用默认值");
        return System.DateTime.MinValue;
    }
    
    // OnMouseDown 已移除，现在统一由FurnitureManager.Update()处理点击检测
    
    /// <summary>
    /// 取消选中植物
    /// </summary>
    public void DeselectPlant()
    {
        isSelected = false;
        
        // 隐藏描边
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(false);
        }
    }
    
    // ===== ISelectableFurniture 接口实现 =====
    
    public string FurnitureType => "Plant";
    
    public string FurnitureName => PlantName;
    
    public bool IsSelected 
    { 
        get => isSelected; 
        set => isSelected = value; 
    }
    
    public GameObject GameObject => gameObject;
    
    public void OnSelected()
    {
        // 激活描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(true);
        }
        
        // 显示植物信息面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPlantInfo(this);
        }
    }
    
    public void OnDeselected()
    {
        isSelected = false;
        
        // 停用描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(false);
        }
        
        // 关闭植物信息面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePlantInfo();
        }
    }
    
    public Sprite GetIcon()
    {
        return PlantIcon;
    }
    
    #region 离线时间计算相关
    
    /// <summary>
    /// 更新离线时间戳（每秒更新一次）
    /// </summary>
    private void UpdateOfflineTimeStamps()
    {
        offlineTimeStampUpdateTimer += Time.deltaTime;
        
        // 每秒更新一次时间戳
        if (offlineTimeStampUpdateTimer >= 1.0f)
        {
            lastHealthUpdateTime = System.DateTime.Now;
            offlineTimeStampUpdateTimer -= 1.0f;
        }
    }
    
    /// <summary>
    /// 应用程序暂停时调用
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // 游戏暂停（切换到后台）
            pauseTime = System.DateTime.Now;
            wasApplicationPaused = true;
        }
        else if (wasApplicationPaused)
        {
            // 游戏恢复（从后台回到前台）
            System.DateTime resumeTime = System.DateTime.Now;
            ApplyBackgroundTimeChanges(pauseTime, resumeTime);
            wasApplicationPaused = false;
        }
    }
    
    /// <summary>
    /// 应用程序焦点变化时调用
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // 游戏失去焦点
            pauseTime = System.DateTime.Now;
            wasApplicationPaused = true;
        }
        else if (wasApplicationPaused)
        {
            // 游戏获得焦点
            System.DateTime resumeTime = System.DateTime.Now;
            ApplyBackgroundTimeChanges(pauseTime, resumeTime);
            wasApplicationPaused = false;
        }
    }
    
    /// <summary>
    /// 应用后台时间变化（与完全离线类似，但时间较短）
    /// </summary>
    private void ApplyBackgroundTimeChanges(System.DateTime pauseTime, System.DateTime resumeTime)
    {
        System.TimeSpan backgroundTime = resumeTime - pauseTime;
        double backgroundSeconds = backgroundTime.TotalSeconds;
        
        // 只有后台时间超过5秒才进行计算，避免频繁切换的影响
        if (backgroundSeconds < 5.0)
            return;
            
        // Debug.Log($"植物 {PlantName} 在后台 {backgroundSeconds:F0} 秒，开始计算健康度变化");
        
        // 应用后台健康度变化
        if (backgroundSeconds > 0)
        {
            // 计算后台期间的健康度变化
            // 使用平均间隔时间来估算下降次数
            float avgDecreaseInterval = (minDecreaseInterval + maxDecreaseInterval) / 2f;
            int healthDecrease = (int)(backgroundSeconds / avgDecreaseInterval) * healthDecreaseValue;
            
            if (healthDecrease > 0)
            {
                HealthLevel = Mathf.Max(0, HealthLevel - healthDecrease);
                // Debug.Log($"植物 {PlantName} 后台时间计算完成：健康度-{healthDecrease}");
            }
        }
        
        // 更新时间戳
        lastHealthUpdateTime = System.DateTime.Now;
    }
    
    /// <summary>
    /// 应用离线时间变化（游戏启动时调用）
    /// </summary>
    public void ApplyOfflineTimeChanges(System.DateTime lastHealthUpdate)
    {
        System.DateTime now = System.DateTime.Now;
        
        // 应用离线健康度变化
        if (lastHealthUpdate != System.DateTime.MinValue)
        {
            System.TimeSpan healthOfflineTime = now - lastHealthUpdate;
            double offlineSeconds = healthOfflineTime.TotalSeconds;
            
            if (offlineSeconds > 0)
            {
                // 计算离线期间的健康度变化
                // 使用平均间隔时间来估算下降次数
                float avgDecreaseInterval = (minDecreaseInterval + maxDecreaseInterval) / 2f;
                int healthDecrease = (int)(offlineSeconds / avgDecreaseInterval) * healthDecreaseValue;
                
                if (healthDecrease > 0)
                {
                    int oldHealth = HealthLevel;
                    HealthLevel = Mathf.Max(0, HealthLevel - healthDecrease);
                    //Debug.Log($"[PlantController] 植物 {PlantName} 离线 {offlineSeconds:F0} 秒，健康度: {oldHealth} -> {HealthLevel} (减少 {healthDecrease} 点)");
                }
            }
        }
        
        // 更新时间戳
        lastHealthUpdateTime = now;
    }
    
    /// <summary>
    /// 获取离线时间数据（供存档系统使用）
    /// </summary>
    public System.DateTime GetOfflineTimeData()
    {
        return lastHealthUpdateTime;
    }
    
    #endregion
}