using UnityEngine;
using UnityEngine.Events;
using TapSDK.Compliance;
using TapSDK.Login;
using System;
using System.Collections;

/// <summary>
/// TapTap合规认证管理器 - 处理实名认证和防沉迷
/// </summary>
public class TapTapComplianceManager : MonoBehaviour
{
    [Header("合规认证状态")]
    [SerializeField] private bool isComplianceActive = false;
    [SerializeField] private string currentUserIdentifier = "";
    
    // 单例实例
    public static TapTapComplianceManager Instance { get; private set; }
    
    // 合规认证事件
    [System.Serializable]
    public class ComplianceEvent : UnityEvent<int, string> { }
    
    [Header("合规认证回调事件")]
    public ComplianceEvent OnLoginSuccess;           // 500: 玩家未受到限制，正常进入游戏
    public UnityEvent OnExited;                      // 1000: 退出防沉迷认证及检查
    public UnityEvent OnSwitchAccount;               // 1001: 用户点击切换账号
    public ComplianceEvent OnPeriodRestrict;         // 1030: 用户当前时间无法进行游戏
    public ComplianceEvent OnDurationLimit;          // 1050: 用户无可玩时长
    public ComplianceEvent OnAgeLimit;               // 1100: 当前用户因触发应用设置的年龄限制无法进入游戏
    public ComplianceEvent OnInvalidClientOrNetworkError; // 1200: 数据请求失败
    public UnityEvent OnRealNameStop;                // 9002: 实名过程中点击了关闭实名窗
    
    // 属性
    public bool IsComplianceActive => isComplianceActive;
    public string CurrentUserIdentifier => currentUserIdentifier;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEvents();
            Debug.Log("[TapTapCompliance] TapTapComplianceManager单例已创建");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 等待SDK初始化完成后注册回调
        StartCoroutine(WaitForSDKAndRegisterCallback());
    }
    
    /// <summary>
    /// 初始化所有UnityEvent
    /// </summary>
    private void InitializeEvents()
    {
        if (OnLoginSuccess == null) OnLoginSuccess = new ComplianceEvent();
        if (OnExited == null) OnExited = new UnityEvent();
        if (OnSwitchAccount == null) OnSwitchAccount = new UnityEvent();
        if (OnPeriodRestrict == null) OnPeriodRestrict = new ComplianceEvent();
        if (OnDurationLimit == null) OnDurationLimit = new ComplianceEvent();
        if (OnAgeLimit == null) OnAgeLimit = new ComplianceEvent();
        if (OnInvalidClientOrNetworkError == null) OnInvalidClientOrNetworkError = new ComplianceEvent();
        if (OnRealNameStop == null) OnRealNameStop = new UnityEvent();
    }
    
    /// <summary>
    /// 等待SDK初始化完成后注册合规认证回调
    /// </summary>
    private IEnumerator WaitForSDKAndRegisterCallback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 等待SDK初始化完成
        while (TapTapSDKConfig.Instance == null || !TapTapSDKConfig.Instance.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.5f); // 额外等待确保SDK完全就绪
        
        Debug.Log("[TapTapCompliance] SDK已就绪，注册合规认证回调");
        RegisterComplianceCallback();
#else
        Debug.Log("[TapTapCompliance] 非Android平台，跳过合规认证回调注册");
        yield return null;
#endif
    }
    
    /// <summary>
    /// 注册合规认证回调
    /// </summary>
    private void RegisterComplianceCallback()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            TapTapCompliance.RegisterComplianceCallback(OnComplianceCallback);
            Debug.Log("[TapTapCompliance] 合规认证回调注册成功");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapCompliance] 合规认证回调注册失败: {e.Message}");
        }
