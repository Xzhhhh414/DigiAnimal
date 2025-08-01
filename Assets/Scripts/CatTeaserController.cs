using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 逗猫棒控制器 - 管理逗猫棒的生命周期和状态
/// </summary>
public class CatTeaserController : MonoBehaviour
{
    [Header("逗猫棒设置")]
    [SerializeField] private float initialDetectionTime = 5f; // 初始检测时间（秒）
    
    // 动画控制器
    private Animator animator;
    
    // 状态管理
    private bool isActive = false;
    private float currentLifetime = 0f;
    
    // 被吸引的宠物列表
    private List<PetController2D> attractedPets = new List<PetController2D>();
    private List<PetController2D> interactingPets = new List<PetController2D>();
    
    // 静态引用，确保同时只有一个逗猫棒
    private static CatTeaserController currentInstance;
    
    public static bool HasActiveCatTeaser => currentInstance != null;
    public static CatTeaserController CurrentInstance => currentInstance;
    
    // 位置属性，供外部访问
    public Vector3 Position => transform.position;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 检查是否已有活跃的逗猫棒
        if (currentInstance != null)
        {
            Debug.LogWarning("已存在活跃的逗猫棒，销毁新创建的实例");
            Destroy(gameObject);
            return;
        }
        
        currentInstance = this;
    }
    
    private void Start()
    {
        StartCatTeaser();
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // 更新生命周期
        currentLifetime += Time.deltaTime;
        
        // 检查销毁条件：被吸引宠物列表为空 且 过了初始检测时间
        if (attractedPets.Count == 0 && interactingPets.Count == 0 && currentLifetime >= initialDetectionTime)
        {
            DestroyCatTeaser();
        }
    }
    
    /// <summary>
    /// 启动逗猫棒
    /// </summary>
    private void StartCatTeaser()
    {
        isActive = true;
        currentLifetime = 0f;
        
        // 触发挥动动画
        if (animator != null)
        {
            animator.SetTrigger(AnimationStrings.startWaveTrigger);
        }
        
        Debug.Log($"逗猫棒启动，初始检测时间: {initialDetectionTime}秒");
    }
    
    /// <summary>
    /// 宠物被吸引时调用
    /// </summary>
    /// <param name="pet">被吸引的宠物</param>
    public void OnPetAttracted(PetController2D pet)
    {
        if (pet == null || attractedPets.Contains(pet)) return;
        
        attractedPets.Add(pet);
        Debug.Log($"宠物 {pet.PetDisplayName} 被逗猫棒吸引，当前被吸引宠物数量: {attractedPets.Count}");
    }
    
    /// <summary>
    /// 宠物开始与逗猫棒互动时调用
    /// </summary>
    /// <param name="pet">开始互动的宠物</param>
    public void OnPetStartInteraction(PetController2D pet)
    {
        if (pet == null || interactingPets.Contains(pet)) return;
        
        interactingPets.Add(pet);
        Debug.Log($"宠物 {pet.PetDisplayName} 开始与逗猫棒互动");
        
        // 从吸引列表中移除
        if (attractedPets.Contains(pet))
        {
            attractedPets.Remove(pet);
        }
    }
    
    /// <summary>
    /// 宠物结束与逗猫棒互动时调用
    /// </summary>
    /// <param name="pet">结束互动的宠物</param>
    public void OnPetEndInteraction(PetController2D pet)
    {
        if (pet == null) return;
        
        if (interactingPets.Contains(pet))
        {
            interactingPets.Remove(pet);
            Debug.Log($"宠物 {pet.PetDisplayName} 结束与逗猫棒互动，当前互动宠物数量: {interactingPets.Count}");
        }
        
        if (attractedPets.Contains(pet))
        {
            attractedPets.Remove(pet);
            Debug.Log($"宠物 {pet.PetDisplayName} 从被吸引列表移除，当前被吸引宠物数量: {attractedPets.Count}");
        }
        
        // 检查销毁条件：没有被吸引的宠物且没有互动中的宠物且过了初始检测时间
        if (attractedPets.Count == 0 && interactingPets.Count == 0 && currentLifetime >= initialDetectionTime)
        {
            Debug.Log("所有宠物都结束了互动且过了初始检测时间，准备销毁逗猫棒");
            DestroyCatTeaser();
        }
    }
    
    /// <summary>
    /// 检查指定位置是否在逗猫棒的影响范围内
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="detectionRadius">检测半径</param>
    /// <returns>是否在范围内</returns>
    public bool IsInRange(Vector3 position, float detectionRadius)
    {
        return Vector3.Distance(transform.position, position) <= detectionRadius;
    }
    
    /// <summary>
    /// 获取当前状态信息
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusInfo()
    {
        if (attractedPets.Count > 0 || interactingPets.Count > 0)
        {
            return $"有宠物感兴趣 (被吸引:{attractedPets.Count}, 互动中:{interactingPets.Count})";
        }
        else
        {
            float remainingTime = Mathf.Max(0f, initialDetectionTime - currentLifetime);
            return $"等待宠物发现 (剩余:{remainingTime:F1}秒)";
        }
    }
    
    /// <summary>
    /// 强制销毁逗猫棒
    /// </summary>
    public void DestroyCatTeaser()
    {
        isActive = false;
        
        // 通知所有相关宠物
        foreach (var pet in attractedPets)
        {
            if (pet != null)
            {
                pet.IsAttracted = false;
            }
        }
        
        foreach (var pet in interactingPets)
        {
            if (pet != null)
            {
                pet.IsCatTeasering = false;
            }
        }
        
        // 清理静态引用
        if (currentInstance == this)
        {
            currentInstance = null;
        }
        
        Debug.Log("逗猫棒被销毁");
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // 确保清理静态引用
        if (currentInstance == this)
        {
            currentInstance = null;
        }
    }
    
    /// <summary>
    /// 检查是否可以在指定位置放置逗猫棒
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="checkRadius">检测半径</param>
    /// <returns>是否可以放置</returns>
    public static bool CanPlaceAt(Vector3 position, float checkRadius = 1f)
    {
        // 检查是否已有逗猫棒
        if (HasActiveCatTeaser)
        {
            return false;
        }
        
        // 检查位置是否有阻挡物
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, checkRadius);
        
        foreach (var collider in colliders)
        {
            // 检查是否有阻挡物（排除宠物和触发器）
            if (collider.CompareTag("Obstacle") || collider.CompareTag("Wall"))
            {
                return false;
            }
            
            // 如果有其他重要的游戏物体，也不能放置
            if (collider.GetComponent<FoodController>() != null)
            {
                return false;
            }
        }
        
        return true;
    }
}