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
    
    // 是否处于自由活动状态（由AI控制）
    [SerializeField]
    [Tooltip("宠物是否处于自由活动状态，处于此状态时会由AI控制行为")]
    public bool isFreeRoaming = true;
    
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
        animator.SetBool("isMoving", false);
        
        // 默认为自由活动状态
        isFreeRoaming = true;
    }

    // Update is called once per frame
    void Update()
    {
        // 根据NavMeshAgent的速度更新动画
        if (agent.velocity.magnitude > 0.1f)
        {
            animator.SetBool("isMoving", true);
            // 使用NavMeshAgent的速度方向更新动画参数
            animator.SetFloat("horizontal", agent.velocity.normalized.x);
            animator.SetFloat("vertical", agent.velocity.normalized.y);
        }
        else
        {
            // 当NavMeshAgent停止时
            animator.SetBool("isMoving", false);
            animator.SetFloat("horizontal", 0);
            animator.SetFloat("vertical", 0);
            
            // 如果宠物停止移动且之前是由玩家指定移动的（非自由活动状态），恢复自由活动状态
            if (!isFreeRoaming && !agent.pathPending && agent.remainingDistance < 0.1f)
            {
                isFreeRoaming = true;
            }
        }
        
        // 更新目标位置以便与其他系统兼容
        targetPosition = agent.destination;
    }

    void FixedUpdate()
    {
        // NavMeshAgent现在处理移动，不需要额外调用Move()
    }

    // 保留Move方法用于向后兼容或非导航移动
    void Move()
    {
        // 此方法保留但不再被FixedUpdate调用
        // NavMeshAgent现在控制移动
    }
    
    // 移除IsTouchingPet方法，不再需要
    
    // 当宠物被选中/取消选中时调用此方法
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 激活/停用描边效果
        if (pixelOutlineManager != null)
        {
            pixelOutlineManager.SetOutlineActive(selected);
        }
        
        // 如果取消选中，且宠物不在移动，恢复自由活动状态
        if (!selected && !animator.GetBool("isMoving"))
        {
            isFreeRoaming = true;
        }
    }
    
    // 修改MoveTo方法兼容NavMeshAgent
    public void MoveTo(Vector2 position)
    {
        // 设置为非自由活动状态
        isFreeRoaming = false;
        
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
        if (isFreeRoaming)
        {
            agent.SetDestination(new Vector3(position.x, position.y, transform.position.z));
        }
    }
    
    // 获取宠物是否处于自由活动状态
    public bool GetIsFreeRoaming()
    {
        return isFreeRoaming;
    }
    
    // 手动设置自由活动状态（用于外部控制）
    public void SetFreeRoaming(bool state)
    {
        isFreeRoaming = state;
    }
}
