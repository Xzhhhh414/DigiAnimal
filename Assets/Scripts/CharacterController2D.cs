using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
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
    
    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // 获取描边管理器
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
    }
    
    void Start()
    {
        currentPosition = rb.position;
        targetPosition = currentPosition;
    }

    // Update is called once per frame
    void Update()
    {
        // 计算移动方向用于动画
        if (animator.GetBool("isMoving"))
        {
            Vector2 direction = (targetPosition - rb.position).normalized;
            animator.SetFloat("horizontal", direction.x);
            animator.SetFloat("vertical", direction.y);
        }
        else
        {
            animator.SetFloat("horizontal", 0);
            animator.SetFloat("vertical", 0);
        }
    }

    void FixedUpdate()
    {
        if (animator.GetBool("isMoving"))
        {
            Move();
        }
    }

    void Move()
    {
        // 计算当前位置到目标位置的方向和距离
        Vector2 direction = targetPosition - rb.position;
        float distance = direction.magnitude;
        
        if (distance > 0.1f) // 如果距离大于阈值，继续移动
        {
            // 设置速度而不是直接位置，这样会尊重碰撞体
            rb.velocity = direction.normalized * moveSpeed;
        }
        else
        {
            // 已到达目标位置或无法继续前进，停止移动
            rb.velocity = Vector2.zero;
            animator.SetBool("isMoving", false);
        }
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
    
    // 添加供AI系统使用的方法，控制宠物移动到指定位置
    public void MoveTo(Vector2 position)
    {
        targetPosition = position;
        animator.SetBool("isMoving", true);
    }
}
