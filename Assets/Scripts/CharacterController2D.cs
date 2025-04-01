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
        // 只有选中的宠物才响应移动指令
        if (isSelected)
        {
            // 检测触摸输入
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0); // 获取第一个触摸点
                
                if (touch.phase == TouchPhase.Began)
                {
                    // 检查是否点击到了UI或其他宠物
                    if (!IsTouchingPet(touch.position))
                    {
                        // 将触摸点从屏幕坐标转换为世界坐标
                        Vector3 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);
                        targetPosition = new Vector2(touchPosition.x, touchPosition.y);
                        animator.SetBool("isMoving", true);
                    }
                }
            }
            
            // 同时支持鼠标点击（在编辑器中测试用）
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击到了UI或其他宠物
                if (!IsTouchingPet(Input.mousePosition))
                {
                    Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    targetPosition = new Vector2(mousePosition.x, mousePosition.y);
                    animator.SetBool("isMoving", true);
                }
            }
        }
        
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
            Vector2 newPosition = rb.position + direction.normalized * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPosition);
        }
        else
        {
            // 已到达目标位置
            animator.SetBool("isMoving", false);
        }
    }
    
    // 判断是否点击到了宠物
    bool IsTouchingPet(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // 如果点击到了宠物，返回true
        return hit.collider != null && hit.collider.GetComponent<CharacterController2D>() != null;
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
}
