using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Start场景加载管理器 - 处理从Gameplay场景返回Start场景的转场动画
/// </summary>
public class StartSceneLoadingManager : MonoBehaviour
{
    [Header("过渡动画")]
    [SerializeField] private GameObject transitionOverlayPrefab;  // 过渡动画预制体
    
    // 过渡动画控制器（运行时创建）
    private GameObject transitionOverlayInstance;
    
    // 静态实例引用，用于紧急情况下的访问
    private static StartSceneLoadingManager _instance;
    
    private void Awake()
    {
        //Debug.Log($"[StartSceneLoadingManager] Awake() 被调用 - GameObject: {gameObject.name}, Enabled: {enabled}, ActiveInHierarchy: {gameObject.activeInHierarchy}, Scene: {gameObject.scene.name}");
        //Debug.Log($"[StartSceneLoadingManager] Awake() - GameState.IsReturningFromGameplay = {GameState.IsReturningFromGameplay}");
        
        // 设置静态实例引用
        if (_instance == null)
        {
            _instance = this;
            //Debug.Log($"[StartSceneLoadingManager] 设置静态实例引用 - Scene: {gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // 检查是否来自不同场景
            if (_instance.gameObject.scene != this.gameObject.scene)
            {
                //Debug.Log($"[StartSceneLoadingManager] 检测到跨场景实例冲突，替换为当前场景实例 - 旧场景: {_instance.gameObject.scene.name}, 新场景: {gameObject.scene.name}");
                var oldInstance = _instance;
                _instance = this;
                if (oldInstance != null)
                {
                    Destroy(oldInstance.gameObject);
                }
            }
            else
            {
                //Debug.Log($"[StartSceneLoadingManager] 销毁同场景重复实例 - Scene: {gameObject.scene.name}");
                Destroy(gameObject);
                return;
            }
        }
        
        // 强制确保组件处于启用状态
        if (!enabled)
        {
            Debug.LogWarning($"[StartSceneLoadingManager] 组件在Awake时被禁用，强制启用");
            enabled = true;
        }
        
        // 确保GameObject处于活跃状态
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[StartSceneLoadingManager] GameObject在Awake时不活跃，强制激活");
            gameObject.SetActive(true);
        }
        
        //Debug.Log($"[StartSceneLoadingManager] Awake() 完成 - Scene: {gameObject.scene.name}");
        
        // 注册场景加载完成事件（备用机制）
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        //Debug.Log($"[StartSceneLoadingManager] Start() 被调用，GameState.IsReturningFromGameplay = {GameState.IsReturningFromGameplay}");
        //Debug.Log($"[StartSceneLoadingManager] Start() - GameObject: {gameObject.name}, Enabled: {enabled}, ActiveInHierarchy: {gameObject.activeInHierarchy}, Scene: {gameObject.scene.name}");
        
