using UnityEngine;
using System.Collections;

public class PlantController : MonoBehaviour, ISelectableFurniture
{
    // 植物健康度 (0-100)
    [SerializeField]
    [Range(0, 100)]
    private int _healthLevel = 100;
    
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
        
        // 生成植物ID
        if (string.IsNullOrEmpty(plantId))
        {
            GeneratePlantId();
        }
    }
    
    void Start()
    {
        // 初始化植物外观
        UpdatePlantAppearance();
        
        // 开始健康度下降协程
        StartHealthDecreaseCoroutine();
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
                
                // Debug.Log($"植物 {PlantName} 健康度变化: {oldValue} -> {_healthLevel}");
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
    }
    
    /// <summary>
    /// 获取存档数据
    /// </summary>
    public PlantSaveData GetSaveData()
    {
        return new PlantSaveData
        {
            plantId = this.plantId,
            plantName = this.plantName,
            healthLevel = this._healthLevel,
            position = transform.position,
            wateringHeartCost = 0, // 浇水免费，但保持存档兼容性
            healthRecoveryValue = this.healthRecoveryValue
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
            
            plantId = saveData.plantId;
            plantName = saveData.plantName;
            HealthLevel = saveData.healthLevel; // 这会触发外观更新
            // wateringHeartCost 已移除，浇水免费
            healthRecoveryValue = saveData.healthRecoveryValue;
            
            // 设置位置
            transform.position = saveData.position;
            
            // Debug.Log($"植物 {PlantName} 加载存档数据完成，健康度: {_healthLevel}");
        }
        finally
        {
            isLoadingFromSave = false;
        }
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
}