using UnityEngine;

public class FoodController : MonoBehaviour, ISelectableFurniture, ISpawnableFurniture
{
    // 食物是否正在被使用
    [SerializeField]
    private bool _isUsing = false;
    
    // 食物是否已空盘
    [SerializeField]
    private bool _isEmpty = false;
    
    // 食物的美味度(1-5)
    [SerializeField]
    [Range(1, 5)]
    private int _tasty = 3;
    
    // 食物恢复的饱腹度数值
    [SerializeField]
    private int satietyRecoveryValue = 25;
    
    // 添加食物所需的爱心消耗
    [Header("经济设置")]
    [SerializeField]
    private int refillHeartCost = 5; // 添加食物需要消耗的爱心数量
    
    // 食物图标 - 满盘状态
    [Header("食物图标设置")]
    [SerializeField]
    private Sprite fullFoodIcon;
    
    // 食物图标 - 空盘状态
    [SerializeField]
    private Sprite emptyFoodIcon;
    
    // 食物名称
    [Header("食物基本信息")]
    [SerializeField]
    private string foodName = "猫粮";  // 食物名称
    
    // 食物是否被选中
    [SerializeField]
    public bool isSelected = false;
    
    // 食物唯一标识符（用于存档）
    [Header("存档设置")]
    [SerializeField]
    private string foodId = "";
    
    // 默认家具标识符（如果是默认创建的家具）
    [SerializeField]
    private string saveDataId = "";
    
    // 家具配置ID（来自FurnitureDatabase）
    [SerializeField]
    private string configId = "";
    
    // 是否正在加载存档数据（用于避免在加载时触发自动保存）
    private bool isLoadingFromSave = false;
    
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    // 添加动画控制器引用
    private Animator animator;
    
    // 动画参数名称
    private const string IS_EMPTY = "isEmpty";
    
    // 公开的属性，用于获取和设置食物的使用状态
    public bool IsUsing
    {
        get { return _isUsing; }
        set { _isUsing = value; }
    }
    
    // 空盘状态属性
    public bool IsEmpty
    {
        get { return _isEmpty; }
        set 
        { 
            _isEmpty = value;
            
            // 更新动画状态机参数
            if (animator != null)
            {
                animator.SetBool(IS_EMPTY, _isEmpty);
            }
            
            // 当空盘状态改变时，触发事件通知UI更新
            if (isSelected)
            {
                // 如果食物被选中，触发事件让UI更新
                EventManager.Instance.TriggerEvent(CustomEventType.FoodStatusChanged, this);
            }
            
            // 通知GameDataManager食物状态发生变化，触发自动保存（但在加载存档时不触发）
            if (!isLoadingFromSave && GameDataManager.Instance != null)
            {
                // Debug.Log($"[FoodController] 食物 {gameObject.name} 状态改变为 isEmpty={_isEmpty}，通知GameDataManager");
                GameDataManager.Instance.OnFoodDataChanged();
            }
            else if (isLoadingFromSave)
            {
                // Debug.Log($"[FoodController] 食物 {gameObject.name} 正在加载存档，跳过自动保存");
            }
            
            // 更新精灵渲染器的图片
            UpdateSpriteRendererImage();
        }
    }
    
    // 美味度属性
    public int Tasty
    {
        get { return _tasty; }
        set { _tasty = Mathf.Clamp(value, 1, 5); }
    }
    
    // 获取饱腹度恢复值
    public int SatietyRecoveryValue => satietyRecoveryValue;
    
    // 获取添加食物的爱心消耗
    public int RefillHeartCost => refillHeartCost;
    
    // 食物名称属性
    public string FoodName
    {
        get { return foodName; }
        set { foodName = value; }
    }
    
    // 食物唯一标识符属性
    public string FoodId
    {
        get 
        { 
            // 如果没有设置ID，自动生成一个基于位置和名称的ID
            if (string.IsNullOrEmpty(foodId))
            {
                foodId = GenerateFoodId();
            }
            return foodId; 
        }
        set { foodId = value; }
    }
    
