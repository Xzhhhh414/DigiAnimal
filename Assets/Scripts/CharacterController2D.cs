using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D rb;
    //[SerializeField] float moveSpeed;
    Vector2 targetPosition;
    Vector2 currentPosition;
    Animator animator;
    
    [Header("宠物基本信息")]
    [SerializeField] private string petDisplayName;     // 宠物显示名称
    [SerializeField] private Sprite petProfileImage;    // 宠物头像图片
    [SerializeField] private string preference;         // 宠物偏好
    
    [SerializeField]
    public bool isSelected = false;
    
    // 动画状态字段
    [SerializeField]
    private bool _IsMoving = false;
    private float _Horizontal = 0f;
    private float _Vertical = 0f;
    
    // 精力值字段 (0-100)
    [SerializeField]
    [Range(0, 100)]
    private int _Energy = 100;
    
    // 饱腹度字段 (0-100)

    
    // 精力值减少的频率 (每N秒)
    [SerializeField]
    private int energyDecreaseInterval = 1;
    // 每个频率下精力值减少的数值 
    [SerializeField]
    private int energyDecreaseValue = 1; 
    // 精力值恢复的速度 (每秒)
    [SerializeField]
    private int energyRecoveryValue = 2;


    [SerializeField]
    [Range(0, 100)]
    private int _Satiety = 100;
    
    // 饱腹度减少的频率 (每N秒)
    [SerializeField]
    private int satietyDecreaseInterval = 2;
    // 每个频率下饱腹度减少的数值
    [SerializeField]
    private int satietyDecreaseValue = 1;
    
    // 是否正在吃食物
    [SerializeField]
    private bool _IsEating = false;
    
    // [SerializeField]    
    // private bool _InFreeMode = true; // 自由活动状态
    
    // 需求气泡相关
    [Header("需求气泡设置")]
    [SerializeField] private NeedBubbleController needBubbleController;
    [SerializeField] [Range(0, 100)] private int hungryThreshold = 5;
    [SerializeField] [Range(0, 100)] private int tiredThreshold = 5;
    private bool isShowingHungryBubble = false;
    private bool isShowingTiredBubble = false;
    
    // 宠物名称属性
    public string PetDisplayName
    {
        get 
        { 
            // 如果没有设置显示名称，则使用游戏对象名称
            if (string.IsNullOrEmpty(petDisplayName))
            {
                string objName = gameObject.name;
                // 去除Clone后缀（如果是实例化的预制体）
                if (objName.EndsWith("(Clone)"))
                {
                    objName = objName.Substring(0, objName.Length - 7);
                }
                return objName;
            }
            return petDisplayName; 
        }
        set { petDisplayName = value; }
    }
    
    // 宠物头像属性
    public Sprite PetProfileImage
    {
        get 
        {
            // 如果没有设置头像，则使用SpriteRenderer中的精灵
            if (petProfileImage == null)
            {
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    return spriteRenderer.sprite;
                }
            }
            return petProfileImage; 
        }
        set { petProfileImage = value; }
    }
    
    // 宠物偏好属性
    public string Preference
    {
        get { return preference; }
        set { preference = value; }
    }
    
    // 动画状态属性
    public bool IsMoving
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsMoving = animator.GetBool(AnimationStrings.isMoving);
            return _IsMoving; 
        }
        private set
        {
            _IsMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }
    
    public float Horizontal
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _Horizontal = animator.GetFloat(AnimationStrings.horizontal);
            return _Horizontal; 
        }
        private set
        {
            _Horizontal = value;
            animator.SetFloat(AnimationStrings.horizontal, value);
        }
    }
    
    public float Vertical
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _Vertical = animator.GetFloat(AnimationStrings.vertical);
            return _Vertical; 
        }
        private set
        {
            _Vertical = value;
            animator.SetFloat(AnimationStrings.vertical, value);
        }
    }
    
    // 精力值属性 (0-100)
    [Tooltip("宠物的精力值 (0-100)，低于一定值时宠物会想要睡觉")]
    public int Energy
    {
        get { return _Energy; }
        set 
        { 
            // 限制精力值在0-100范围内
            _Energy = Mathf.Clamp(value, 0, 100);
        }
    }
    
    // 饱腹度属性 (0-100)
    [Tooltip("宠物的饱腹度 (0-100)，低于一定值时宠物会想要吃东西")]
    public int Satiety
    {
        get { return _Satiety; }
        set
        {
            // 限制饱腹度在0-100范围内
            _Satiety = Mathf.Clamp(value, 0, 100);
        }
    }
    
    // 睡眠状态属性
    [SerializeField]
    [Tooltip("宠物是否处于睡眠状态")]
    public bool IsSleeping
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsSleeping = animator.GetBool(AnimationStrings.isSleeping);
            return _IsSleeping; 
        }
        set
        {
            _IsSleeping = value;
            animator.SetBool(AnimationStrings.isSleeping, value);
        }
    }
    
    // 吃食物状态属性
    [Tooltip("宠物是否正在吃食物")]
    public bool IsEating
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsEating = animator.GetBool(AnimationStrings.isEating);
            return _IsEating; 
        }
        set
        {
            _IsEating = value;
            animator.SetBool(AnimationStrings.isEating, value);
        }
    }
    
    private bool _IsSleeping;
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    private NavMeshAgent agent;
    
    // 计时器，用于控制精力值变化频率
    private float energyTimer = 0f;
    // 计时器，用于控制饱腹度变化频率
    private float satietyTimer = 0f;
    
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // 获取描边管理器
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
        
        agent = GetComponent<NavMeshAgent>();
        
        // 配置为2D模式
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }
    
    void Start()
    {
        currentPosition = rb.position;
        targetPosition = currentPosition;
        
        // 设置NavMeshAgent速度与moveSpeed匹配
        //agent.speed = moveSpeed;
        
        // 确保Rigidbody2D与NavMeshAgent不冲突
        rb.isKinematic = true;
        
        // 初始化时禁用动画移动状态
        IsMoving = false;
        
        // // 默认为自由活动状态
        // InFreeMode = true;
        
        // 确保需求气泡控制器已分配
        if (needBubbleController == null)
        {
            needBubbleController = GetComponentInChildren<NeedBubbleController>();
            if (needBubbleController == null)
            {
                Debug.LogWarning($"宠物 {gameObject.name} 没有分配NeedBubbleController组件，气泡功能将不可用");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 同步Animator中的参数到脚本中的字段
        // if (_InFreeMode != animator.GetBool(AnimationStrings.InFreeMode))
        // {
        //     _InFreeMode = animator.GetBool(AnimationStrings.InFreeMode);
        // }
        
        if (_IsMoving != animator.GetBool(AnimationStrings.isMoving))
        {
            _IsMoving = animator.GetBool(AnimationStrings.isMoving);
        }
        
        if (_Horizontal != animator.GetFloat(AnimationStrings.horizontal))
        {
            _Horizontal = animator.GetFloat(AnimationStrings.horizontal);
        }
        
        if (_Vertical != animator.GetFloat(AnimationStrings.vertical))
        {
            _Vertical = animator.GetFloat(AnimationStrings.vertical);
        }
        
        // 根据NavMeshAgent的速度更新动画
        if (agent.velocity.magnitude > 0.1f)
        {
            IsMoving = true;
            // 使用NavMeshAgent的速度方向更新动画参数
            Horizontal = agent.velocity.normalized.x;
            Vertical = agent.velocity.normalized.y;
        }
        else
        {
            // 当NavMeshAgent停止时
            IsMoving = false;
            Horizontal = 0;
            Vertical = 0;
        }
        
        // 更新目标位置以便与其他系统兼容
        targetPosition = agent.destination;
        
        // 处理精力值和饱腹度变化逻辑
        UpdateEnergy();
        UpdateSatiety();
        
        // 检查并更新需求气泡
        UpdateNeedBubbles();
    }
    
    // 更新精力值
    private void UpdateEnergy()
    {
        // 累计时间
        energyTimer += Time.deltaTime;
        
        if (IsSleeping)
        {
            // 睡眠状态下，每秒恢复精力值
            if (energyTimer >= 1.0f)
            {
                // 增加精力值
                Energy += energyRecoveryValue;
                
                // 重置计时器，减去整数部分，保留小数部分
                energyTimer -= Mathf.Floor(energyTimer);
            }
        }
        else
        {
            // 非睡眠状态下，每N秒减少精力值
            if (energyTimer >= energyDecreaseInterval)
            {
                // 减少精力值
                Energy -= energyDecreaseValue;
                
                // 重置计时器，减去整数部分，保留小数部分
                energyTimer -= Mathf.Floor(energyTimer);
            }
        }
    }
    
    // 更新饱腹度
    private void UpdateSatiety()
    {
        // 累计时间
        satietyTimer += Time.deltaTime;
        
        // 随着时间流逝，饱腹度逐渐降低
        if (satietyTimer >= satietyDecreaseInterval)
        {
            // 减少饱腹度
            Satiety -= satietyDecreaseValue;
            
            // 重置计时器，减去整数部分，保留小数部分
            satietyTimer -= Mathf.Floor(satietyTimer);
        }
    }
    
    // 更新需求气泡显示
    private void UpdateNeedBubbles()
    {
        if (needBubbleController == null) return;
        
        // 如果宠物正在吃东西或睡觉，隐藏所有气泡
        if (IsEating || IsSleeping)
        {
            // 如果当前有气泡显示，隐藏它
            if (isShowingHungryBubble || isShowingTiredBubble)
            {
                needBubbleController.HideAllNeeds();
                isShowingHungryBubble = false;
                isShowingTiredBubble = false;
            }
            return;
        }
        
        // 检查饥饿状态
        if (Satiety <= hungryThreshold && !isShowingHungryBubble && !IsEating)
        {
            // 显示饥饿气泡
            needBubbleController.ShowNeed(PetNeedType.Hungry);
            isShowingHungryBubble = true;
        }
        else if (Satiety > hungryThreshold && isShowingHungryBubble)
        {
            // 如果当前显示的是饥饿气泡，则隐藏它
            if (needBubbleController.GetCurrentNeed() == PetNeedType.Hungry)
            {
                needBubbleController.HideNeed(PetNeedType.Hungry);
            }
            isShowingHungryBubble = false;
        }
        
        // 检查疲劳状态
        if (Energy <= tiredThreshold && !isShowingTiredBubble && !IsSleeping)
        {
            // 显示疲劳气泡
            needBubbleController.ShowNeed(PetNeedType.Tired);
            isShowingTiredBubble = true;
        }
        else if (Energy > tiredThreshold && isShowingTiredBubble)
        {
            // 如果当前显示的是疲劳气泡，则隐藏它
            if (needBubbleController.GetCurrentNeed() == PetNeedType.Tired)
            {
                needBubbleController.HideNeed(PetNeedType.Tired);
            }
            isShowingTiredBubble = false;
        }
    }
    
    // 开始吃食物
    public void Eating(GameObject foodObj)
    {
        if (foodObj == null)
        {
            Debug.LogWarning("食物对象为空，无法开始吃食物");
            return;
        }
        
        FoodController food = foodObj.GetComponent<FoodController>();
        if (food == null)
        {
            Debug.LogWarning("提供的对象不是食物，无法开始吃食物");
            return;
        }

        // 触发吃食物动画
        animator.SetTrigger(AnimationStrings.startEatTrigger);
        // 设置正在吃食物状态
        IsEating = true;
        
        // 隐藏饥饿气泡
        if (needBubbleController != null && isShowingHungryBubble)
        {
            needBubbleController.HideNeed(PetNeedType.Hungry);
            isShowingHungryBubble = false;
        }
        
        // 注意：饱腹度增加逻辑已移至StartEating.cs的OnUpdate方法中
        // 每秒逐步增加饱腹度，而不是一次性增加
    }
    
    // 完成吃食物，实际增加饱腹度
    public void FinishEating()
    {
        // 触发结束吃食物动画
        animator.SetTrigger(AnimationStrings.finishEatTrigger);
        // 重置吃食物状态
        IsEating = false;
    }
    
    void FixedUpdate()
    {

    }

    // 当宠物被选中/取消选中时调用此方法
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 激活/停用描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(selected);
        }
    }
    
    /// <summary>
    /// 触发宠物睡眠动画
    /// </summary>
    public void Sleep()
    {
        // 直接触发睡眠动画
        animator.SetTrigger(AnimationStrings.sleepTrigger);
        IsSleeping = true;

        // 隐藏疲劳气泡
        if (needBubbleController != null && isShowingTiredBubble)
        {
            needBubbleController.HideNeed(PetNeedType.Tired);
            isShowingTiredBubble = false;
        }
        
        //Debug.Log($"{gameObject.name} 触发睡眠动画，开始恢复精力");
    }
    
    /// <summary>
    /// 触发宠物起床动画
    /// </summary>
    public void GetUp()
    {
        // 直接触发起床动画
        animator.SetTrigger(AnimationStrings.getUpTrigger);
        IsSleeping = false;  
        //Debug.Log($"{gameObject.name} 触发起床动画");
    }
}
