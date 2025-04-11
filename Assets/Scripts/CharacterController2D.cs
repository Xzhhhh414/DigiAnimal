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
    
    [HideInInspector]
    public bool isSelected = false;
    
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
    }
    
    // 修改MoveTo方法兼容NavMeshAgent
    public void MoveTo(Vector2 position)
    {
        targetPosition = position;
        agent.SetDestination(new Vector3(position.x, position.y, transform.position.z));
        
        // 确保NavMeshAgent被激活
        if (!agent.enabled)
            agent.enabled = true;
    }
}
