using UnityEngine;
using UnityEngine.Events;
using TapSDK.Login;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// TapTap登录管理器 - 处理TapTap登录相关功能
/// </summary>
public class TapTapLoginManager : MonoBehaviour
{
    [Header("登录配置")]
    [SerializeField] private bool useBasicInfoScope = true; // 使用basic_info实现无感登录
    
    [Header("事件")]
    public UnityEvent<TapTapAccount> OnLoginSuccess;
    public UnityEvent<string> OnLoginFailed;
    public UnityEvent OnLoginCancelled;
    public UnityEvent OnLogout;
    public UnityEvent OnSDKReady;  // SDK初始化完成事件
    
    [Header("合规认证配置")]
    [SerializeField] private bool enableCompliance = true;  // 是否启用合规认证
    
    // 单例实例
    public static TapTapLoginManager Instance { get; private set; }
    
    // 当前用户信息
    public TapTapAccount CurrentAccount { get; private set; }
    
    // 登录状态
    public bool IsLoggedIn => CurrentAccount != null;
    
    // SDK初始化状态
    private bool sdkInitialized = false;
    
    // 合规认证管理器引用
    private TapTapComplianceManager complianceManager;
    
    private void Awake()
    {
        // Debug.Log("[TapTapLogin] ===== TapTapLoginManager.Awake() 开始执行 =====");
        
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Debug.Log("[TapTapLogin] TapTapLoginManager单例已创建");
            
            // 初始化UnityEvent
            InitializeEvents();
        }
        else
        {
            // Debug.Log("[TapTapLogin] TapTapLoginManager单例已存在，销毁重复实例");
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// 初始化UnityEvent事件
    /// </summary>
    private void InitializeEvents()
    {
        if (OnLoginSuccess == null) OnLoginSuccess = new UnityEvent<TapTapAccount>();
        if (OnLoginFailed == null) OnLoginFailed = new UnityEvent<string>();
        if (OnLoginCancelled == null) OnLoginCancelled = new UnityEvent();
        if (OnLogout == null) OnLogout = new UnityEvent();
        if (OnSDKReady == null) OnSDKReady = new UnityEvent();
        
        // Debug.Log("[TapTapLogin] UnityEvent事件已初始化");
        // Debug.Log($"[TapTapLogin] OnSDKReady是否为空: {OnSDKReady == null}");
    }
    
    /// <summary>
    /// 初始化合规认证管理器
    /// </summary>
    private void InitializeComplianceManager()
    {
        if (!enableCompliance)
        {
            Debug.Log("[TapTapLogin] 合规认证已禁用");
            return;
        }
        
        // 查找或创建合规认证管理器
        complianceManager = TapTapComplianceManager.Instance;
        if (complianceManager == null)
        {
            Debug.Log("[TapTapLogin] 创建TapTapComplianceManager实例");
            GameObject complianceObj = new GameObject("TapTapComplianceManager");
            complianceManager = complianceObj.AddComponent<TapTapComplianceManager>();
            
            // 设置合规认证回调
            SetupComplianceCallbacks();
        }
        else
        {
            Debug.Log("[TapTapLogin] 使用现有的TapTapComplianceManager实例");
            SetupComplianceCallbacks();
        }
    }
    
    /// <summary>
    /// 设置合规认证回调
    /// </summary>
    private void SetupComplianceCallbacks()
    {
        if (complianceManager != null)
        {
            // 合规认证成功 - 用户可以正常进入游戏
            complianceManager.OnLoginSuccess.AddListener(OnComplianceLoginSuccess);
            
            // 退出合规认证
            complianceManager.OnExited.AddListener(OnComplianceExited);
            
            // 切换账号
            complianceManager.OnSwitchAccount.AddListener(OnComplianceSwitchAccount);
            
            // 时间限制
            complianceManager.OnPeriodRestrict.AddListener(OnCompliancePeriodRestrict);
            
            // 时长限制
            complianceManager.OnDurationLimit.AddListener(OnComplianceDurationLimit);
            
            // 年龄限制
            complianceManager.OnAgeLimit.AddListener(OnComplianceAgeLimit);
            
            // 网络错误
            complianceManager.OnInvalidClientOrNetworkError.AddListener(OnComplianceNetworkError);
            
            // 实名认证关闭
            complianceManager.OnRealNameStop.AddListener(OnComplianceRealNameStop);
            
            Debug.Log("[TapTapLogin] 合规认证回调设置完成");
        }
    }
    
    private void Start()
    {
        // Debug.Log("[TapTapLogin] ===== TapTapLoginManager.Start() 开始执行 =====");
        
        // 初始化合规认证管理器
        InitializeComplianceManager();
        
        // 等待SDK初始化完成后再检查登录状态
        StartCoroutine(WaitForSDKAndCheckLogin());
        
        // Debug.Log("[TapTapLogin] ===== TapTapLoginManager.Start() 执行完成 =====");
    }
    
    /// <summary>
    /// 等待SDK初始化完成后检查登录状态
    /// </summary>
    private IEnumerator WaitForSDKAndCheckLogin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 等待TapTapSDKConfig初始化
        while (TapTapSDKConfig.Instance == null || !TapTapSDKConfig.Instance.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 额外等待一段时间，让SDK完全初始化
        yield return new WaitForSeconds(1.0f);
        
        sdkInitialized = true;
        // Debug.Log("[TapTapLogin] SDK已初始化，开始检查登录状态");
#else
        // 非Android平台直接标记为已初始化
        sdkInitialized = true;
        // Debug.Log("[TapTapLogin] 非Android平台，跳过SDK等待");
        yield return null; // 确保协程正确执行
#endif
        
        // SDK初始化完成，触发事件
        // Debug.Log("[TapTapLogin] SDK准备就绪，触发OnSDKReady事件");
        OnSDKReady?.Invoke();
        
        // 检查登录状态
        CheckLoginStatusAsync();
        
        // 等待一小段时间，然后再次检查是否需要登录（应急方案）
        yield return new WaitForSeconds(0.5f);
        
        if (!IsLoggedIn)
        {
            // Debug.Log("[TapTapLogin] 应急检查：用户未登录，尝试弹出登录界面");
            StartLoginAsync();
        }
    }
    
    /// <summary>
    /// 异步检查登录状态
    /// </summary>
#pragma warning disable CS1998 // 异步方法缺少 await 操作符，将以同步方式运行
    public async void CheckLoginStatusAsync()
#pragma warning restore CS1998
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Debug.Log("[TapTapLogin] 正在检查登录状态...");
            
            TapTapAccount account = await TapTapLogin.Instance.GetCurrentTapAccount();
            
            if (account != null)
            {
                CurrentAccount = account;
                // Debug.Log($"[TapTapLogin] 用户已登录 - OpenID: {account.openId}");
                // Debug.Log($"[TapTapLogin] 用户昵称: {account.name}");
                
                // 启动合规认证
                StartComplianceIfEnabled(account);
            }
            else
            {
                // Debug.Log("[TapTapLogin] 用户未登录");
                CurrentAccount = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapLogin] 检查登录状态失败: {e.Message}");
            CurrentAccount = null;
        }
#else
        // Debug.Log("[TapTapLogin] 非Android平台，跳过TapTap登录检查");
        // 在非Android平台，模拟已登录状态
        CurrentAccount = null;
#endif
    }
    
    /// <summary>
    /// 开始TapTap登录流程
    /// </summary>
