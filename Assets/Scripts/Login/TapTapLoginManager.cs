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
    
    // 单例实例
    public static TapTapLoginManager Instance { get; private set; }
    
    // 当前用户信息
    public TapTapAccount CurrentAccount { get; private set; }
    
    // 登录状态
    public bool IsLoggedIn => CurrentAccount != null;
    
    // SDK初始化状态
    private bool sdkInitialized = false;
    
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
    
    private void Start()
    {
        // Debug.Log("[TapTapLogin] ===== TapTapLoginManager.Start() 开始执行 =====");
        
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
                
                // 触发登录成功事件
                OnLoginSuccess?.Invoke(account);
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
                
                // 触发登录成功事件
                // Debug.Log($"[TapTapLogin] 准备触发OnLoginSuccess事件，监听器数量: {OnLoginSuccess?.GetPersistentEventCount() ?? 0}");
                OnLoginSuccess?.Invoke(userInfo);
                // Debug.Log("[TapTapLogin] OnLoginSuccess事件已触发");
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
    /// 登出
    /// </summary>
    public void Logout()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Debug.Log("[TapTapLogin] 用户登出");
            
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
