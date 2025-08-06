using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NavMeshAgent))]
public class PetController2D : MonoBehaviour
{
    Rigidbody2D rb;
    //[SerializeField] float moveSpeed;
    Vector2 targetPosition;
    Vector2 currentPosition;
    Animator animator;
    
    [Header("宠物基本信息")]
    [SerializeField] private string petDisplayName;     // 宠物显示名称
    [SerializeField] private Sprite petProfileImage;    // 宠物头像图片
    [SerializeField] private string petIntroduction;         // 宠物介绍
    
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

    // 是否正在睡觉
    [SerializeField]
    private bool _IsSleeping = false;  
    
    // 是否正在吃食物
    [SerializeField]
    private bool _IsEating = false;
    
    // 是否正在被摸摸
    [SerializeField]
    private bool _IsPatting = false;
    
    // 是否被逗猫棒吸引
    [SerializeField]
    private bool _IsAttracted = false;
    
    // 是否正在与逗猫棒互动
    [SerializeField]
    private bool _IsCatTeasering = false;
    
    // [SerializeField]    
    // private bool _InFreeMode = true; // 自由活动状态
    
    // 需求气泡相关
    [Header("需求气泡设置")]
    [SerializeField] private NeedBubbleController needBubbleController;
    [SerializeField] [Range(0, 100)] private int hungryThreshold = 5;
    [SerializeField] [Range(0, 100)] private int tiredThreshold = 5;
    private bool isShowingHungryBubble = false;
    private bool isShowingTiredBubble = false;
    
    // 玩具互动系统
    
    [Header("厌倦状态设置")]
    [SerializeField] [Range(0f, 1f)] private float boredomChance = 0.3f; // 进入厌倦状态的几率 (0-1)
    [SerializeField] private float boredomRecoveryMinutes = 2f; // 厌倦状态恢复时间（分钟）
    private float lastBoredomTime = -1f; // 上次进入厌倦状态的时间
    [SerializeField] private bool isBored = false; // 当前是否处于厌倦状态
    
    // 存档系统相关
    [Header("存档设置")]
    public string petId = ""; // 宠物唯一ID，由存档系统设置
    
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
        set 
        { 
            string oldValue = petDisplayName;
            petDisplayName = value;
            
            // 如果值发生变化，通知GameDataManager（用于更新系统设置面板等UI）
            if (oldValue != petDisplayName && GameDataManager.Instance != null)
            {
                GameDataManager.Instance.OnPetDataChanged();
            }
        }
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
    