#pragma warning disable CS1998 // 异步方法缺少 await 操作符，将以同步方式运行
    public async void StartLoginAsync()
#pragma warning restore CS1998
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 检查SDK是否已初始化
        if (!sdkInitialized)
        {
            Debug.LogError("[TapTapLogin] SDK未初始化，无法进行登录");
            OnLoginFailed?.Invoke("SDK未初始化");
            return;
        }
        
        try
        {
            // Debug.Log("[TapTapLogin] 开始登录流程...");
            
            // 定义授权范围 - 默认使用basic_info实现无感登录
            List<string> scopes = new List<string>();
            
            if (useBasicInfoScope)
            {
                scopes.Add(TapTapLogin.TAP_LOGIN_SCOPE_BASIC_INFO);
                // Debug.Log("[TapTapLogin] 使用basic_info授权范围（无感登录）");
            }
            else
            {
                scopes.Add(TapTapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE);
                // Debug.Log("[TapTapLogin] 使用public_profile授权范围（需要用户手动确认）");
            }
            
            // 发起TapTap登录 - SDK会显示自带的登录界面
            var userInfo = await TapTapLogin.Instance.LoginWithScopes(scopes.ToArray());
            
            if (userInfo != null)
            {
                CurrentAccount = userInfo;
                // Debug.Log($"[TapTapLogin] 登录成功！");
                // Debug.Log($"[TapTapLogin] 用户ID: {userInfo.unionId}");
                // Debug.Log($"[TapTapLogin] OpenID: {userInfo.openId}");
                // Debug.Log($"[TapTapLogin] 用户昵称: {userInfo.name}");
                
                // 启动合规认证
                StartComplianceIfEnabled(userInfo);
            }
            else
            {
                Debug.LogWarning("[TapTapLogin] 登录返回空用户信息");
                OnLoginFailed?.Invoke("登录返回空用户信息");
            }
        }
        catch (TaskCanceledException)
        {
            // Debug.Log("[TapTapLogin] 用户取消登录");
            OnLoginCancelled?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[TapTapLogin] 登录失败: {exception.Message}");
            OnLoginFailed?.Invoke(exception.Message);
        }
