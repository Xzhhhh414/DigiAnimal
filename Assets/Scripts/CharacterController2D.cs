using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NavMeshAgent))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D rb;
    [SerializeField] float moveSpeed;
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
    
    [SerializeField]    
    private bool _IsFree = true; // 自由活动状态
    
    // 动画状态属性
    public bool IsMoving
    {
        get { return _IsMoving; }
        private set
        {
            _IsMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);
        }
    }
    
    public float Horizontal
    {
        get { return _Horizontal; }
        private set
        {
            _Horizontal = value;
            animator.SetFloat(AnimationStrings.horizontal, value);
        }
    }
    
    public float Vertical
    {
        get { return _Vertical; }
        private set
        {
            _Vertical = value;
            animator.SetFloat(AnimationStrings.vertical, value);
        }
    }
    
    // 自由活动状态属性
    [SerializeField]
    [Tooltip("宠物是否处于自由活动状态，处于此状态时会由AI控制行为")]
    public bool IsFree
    {
        get { return _IsFree; }
        set
        {
            _IsFree = value;
            animator.SetBool(AnimationStrings.isFree, value);
        }
    }
    
    // 像素描边管理器
    private PixelOutlineManager pixelOutlineManager;
    
    private NavMeshAgent agent;
    
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
        agent.speed = moveSpeed;
        
        // 确保Rigidbody2D与NavMeshAgent不冲突
        rb.isKinematic = true;
        
        // 初始化时禁用动画移动状态
        IsMoving = false;
        
        // 默认为自由活动状态
        IsFree = true;
    }

    // Update is called once per frame
    void Update()
    {
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
    
    // 修改MoveTo方法兼容NavMeshAgent
    public void MoveTo(Vector2 position)
    {
        // 设置为非自由活动状态
        IsFree = false;
        
        targetPosition = position;
        agent.SetDestination(new Vector3(position.x, position.y, transform.position.z));
        
        // 确保NavMeshAgent被激活
        if (!agent.enabled)
            agent.enabled = true;
    }
    
    // 提供方法让AI可以控制宠物移动
    public void AIMoveTo(Vector2 position)
    {
        // 仅当处于自由活动状态时，AI才能控制移动
        if (IsFree)
        {
            agent.SetDestination(new Vector3(position.x, position.y, transform.position.z));
        }
    }
}
