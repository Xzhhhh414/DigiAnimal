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
        
        // 检查对象是否应该作为NavMesh障碍物
        if (ShouldBeNavMeshObstacle(obj))
        {
            // NavMeshObstacle不需要额外的3D碰撞体，它可以独立工作
            DebugLog($"{obj.name} 被识别为障碍物 (isTrigger=false)");
            
            // 添加NavMeshObstacle（如果还没有的话）
            if (obj.GetComponent<NavMeshObstacle>() == null)
            {
                NavMeshObstacle obstacle = obj.AddComponent<NavMeshObstacle>();
                obstacle.carving = true; // 启用雕刻，这样会在NavMesh中创建洞
                obstacle.carvingMoveThreshold = 0.1f; // 设置移动阈值
                obstacle.carvingTimeToStationary = 0.5f; // 设置静止时间
                
                // 自动计算障碍物尺寸
                Bounds bounds = GetObjectBounds(obj);
                if (bounds.size != Vector3.zero)
                {
                    obstacle.size = bounds.size;
                    obstacle.center = bounds.center - obj.transform.position;
                }
                else
                {
                    // 默认尺寸
                    obstacle.size = Vector3.one;
                }
                
                DebugLog($"为 {obj.name} 添加了NavMeshObstacle - Size: {obstacle.size}, Center: {obstacle.center}, Carving: {obstacle.carving}");
            }
            
            prepared = true;
        }
        else
        {
            // 如果对象不应该是障碍物，确保它没有NavMeshObstacle组件
            NavMeshObstacle existingObstacle = obj.GetComponent<NavMeshObstacle>();
            if (existingObstacle != null)
            {
                DestroyImmediate(existingObstacle);
                DebugLog($"移除了 {obj.name} 的NavMeshObstacle（对象应该是可行走的）");
            }
            
            DebugLog($"{obj.name} 被标记为可行走区域，跳过障碍物设置");
        }
        
        return prepared;
    }
    
    /// <summary>
    /// 检查对象是否应该作为NavMesh障碍物
    /// 只根据碰撞体的isTrigger属性判断：触发器=可行走，非触发器=障碍物
    /// </summary>
    private bool ShouldBeNavMeshObstacle(GameObject obj)
    {
        // 检查2D碰撞体设置
        Collider2D collider2D = obj.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            // 如果是触发器，则不应该是NavMesh障碍
            return !collider2D.isTrigger;
        }
        
        // 检查3D碰撞体设置
        Collider collider3D = obj.GetComponent<Collider>();
        if (collider3D != null)
        {
            // 如果是触发器，则不应该是NavMesh障碍
            return !collider3D.isTrigger;
        }
        
        // 如果没有碰撞体，默认不作为障碍物
        return false;
    }
    
    /// <summary>
    /// 获取对象的边界框
    /// </summary>
    private Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        bool hasBounds = false;
        
        // 尝试从Renderer获取边界
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            hasBounds = true;
        }
        else
        {
            // 尝试从子对象的Renderer获取边界
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }
        }
        
        // 如果没有Renderer，尝试从碰撞体获取边界
        if (!hasBounds)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                Collider2D collider2D = obj.GetComponent<Collider2D>();
                if (collider2D != null)
                {
                    bounds = collider2D.bounds;
                    hasBounds = true;
                }
            }
        }
        
        return bounds;
    }
    
    /// <summary>
    /// 验证NavMeshObstacle设置
    /// </summary>
    private void ValidateNavMeshObstacles()
    {
        NavMeshObstacle[] obstacles = FindObjectsOfType<NavMeshObstacle>();
        DebugLog($"场景中共有 {obstacles.Length} 个NavMeshObstacle组件");
        
        foreach (var obstacle in obstacles)
        {
            if (obstacle.carving)
            {
                DebugLog($"障碍物 {obstacle.gameObject.name}: Size={obstacle.size}, Center={obstacle.center}, Carving=true");
            }
            else
            {
                DebugLog($"障碍物 {obstacle.gameObject.name}: Carving=false (不会在NavMesh中创建洞)");
            }
        }
    }
    
    /// <summary>
    /// 在NavMesh烘焙完成后设置NavMeshObstacle
    /// </summary>
    private void SetupNavMeshObstaclesAfterBaking()
    {
        // 查找所有动态生成的家具
        var plants = FindObjectsOfType<PlantController>();
        var foods = FindObjectsOfType<FoodController>();
        var speakers = FindObjectsOfType<SpeakerController>();
        var tvs = FindObjectsOfType<TVController>();
        
        int obstacleCount = 0;
        
        // 处理植物
        foreach (var plant in plants)
        {
            if (SetupSingleNavMeshObstacle(plant.gameObject))
                obstacleCount++;
        }
        
        // 处理食物
        foreach (var food in foods)
        {
            if (SetupSingleNavMeshObstacle(food.gameObject))
                obstacleCount++;
        }
        
        // 处理音响
        foreach (var speaker in speakers)
        {
            if (SetupSingleNavMeshObstacle(speaker.gameObject))
                obstacleCount++;
        }
        
        // 处理电视机
        foreach (var tv in tvs)
        {
            if (SetupSingleNavMeshObstacle(tv.gameObject))
                obstacleCount++;
        }
        
        DebugLog($"烘焙完成后设置了 {obstacleCount} 个NavMeshObstacle");
    }
    
    /// <summary>
    /// 为单个对象设置NavMeshObstacle
    /// </summary>
    private bool SetupSingleNavMeshObstacle(GameObject obj)
    {
        if (obj == null) return false;
        
        // 检查对象是否应该作为NavMesh障碍物
        if (ShouldBeNavMeshObstacle(obj))
        {
            NavMeshObstacle obstacle = obj.GetComponent<NavMeshObstacle>();
            if (obstacle != null)
            {
                // 重新配置现有的NavMeshObstacle
                obstacle.carving = true;
                obstacle.carvingMoveThreshold = 0.1f;
                obstacle.carvingTimeToStationary = 0.5f;
                
                // 重新计算尺寸
                Bounds bounds = GetObjectBounds(obj);
                if (bounds.size != Vector3.zero)
                {
                    obstacle.size = bounds.size;
                    obstacle.center = bounds.center - obj.transform.position;
                }
                
                DebugLog($"重新配置 {obj.name} 的NavMeshObstacle - Size: {obstacle.size}");
                return true;
            }
        }
        else
        {
            // 移除不需要的NavMeshObstacle
            NavMeshObstacle existingObstacle = obj.GetComponent<NavMeshObstacle>();
            if (existingObstacle != null)
            {
                DestroyImmediate(existingObstacle);
                DebugLog($"烘焙后移除了 {obj.name} 的NavMeshObstacle（应该是可行走的）");
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查对象是否有有效的NavMesh碰撞体
    /// </summary>
    private bool HasValidNavMeshCollider(GameObject obj)
    {
        // 检查3D碰撞体（非触发器）
        Collider collider3D = obj.GetComponent<Collider>();
        if (collider3D != null && !collider3D.isTrigger)
        {
            return true;
        }
        
        // 检查子对象中的3D碰撞体
        collider3D = obj.GetComponentInChildren<Collider>();
        if (collider3D != null && !collider3D.isTrigger)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// NavMesh烘焙完成回调
    /// </summary>
    private void OnNavMeshBakeComplete()
    {
        // 在NavMesh烘焙完成后，重新设置NavMeshObstacle
        SetupNavMeshObstaclesAfterBaking();
        
        // 验证NavMeshObstacle设置
        ValidateNavMeshObstacles();
        
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
    
    /// <summary>
    /// 检查对象是否有任何类型的碰撞体（2D或3D）
    /// </summary>
    private bool HasAnyCollider(GameObject obj)
    {
        // 检查3D碰撞体
        if (obj.GetComponent<Collider>() != null)
            return true;
        
        // 检查2D碰撞体
        if (obj.GetComponent<Collider2D>() != null)
            return true;
        
        // 检查子对象中的碰撞体
        if (obj.GetComponentInChildren<Collider>() != null)
            return true;
        
        if (obj.GetComponentInChildren<Collider2D>() != null)
            return true;
        
        return false;
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
