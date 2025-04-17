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
    
    // 精力值减少的频率 (每N秒)
    [SerializeField]
    private int energyDecreaseInterval = 1;
    // 每个频率下精力值减少的数值 
    [SerializeField]
    private int energyDecreaseValue = 1; 
    // 精力值恢复的速度 (每秒)
    [SerializeField]
    private int energyRecoveryValue = 2;
    
    // [SerializeField]    
    // private bool _InFreeMode = true; // 自由活动状态
    
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
    
    private bool _IsSleeping;
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    private NavMeshAgent agent;
    
    // 计时器，用于控制精力值变化频率
    private float energyTimer = 0f;
    
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
        
        // 处理精力值变化逻辑
        UpdateEnergy();
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
        Debug.Log($"{gameObject.name} 触发睡眠动画，开始恢复精力");
    }
    
    /// <summary>
    /// 触发宠物起床动画
    /// </summary>
    public void GetUp()
    {
        // 直接触发起床动画
        animator.SetTrigger(AnimationStrings.getUpTrigger);
        IsSleeping = false;  
        Debug.Log($"{gameObject.name} 触发起床动画");
    }
}
