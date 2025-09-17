using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



/// <summary>
/// 玩具老鼠控制器
/// 负责老鼠的生命周期管理和与宠物的互动（移动逻辑由行为树控制）
/// </summary>
public class ToyMouseController : MonoBehaviour
{
    [Header("老鼠设置")]
    [SerializeField] private float lifeTime = 10f; // 生命周期（秒）
    [SerializeField] private Transform interactPos; // 宠物交互位置点（可选）
    
    // 组件引用
    private Animator animator;
    private NavMeshAgent agent;
    private const string isMovingParam = "isMoving";
    private const string horizontalParam = "horizontal";
    private const string verticalParam = "vertical";
    
    // 方向与阈值设置
    [Header("移动方向设置")]
    [SerializeField] private float movingThreshold = 0.05f; // 速度阈值，避免抖动
    [SerializeField] private bool quantizeIdleDirection = true; // 停止时量化方向
    private Vector2 lastNonZeroDir = Vector2.down; // 最后一次非零方向（默认朝下）
    
    // Animator Hash 缓存
    private int hashIsMoving;
    private int hashHorizontal;
    private int hashVertical;
    
    // 状态管理
    private bool isActive = false;
    private float currentLifetime = 0f;
    
    // 互动管理
    private PetController2D interactingPet = null; // 当前互动的宠物
    private bool hasHadInteraction = false; // 是否有过互动
    
    // 静态引用，确保同时只有一个玩具老鼠
    private static ToyMouseController currentInstance;
    
    public static bool HasActiveToyMouse => currentInstance != null;
    public static ToyMouseController CurrentInstance => currentInstance;
    
    // 属性
    public bool IsInteractionListEmpty => interactingPet == null;
    public int InteractingPetCount => interactingPet != null ? 1 : 0;
    public NavMeshAgent Agent => agent;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        // 自动查找InteractPos（如果没有在Inspector中设置）
        if (interactPos == null)
        {
            Transform found = transform.Find("InteractPos");
            if (found != null)
            {
                interactPos = found;
            }
        }
        
        // 生成参数哈希，避免外部依赖AnimationStrings
        hashIsMoving = Animator.StringToHash(isMovingParam);
        hashHorizontal = Animator.StringToHash(horizontalParam);
        hashVertical = Animator.StringToHash(verticalParam);
        
