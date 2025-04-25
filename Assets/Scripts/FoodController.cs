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
    
    // 食物图标
    [SerializeField]
    private Sprite foodIcon;
    
    // 食物是否被选中
    [SerializeField]
    public bool isSelected = false;
    
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
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
            if (_isEmpty && isSelected)
            {
                // 如果食物被选中且变为空盘，触发事件让UI更新
                EventManager.Instance.TriggerEvent(CustomEventType.FoodStatusChanged, this);
            }
            
            // 更新食物外观（如果有不同的空盘精灵图）
            UpdateFoodVisuals();
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
    
    // 食物图标属性
    public Sprite FoodIcon
    {
        get 
        {
            // 如果没有设置图标，则使用SpriteRenderer中的精灵
            if (foodIcon == null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sprite;
                }
            }
            return foodIcon; 
        }
        set { foodIcon = value; }
    }
    
    private void Awake()
    {
        // 获取描边管理器
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
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
    
    // 更新食物视觉效果
    private void UpdateFoodVisuals()
    {
        // 获取SpriteRenderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 这里可以根据是否空盘切换不同的精灵图
            // 例如：spriteRenderer.sprite = _isEmpty ? emptyFoodSprite : fullFoodSprite;
            
            // 或者调整颜色，淡化已空盘的食物
            if (_isEmpty)
            {
                spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 灰色
            }
            else
            {
                spriteRenderer.color = Color.white; // 正常颜色
            }
        }
    }
    
} 