using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;

/// <summary>
/// 动态NavMesh管理器 - 负责在家具生成后重新烘焙NavMesh
/// </summary>
public class DynamicNavMeshManager : MonoBehaviour
{
    [Header("NavMesh配置")]
    [SerializeField] private NavMeshSurface navMeshSurface;  // NavMesh Surface组件引用
    [SerializeField] private bool autoFindNavMeshSurface = true;  // 是否自动查找NavMeshSurface
    
    [Header("烘焙设置")]
    [SerializeField] private float bakeDelay = 1f;  // 烘焙延迟（秒），避免频繁烘焙
    [SerializeField] private bool enableDebugLogs = true;  // 是否启用调试日志
    [SerializeField] private bool hideNavMeshLogs = true;  // 是否隐藏NavMeshPlus插件的详细日志
    
    // 单例实例
    public static DynamicNavMeshManager Instance { get; private set; }
    
    // 烘焙状态
    private bool isBaking = false;
    private bool pendingBake = false;
    
    private void Awake()
    {
        // 单例初始化（支持跨场景持久化，适合GameManager）
        if (Instance == null)
        {
            Instance = this;
            // 如果挂载在GameManager上，通常GameManager已经有DontDestroyOnLoad
            // 这里不重复设置，让GameManager管理生命周期
        }
        else
        {
            // 如果已存在实例，销毁当前组件
            Destroy(this);
            return;
        }
        
        // 自动查找NavMeshSurface
        if (autoFindNavMeshSurface && navMeshSurface == null)
        {
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                Debug.LogError("[DynamicNavMeshManager] 未找到NavMeshSurface组件！请在场景中添加NavMeshSurface组件或手动分配引用。");
            }
            else
            {
                DebugLog($"自动找到NavMeshSurface: {navMeshSurface.gameObject.name}");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 场景切换时重新初始化NavMeshSurface引用
    /// 当GameManager跨场景时，需要重新查找当前场景的NavMeshSurface
    /// </summary>
    public void RefreshNavMeshSurface()
    {
        if (autoFindNavMeshSurface)
        {
            navMeshSurface = FindObjectOfType<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                Debug.LogWarning("[DynamicNavMeshManager] 场景切换后未找到NavMeshSurface组件！");
            }
            else
            {
                DebugLog($"场景切换后重新找到NavMeshSurface: {navMeshSurface.gameObject.name}");
            }
        }
    }
    
    /// <summary>
    /// 请求重新烘焙NavMesh（带延迟，避免频繁烘焙）
    /// </summary>
    public void RequestNavMeshBake()
    {
        // 如果NavMeshSurface为空，尝试重新查找（适用于跨场景的情况）
        if (navMeshSurface == null && autoFindNavMeshSurface)
        {
            RefreshNavMeshSurface();
        }
        
        if (navMeshSurface == null)
        {
            Debug.LogError("[DynamicNavMeshManager] NavMeshSurface未设置，无法烘焙NavMesh！请确保当前场景有NavMeshSurface组件。");
            return;
        }
        
        if (isBaking)
        {
            // 如果正在烘焙，标记待烘焙
            pendingBake = true;
            DebugLog("NavMesh正在烘焙中，已标记待烘焙");
            return;
        }
        
        DebugLog("请求NavMesh烘焙...");
        StartCoroutine(DelayedBake());
    }
    
    /// <summary>
    /// 立即烘焙NavMesh（不推荐频繁使用）
    /// </summary>
    public void BakeNavMeshImmediate()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("[DynamicNavMeshManager] NavMeshSurface未设置，无法烘焙NavMesh！");
            return;
        }
        
        if (isBaking)
        {
            DebugLog("NavMesh正在烘焙中，跳过立即烘焙请求");
            return;
        }
        
