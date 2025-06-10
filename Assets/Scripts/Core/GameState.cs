/// <summary>
/// 全局游戏状态管理 - 控制游戏各系统的启动状态
/// </summary>
public static class GameState
{
    /// <summary>
    /// 是否正在初始化中
    /// </summary>
    public static bool IsInitializing { get; set; } = true;
    
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
        IsInitializing = true;
    }
    
    /// <summary>
    /// 完成初始化
    /// </summary>
    public static void CompleteInitialization()
    {
        IsInitializing = false;
        // UnityEngine.Debug.Log("GameState: 游戏初始化完成，所有系统可以正常运行");
    }
} 