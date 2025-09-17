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
    [SerializeField] private Transform interactPos; // 宠物交互位置点
    
    // 动画控制器
    private Animator animator;
    
    // 状态管理
    private bool isActive = false;
    private float currentLifetime = 0f;
    private bool hasHadInteraction = false; // 是否有过宠物互动
    
    // 被吸引的宠物列表
    private List<PetController2D> attractedPets = new List<PetController2D>();
    private List<PetController2D> interactingPets = new List<PetController2D>();
    
    // 静态引用，确保同时只有一个逗猫棒
    private static CatTeaserController currentInstance;
    
    public static bool HasActiveCatTeaser => currentInstance != null;
    public static CatTeaserController CurrentInstance => currentInstance;
    
    // 位置属性，供外部访问
    public Vector3 Position => transform.position;
    
    // 交互位置属性，供外部访问
    public Transform InteractPos => interactPos;
    
    // 互动列表状态属性，供外部访问
    public bool IsInteractionListEmpty => interactingPets.Count == 0;
    public int InteractingPetCount => interactingPets.Count;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 自动查找InteractPos（如果没有在Inspector中设置）
        if (interactPos == null)
        {
            interactPos = transform.Find("InteractPos");
            if (interactPos == null)
            {
                // Debug.LogWarning($"逗猫棒 {gameObject.name} 没有找到InteractPos子对象，宠物将移动到逗猫棒中心位置");
            }
        }
        
        // 检查是否已有活跃的逗猫棒
        if (currentInstance != null)
        {
            // Debug.LogWarning("已存在活跃的逗猫棒，销毁新创建的实例");
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
            // 根据是否有过互动来决定结束方式
            DestroyCatTeaser(!hasHadInteraction); // true 表示无互动，false 表示有互动
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
        
        // Debug.Log($"逗猫棒启动，初始检测时间: {initialDetectionTime}秒");
    }
    
    /// <summary>
    /// 宠物被吸引时调用
    /// </summary>
    /// <param name="pet">被吸引的宠物</param>
    public void OnPetAttracted(PetController2D pet)
    {
        if (pet == null || attractedPets.Contains(pet)) return;
        
        attractedPets.Add(pet);
        // Debug.Log($"宠物 {pet.PetDisplayName} 被逗猫棒吸引，当前被吸引宠物数量: {attractedPets.Count}");
    }
    
    /// <summary>
    /// 宠物开始与逗猫棒互动时调用
    /// </summary>
    /// <param name="pet">开始互动的宠物</param>
    /// <returns>是否成功加入互动列表</returns>
    public bool OnPetStartInteraction(PetController2D pet)
    {
        if (pet == null) return false;
        
        // 检查是否已有宠物在互动（限制为单宠物互动）
        if (interactingPets.Count > 0)
        {
            // Debug.Log($"逗猫棒已有宠物 {interactingPets[0].PetDisplayName} 在互动，拒绝宠物 {pet.PetDisplayName} 的互动请求");
            return false;
        }
        
        // 检查该宠物是否已在列表中
        if (interactingPets.Contains(pet))
        {
            return true; // 已在列表中，视为成功
        }
        
        interactingPets.Add(pet);
        hasHadInteraction = true; // 标记有过互动
        // Debug.Log($"宠物 {pet.PetDisplayName} 开始与逗猫棒互动（互动列表：{interactingPets.Count}/1）");
        
        // 从吸引列表中移除
        if (attractedPets.Contains(pet))
        {
            attractedPets.Remove(pet);
        }
        
        return true;
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
            // Debug.Log($"宠物 {pet.PetDisplayName} 结束与逗猫棒互动，当前互动宠物数量: {interactingPets.Count}");
        }
        
        if (attractedPets.Contains(pet))
        {
            attractedPets.Remove(pet);
            // Debug.Log($"宠物 {pet.PetDisplayName} 从被吸引列表移除，当前被吸引宠物数量: {attractedPets.Count}");
        }
        
        // 检查销毁条件：没有被吸引的宠物且没有互动中的宠物且过了初始检测时间
        if (attractedPets.Count == 0 && interactingPets.Count == 0 && currentLifetime >= initialDetectionTime)
        {
            // Debug.Log("所有宠物都结束了互动且过了初始检测时间，准备销毁逗猫棒");
            // 注意：这里不调用DestroyCatTeaser，因为有互动的结束阶段由CatTeaserInteraction处理
            // 只是清理引用和销毁GameObject
            if (currentInstance == this)
            {
                currentInstance = null;
            }
            // Debug.Log("逗猫棒被销毁 (有互动结束)");
            Destroy(gameObject);
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
    /// <param name="noInteraction">是否为无互动销毁（true=无互动，false=有互动）</param>
    public void DestroyCatTeaser(bool noInteraction = false)
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
        
        // 根据是否有互动来调用不同的结束阶段
        if (ToolInteractionManager.Instance != null)
        {
            if (noInteraction)
            {
                // 无互动的结束阶段
                ToolInteractionManager.Instance.StartNoInteractionEndingPhase("逗猫棒");
                // Debug.Log("逗猫棒无宠物互动，开始无互动结束阶段");
            }
            // 注意：有互动的情况由 CatTeaserInteraction.EndInteraction() 处理
        }
        
        // 清理静态引用
        if (currentInstance == this)
        {
            currentInstance = null;
        }
        
        // Debug.Log($"逗猫棒开始渐变消失 (无互动: {noInteraction})");
        
        // 使用渐变效果销毁（通过反射避免类型引用问题）
        var fadeOut = GetComponent("FadeOutDestroy");
        if (fadeOut != null)
        {
            var fadeOutType = fadeOut.GetType();
            var method = fadeOutType.GetMethod("StartFadeOut", new System.Type[] { typeof(bool) });
            if (method != null)
            {
                method.Invoke(fadeOut, new object[] { true });
            }
            else
            {
                // Debug.LogWarning("逗猫棒的FadeOutDestroy组件没有找到StartFadeOut方法，直接销毁");
                Destroy(gameObject);
            }
        }
        else
        {
            // 如果没有FadeOutDestroy组件，直接销毁（向后兼容）
            // Debug.LogWarning("逗猫棒没有FadeOutDestroy组件，直接销毁");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // 注意：OnDestroy 时不调用结束阶段
        // 正常的结束阶段应该由 CatTeaserInteraction.EndInteraction() 处理
        // OnDestroy 只负责清理工作
        
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
            // 检查layer是否为Pet(宠物)或Scene(场景阻挡物)
            int layerMask = collider.gameObject.layer;
            string layerName = LayerMask.LayerToName(layerMask);
            
            if (layerName == "Pet" || layerName == "Scene")
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