        // 配置为2D模式
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            // 移动速度使用NavMeshAgent自己的配置
        }
    }
    
    private void Start()
    {
        // 设置为当前实例
        if (currentInstance != null && currentInstance != this)
        {
            // 如果已有实例，销毁当前对象
            Debug.LogWarning($"已存在玩具老鼠实例 {currentInstance.name}，销毁新创建的实例 {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        currentInstance = this;
        
        // 确保新实例的组件都是启用状态（防止prefab保存了禁用状态）
        EnsureComponentsEnabled();
        
        // 启动老鼠
        StartToyMouse();
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // 更新生命周期
        currentLifetime += Time.deltaTime;
        
        // 检查销毁条件：超过生命周期且无宠物互动
        if (currentLifetime >= lifeTime && interactingPet == null)
        {
            // 根据是否有过互动来决定结束方式
            DestroyToyMouse(!hasHadInteraction); // true 表示无互动，false 表示有互动
            return;
        }
        
        // 同步动画（根据NavMeshAgent速度）
        UpdateAnimationByVelocity();
    }
    
    /// <summary>
    /// 启动玩具老鼠
    /// </summary>
    private void StartToyMouse()
    {
        isActive = true;
        currentLifetime = 0f;
        
        // Debug.Log("玩具老鼠开始活动");
    }

    /// <summary>
    /// 根据NavMeshAgent当前速度更新Animator参数
    /// </summary>
    private void UpdateAnimationByVelocity()
    {
        if (animator == null || agent == null) return;
        
        Vector3 velocity = agent.velocity;
        bool isMoving = velocity.sqrMagnitude > (movingThreshold * movingThreshold);
        
        animator.SetBool(hashIsMoving, isMoving);
        
        if (isMoving)
        {
            Vector3 dir = velocity.normalized;
            lastNonZeroDir = new Vector2((float)dir.x, (float)dir.y);
            animator.SetFloat(hashHorizontal, lastNonZeroDir.x);
            animator.SetFloat(hashVertical, lastNonZeroDir.y);
        }
        else
        {
            Vector2 idleDir = lastNonZeroDir;
            if (quantizeIdleDirection)
            {
                if (Mathf.Abs(idleDir.x) >= Mathf.Abs(idleDir.y))
                {
                    idleDir = new Vector2(Mathf.Sign(idleDir.x), 0f);
                }
                else
                {
                    idleDir = new Vector2(0f, Mathf.Sign(idleDir.y));
                }
            }
            if (idleDir.sqrMagnitude < 0.0001f)
            {
                idleDir = Vector2.down;
            }
            animator.SetFloat(hashHorizontal, idleDir.x);
            animator.SetFloat(hashVertical, idleDir.y);
        }
    }
    
    /// <summary>
    /// 检查指定位置是否在玩具老鼠的影响范围内
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="detectionRadius">检测半径</param>
    /// <returns>是否在范围内</returns>
    public bool IsInRange(Vector3 position, float detectionRadius)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= detectionRadius;
    }
    
    /// <summary>
    /// 宠物开始与玩具老鼠互动
    /// </summary>
    /// <param name="pet">要互动的宠物</param>
    /// <returns>是否成功开始互动</returns>
    public bool OnPetStartInteraction(PetController2D pet)
    {
        if (pet == null)
        {
            return false;
        }
        
        // 检查是否已有宠物在互动
        if (interactingPet != null)
        {
            // 若已是同一只宠物，则视为已在互动中，返回成功，避免重复加入失败
            if (interactingPet == pet)
            {
                return true;
            }
            // Debug.Log($"玩具老鼠已有宠物 {interactingPet.PetDisplayName} 在互动，拒绝宠物 {pet.PetDisplayName} 的互动请求");
            return false;
        }
        
        interactingPet = pet;
        hasHadInteraction = true; // 标记有过互动
        
        // Debug.Log($"宠物 {pet.PetDisplayName} 开始与玩具老鼠互动");
        
        return true;
    }
    
    /// <summary>
    /// 宠物结束与玩具老鼠互动
    /// </summary>
    /// <param name="pet">结束互动的宠物</param>
    public void OnPetEndInteraction(PetController2D pet)
    {
        if (pet == null || interactingPet != pet)
        {
            return;
        }
        
        interactingPet = null;
        
        // Debug.Log($"宠物 {pet.PetDisplayName} 结束与玩具老鼠互动，准备销毁老鼠");
        
        // 互动结束后立即销毁老鼠（不管生命周期是否到期）
        DestroyToyMouse(false); // false 表示有过互动
    }
    
    /// <summary>
    /// 销毁玩具老鼠
    /// </summary>
    /// <param name="noInteraction">是否为无互动销毁（true=无互动，false=有互动）</param>
    public void DestroyToyMouse(bool noInteraction = false)
    {
        isActive = false;
        
        // 通知互动中的宠物
        if (interactingPet != null)
        {
            interactingPet.IsPlayingMouse = false;
        }
        
        // 根据是否有互动来调用不同的结束阶段
        if (ToolInteractionManager.Instance != null)
        {
            if (noInteraction)
            {
                // 无互动的结束阶段
                ToolInteractionManager.Instance.StartNoInteractionEndingPhase("玩具老鼠");
                // Debug.Log("玩具老鼠无宠物互动，开始无互动结束阶段");
            }
            // 注意：有互动的情况由 ToyMouseInteraction.EndInteraction() 处理
        }
        
        // 清理静态引用
        if (currentInstance == this)
        {
            currentInstance = null;
            // Debug.Log($"[ToyMouseController] 已清理 currentInstance: {gameObject.name}");
        }
        
                    // Debug.Log($"玩具老鼠开始渐变消失 (无互动: {noInteraction})");
        
        // 使用渐变效果销毁（通过反射避免类型引用问题）
        var fadeOut = GetComponent("FadeOutDestroy");
        if (fadeOut != null)
        {
            var fadeOutType = fadeOut.GetType();
            // 优先匹配 (bool, System.Action) 签名
            var method = fadeOutType.GetMethod("StartFadeOut", new System.Type[] { typeof(bool), typeof(System.Action) });
            if (method != null)
            {
                method.Invoke(fadeOut, new object[] { true, null });
            }
            else
            {
                // 尝试仅(bool)的重载
                method = fadeOutType.GetMethod("StartFadeOut", new System.Type[] { typeof(bool) });
                if (method != null)
                {
                    method.Invoke(fadeOut, new object[] { true });
                }
                else
                {
                    Debug.LogWarning("玩具老鼠的FadeOutDestroy组件没有找到StartFadeOut方法，直接销毁");
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // 如果没有FadeOutDestroy组件，直接销毁（向后兼容）
            Debug.LogWarning("玩具老鼠没有FadeOutDestroy组件，直接销毁");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // 注意：OnDestroy 时不调用结束阶段
        // 正常的结束阶段应该由 ToyMouseInteraction.EndInteraction() 或 DestroyToyMouse() 处理
        // OnDestroy 只负责清理工作
        
        // 清理静态引用
        if (currentInstance == this)
        {
            currentInstance = null;
        }
    }
    
    /// <summary>
    /// 获取状态信息（供调试使用）
    /// </summary>
    /// <returns>状态描述</returns>
    public string GetStatusInfo()
    {
        if (interactingPet != null)
        {
            return $"与 {interactingPet.PetDisplayName} 互动中";
        }
        else
        {
            float remainingTime = Mathf.Max(0f, lifeTime - currentLifetime);
            return $"自由漫游 (剩余:{remainingTime:F1}秒)";
        }
    }

    /// <summary>
    /// 交互位置属性，供外部访问
    /// </summary>
    public Transform InteractPos => interactPos;

    /// <summary>
    /// 确保组件都是启用状态（用于新实例初始化）
    /// </summary>
    private void EnsureComponentsEnabled()
    {
        // 启用动画
        if (animator != null) { animator.enabled = true; }
        
        // 启用所有SpriteRenderer
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) { if (r != null) r.enabled = true; }
        
        // 启用所有Collider2D
        var colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in colliders) { if (c != null) c.enabled = true; }
        
                    // Debug.Log($"[ToyMouseController] 已确保 {gameObject.name} 的所有组件启用状态");
    }

    /// <summary>
    /// 在宠物抓到老鼠的一刻，隐藏老鼠外观并冻结其行为（不销毁）
    /// </summary>
    public void HideForInteraction()
    {
        // 停止Update驱动
        isActive = false;

        // 停止导航
        if (agent != null && agent.isOnNavMesh)
        {
            try { agent.isStopped = true; agent.ResetPath(); } catch { }
        }

        // 关闭动画与渲染（仅隐藏外观，不销毁对象）
        if (animator != null) { animator.enabled = false; }
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) { if (r != null) r.enabled = false; }

        // 关闭碰撞体，避免后续检测/阻挡
        var colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (var c in colliders) { if (c != null) c.enabled = false; }
        
        // Debug.Log($"[ToyMouseController] 已隐藏 {gameObject.name} 的外观和碰撞体");
    }
}