using UnityEngine;
using TapSDK.Core;
using System;

/// <summary>
/// TapTap SDK配置和初始化管理器
/// </summary>
public class TapTapSDKConfig : MonoBehaviour
{
    [Header("TapTap SDK配置")]
    [SerializeField] private string clientId = "x1ve5pzlulxx1amjqe";           // TapTap Client ID
    [SerializeField] private string clientToken = "Iu9kCcMygCpPfk879vuOXk1zIEl8wKhMX2fZqD4h";        // TapTap Client Token
    [SerializeField] private TapTapRegionType region = TapTapRegionType.CN;  // 地区：CN国内，Overseas海外
    [SerializeField] private TapTapLanguageType language = TapTapLanguageType.zh_Hans; // 语言
    
    [Header("调试设置")]
    [SerializeField] private bool enableLog = false;          // 是否开启日志（发布版本建议false）
    
    [Header("状态")]
    [SerializeField] private bool isInitialized = false;     // SDK是否已初始化
    
    // 单例实例
    public static TapTapSDKConfig Instance { get; private set; }
    
    // 初始化完成事件
    public static event Action OnSDKInitialized;
    
    // 属性
    public bool IsInitialized => isInitialized;
    public string ClientId => clientId;
    public string ClientToken => clientToken;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 不在Awake中立即初始化SDK，而是等待隐私协议同意后再初始化
            CheckPrivacyAndInitialize();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// 检查隐私协议同意状态，决定是否初始化SDK
    /// </summary>
    private void CheckPrivacyAndInitialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 检查用户是否已经同意隐私协议
        if (HasPrivacyConsent())
        {
            Debug.Log("[TapTapSDK] 检测到用户已同意隐私协议，开始初始化SDK");
            InitializeSDK();
        }
        else
        {
            Debug.Log("[TapTapSDK] 用户尚未同意隐私协议，延迟初始化SDK");
            // 启动协程定期检查隐私协议同意状态
            StartCoroutine(WaitForPrivacyConsent());
        }
#else
        // 非Android平台直接初始化（开发环境）
        Debug.Log("[TapTapSDK] 非Android平台，直接初始化SDK");
        InitializeSDK();
#endif
    }
    
    /// <summary>
    /// 等待用户同意隐私协议
    /// </summary>
    private System.Collections.IEnumerator WaitForPrivacyConsent()
    {
        Debug.Log("[TapTapSDK] 开始等待用户同意隐私协议...");
        
        while (!HasPrivacyConsent())
        {
            yield return new WaitForSeconds(0.5f); // 每0.5秒检查一次
        }
        
        Debug.Log("[TapTapSDK] 检测到用户已同意隐私协议，现在初始化SDK");
        InitializeSDK();
    }
    
    /// <summary>
    /// 公共方法：强制检查隐私协议状态并初始化SDK（如果需要）
    /// </summary>
    public void CheckAndInitializeIfNeeded()
    {
        if (!isInitialized && HasPrivacyConsent())
        {
            Debug.Log("[TapTapSDK] 外部触发：检测到隐私协议已同意，开始初始化SDK");
            InitializeSDK();
        }
    }
    
    /// <summary>
    /// 检查用户是否已同意隐私协议
    /// </summary>
    private bool HasPrivacyConsent()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 使用与PrivacyActivity相同的SharedPreferences检查方式
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var sharedPrefs = currentActivity.Call<AndroidJavaObject>("getSharedPreferences", "PlayerPrefs", 0))
            {
                return sharedPrefs.Call<bool>("getBoolean", "PrivacyAcceptedKey", false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TapTapSDK] 检查隐私协议状态失败: {e.Message}");
            return false;
        }
#else
        // 非Android平台假设已同意（开发环境）
        return true;
#endif
    }
    
    /// <summary>
    /// 初始化TapTap SDK
    /// </summary>
    private void InitializeSDK()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 检查是否已经初始化过
        if (isInitialized)
        {
            Debug.Log("[TapTapSDK] SDK已经初始化过，跳过重复初始化");
            OnSDKInitialized?.Invoke();
            return;
        }
        
        try
        {
            // 验证配置
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientToken))
            {
                Debug.LogError("[TapTapSDK] Client ID 或 Client Token 未配置！请在Inspector中设置正确的值");
                return;
            }
            
            Debug.Log("[TapTapSDK] 开始初始化SDK...");
            Debug.Log($"[TapTapSDK] Client ID: {clientId}");
            Debug.Log($"[TapTapSDK] Region: {region}");
            Debug.Log($"[TapTapSDK] Language: {language}");
            
            // 创建SDK配置
            TapTapSdkOptions coreOptions = new TapTapSdkOptions
            {
                clientId = this.clientId,
                clientToken = this.clientToken,
                region = this.region,
                preferredLanguage = this.language,
                enableLog = this.enableLog
            };
            
            // 初始化SDK
            TapTapSDK.Init(coreOptions);
            
            isInitialized = true;
            Debug.Log("[TapTapSDK] SDK初始化成功！");
            
            // 触发初始化完成事件
            OnSDKInitialized?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapSDK] SDK初始化失败: {e.Message}");
            // 如果是重复初始化的错误，仍然标记为已初始化
            if (e.Message.Contains("had already been started"))
            {
                Debug.Log("[TapTapSDK] 检测到SDK已被初始化，标记为成功状态");
                isInitialized = true;
                OnSDKInitialized?.Invoke();
            }
            else
            {
                isInitialized = false;
            }
        }
#else
        Debug.Log("[TapTapSDK] 非Android平台，跳过SDK初始化");
        isInitialized = true; // 在非Android平台标记为已初始化，避免阻塞其他功能
        OnSDKInitialized?.Invoke();
#endif
    }
    
    /// <summary>
    /// 验证SDK配置是否正确
    /// </summary>
    public bool ValidateConfig()
    {
        if (string.IsNullOrEmpty(clientId))
        {
            Debug.LogError("[TapTapSDK] Client ID 未配置");
            return false;
        }
        
        if (string.IsNullOrEmpty(clientToken))
        {
            Debug.LogError("[TapTapSDK] Client Token 未配置");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取当前配置信息（用于调试）
    /// </summary>
    [ContextMenu("显示当前配置")]
    public void ShowCurrentConfig()
    {
        Debug.Log("=== TapTap SDK 配置信息 ===");
        Debug.Log($"Client ID: {clientId}");
        Debug.Log($"Client Token: {(string.IsNullOrEmpty(clientToken) ? "未配置" : "已配置")}");
        Debug.Log($"Region: {region}");
        Debug.Log($"Language: {language}");
        Debug.Log($"Enable Log: {enableLog}");
        Debug.Log($"Is Initialized: {isInitialized}");
        Debug.Log("========================");
    }
    
    /// <summary>
    /// 重新初始化SDK（仅用于调试）
    /// </summary>
    [ContextMenu("重新初始化SDK")]
    public void ReinitializeSDK()
    {
        isInitialized = false;
        InitializeSDK();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 编辑器验证
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        // 在编辑器中提供配置提示
        if (string.IsNullOrEmpty(clientId))
        {
            Debug.LogWarning("[TapTapSDK] 请配置 Client ID");
        }
        
        if (string.IsNullOrEmpty(clientToken))
        {
            Debug.LogWarning("[TapTapSDK] 请配置 Client Token");
        }
#endif
    }
}
