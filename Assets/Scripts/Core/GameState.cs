/// <summary>
/// 全局游戏状态管理 - 控制游戏各系统的启动状态
/// </summary>
public static class GameState
{
    private static bool _isInitializing = true;
    private static bool _isReturningFromGameplay = false;
    
    /// <summary>
    /// 是否正在初始化中
    /// </summary>
    public static bool IsInitializing 
    { 
        get => _isInitializing;
        set 
        {
            if (_isInitializing != value)
            {
                UnityEngine.Debug.Log($"[GameState] IsInitializing 变更: {_isInitializing} → {value}");
                _isInitializing = value;
            }
        }
    }
    
    /// <summary>
    /// 是否从Gameplay场景返回Start场景
    /// </summary>
    public static bool IsReturningFromGameplay 
    { 
        get => _isReturningFromGameplay;
        set 
        {
            if (_isReturningFromGameplay != value)
            {
                UnityEngine.Debug.Log($"[GameState] IsReturningFromGameplay 变更: {_isReturningFromGameplay} → {value}");
                _isReturningFromGameplay = value;
            }
        }
    }
    
    /// <summary>
    /// 宠物系统是否可以更新
    /// </summary>
    public static bool CanPetUpdate => !IsInitializing;
    
    /// <summary>
    /// 相机是否可以交互
    /// </summary>
    public static bool CanCameraInteract => !IsInitializing;
    
    /// <summary>
    /// UI是否可以交互
    /// </summary>
    public static bool CanUIInteract => !IsInitializing;
    
    /// <summary>
    /// 重置游戏状态（场景切换时调用）
    /// </summary>
    public static void ResetForNewScene()
    {
        UnityEngine.Debug.Log("[GameState] ResetForNewScene() 被调用");
        IsInitializing = true;
    }
    
    /// <summary>
    /// 完成初始化
    /// </summary>
    public static void CompleteInitialization()
    {
        UnityEngine.Debug.Log("[GameState] CompleteInitialization() 被调用");
        IsInitializing = false;
        // UnityEngine.Debug.Log("GameState: 游戏初始化完成，所有系统可以正常运行");
    }
    
    /// <summary>
    /// 获取当前状态的调试信息
    /// </summary>
    public static string GetDebugInfo()
    {
        return $"IsInitializing: {IsInitializing}, IsReturningFromGameplay: {IsReturningFromGameplay}";
    }
} 