    // 食物图标属性 - 根据当前状态返回对应图标
    public Sprite FoodIcon
    {
        get 
        {
            // 根据当前状态返回对应的图标
            Sprite currentIcon = _isEmpty ? emptyFoodIcon : fullFoodIcon;
            
            // 如果没有设置对应状态的图标，尝试使用SpriteRenderer中的精灵
            if (currentIcon == null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sprite;
                }
            }
            return currentIcon; 
        }
    }
    
    private void Awake()
    {
        // 获取描边管理器
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
        
        // 获取动画控制器
        animator = GetComponent<Animator>();
        
        // 初始化动画参数
        if (animator != null)
        {
            animator.SetBool(IS_EMPTY, _isEmpty);
        }
        
        // 初始化时更新精灵渲染器
        UpdateSpriteRendererImage();
    }
    
    // 更新精灵渲染器中的图像，与当前状态保持一致
    private void UpdateSpriteRendererImage()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Sprite iconToUse = _isEmpty ? emptyFoodIcon : fullFoodIcon;
            
            // 只有在图标存在的情况下才更新
            if (iconToUse != null)
            {
                spriteRenderer.sprite = iconToUse;
            }
        }
    }
    
    // 旧的SetSelected方法已移除，现在使用ISelectableFurniture接口的OnSelected/OnDeselected
    
    // 食物被吃完，变成空盘
    public void SetEmpty()
    {
        IsEmpty = true;
        // 动画参数已在IsEmpty的setter中设置
    }
    
    // 填满食物
    public void RefillFood()
    {
        IsEmpty = false;
        _isUsing = false;
        // 动画参数已在IsEmpty的setter中设置
    }
    
    /// <summary>
    /// 生成食物唯一标识符
    /// </summary>
    private string GenerateFoodId()
    {
        // 基于游戏对象名称和位置生成唯一ID
        Vector3 pos = transform.position;
        string name = gameObject.name.Replace("(Clone)", "");
        return $"{name}_{pos.x:F2}_{pos.y:F2}";
    }
    
    /// <summary>
    /// 获取食物存档数据
    /// </summary>
    public FoodSaveData GetSaveData()
    {
        return new FoodSaveData(
            FoodId,
            gameObject.name.Replace("(Clone)", ""),
            configId,
            saveDataId,  // 添加 saveDataId 参数
            IsEmpty,
            transform.position,
            Tasty,
            SatietyRecoveryValue
        );
    }
    
    /// <summary>
    /// 从存档数据加载食物状态
    /// </summary>
    public void LoadFromSaveData(FoodSaveData saveData)
    {
        if (saveData == null) return;
        
        // Debug.Log($"[FoodController] {gameObject.name} 开始加载存档数据: isEmpty={saveData.isEmpty}");
        
        // 设置加载标志，避免在加载过程中触发自动保存
        isLoadingFromSave = true;
        
        try
        {
            foodId = saveData.foodId;
            configId = saveData.configId;
            saveDataId = saveData.saveDataId;  // 加载 saveDataId
            
            // 设置位置
            transform.position = saveData.position;
            
            // Debug.Log($"[FoodController] {gameObject.name} 设置 IsEmpty 从 {_isEmpty} 到 {saveData.isEmpty}");
            IsEmpty = saveData.isEmpty; // 这会自动更新动画参数和视觉状态
            
            // 对于默认家具，首次创建时保持prefab中的Inspector配置
            // 只有非默认家具或已存在的家具才使用存档数据
            bool isDefaultFurniture = !string.IsNullOrEmpty(saveData.saveDataId);
            bool shouldPreserveInspectorValues = isDefaultFurniture && (saveData.tasty == 3 && saveData.satietyRecoveryValue == 25);
            
            if (!shouldPreserveInspectorValues)
            {
                // 使用存档中的值
                Tasty = saveData.tasty;
                satietyRecoveryValue = saveData.satietyRecoveryValue;
            }
            else
            {
                //Debug.Log($"[FoodController] {gameObject.name} 是默认家具，保持Inspector配置: Tasty={Tasty}, SatietyRecovery={satietyRecoveryValue}");
            }
            // Debug.Log($"[FoodController] {gameObject.name} 加载完成，最终状态: isEmpty={_isEmpty}");
        }
        finally
        {
            // 确保无论如何都会清除加载标志
            isLoadingFromSave = false;
        }
    }
    
    // ===== ISelectableFurniture 接口实现 =====
    
    public string FurnitureType => "Food";
    
    public string FurnitureName => FoodName;
    
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
        
        // 通知UI状态变化
        EventManager.Instance.TriggerEvent(CustomEventType.FoodStatusChanged, this);
    }
    
    public void OnDeselected()
    {
        isSelected = false;
        
        // 停用描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(false);
        }
    }
    
    public Sprite GetIcon()
    {
        return FoodIcon;
    }
    
    // ===== ISpawnableFurniture 接口实现 =====
    
    public string FurnitureId 
    { 
        get => foodId; 
        set => foodId = value; 
    }
    
    public FurnitureType SpawnableFurnitureType => global::FurnitureType.Food;
    
    string ISpawnableFurniture.FurnitureName 
    { 
        get => FoodName; 
        set => foodName = value; 
    }
    
    public Vector3 Position 
    { 
        get => transform.position; 
        set => transform.position = value; 
    }
    
    GameObject ISpawnableFurniture.GameObject => gameObject;
    
    public void InitializeFromSaveData(object saveData)
    {
        if (saveData is FoodSaveData foodSaveData)
        {
            LoadFromSaveData(foodSaveData);
        }
    }
    
    object ISpawnableFurniture.GetSaveData()
    {
        return GetSaveData();
    }
    
    public void GenerateFurnitureId()
    {
        // 这个方法现在会由FurnitureSpawner调用，不需要自己生成ID
        // 保留原有的GenerateFoodId逻辑作为备用
        if (string.IsNullOrEmpty(foodId))
        {
            foodId = GenerateFoodId();
        }
    }
} 