    // 宠物介绍
    public string PetIntroduction
    {
        get { return petIntroduction; }
        set { petIntroduction = value; }
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
            int oldValue = _Energy;
            // 限制精力值在0-100范围内
            _Energy = Mathf.Clamp(value, 0, 100);
            
            // 如果值发生变化，通知存档系统
            if (oldValue != _Energy && GameDataManager.Instance != null)
            {
                GameDataManager.Instance.OnPetDataChanged();
            }
        }
    }
    
    // 饱腹度属性 (0-100)
    [Tooltip("宠物的饱腹度 (0-100)，低于一定值时宠物会想要吃东西")]
    public int Satiety
    {
        get { return _Satiety; }
        set
        {
            int oldValue = _Satiety;
            // 限制饱腹度在0-100范围内
            _Satiety = Mathf.Clamp(value, 0, 100);
            
            // 如果值发生变化，通知存档系统
            if (oldValue != _Satiety && GameDataManager.Instance != null)
            {
                GameDataManager.Instance.OnPetDataChanged();
            }
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
    
    // 摸摸互动状态属性
    [Tooltip("宠物是否正在被摸摸")]
    public bool IsPatting
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsPatting = animator.GetBool(AnimationStrings.isPatting);
            return _IsPatting; 
        }
        set
        {
            _IsPatting = value;
            animator.SetBool(AnimationStrings.isPatting, value);
        }
    }
    
    // 被逗猫棒吸引状态属性
    [Tooltip("宠物是否被逗猫棒吸引")]
    public bool IsAttracted
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsAttracted = animator.GetBool(AnimationStrings.isAttracted);
            return _IsAttracted; 
        }
        set
        {
            _IsAttracted = value;
            animator.SetBool(AnimationStrings.isAttracted, value);
        }
    }
    
    // 逗猫棒互动状态属性
    [Tooltip("宠物是否正在与逗猫棒互动")]
    public bool IsCatTeasering
    {
        get 
        { 
            // 从Animator获取最新值，确保同步
            _IsCatTeasering = animator.GetBool(AnimationStrings.isCatTeasering);
            return _IsCatTeasering; 
        }
        set
        {
            _IsCatTeasering = value;
            animator.SetBool(AnimationStrings.isCatTeasering, value);
        }
    }
    
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
        // 同步currentPosition和targetPosition到当前实际位置
        // 这样不会覆盖由存档系统设置的位置
        currentPosition = transform.position;
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
        
        // 启动延迟初始化协程，确保状态同步
        StartCoroutine(DelayedStateCheck());
    }
    
    // 延迟检查状态的协程
    private IEnumerator DelayedStateCheck()
    {
        // 等待一小段时间，确保所有组件都初始化完成
        yield return new WaitForSeconds(0.5f);
        
        // 强制更新一次需求状态
        UpdateNeedBubbles();
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
        
        // 如果宠物正在吃东西、睡觉、被摸摸、被吸引或与逗猫棒互动，隐藏所有气泡
        if (IsEating || IsSleeping || IsPatting || IsAttracted || IsCatTeasering)
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
        if (Satiety <= hungryThreshold && !IsEating)
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
        if (Energy <= tiredThreshold && !IsSleeping)
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
    
    /// <summary>
    /// 获取是否处于厌倦状态
    /// </summary>
    public bool IsBored
    {
        get
        {
            // 检查厌倦状态是否已恢复
            if (isBored && lastBoredomTime >= 0)
            {
                float boredomRecoverySeconds = boredomRecoveryMinutes * 60f;
                if (Time.time - lastBoredomTime >= boredomRecoverySeconds)
                {
                    isBored = false;
                    // Debug.Log($"宠物 {PetDisplayName} 从厌倦状态中恢复了");
                }
            }
            return isBored;
        }
    }
    
    /// <summary>
    /// 获取厌倦状态剩余时间（分钟）
    /// </summary>
    public float BoredomRecoveryRemaining
    {
        get
        {
            if (!isBored || lastBoredomTime < 0)
                return 0f;
                
            float boredomRecoverySeconds = boredomRecoveryMinutes * 60f;
            float remaining = boredomRecoverySeconds - (Time.time - lastBoredomTime);
            return Mathf.Max(0f, remaining / 60f); // 转换为分钟
        }
    }
    
    /// <summary>
    /// 获取/设置上次厌倦时间（供存档系统使用）
    /// </summary>
    public float LastBoredomTime
    {
        get { return lastBoredomTime; }
        set { lastBoredomTime = value; }
    }
    
    /// <summary>
    /// 获取厌倦几率（供外部系统使用）
    /// </summary>
    public float BoredomChance
    {
        get { return boredomChance; }
    }
    
    /// <summary>
    /// 设置厌倦状态（供外部系统使用）
    /// </summary>
    /// <param name="bored">是否厌倦</param>
    public void SetBored(bool bored)
    {
        isBored = bored;
        if (bored)
        {
            lastBoredomTime = Time.time;
        }
    }
    
    // 玩具互动相关属性和方法
    
    /// <summary>
    /// 获取是否可以进行玩具互动（只检查厌倦状态）
    /// </summary>
    public bool CanInteractWithToy
    {
        get
        {
            // 如果处于厌倦状态，则不能互动
            return !IsBored;
        }
    }
    
    /// <summary>
    /// 尝试与玩具互动
    /// </summary>
    /// <param name="heartReward">获得的爱心奖励数量</param>
    /// <returns>互动结果：0=成功，1=厌倦状态，2=正在睡觉，3=正在吃饭，4=正在被摸摸</returns>
    public int TryToyInteraction(float heartReward)
    {
        // 检查是否正在睡觉
        if (IsSleeping)
        {
            return 2; // 正在睡觉
        }
        
        // 检查是否正在吃饭
        if (IsEating)
        {
            return 3; // 正在吃饭
        }
        
        // 检查是否正在被摸摸
        if (IsPatting)
        {
            return 4; // 正在被摸摸
        }
        
        // 检查是否被逗猫棒吸引
        if (IsAttracted)
        {
            return 5; // 被逗猫棒吸引
        }
        
        // 检查是否正在与逗猫棒互动
        if (IsCatTeasering)
        {
            return 6; // 正在与逗猫棒互动
        }
        
        // 检查是否可以进行玩具互动（厌倦状态检查）
        if (!CanInteractWithToy)
        {
            return 1; // 厌倦状态
        }
        
        // 注意：爱心奖励不在这里发放，而是在通用的互动结束阶段统一发放
        
        // 检查是否进入厌倦状态
        bool willBeBored = Random.Range(0f, 1f) < boredomChance;
        
        if (willBeBored)
        {
            // 进入厌倦状态
            isBored = true;
            lastBoredomTime = Time.time;
            // Debug.Log($"宠物 {PetDisplayName} 对玩具感到厌倦了，需要 {boredomRecoveryMinutes} 分钟恢复");
        }
        else
        {
            // Debug.Log($"宠物 {PetDisplayName} 很开心地玩着玩具");
        }
        
        return 0; // 成功
    }
    
    /// <summary>
    /// 启动直接互动类型的玩具互动（仅限DirectInteraction类型工具）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    public void StartToyInteraction(string toolName)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            Debug.LogWarning("工具名称为空，无法启动互动");
            return;
        }
        
        // 根据直接互动工具类型设置对应的状态，让BT接管后续流程
        switch (toolName)
        {
            case "摸摸":
                IsPatting = true;
                // Debug.Log($"{gameObject.name} 设置摸摸状态，等待BT接管");
                break;
            // 注意：逗猫棒、玩具老鼠等PlaceableObject类型工具不会走到这里
            // 它们通过放置到场景中，然后宠物通过BT主动检测和互动
            default:
                Debug.LogWarning($"未知的直接互动工具类型: {toolName}");
                break;
        }
    }
    
    /// <summary>
    /// 开始摸摸互动（只负责动画触发，由BT调用）
    /// </summary>
    public void StartPatting()
    {
        // 触发开始摸摸动画
        animator.SetTrigger(AnimationStrings.startPatTrigger);
        
        // Debug.Log($"{gameObject.name} 触发开始摸摸动画");
    }
    
    /// <summary>
    /// 结束摸摸互动（只负责动画触发，由BT调用）
    /// </summary>
    public void EndPatting()
    {
        // 触发结束摸摸动画
        animator.SetTrigger(AnimationStrings.endPatTrigger);
        
        // Debug.Log($"{gameObject.name} 触发结束摸摸动画");
    }

      public void StartCatTeaser()
    {
        // 触发开始摸摸动画
        animator.SetTrigger(AnimationStrings.startCatTeaserTrigger);
        
        // Debug.Log($"{gameObject.name} 触发开始摸摸动画");
    }
    
    /// <summary>
    /// 结束摸摸互动（只负责动画触发，由BT调用）
    /// </summary>
    public void EndCatTeaser()
    {
        // 触发结束摸摸动画
        animator.SetTrigger(AnimationStrings.endCatTeaserTrigger);
        
        // Debug.Log($"{gameObject.name} 触发结束摸摸动画");
    }

    /// <summary>
    /// 显示情感气泡（供外部调用）
    /// </summary>
    /// <param name="emotionType">情感类型</param>
    public void ShowEmotionBubble(PetNeedType emotionType)
    {
        if (needBubbleController != null)
        {
            needBubbleController.ShowNeed(emotionType);
        }
    }
    
    /// <summary>
    /// 隐藏情感气泡（供外部调用）
    /// </summary>
    /// <param name="emotionType">情感类型</param>
    public void HideEmotionBubble(PetNeedType emotionType)
    {
        if (needBubbleController != null)
        {
            needBubbleController.HideNeed(emotionType);
        }
    }
    
    /// <summary>
    /// 销毁时的清理工作
    /// </summary>
    private void OnDestroy()
    {
        // 安全地停止NavMeshAgent
        if (agent != null && agent.gameObject != null && agent.gameObject.activeSelf && 
            agent.enabled && agent.isOnNavMesh)
        {
            try
            {
                agent.ResetPath();
                agent.enabled = false;
            }
            catch
            {
                // 忽略销毁时的NavMeshAgent错误
            }
        }
    }
} 