#else
        // Debug.Log("[TapTapLogin] 非Android平台，跳过TapTap登录");
        // 在非Android平台，直接触发登录取消（用于测试）
        OnLoginCancelled?.Invoke();
#endif
    }
    
    /// <summary>
    /// 获取当前用户详细信息
    /// </summary>
#pragma warning disable CS1998 // 异步方法缺少 await 操作符，将以同步方式运行
    public async Task<TapTapAccount> GetCurrentUserInfoAsync()
#pragma warning restore CS1998
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            TapTapAccount account = await TapTapLogin.Instance.GetCurrentTapAccount();
            CurrentAccount = account;
            return account;
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapLogin] 获取用户信息失败: {e.Message}");
            return null;
        }
#else
        // Debug.Log("[TapTapLogin] 非Android平台，返回空用户信息");
        return null;
#endif
    }
    
    /// <summary>
    /// 启动合规认证（如果启用）
    /// </summary>
    private void StartComplianceIfEnabled(TapTapAccount account)
    {
        if (!enableCompliance || complianceManager == null)
        {
            // 如果未启用合规认证，直接触发登录成功事件
            Debug.Log("[TapTapLogin] 合规认证未启用，直接触发登录成功");
            OnLoginSuccess?.Invoke(account);
            return;
        }
        
        if (string.IsNullOrEmpty(account.unionId))
        {
            Debug.LogError("[TapTapLogin] 用户UnionID为空，无法进行合规认证");
            OnLoginFailed?.Invoke("用户UnionID为空，无法进行合规认证");
            return;
        }
        
        Debug.Log($"[TapTapLogin] 开始合规认证，用户UnionID: {account.unionId}");
        complianceManager.StartupCompliance(account.unionId);
    }
    
    /// <summary>
    /// 合规认证成功回调
    /// </summary>
    private void OnComplianceLoginSuccess(int code, string extra)
    {
        Debug.Log($"[TapTapLogin] 合规认证通过，用户可以正常进入游戏 (Code: {code})");
        
        // 现在才触发真正的登录成功事件
        if (CurrentAccount != null)
        {
            OnLoginSuccess?.Invoke(CurrentAccount);
        }
    }
    
    /// <summary>
    /// 合规认证退出回调
    /// </summary>
    private void OnComplianceExited()
    {
        Debug.Log("[TapTapLogin] 用户退出合规认证，返回登录页");
        // 清理当前账号并触发登出
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证切换账号回调
    /// </summary>
    private void OnComplianceSwitchAccount()
    {
        // Debug.Log("[TapTapLogin] 用户请求切换账号，返回登录页");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 先清理TapTap登录状态，但不退出合规认证（避免状态冲突）
            TapTapLogin.Instance.Logout();
            // Debug.Log("[TapTapLogin] TapTap登录状态已清理");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapLogin] 清理TapTap登录状态失败: {e.Message}");
        }
#endif
        
        // 清理当前账号并触发登出事件，让GameStartManager显示登录界面
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证时间限制回调
    /// </summary>
    private void OnCompliancePeriodRestrict(int code, string extra)
    {
        Debug.LogWarning($"[TapTapLogin] 用户当前时间无法进行游戏 (Code: {code}, Extra: {extra})");
        // Debug.Log("[TapTapLogin] 时间限制，返回登录页面让用户重新尝试");
        
        // 清理当前账号并触发登出，返回登录页面
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证时长限制回调
    /// </summary>
    private void OnComplianceDurationLimit(int code, string extra)
    {
        Debug.LogWarning($"[TapTapLogin] 用户无可玩时长 (Code: {code}, Extra: {extra})");
        // Debug.Log("[TapTapLogin] 时长限制，返回登录页面让用户重新尝试");
        
        // 清理当前账号并触发登出，返回登录页面
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证年龄限制回调
    /// </summary>
    private void OnComplianceAgeLimit(int code, string extra)
    {
        Debug.LogWarning($"[TapTapLogin] 用户因年龄限制无法进入游戏 (Code: {code}, Extra: {extra})");
        // Debug.Log("[TapTapLogin] 年龄限制，返回登录页面让用户重新尝试");
        
        // 清理当前账号并触发登出，返回登录页面
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证网络错误回调
    /// </summary>
    private void OnComplianceNetworkError(int code, string extra)
    {
        Debug.LogError($"[TapTapLogin] 合规认证数据请求失败 (Code: {code}, Extra: {extra})");
        // Debug.Log("[TapTapLogin] 网络错误，返回登录页面让用户重试");
        
        // 清理当前账号并触发登出，返回登录页面
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 合规认证实名认证关闭回调
    /// </summary>
    private void OnComplianceRealNameStop()
    {
        // Debug.Log("[TapTapLogin] 用户关闭了实名认证窗口，返回登录页面");
        
        // 清理当前账号并触发登出，返回登录页面让用户重试
        CurrentAccount = null;
        OnLogout?.Invoke();
    }
    
    /// <summary>
    /// 登出
    /// </summary>
    public void Logout()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Debug.Log("[TapTapLogin] 用户登出");
            
            // 退出合规认证
            if (enableCompliance && complianceManager != null)
            {
                complianceManager.ExitCompliance();
            }
            
            TapTapLogin.Instance.Logout();
            CurrentAccount = null;
            
            // 触发登出事件
            OnLogout?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TapTapLogin] 登出失败: {e.Message}");
        }
#else
        // Debug.Log("[TapTapLogin] 非Android平台，跳过登出操作");
        CurrentAccount = null;
        OnLogout?.Invoke();
#endif
    }
    
    /// <summary>
    /// 获取用户显示名称
    /// </summary>
    public string GetUserDisplayName()
    {
        if (CurrentAccount != null && !string.IsNullOrEmpty(CurrentAccount.name))
        {
            return CurrentAccount.name;
        }
        return "TapTap用户";
    }
    
    /// <summary>
    /// 获取用户头像URL
    /// </summary>
    public string GetUserAvatarUrl()
    {
        if (CurrentAccount != null && !string.IsNullOrEmpty(CurrentAccount.avatar))
        {
            return CurrentAccount.avatar;
        }
        return null;
    }
    
    /// <summary>
    /// 获取用户OpenID
    /// </summary>
    public string GetUserOpenId()
    {
        return CurrentAccount?.openId;
    }
    
    /// <summary>
    /// 获取用户UnionID
    /// </summary>
    public string GetUserUnionId()
    {
        return CurrentAccount?.unionId;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
