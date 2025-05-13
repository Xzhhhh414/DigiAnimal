using UnityEngine;

public class FoodController : MonoBehaviour
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
    
    // 食物图标 - 满盘状态
    [Header("食物图标设置")]
    [SerializeField]
    private Sprite fullFoodIcon;
    
    // 食物图标 - 空盘状态
    [SerializeField]
    private Sprite emptyFoodIcon;
    
    // 食物是否被选中
    [SerializeField]
    public bool isSelected = false;
    
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    // 添加动画控制器引用
    private Animator animator;
    
    // 动画触发器参数名称
    private const string EMPTY_TRIGGER = "emptyTrigger";
    private const string FULL_TRIGGER = "fullTrigger";
    
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
            // 当空盘状态改变时，触发事件通知UI更新
            if (isSelected)
            {
                // 如果食物被选中，触发事件让UI更新
                EventManager.Instance.TriggerEvent(CustomEventType.FoodStatusChanged, this);
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
    
    // 设置食物是否被选中
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 激活/停用描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(selected);
        }
        
        // 如果被选中，通知UI状态变化
        if (selected)
        {
            EventManager.Instance.TriggerEvent(CustomEventType.FoodStatusChanged, this);
        }
    }
    
    // 食物被吃完，变成空盘
    public void SetEmpty()
    {
        IsEmpty = true;
        
        // 触发空盘动画
        if (animator != null)
        {
            animator.SetTrigger(EMPTY_TRIGGER);
        }
    }
    
    // 填满食物
    public void RefillFood()
    {
        IsEmpty = false;
        
        // 触发填满动画
        if (animator != null)
        {
            animator.SetTrigger(FULL_TRIGGER);
        }
    }
} 