        // 检查是否从其他场景跳转而来（通过GameState判断）
        if (GameState.IsReturningFromGameplay)
        {
            //Debug.Log("[StartSceneLoadingManager] 检测到从Gameplay返回，需要播放转场动画");
            // 从Gameplay返回，需要播放转场动画
            SetupTransitionOverlay();
            StartCoroutine(WaitForDataReloadAndPlayExit());
        }
        else
        {
            //Debug.Log("[StartSceneLoadingManager] 正常启动Start场景，不需要转场动画");
            // 正常启动Start场景，不需要转场动画
            CompleteInitialization();
        }
    }
    
    private void OnEnable()
    {
        //Debug.Log($"[StartSceneLoadingManager] OnEnable() 被调用 - GameState.IsReturningFromGameplay = {GameState.IsReturningFromGameplay}");
    }
    
    private void OnDisable()
    {
        //Debug.LogWarning("[StartSceneLoadingManager] OnDisable() 被调用 - 组件被禁用！");
        //Debug.LogWarning($"[StartSceneLoadingManager] 调用堆栈: {System.Environment.StackTrace}");
    }
    
    /// <summary>
    /// 设置过渡动画覆盖层
    /// </summary>
    private void SetupTransitionOverlay()
    {
        //Debug.Log("[StartSceneLoadingManager] 开始设置过渡动画覆盖层");
        
        if (transitionOverlayPrefab == null)
        {
            Debug.LogWarning("[StartSceneLoadingManager] 过渡动画预制体未配置，跳过Loading界面");
            return;
        }
        
        try
        {
            // 实例化过渡动画
            transitionOverlayInstance = Instantiate(transitionOverlayPrefab);
            var controller = transitionOverlayInstance.GetComponent("TransitionController");
            
            if (controller != null)
            {
                // 设置启动时直接为静止状态（全屏显示）
                controller.GetType().GetMethod("StartWithStaticState").Invoke(controller, null);
                //Debug.Log("[StartSceneLoadingManager] 过渡动画已设置为静止状态");
            }
            else
            {
                Debug.LogError("[StartSceneLoadingManager] 过渡动画预制体缺少TransitionController组件");
                Destroy(transitionOverlayInstance);
                transitionOverlayInstance = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartSceneLoadingManager] 设置过渡动画失败: {e.Message}");
            if (transitionOverlayInstance != null)
            {
                Destroy(transitionOverlayInstance);
                transitionOverlayInstance = null;
            }
        }
    }
    
    /// <summary>
    /// 等待数据重新加载完成并播放退出动画
    /// </summary>
    private IEnumerator WaitForDataReloadAndPlayExit()
    {
        //Debug.Log("[StartSceneLoadingManager] 开始等待数据重新加载");
        
        // 等待SaveManager初始化
        while (SaveManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 等待PetDatabaseManager初始化
        while (PetDatabaseManager.Instance == null || !PetDatabaseManager.Instance.IsDatabaseLoaded())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        //Debug.Log("[StartSceneLoadingManager] 核心管理器已初始化，开始重新加载数据");
        
        // 触发数据重新加载
        yield return StartCoroutine(ReloadSceneData());
        
        // 等待一帧确保UI更新完成
        yield return null;
        
        //Debug.Log("[StartSceneLoadingManager] 数据重新加载完成，开始播放退出动画");
        
        // 播放退出动画
        PlayExitAnimation();
    }
    
    /// <summary>
    /// 重新加载场景数据
    /// </summary>
    private IEnumerator ReloadSceneData()
    {
        // 1. 刷新存档信息显示
        GameStartManager gameStartManager = FindObjectOfType<GameStartManager>();
        if (gameStartManager != null)
        {
            gameStartManager.RefreshSaveInfoPublic();
            //Debug.Log("[StartSceneLoadingManager] 存档信息已刷新");
        }
        else
        {
            //Debug.LogWarning("[StartSceneLoadingManager] 未找到GameStartManager");
        }
        
        // 等待一帧
        yield return null;
        
        // 2. 重新显示宠物预览
        StartScenePetDisplay petDisplay = StartScenePetDisplay.Instance;
        if (petDisplay != null)
        {
            //Debug.Log("[StartSceneLoadingManager] 开始刷新宠物预览");
            petDisplay.RefreshPetDisplay();
            //Debug.Log("[StartSceneLoadingManager] 宠物预览刷新完成");
        }
        else
        {
            Debug.LogWarning("[StartSceneLoadingManager] 未找到StartScenePetDisplay");
        }
        
        // 等待更多帧确保数据加载完成
        yield return new WaitForSeconds(0.2f);
    }
    
    /// <summary>
    /// 播放退出动画
    /// </summary>
    private void PlayExitAnimation()
    {
        //Debug.Log("[StartSceneLoadingManager] 开始播放退出动画");
        
        if (transitionOverlayInstance == null)
        {
            Debug.LogWarning("[StartSceneLoadingManager] 过渡动画实例为空，直接完成初始化");
            // 没有过渡动画，直接完成初始化
            CompleteInitialization();
            return;
        }
        
        try
        {
            var controller = transitionOverlayInstance.GetComponent("TransitionController");
            
            if (controller != null)
            {
                // 监听退出动画完成事件
                var exitCompleteEvent = controller.GetType().GetField("OnExitComplete").GetValue(controller) as UnityEngine.Events.UnityEvent;
                if (exitCompleteEvent != null)
                {
                    exitCompleteEvent.AddListener(CompleteInitialization);
                    //Debug.Log("[StartSceneLoadingManager] 已注册退出动画完成事件");
                }
                
                // 播放退出动画
                controller.GetType().GetMethod("PlayExitAnimation").Invoke(controller, null);
                
                //Debug.Log("[StartSceneLoadingManager] 开始播放退出动画");
            }
            else
            {
                Debug.LogError("[StartSceneLoadingManager] 过渡动画控制器丢失");
                CompleteInitialization();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StartSceneLoadingManager] 播放退出动画失败: {e.Message}");
            CompleteInitialization();
        }
    }
    
    /// <summary>
    /// 完成初始化过程
    /// </summary>
    private void CompleteInitialization()
    {
        //Debug.Log("[StartSceneLoadingManager] Loading过程完成，Start场景准备就绪");
        
        // 清除返回标志
        GameState.IsReturningFromGameplay = false;
        
        // 销毁过渡动画实例
        if (transitionOverlayInstance != null)
        {
            Destroy(transitionOverlayInstance);
            transitionOverlayInstance = null;
        }
        
        //Debug.Log("[StartSceneLoadingManager] 过渡动画已清理，Start场景可以正常使用");
    }
    
    /// <summary>
    /// 场景加载完成时的备用检查机制
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只处理Start场景
        if (scene.name == "Start" && GameState.IsReturningFromGameplay)
        {
            //Debug.Log($"[StartSceneLoadingManager] 场景加载事件触发 - 备用检查机制");
            
            // 如果当前实例被禁用，尝试找到其他实例或强制启用
            if (_instance == null || !_instance.enabled)
            {
                Debug.LogWarning($"[StartSceneLoadingManager] 检测到实例问题，尝试修复");
                
                StartSceneLoadingManager[] allManagers = FindObjectsOfType<StartSceneLoadingManager>();
                foreach (var manager in allManagers)
                {
                    if (manager != null)
                    {
                        //Debug.Log($"[StartSceneLoadingManager] 找到管理器实例: {manager.gameObject.name}, Enabled: {manager.enabled}");
                        if (!manager.enabled)
                        {
                            manager.enabled = true;
                            //Debug.Log($"[StartSceneLoadingManager] 强制启用管理器: {manager.gameObject.name}");
                        }
                        
                        // 手动触发转场逻辑
                        manager.ForceExecuteTransition();
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 强制执行转场逻辑（备用方法）
    /// </summary>
    public void ForceExecuteTransition()
    {
        //Debug.Log($"[StartSceneLoadingManager] ForceExecuteTransition() 被调用");
        
        if (GameState.IsReturningFromGameplay)
        {
            //Debug.Log("[StartSceneLoadingManager] 强制执行转场动画逻辑");
            SetupTransitionOverlay();
            StartCoroutine(WaitForDataReloadAndPlayExit());
        }
    }
    
    private void OnDestroy()
    {
        //Debug.Log($"[StartSceneLoadingManager] OnDestroy() 被调用 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 清理事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 清理静态引用
        if (_instance == this)
        {
            //Debug.Log($"[StartSceneLoadingManager] 清除静态实例引用 - Scene: {gameObject.scene.name}");
            _instance = null;
        }
        
        // 清理过渡动画实例
        if (transitionOverlayInstance != null)
        {
            //Debug.Log($"[StartSceneLoadingManager] 清理过渡动画实例");
            Destroy(transitionOverlayInstance);
            transitionOverlayInstance = null;
        }
    }
} 