        StartCoroutine(BakeNavMesh());
    }
    
    /// <summary>
    /// 家具移动时请求NavMesh烘焙
    /// 专门用于运行时家具移动的场景
    /// </summary>
    public void RequestNavMeshBakeForFurnitureMove()
    {
        DebugLog("家具移动触发NavMesh烘焙请求");
        RequestNavMeshBake();
    }
    
    /// <summary>
    /// 延迟烘焙协程
    /// </summary>
    private IEnumerator DelayedBake()
    {
        // 等待延迟时间
        yield return new WaitForSeconds(bakeDelay);
        
        // 开始烘焙
        yield return StartCoroutine(BakeNavMesh());
    }
    
    /// <summary>
    /// 烘焙NavMesh协程
    /// </summary>
    private IEnumerator BakeNavMesh()
    {
        if (isBaking)
        {
            yield break;
        }
        
        isBaking = true;
        DebugLog("开始烘焙NavMesh...");
        
        // 确保所有动态生成的家具都有正确的NavMeshObstacle或静态标记
        PrepareObjectsForBaking();
        
        // 等待一帧，确保所有对象状态已更新
        yield return null;
        
        try
        {
            // 设置NavMeshSurface的日志控制
            bool originalHideEditorLogs = navMeshSurface.hideEditorLogs;
            if (hideNavMeshLogs)
            {
                navMeshSurface.hideEditorLogs = true;
            }
            
            // 执行烘焙
            navMeshSurface.BuildNavMesh();
            DebugLog("NavMesh烘焙完成！");
            
            // 恢复原始日志设置
            navMeshSurface.hideEditorLogs = originalHideEditorLogs;
            
            // 通知其他系统NavMesh已更新
            OnNavMeshBakeComplete();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DynamicNavMeshManager] NavMesh烘焙失败: {e.Message}");
        }
        finally
        {
            isBaking = false;
            
            // 如果有待处理的烘焙请求，继续处理
            if (pendingBake)
            {
                pendingBake = false;
                DebugLog("处理待烘焙请求...");
                StartCoroutine(DelayedBake());
            }
        }
    }
    
    /// <summary>
    /// 为烘焙准备对象（确保家具有正确的碰撞体和静态标记）
    /// </summary>
    private void PrepareObjectsForBaking()
    {
        // 查找所有动态生成的家具
        var plants = FindObjectsOfType<PlantController>();
        var foods = FindObjectsOfType<FoodController>();
        var speakers = FindObjectsOfType<SpeakerController>();
        var tvs = FindObjectsOfType<TVController>();
        
        int preparedCount = 0;
        
        // 处理植物
        foreach (var plant in plants)
        {
            if (PrepareObjectForNavMesh(plant.gameObject))
                preparedCount++;
        }
        
        // 处理食物
        foreach (var food in foods)
        {
            if (PrepareObjectForNavMesh(food.gameObject))
                preparedCount++;
        }
        
        // 处理音响
        foreach (var speaker in speakers)
        {
            if (PrepareObjectForNavMesh(speaker.gameObject))
                preparedCount++;
        }
        
        // 处理电视机
        foreach (var tv in tvs)
        {
            if (PrepareObjectForNavMesh(tv.gameObject))
                preparedCount++;
        }
        
        DebugLog($"为NavMesh烘焙准备了 {preparedCount} 个家具对象");
    }
    
    /// <summary>
    /// 为单个对象准备NavMesh烘焙
    /// </summary>
    private bool PrepareObjectForNavMesh(GameObject obj)
    {
        if (obj == null) return false;
        
        bool prepared = false;
        
        // 确保对象有碰撞体（用于NavMesh烘焙）
        Collider objCollider = obj.GetComponent<Collider>();
        if (objCollider == null)
        {
            // 尝试在子对象中查找碰撞体
            objCollider = obj.GetComponentInChildren<Collider>();
        }
        
        if (objCollider != null)
        {
            // 设置为Navigation Static，这样NavMesh烘焙时会考虑这个对象
            NavMeshObstacle obstacle = obj.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = obj.AddComponent<NavMeshObstacle>();
                obstacle.carving = true; // 启用雕刻，这样会在NavMesh中创建洞
            }
            prepared = true;
        }
        else
        {
            // 检查是否已有任何类型的碰撞体（包括2D版本）
            bool hasAnyCollider = obj.GetComponent<Collider>() != null || 
                                 obj.GetComponent<Collider2D>() != null;
            
            if (!hasAnyCollider)
            {
                // 如果没有任何碰撞体，添加一个简单的Box Collider
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false; // 确保不是触发器，这样可以阻挡导航
                DebugLog($"为 {obj.name} 添加了BoxCollider用于NavMesh烘焙");
            }
            else
            {
                DebugLog($"{obj.name} 已有碰撞体（可能是2D版本），跳过添加BoxCollider");
            }
            
            // 添加NavMeshObstacle（如果还没有的话）
            if (obj.GetComponent<NavMeshObstacle>() == null)
            {
                NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
                obstacle.carving = true; // 启用雕刻，这样会在NavMesh中创建洞
                DebugLog($"为 {obj.name} 添加了NavMeshObstacle用于NavMesh烘焙");
            }
            
            prepared = true;
        }
        
        return prepared;
    }
    
    /// <summary>
    /// NavMesh烘焙完成回调
    /// </summary>
    private void OnNavMeshBakeComplete()
    {
        // 通知所有NavMeshAgent更新路径
        RefreshAllNavMeshAgents();
        
        // 可以在这里添加其他需要在NavMesh更新后执行的逻辑
        // 例如：更新AI行为、重新计算路径等
    }
    
    /// <summary>
    /// 刷新所有NavMeshAgent的路径
    /// </summary>
    private void RefreshAllNavMeshAgents()
    {
        NavMeshAgent[] agents = FindObjectsOfType<NavMeshAgent>();
        int refreshedCount = 0;
        
        foreach (var agent in agents)
        {
            if (NavMeshAgentHelper.IsValid(agent))
            {
                // 重置当前路径，让AI重新计算
                if (NavMeshAgentHelper.SafeResetPath(agent))
                {
                    refreshedCount++;
                }
            }
        }
        
        DebugLog($"刷新了 {refreshedCount} 个NavMeshAgent的路径");
    }
    
    /// <summary>
    /// 检查NavMesh是否需要重新烘焙
    /// </summary>
    public bool IsNavMeshUpdateNeeded()
    {
        // 这里可以添加更复杂的逻辑来判断是否需要更新
        // 例如：检查是否有新的家具生成、家具位置是否改变等
        return !isBaking;
    }
    
    /// <summary>
    /// 获取当前烘焙状态
    /// </summary>
    public bool IsBaking => isBaking;
    
    /// <summary>
    /// 调试日志输出
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DynamicNavMeshManager] {message}");
        }
    }
    
    #region 编辑器支持
    
#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中的测试方法
    /// </summary>
    [ContextMenu("测试NavMesh烘焙")]
    private void TestBakeNavMesh()
    {
        if (Application.isPlaying)
        {
            RequestNavMeshBake();
        }
        else
        {
            Debug.Log("[DynamicNavMeshManager] 请在运行时测试NavMesh烘焙功能");
        }
    }
    
    /// <summary>
    /// 编辑器中显示NavMesh信息
    /// </summary>
    [ContextMenu("显示NavMesh信息")]
    private void ShowNavMeshInfo()
    {
        if (navMeshSurface != null)
        {
            Debug.Log($"[DynamicNavMeshManager] NavMeshSurface信息:\n" +
                     $"- GameObject: {navMeshSurface.gameObject.name}\n" +
                     $"- AgentTypeID: {navMeshSurface.agentTypeID}\n" +
                     $"- LayerMask: {navMeshSurface.layerMask.value}\n" +
                     $"- UseGeometry: {navMeshSurface.useGeometry}");
        }
        else
        {
            Debug.Log("[DynamicNavMeshManager] NavMeshSurface未设置");
        }
    }
#endif
    
    #endregion
}
