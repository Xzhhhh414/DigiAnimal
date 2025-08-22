using UnityEngine;
using System.Collections;

public class PlantController : MonoBehaviour
{
    // 植物健康度 (0-100)
    [SerializeField]
    [Range(0, 100)]
    private int _healthLevel = 100;
    
    // 植物类型名称
    [SerializeField]
    private string plantName = "盆栽植物";
    
    // 每次浇水恢复的健康度
    [SerializeField]
    private int healthRecoveryValue = 25;
    
    // 植物外观通过Animator控制，不需要手动配置图片
    
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
    
    // 动画控制器
    private Animator animator;
    
    // SpriteRenderer组件
    private SpriteRenderer spriteRenderer;
    
    // 健康度下降协程
    private Coroutine healthDecreaseCoroutine;
    
    // 动画参数常量
    private const string IS_HEALTHY = "isHealthy";
    private const string IS_THIRSTY = "isThirsty";
    private const string IS_WITHERED = "isWithered";
    
    // 植物状态枚举
    public enum PlantState
    {
        Healthy,   // 健康 (80-100)
        Thirsty,   // 缺水 (30-79)
        Withered   // 枯萎 (0-29)
    }
    
    void Awake()
    {
        // 获取组件
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
        animator = GetComponent<Animator>();
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
    /// 浇水操作
    /// </summary>
    /// <returns>是否浇水成功</returns>
    public bool TryWatering()
    {
        // 恢复健康度
        HealthLevel += healthRecoveryValue;
        
        // Debug.Log($"植物 {PlantName} 浇水成功，健康度恢复 {healthRecoveryValue} 点");
        
        return true;
    }
    
    /// <summary>
    /// 更新植物外观
    /// </summary>
    private void UpdatePlantAppearance()
    {
        // 植物外观完全由Animator控制，通过三个bool参数控制状态
        if (animator != null)
        {
            PlantState currentState = CurrentState;
            
            // 重置所有状态为false
            animator.SetBool(IS_HEALTHY, false);
            animator.SetBool(IS_THIRSTY, false);
            animator.SetBool(IS_WITHERED, false);
            
            // 根据当前状态设置对应的bool为true
            switch (currentState)
            {
                case PlantState.Healthy:
                    animator.SetBool(IS_HEALTHY, true);
                    break;
                case PlantState.Thirsty:
                    animator.SetBool(IS_THIRSTY, true);
                    break;
                case PlantState.Withered:
                    animator.SetBool(IS_WITHERED, true);
                    break;
            }
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
    
    /// <summary>
    /// 处理点击事件
    /// </summary>
    void OnMouseDown()
    {
        // 检查是否点击在UI上
        if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
        {
            return;
        }
        
        // 选中植物
        SelectPlant();
    }
    
    /// <summary>
    /// 选中植物
    /// </summary>
    public void SelectPlant()
    {
        // 取消其他植物的选中状态
        PlantController[] allPlants = FindObjectsOfType<PlantController>();
        foreach (PlantController plant in allPlants)
        {
            plant.DeselectPlant();
        }
        
        // 选中当前植物
        isSelected = true;
        
        // 显示描边
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(true);
        }
        
        // 显示植物信息面板
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenPlantInfo(this);
        }
        
        // Debug.Log($"选中植物: {PlantName}");
    }
    
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
}