#endif
    }
    
    /// <summary>
    /// 合规认证回调处理
    /// </summary>
    private void OnComplianceCallback(int code, string extra)
    {
        // Debug.Log($"[TapTapCompliance] 收到合规认证回调 - Code: {code}, Extra: {extra}");
        
        switch (code)
        {
            case 500: // LOGIN_SUCCESS - 玩家未受到限制，正常进入游戏
                // Debug.Log("[TapTapCompliance] ✅ 合规认证通过，玩家可以正常进入游戏");
                OnLoginSuccess?.Invoke(code, extra);
                break;
                
            case 1000: // EXITED - 退出防沉迷认证及检查
                // Debug.Log("[TapTapCompliance] ⚠️ 退出防沉迷认证及检查");
                isComplianceActive = false;
                OnExited?.Invoke();
                break;
                
            case 1001: // SWITCH_ACCOUNT - 用户点击切换账号
                // Debug.Log("[TapTapCompliance] 🔄 用户点击切换账号");
                isComplianceActive = false;
                OnSwitchAccount?.Invoke();
                break;
                
            case 1030: // PERIOD_RESTRICT - 用户当前时间无法进行游戏
                Debug.LogWarning("[TapTapCompliance] ⏰ 用户当前时间无法进行游戏");
                OnPeriodRestrict?.Invoke(code, extra);
                break;
                
            case 1050: // DURATION_LIMIT - 用户无可玩时长
                Debug.LogWarning("[TapTapCompliance] ⏱️ 用户无可玩时长");
                OnDurationLimit?.Invoke(code, extra);
                break;
                
            case 1100: // AGE_LIMIT - 当前用户因触发应用设置的年龄限制无法进入游戏
                Debug.LogWarning("[TapTapCompliance] 🔞 当前用户因年龄限制无法进入游戏");
                OnAgeLimit?.Invoke(code, extra);
                break;
                
            case 1200: // INVALID_CLIENT_OR_NETWORK_ERROR - 数据请求失败
                Debug.LogError("[TapTapCompliance] ❌ 数据请求失败，请检查应用信息和网络连接");
                OnInvalidClientOrNetworkError?.Invoke(code, extra);
                break;
                
            case 9002: // REAL_NAME_STOP - 实名过程中点击了关闭实名窗
                Debug.LogWarning("[TapTapCompliance] ❌ 实名过程中点击了关闭实名窗");
                OnRealNameStop?.Invoke();
                break;
                
            default:
                Debug.LogWarning($"[TapTapCompliance] 未知的合规认证回调代码: {code}");
                break;
        }
    }
    
    /// <summary>
    /// 开始合规认证
    /// </summary>
    /// <param name="userIdentifier">用户唯一标识（建议使用TapTap的openId或unionId）</param>
    public void StartupCompliance(string userIdentifier)
    {
        if (string.IsNullOrEmpty(userIdentifier))
        {
            Debug.LogError("[TapTapCompliance] 用户标识为空，无法开始合规认证");
            return;
        }
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            currentUserIdentifier = userIdentifier;
            isComplianceActive = true;
            
            Debug.Log($"[TapTapCompliance] 开始合规认证，用户标识: {userIdentifier}");
            TapTapCompliance.Startup(userIdentifier);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapCompliance] 合规认证启动失败: {e.Message}");
            isComplianceActive = false;
        }
#else
        Debug.Log("[TapTapCompliance] 非Android平台，跳过合规认证");
        // 在非Android平台直接触发成功回调，模拟通过认证
        OnLoginSuccess?.Invoke(500, "非Android平台模拟通过");
#endif
    }
    
    /// <summary>
    /// 使用当前登录的TapTap账号开始合规认证
    /// </summary>
    public async void StartupComplianceWithCurrentAccount()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var account = await TapTapLogin.Instance.GetCurrentTapAccount();
            if (account != null && !string.IsNullOrEmpty(account.unionId))
            {
                StartupCompliance(account.unionId);
            }
            else
            {
                Debug.LogError("[TapTapCompliance] 当前没有有效的TapTap账号，无法开始合规认证");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapCompliance] 获取当前账号失败: {e.Message}");
        }
#else
        Debug.Log("[TapTapCompliance] 非Android平台，跳过合规认证");
        OnLoginSuccess?.Invoke(500, "非Android平台模拟通过");
#endif
    }
    
    /// <summary>
    /// 退出合规认证
    /// </summary>
    public void ExitCompliance()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("[TapTapCompliance] 退出合规认证");
            TapTapCompliance.Exit();
            isComplianceActive = false;
            currentUserIdentifier = "";
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapCompliance] 退出合规认证失败: {e.Message}");
        }
#else
        Debug.Log("[TapTapCompliance] 非Android平台，跳过合规认证退出");
        isComplianceActive = false;
        currentUserIdentifier = "";
#endif
    }
    
    /// <summary>
    /// 检查合规认证状态（用于调试）
    /// </summary>
    [ContextMenu("显示合规认证状态")]
    public void ShowComplianceStatus()
    {
        Debug.Log("=== TapTap 合规认证状态 ===");
        Debug.Log($"合规认证是否激活: {isComplianceActive}");
        Debug.Log($"当前用户标识: {(string.IsNullOrEmpty(currentUserIdentifier) ? "无" : currentUserIdentifier)}");
        Debug.Log("========================");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
