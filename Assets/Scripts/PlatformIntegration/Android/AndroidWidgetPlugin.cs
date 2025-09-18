using UnityEngine;

/// <summary>
/// Android小组件插件接口
/// 提供Unity调用Android原生功能的接口
/// </summary>
public static class AndroidWidgetPlugin
{
    private const string ANDROID_PLUGIN_CLASS = "com.zher.meow.widget.AndroidWidgetPlugin";
    
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject pluginInstance;
    private static AndroidJavaClass unityClass;
    private static AndroidJavaObject unityActivity;
    
    static AndroidWidgetPlugin()
    {
        InitializePlugin();
    }
    
    /// <summary>
    /// 初始化Android插件
    /// </summary>
    private static void InitializePlugin()
    {
        try
        {
            unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            pluginInstance = new AndroidJavaObject(ANDROID_PLUGIN_CLASS, unityActivity);
            
            Debug.Log("[AndroidWidgetPlugin] 插件初始化成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 插件初始化失败: {e.Message}");
        }
    }
#endif
    
    /// <summary>
    /// 更新小组件数据
    /// </summary>
    /// <param name="jsonData">JSON格式的宠物数据</param>
    public static void UpdateWidgetData(string jsonData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (pluginInstance != null)
            {
                pluginInstance.Call("updateWidgetData", jsonData);
                Debug.Log("[AndroidWidgetPlugin] 小组件数据已更新");
            }
            else
            {
                Debug.LogWarning("[AndroidWidgetPlugin] 插件未初始化");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 更新小组件数据失败: {e.Message}");
        }
#else
        Debug.Log($"[AndroidWidgetPlugin] 更新小组件数据（编辑器模式）: {jsonData}");
#endif
    }
    
    /// <summary>
    /// 播放宠物动画
    /// </summary>
    /// <param name="petPrefabName">宠物预制体名称</param>
    /// <param name="animationType">动画类型 (sit/run/laydown)</param>
    public static void PlayPetAnimation(string petPrefabName, string animationType)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (pluginInstance != null)
            {
                pluginInstance.Call("playPetAnimation", petPrefabName, animationType);
                Debug.Log($"[AndroidWidgetPlugin] 播放动画: {petPrefabName} - {animationType}");
            }
            else
            {
                Debug.LogWarning("[AndroidWidgetPlugin] 插件未初始化");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 播放动画失败: {e.Message}");
        }
#else
        Debug.Log($"[AndroidWidgetPlugin] 播放动画（编辑器模式）: {petPrefabName} - {animationType}");
#endif
    }
    
    /// <summary>
    /// 刷新所有小组件
    /// </summary>
    public static void RefreshAllWidgets()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (pluginInstance != null)
            {
                pluginInstance.Call("refreshAllWidgets");
                Debug.Log("[AndroidWidgetPlugin] 已刷新所有小组件");
            }
            else
            {
                Debug.LogWarning("[AndroidWidgetPlugin] 插件未初始化");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 刷新小组件失败: {e.Message}");
        }
#else
        Debug.Log("[AndroidWidgetPlugin] 刷新所有小组件（编辑器模式）");
#endif
    }
    
    /// <summary>
    /// 检查小组件是否支持
    /// </summary>
    /// <returns>是否支持小组件功能</returns>
    public static bool IsWidgetSupported()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (pluginInstance != null)
            {
                return pluginInstance.Call<bool>("isWidgetSupported");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 检查小组件支持失败: {e.Message}");
        }
        return false;
#else
        // 编辑器模式下返回true用于测试
        return true;
#endif
    }
    
    /// <summary>
    /// 获取当前设备信息
    /// </summary>
    /// <returns>设备信息JSON字符串</returns>
    public static string GetDeviceInfo()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (pluginInstance != null)
            {
                return pluginInstance.Call<string>("getDeviceInfo");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 获取设备信息失败: {e.Message}");
        }
        return "{}";
#else
        // 编辑器模式下返回模拟数据
        return "{\"manufacturer\":\"Unity\",\"model\":\"Editor\",\"apiLevel\":30}";
#endif
    }
    
    /// <summary>
    /// 清理插件资源
    /// </summary>
    public static void Cleanup()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            pluginInstance?.Dispose();
            unityActivity?.Dispose();
            unityClass?.Dispose();
            
            pluginInstance = null;
            unityActivity = null;
            unityClass = null;
            
            Debug.Log("[AndroidWidgetPlugin] 插件资源已清理");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidWidgetPlugin] 清理插件资源失败: {e.Message}");
        }
#endif
    }
}

/// <summary>
/// Android动画类型枚举
/// </summary>
public enum AndroidAnimationType
{
    Sit,        // 坐下
    Run,        // 跑步  
    Laydown     // 趴下（去看看）
}

/// <summary>
/// Android动画工具类
/// </summary>
public static class AndroidAnimationHelper
{
    /// <summary>
    /// 将动画枚举转换为字符串
    /// </summary>
    public static string GetAnimationString(AndroidAnimationType animationType)
    {
        switch (animationType)
        {
            case AndroidAnimationType.Sit:
                return "sit";
            case AndroidAnimationType.Run:
                return "run";
            case AndroidAnimationType.Laydown:
                return "laydown";
            default:
                return "sit";
        }
    }
    
    /// <summary>
    /// 获取动画帧数
    /// </summary>
    public static int GetAnimationFrameCount(AndroidAnimationType animationType)
    {
        switch (animationType)
        {
            case AndroidAnimationType.Sit:
                return 5;
            case AndroidAnimationType.Run:
                return 4;
            case AndroidAnimationType.Laydown:
                return 2;
            default:
                return 1;
        }
    }
    
    /// <summary>
    /// 获取动画持续时间（毫秒）
    /// </summary>
    public static int GetAnimationDuration(AndroidAnimationType animationType)
    {
        int frameCount = GetAnimationFrameCount(animationType);
        int frameInterval = 150; // 每帧150ms
        return frameCount * frameInterval;
    }
}
