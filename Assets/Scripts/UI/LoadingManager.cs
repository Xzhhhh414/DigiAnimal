using System.Collections;
using UnityEngine;

/// <summary>
/// 加载管理器 - 处理Gameplay场景的初始化加载过程
/// </summary>
public class LoadingManager : MonoBehaviour
{
    [Header("过渡动画")]
    [SerializeField] private GameObject transitionOverlayPrefab;  // 过渡动画预制体
    
    // 过渡动画控制器（运行时创建）
    private GameObject transitionOverlayInstance;
    
    private void Start()
    {
        // 创建并设置过渡动画为静止状态
        SetupTransitionOverlay();
        
        // 等待初始化完成
        StartCoroutine(WaitForInitializationComplete());
    }
    
    /// <summary>
    /// 设置过渡动画覆盖层
    /// </summary>
    private void SetupTransitionOverlay()
    {
        if (transitionOverlayPrefab == null)
        {
            Debug.LogWarning("过渡动画预制体未配置，跳过Loading界面");
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
                // Debug.Log("过渡动画已设置为静止状态");
            }
            else
            {
                Debug.LogError("过渡动画预制体缺少TransitionController组件");
                Destroy(transitionOverlayInstance);
                transitionOverlayInstance = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"设置过渡动画失败: {e.Message}");
            if (transitionOverlayInstance != null)
            {
                Destroy(transitionOverlayInstance);
                transitionOverlayInstance = null;
            }
        }
    }
    
    /// <summary>
    /// 等待初始化完成
    /// </summary>
    private IEnumerator WaitForInitializationComplete()
    {
        // 等待GameInitializer存在并完成初始化
        yield return new WaitUntil(() => {
            GameInitializer gameInitializer = FindObjectOfType<GameInitializer>();
            return gameInitializer != null && gameInitializer.IsInitialized;
        });
        
        // Debug.Log("游戏初始化完成，开始播放退出动画");
        
        // 播放退出动画
        PlayExitAnimation();
    }
    
    /// <summary>
    /// 播放退出动画
    /// </summary>
    private void PlayExitAnimation()
    {
        if (transitionOverlayInstance == null)
        {
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
                }
                
                // 播放退出动画
                controller.GetType().GetMethod("PlayExitAnimation").Invoke(controller, null);
                
                // Debug.Log("开始播放退出动画");
            }
            else
            {
                Debug.LogError("过渡动画控制器丢失");
                CompleteInitialization();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"播放退出动画失败: {e.Message}");
            CompleteInitialization();
        }
    }
    
    /// <summary>
    /// 完成初始化过程
    /// </summary>
    private void CompleteInitialization()
    {
        // Debug.Log("Loading过程完成，游戏开始");
        
        // 通知全局状态：初始化完成
        GameState.IsInitializing = false;
        
        // 销毁过渡动画实例
        if (transitionOverlayInstance != null)
        {
            Destroy(transitionOverlayInstance);
            transitionOverlayInstance = null;
        }
        
        // Debug.Log("过渡动画已清理，游戏可以正常运行");
    }
    
    private void OnDestroy()
    {
        // 确保清理过渡动画实例
        if (transitionOverlayInstance != null)
        {
            Destroy(transitionOverlayInstance);
        }
    }
} 