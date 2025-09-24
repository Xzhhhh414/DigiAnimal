using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using TapSDK.Login;

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0414 // 禁用"字段被赋值但从未使用"警告

/// <summary>
/// Start场景管理器 - 处理开始游戏界面和存档显示
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button startGameButton;          // 开始游戏按钮（永远显示）
    [SerializeField] private GameObject saveInfoPanel;        // 存档信息面板（有存档时显示）
    [SerializeField] private Button deleteSaveButton;         // 删除存档按钮（在存档信息面板内）
    [SerializeField] private Text saveInfoText;               // 存档信息显示文本（在存档信息面板内）
    
    [Header("删除确认界面")]
    [SerializeField] private GameObject deleteConfirmPanel;   // 删除确认面板
    [SerializeField] private Button confirmDeleteButton;      // 确认删除按钮
    [SerializeField] private Button cancelDeleteButton;       // 取消删除按钮
    
    [Header("场景设置")]
    [SerializeField] private string gameplaySceneName = "Gameplay";        // Gameplay场景名称
    
    [Header("过渡动画")]
    [SerializeField] private GameObject transitionOverlayPrefab;  // 过渡动画预制体
    
    [Header("文本设置")]
    [SerializeField] private string saveInfoFormat = "{0}";
    
    [Header("TapTap登录")]
    [SerializeField] private bool requireLogin = true;        // 是否需要登录才能开始游戏（仅Android）
    [SerializeField] private GameObject loginPanel;           // 登录信息面板
    [SerializeField] private Button retryLoginButton;         // 重试登录按钮
    [SerializeField] private Text loginInfoText;              // 登录状态文本
    
    [Header("登录状态文本配置")]
    [SerializeField] private string loginInProgressText = "正在连接TapTap...";
    [SerializeField] private string loginFailedText = "登录失败，请重试";
    
    // 组件引用
    private TapTapLoginManager tapTapLoginManager;
    
    // 登录状态管理
    private bool isCheckingLogin = false;
    private bool loginCheckCompleted = false;
    
    // 过渡动画控制器
    // private TransitionController transitionController;
    
    private void Start()
    {
        // Debug.Log("[GameStart] ===== GameStartManager.Start() 开始执行 =====");
        
        InitializeUI();
        RefreshSaveInfo();
        
        // 初始化Android数据桥接
        InitializeAndroidBridge();
        
        // 初始化TapTap登录
        InitializeTapTapLogin();
        
        // Debug.Log("[GameStart] ===== GameStartManager.Start() 执行完成 =====");
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置按钮事件
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        
        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        }
        
        // 设置删除确认界面按钮事件
        if (confirmDeleteButton != null)
        {
            confirmDeleteButton.onClick.AddListener(OnConfirmDeleteClicked);
        }
        
        if (cancelDeleteButton != null)
        {
            cancelDeleteButton.onClick.AddListener(OnCancelDeleteClicked);
        }
        
        // 设置重试登录按钮事件
        if (retryLoginButton != null)
        {
            retryLoginButton.onClick.AddListener(OnRetryLoginClicked);
        }
        
        // 确保删除确认面板初始状态为隐藏
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 初始化Android数据桥接
    /// </summary>
    private void InitializeAndroidBridge()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 确保AndroidDataBridge实例存在
            var androidBridge = AndroidDataBridge.Instance;
            Debug.Log("[GameStartManager] Android数据桥接已初始化");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameStartManager] 初始化Android数据桥接失败: {e.Message}");
        }
#else
        Debug.Log("[GameStartManager] 非Android平台，跳过Android数据桥接初始化");
#endif
    }
    
    /// <summary>
    /// 初始化TapTap登录
    /// </summary>
    private void InitializeTapTapLogin()
    {
        // Debug.Log("[GameStart] ===== InitializeTapTapLogin 开始执行 =====");
#if UNITY_ANDROID && !UNITY_EDITOR
        // 首先确保TapTapSDKConfig存在
        TapTapSDKConfig sdkConfig = TapTapSDKConfig.Instance;
        if (sdkConfig == null)
        {
            // 如果没有找到SDK配置，创建一个
            GameObject sdkConfigGO = new GameObject("TapTapSDKConfig");
            sdkConfig = sdkConfigGO.AddComponent<TapTapSDKConfig>();
            // Debug.Log("[GameStart] 创建了新的TapTapSDKConfig实例");
            // Debug.LogWarning("[GameStart] 请在TapTapSDKConfig组件中配置Client ID和Client Token");
        }
        
        // 获取或创建TapTapLoginManager实例
        tapTapLoginManager = TapTapLoginManager.Instance;
        if (tapTapLoginManager == null)
        {
            // 如果没有找到实例，尝试在场景中查找或创建
            GameObject loginManagerGO = new GameObject("TapTapLoginManager");
            tapTapLoginManager = loginManagerGO.AddComponent<TapTapLoginManager>();
            // Debug.Log("[GameStart] 创建了新的TapTapLoginManager实例");
            
            // 在组件创建后立即设置事件监听器，避免错过事件
            SetupLoginEventListeners();
        }
        else
        {
            // Debug.Log("[GameStart] 找到现有的TapTapLoginManager实例");
            // 设置事件监听器
            SetupLoginEventListeners();
        }
        
        // Debug.Log("[GameStart] TapTap登录系统初始化完成");
        // Debug.Log($"[GameStart] tapTapLoginManager实例: {tapTapLoginManager != null}");
        // Debug.Log($"[GameStart] requireLogin: {requireLogin}");
        
        // 开始登录检查，显示登录面板
        StartLoginCheck();
        
        // 主动检查当前登录状态（防止错过事件）
        CheckCurrentLoginStatus();
#else
        // Debug.Log("[GameStart] 非Android平台，跳过TapTap登录初始化");
        // 非Android平台不需要登录检查，显示正常界面
        HideLoginPanel();
        SetButtonsVisibility(true);
        
        // 标记登录检查已完成
        loginCheckCompleted = true;
#endif
    }
    
    /// <summary>
    /// 设置登录事件监听器
    /// </summary>
    private void SetupLoginEventListeners()
    {
        if (tapTapLoginManager != null)
        {
            // Debug.Log("[GameStart] 设置TapTap登录事件监听器");
            // Debug.Log($"[GameStart] OnLoginSuccess是否为空: {tapTapLoginManager.OnLoginSuccess == null}");
            // Debug.Log($"[GameStart] OnSDKReady是否为空: {tapTapLoginManager.OnSDKReady == null}");
            
            // 设置登录成功监听器
            if (tapTapLoginManager.OnLoginSuccess != null)
            {
                tapTapLoginManager.OnLoginSuccess.AddListener(OnTapTapLoginSuccess);
                // Debug.Log("[GameStart] 已添加OnLoginSuccess事件监听器");
            }
            else
            {
                Debug.LogError("[GameStart] tapTapLoginManager.OnLoginSuccess为空，无法添加监听器");
            }
            
            // 设置SDK准备就绪监听器
            if (tapTapLoginManager.OnSDKReady != null)
            {
                tapTapLoginManager.OnSDKReady.AddListener(OnTapTapSDKReady);
                // Debug.Log("[GameStart] 已添加OnSDKReady事件监听器");
            }
            else
            {
                Debug.LogError("[GameStart] tapTapLoginManager.OnSDKReady为空，无法添加监听器");
            }
            
            // 设置登录失败和取消监听器
            if (tapTapLoginManager.OnLoginFailed != null)
            {
                tapTapLoginManager.OnLoginFailed.AddListener(OnTapTapLoginFailed);
            }
            
            if (tapTapLoginManager.OnLoginCancelled != null)
            {
                tapTapLoginManager.OnLoginCancelled.AddListener(OnTapTapLoginCancelled);
            }
            
            // 设置登出监听器（用于合规认证退出和切换账号）
            if (tapTapLoginManager.OnLogout != null)
            {
                tapTapLoginManager.OnLogout.AddListener(OnTapTapLogout);
            }
        }
        else
        {
            Debug.LogError("[GameStart] TapTapLoginManager为空，无法设置事件监听器");
        }
    }
    
    /// <summary>
    /// 主动检查当前登录状态（防止错过事件）
    /// </summary>
    private void CheckCurrentLoginStatus()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Debug.Log("[GameStart] ===== CheckCurrentLoginStatus被调用 =====");
        
        if (tapTapLoginManager != null)
        {
            bool isLoggedIn = tapTapLoginManager.IsLoggedIn;
            // Debug.Log($"[GameStart] 当前登录状态: {isLoggedIn}");
            
            if (isLoggedIn)
            {
                // Debug.Log("[GameStart] 检测到用户已登录，立即完成登录检查");
                var account = tapTapLoginManager.CurrentAccount;
                if (account != null)
                {
                    // Debug.Log($"[GameStart] 当前用户: {account.name}");
                    // 直接调用登录成功处理
                    OnTapTapLoginSuccess(account);
                }
                else
                {
                    Debug.LogWarning("[GameStart] 用户已登录但账户信息为空");
                }
            }
            else
            {
                // Debug.Log("[GameStart] 用户未登录，保持登录界面显示");
            }
        }
        else
        {
            Debug.LogError("[GameStart] TapTapLoginManager为空，无法检查登录状态");
        }
#endif
    }
    
    /// <summary>
    /// 开始登录检查
    /// </summary>
    private void StartLoginCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Debug.Log($"[GameStart] ===== StartLoginCheck被调用 =====");
        // Debug.Log($"[GameStart] requireLogin: {requireLogin}");
        
        if (requireLogin)
        {
            isCheckingLogin = true;
            // Debug.Log("[GameStart] 开始登录检查，显示登录面板，隐藏游戏按钮");
            
            // 显示登录面板，隐藏游戏按钮
            ShowLoginPanel();
            SetButtonsVisibility(false);
            
            // Debug.Log("[GameStart] 登录检查UI更新完成");
        }
        else
        {
            // Debug.Log("[GameStart] 不需要登录，直接显示正常界面");
            // 不需要登录，直接显示正常界面
            HideLoginPanel();
            SetButtonsVisibility(true);
        }
#endif
    }
    
    /// <summary>
    /// 完成登录检查
    /// </summary>
    private void FinishLoginCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isCheckingLogin && !loginCheckCompleted)
        {
            isCheckingLogin = false;
            loginCheckCompleted = true;
            // Debug.Log("[GameStart] 登录检查完成，隐藏登录面板，显示游戏按钮");
            
            // 不再需要取消备用检查
            
            // 隐藏登录面板，显示游戏按钮
            HideLoginPanel();
            SetButtonsVisibility(true);
        }
        else
        {
            // Debug.Log($"[GameStart] FinishLoginCheck跳过: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        }
#endif
    }
    
    /// <summary>
    /// 强制完成登录检查（不检查状态条件）
    /// </summary>
    private void ForceFinishLoginCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Debug.Log("[GameStart] 强制完成登录检查，隐藏登录面板，显示游戏按钮");
        // Debug.Log($"[GameStart] 强制完成前状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        
        // 更新状态
        isCheckingLogin = false;
        loginCheckCompleted = true;
        
        // 不再需要取消备用检查
        
        // 强制隐藏登录面板，显示游戏按钮
        // Debug.Log("[GameStart] 执行强制UI更新");
        HideLoginPanel();
        SetButtonsVisibility(true);
        
        // Debug.Log($"[GameStart] 强制完成后状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
#else
        // Debug.Log("[GameStart] 非Android平台，ForceFinishLoginCheck无操作");
        // 在非Android平台也要确保UI正确
        HideLoginPanel();
        SetButtonsVisibility(true);
#endif
    }
    
    /// <summary>
    /// 设置基本游戏UI可见性
    /// </summary>
    private void SetButtonsVisibility(bool visible)
    {
        // 开始游戏按钮
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(visible);
        }
        
        if (visible)
        {
            // 显示时，刷新存档信息以正确显示存档相关UI
            RefreshSaveInfo();
        }
        else
        {
            // 隐藏时，隐藏所有存档相关UI
            if (deleteSaveButton != null)
            {
                deleteSaveButton.gameObject.SetActive(false);
            }
            
            if (saveInfoPanel != null)
            {
                saveInfoPanel.SetActive(false);
            }
        }
        
        // Debug.Log($"[GameStart] 游戏UI可见性设置为: {visible}");
    }
    
    /// <summary>
    /// 显示登录面板（登录中状态）
    /// </summary>
    private void ShowLoginPanel()
    {
        // Debug.Log("[GameStart] ===== ShowLoginPanel被调用 =====");
        if (loginPanel != null)
        {
            loginPanel.SetActive(true);
            // Debug.Log("[GameStart] LoginPanel已设置为显示");
            
            // 设置为登录中状态
            SetLoginPanelState(true);
            // Debug.Log("[GameStart] LoginPanel状态已设置为登录中");
        }
        else
        {
            Debug.LogWarning("[GameStart] LoginPanel未配置，无法显示登录信息");
        }
    }
    
    /// <summary>
    /// 隐藏登录面板
    /// </summary>
    private void HideLoginPanel()
    {
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
            // Debug.Log("[GameStart] 隐藏登录面板");
        }
    }
    
    /// <summary>
    /// 设置登录面板状态
    /// </summary>
    /// <param name="isLoginInProgress">true=登录中，false=登录失败</param>
    private void SetLoginPanelState(bool isLoginInProgress)
    {
        if (isLoginInProgress)
        {
            // 登录中状态
            if (loginInfoText != null)
            {
                loginInfoText.text = loginInProgressText;
            }
            
            if (retryLoginButton != null)
            {
                retryLoginButton.gameObject.SetActive(false);
            }
            
            // Debug.Log("[GameStart] 设置登录面板为登录中状态");
        }
        else
        {
            // 登录失败状态
            if (loginInfoText != null)
            {
                loginInfoText.text = loginFailedText;
            }
            
            if (retryLoginButton != null)
            {
                retryLoginButton.gameObject.SetActive(true);
            }
            
            // Debug.Log("[GameStart] 设置登录面板为登录失败状态");
        }
    }
    
    /// <summary>
    /// 刷新存档信息显示
    /// </summary>
    private void RefreshSaveInfo()
    {
        SaveFileInfo saveInfo = SaveManager.Instance.GetSaveFileInfo();
        
        if (saveInfo != null && saveInfo.exists)
        {
            // 有存档
            ShowSaveExists(saveInfo);
        }
        else
        {
            // 无存档
            ShowNoSave();
        }
    }
    
    /// <summary>
    /// 显示有存档的界面
    /// </summary>
    private void ShowSaveExists(SaveFileInfo saveInfo)
    {
        // 显示存档信息面板
        if (saveInfoPanel != null)
        {
            saveInfoPanel.SetActive(true);
        }
        
        // 更新存档信息文本
        if (saveInfoText != null)
        {
            string formattedText = string.Format(saveInfoFormat, 
                saveInfo.saveTime, 
                saveInfo.petCount, 
                saveInfo.heartCurrency);
            saveInfoText.text = formattedText;
        }
        
        // 显示并启用删除存档按钮
        if (deleteSaveButton != null)
        {
            deleteSaveButton.gameObject.SetActive(true);
            deleteSaveButton.interactable = true;
        }
        
        // 存档信息已更新
    }
    
    /// <summary>
    /// 显示无存档的界面
    /// </summary>
    private void ShowNoSave()
    {
        // 隐藏存档信息面板
        if (saveInfoPanel != null)
        {
            saveInfoPanel.SetActive(false);
        }
        
        // 隐藏删除存档按钮
        if (deleteSaveButton != null)
        {
            deleteSaveButton.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    private void OnStartGameClicked()
    {
        // 开始游戏按钮被点击
        
        // 禁用按钮防止重复点击
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }
        
        // 检查是否需要登录（仅Android平台）
#if UNITY_ANDROID && !UNITY_EDITOR
        if (requireLogin && (tapTapLoginManager == null || !tapTapLoginManager.IsLoggedIn))
        {
            Debug.Log("[GameStart] 需要登录才能开始游戏");
            
            // 重新启用按钮
            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }
            
            // 直接调用TapTap SDK登录（SDK会显示自带UI，不需要额外的Toast提示）
            if (tapTapLoginManager != null)
            {
                tapTapLoginManager.StartLoginAsync();
            }
            return;
        }
        
        // 如果已登录，显示登录状态信息
        if (tapTapLoginManager != null && tapTapLoginManager.IsLoggedIn)
        {
            var account = tapTapLoginManager.CurrentAccount;
            if (account != null)
            {
                Debug.Log($"[GameStart] 已登录用户开始游戏 - {account.name}");
            }
        }
#endif
        
        // 登录检查通过，继续游戏流程
        ProceedWithGameStart();
    }
    
    /// <summary>
    /// 继续游戏开始流程
    /// </summary>
    private void ProceedWithGameStart()
    {
        // 检查是否需要显示宠物选择界面
        CheckForFirstTimeSelection();
    }
    
    /// <summary>
    /// 检查是否需要显示宠物选择界面
    /// </summary>
    private void CheckForFirstTimeSelection()
    {
        // 检查是否有存档
        SaveFileInfo saveInfo = SaveManager.Instance.GetSaveFileInfo();
        
        if (saveInfo == null || !saveInfo.exists || saveInfo.petCount == 0)
        {
            // 没有存档或没有宠物，需要显示宠物选择界面
            // 检测到首次游戏，显示宠物选择界面
            
            // 查找FirstTimePetSelectionManager并显示宠物选择界面
            FirstTimePetSelectionManager petSelectionManager = FindObjectOfType<FirstTimePetSelectionManager>();
            if (petSelectionManager != null)
            {
                petSelectionManager.ForceShowPetSelection();
                // 宠物选择界面已激活
                
                // 重新启用开始游戏按钮，因为用户需要完成宠物选择
                if (startGameButton != null)
                {
                    startGameButton.interactable = true;
                }
            }
            else
            {
                Debug.LogError("未找到FirstTimePetSelectionManager组件！");
                
                // 重新启用按钮
                if (startGameButton != null)
                {
                    startGameButton.interactable = true;
                }
                
                ShowToastSafely("宠物选择系统未找到");
            }
        }
        else
        {
            // 已有存档，直接进入游戏
            // 检测到现有存档，直接进入游戏
            
            // 确保UIManager存在（只在真正进入游戏时创建）
            EnsureUIManagerExists();
            
            StartTransitionToGameplay();
        }
    }
    
    /// <summary>
    /// 删除存档按钮点击事件
    /// </summary>
    private void OnDeleteSaveClicked()
    {
        // 显示删除确认界面
        ShowDeleteConfirmation();
    }
    
    /// <summary>
    /// 重试登录按钮点击事件
    /// </summary>
    private void OnRetryLoginClicked()
    {
        // Debug.Log("[GameStart] 用户点击重试登录");
        
        // 设置为登录中状态
        SetLoginPanelState(true);
        
        // 重新尝试登录
        if (tapTapLoginManager != null)
        {
            tapTapLoginManager.StartLoginAsync();
        }
        else
        {
            Debug.LogError("[GameStart] TapTapLoginManager为空，无法重试登录");
            SetLoginPanelState(false);
        }
    }
    
    /// <summary>
    /// 显示删除确认界面
    /// </summary>
    private void ShowDeleteConfirmation()
    {
        if (deleteConfirmPanel != null)
        {
            // 显示删除确认面板
            deleteConfirmPanel.SetActive(true);
            
            // 隐藏其他UI元素
            HideMainUIElements();
        }
        else
        {
            Debug.LogError("DeleteConfirmPanel引用为空！请在Inspector中设置deleteConfirmPanel字段");
        }
    }
    
    /// <summary>
    /// 确认删除按钮点击事件
    /// </summary>
    private void OnConfirmDeleteClicked()
    {
        // 隐藏删除确认界面
        HideDeleteConfirmation();
        
        // 执行删除存档
        DeleteSave();
    }
    
    /// <summary>
    /// 取消删除按钮点击事件
    /// </summary>
    private void OnCancelDeleteClicked()
    {
        // 隐藏删除确认界面
        HideDeleteConfirmation();
    }
    
    /// <summary>
    /// 隐藏删除确认界面
    /// </summary>
    private void HideDeleteConfirmation()
    {
        if (deleteConfirmPanel != null)
        {
            // 隐藏删除确认面板
            deleteConfirmPanel.SetActive(false);
            
            // 显示主要UI元素
            ShowMainUIElements();
        }
    }
    
    /// <summary>
    /// 隐藏主要UI元素（显示删除确认界面时）
    /// </summary>
    private void HideMainUIElements()
    {
        // 隐藏开始游戏按钮
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(false);
        }
        
        // 隐藏删除存档按钮
        if (deleteSaveButton != null)
        {
            deleteSaveButton.gameObject.SetActive(false);
        }
        
        // 隐藏存档信息文本
        if (saveInfoText != null)
        {
            saveInfoText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 显示主要UI元素（隐藏删除确认界面时）
    /// </summary>
    private void ShowMainUIElements()
    {
        // 显示开始游戏按钮
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(true);
        }
        
        // 显示删除存档按钮
        if (deleteSaveButton != null)
        {
            deleteSaveButton.gameObject.SetActive(true);
        }
        
        // 显示存档信息文本
        if (saveInfoText != null)
        {
            saveInfoText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 删除存档
    /// </summary>
    private void DeleteSave()
    {
        bool success = SaveManager.Instance.DeleteSave();
        
        if (success)
        {
            // 存档删除成功
            RefreshSaveInfo(); // 刷新界面
            
            // 尝试显示提示信息（在开始场景中可能没有ToastManager）
            ShowToastSafely("存档已删除");
        }
        else
        {
            Debug.LogError("存档删除失败");
            
            // 尝试显示错误信息
            ShowToastSafely("删除存档失败");
        }
    }
    
    /// <summary>
    /// 确保UIManager存在
    /// </summary>
    private void EnsureUIManagerExists()
    {
        // 先检查场景中是否已经有UIManager
        UIManager existingUIManager = FindObjectOfType<UIManager>();
        if (existingUIManager != null)
        {
            // UIManager已存在
            return;
        }
        
        // 尝试从Resources加载UIManager预制体
        GameObject uiManagerPrefab = Resources.Load<GameObject>("Prefab/Manager/UIManager");
        if (uiManagerPrefab != null)
        {
            GameObject uiManagerInstance = Instantiate(uiManagerPrefab);
            uiManagerInstance.name = "UIManager"; // 设置一个清晰的名称
            // UIManager实例已创建
        }
        else
        {
            Debug.LogWarning("GameStartManager: 未找到UIManager预制体，请确保Resources/Prefab/Manager/UIManager.prefab存在");
        }
    }
    

    
    /// <summary>
    /// 安全显示Toast消息（处理ToastManager不存在的情况）
    /// </summary>
    private void ShowToastSafely(string message)
    {
        try
        {
            // 检查ToastManager是否存在，但不强制创建实例
            ToastManager toastManager = FindObjectOfType<ToastManager>();
            if (toastManager != null)
            {
                toastManager.ShowToast(message);
            }
            else
            {
                // 如果没有ToastManager，只在控制台输出消息
                // Toast消息显示
            }
        }
        catch (System.Exception e)
        {
            // 如果出现任何错误，优雅地处理
            Debug.LogWarning($"显示Toast消息时出错: {e.Message}，消息内容: {message}");
        }
    }
    
    /// <summary>
    /// 开始过渡动画并加载游戏场景
    /// </summary>
    private void StartTransitionToGameplay()
    {
        if (transitionOverlayPrefab == null)
        {
            Debug.LogWarning("过渡动画预制体未配置，直接加载场景");
            LoadGameplayScene();
            return;
        }
        
        try
        {
            // 实例化过渡动画
            GameObject overlayInstance = Instantiate(transitionOverlayPrefab);
            var controller = overlayInstance.GetComponent("TransitionController");
            
            if (controller != null)
            {
                // 设置立即开始进入动画（在Start之前调用）
                controller.GetType().GetMethod("StartWithEnterAnimation").Invoke(controller, null);
                
                // 使用反射调用方法，避免编译时类型检查
                var enterCompleteEvent = controller.GetType().GetField("OnEnterComplete").GetValue(controller) as UnityEngine.Events.UnityEvent;
                if (enterCompleteEvent != null)
                {
                    enterCompleteEvent.AddListener(LoadGameplayScene);
                }
                
                // Debug.Log("已设置过渡动画立即播放");
            }
            else
            {
                Debug.LogError("过渡动画预制体缺少TransitionController组件");
                Destroy(overlayInstance);
                LoadGameplayScene();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"创建过渡动画失败: {e.Message}");
            LoadGameplayScene();
        }
    }
    
    /// <summary>
    /// 加载游戏场景
    /// </summary>
    private void LoadGameplayScene()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("Gameplay场景名称未配置！请在Inspector中设置gameplaySceneName字段");
            
            // 重新启用按钮
            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }
            
            // 显示错误信息
            ShowToastSafely("场景配置错误");
            return;
        }
        
        try
        {            
            // 正在加载场景
            
            // 在场景切换前清理当前场景的管理器对象
            CleanupBeforeSceneTransition();
            
            SceneManager.LoadScene(gameplaySceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"加载场景失败: {e.Message}");
            
            // 重新启用按钮
            if (startGameButton != null)
            {
                startGameButton.interactable = true;
            }
            
            // 显示错误信息
            ShowToastSafely("加载游戏失败");
        }
    }
    
    /// <summary>
    /// 刷新存档信息（公共方法，供外部调用）
    /// </summary>
    [ContextMenu("刷新存档信息")]
    public void RefreshSaveInfoPublic()
    {
        RefreshSaveInfo();
    }
    
    /// <summary>
    /// 获取场景名称
    /// </summary>
    private string GetSceneName()
    {
        return gameplaySceneName;
    }
    
    /// <summary>
    /// 场景切换前的清理工作
    /// </summary>
    private void CleanupBeforeSceneTransition()
    {
        // 清理场景切换前的对象
        
        // 确保GameStartManager对象不会跟随到下一个场景
        // 由于GameStartManager没有使用DontDestroyOnLoad，理论上应该会被自动销毁
        // 但为了确保，我们可以主动标记为待销毁
        
        // 注意：不要在这里直接Destroy(gameObject)，因为还需要完成场景切换
        // Unity的SceneManager.LoadScene会自动清理当前场景的对象
    }
    
    
    /// <summary>
    /// TapTap SDK准备就绪回调
    /// </summary>
    private void OnTapTapSDKReady()
    {
        // Debug.Log("[GameStart] ===== TapTap SDK准备就绪回调被调用 =====");
        // Debug.Log("[GameStart] TapTap SDK准备就绪，立即检查登录状态");
        // Debug.Log($"[GameStart] SDK Ready时状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        
        // SDK准备就绪，立即检查登录状态
        CheckLoginStatusNow();
    }
    
    /// <summary>
    /// 立即检查登录状态
    /// </summary>
    private void CheckLoginStatusNow()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Debug.Log($"[GameStart] CheckLoginStatusNow开始 - 状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        
        if (loginCheckCompleted)
        {
            // Debug.Log("[GameStart] 登录检查已完成，跳过检查");
            return;
        }
        
        if (requireLogin && tapTapLoginManager != null)
        {
            // 检查是否已经登录
            bool isLoggedIn = tapTapLoginManager.IsLoggedIn;
            // Debug.Log($"[GameStart] TapTap登录状态检查: {isLoggedIn}");
            
            if (isLoggedIn)
            {
                // Debug.Log("[GameStart] 用户已登录，立即完成登录检查");
                var account = tapTapLoginManager.CurrentAccount;
                if (account != null)
                {
                    // Debug.Log($"[GameStart] 当前登录用户: {account.name}");
                }
                
                // 用户已登录，完成登录检查
                FinishLoginCheck();
            }
            else
            {
                // Debug.Log("[GameStart] 检测到未登录用户，立即弹出登录界面");
                
                // 设置为登录中状态
                if (loginPanel != null && loginPanel.activeInHierarchy)
                {
                    SetLoginPanelState(true);
                }
                
                // 弹出登录界面
                tapTapLoginManager.StartLoginAsync();
            }
        }
        else
        {
            // Debug.Log($"[GameStart] 跳过登录检查 - requireLogin={requireLogin}, tapTapLoginManager={tapTapLoginManager != null}");
            // 不需要登录，完成检查
            FinishLoginCheck();
        }
#endif
    }
    
    /// <summary>
    /// TapTap登录成功回调
    /// </summary>
    private void OnTapTapLoginSuccess(TapTapAccount account)
    {
        // Debug.Log("[GameStart] ===== OnTapTapLoginSuccess 回调被调用 =====");
        // Debug.Log($"[GameStart] TapTap登录成功 - 用户: {account.name}");
        // Debug.Log($"[GameStart] 当前状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        
        // 强制完成登录检查，隐藏登录面板，显示正常界面
        ForceFinishLoginCheck();
        
        // 确保开始游戏按钮可交互
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
        }
        
        // Debug.Log("[GameStart] ===== OnTapTapLoginSuccess 处理完成 =====");
    }
    
    /// <summary>
    /// TapTap登录失败回调
    /// </summary>
    private void OnTapTapLoginFailed(string error)
    {
        // Debug.LogWarning($"[GameStart] TapTap登录失败 - 错误: {error}");
        
        // 显示登录失败状态，显示重试按钮
        if (loginPanel != null && loginPanel.activeInHierarchy)
        {
            SetLoginPanelState(false);
        }
    }
    
    /// <summary>
    /// TapTap登录取消回调
    /// </summary>
    private void OnTapTapLoginCancelled()
    {
        // Debug.Log("[GameStart] 用户取消了TapTap登录");
        
        // 显示登录失败状态，显示重试按钮
        if (loginPanel != null && loginPanel.activeInHierarchy)
        {
            SetLoginPanelState(false);
        }
    }
    
    /// <summary>
    /// TapTap登出回调（包括合规认证退出和切换账号）
    /// </summary>
    private void OnTapTapLogout()
    {
        // Debug.Log("[GameStart] ===== TapTap用户登出回调被调用 =====");
        // Debug.Log("[GameStart] TapTap用户登出，重新显示登录面板");
        // Debug.Log($"[GameStart] 登出前状态: isCheckingLogin={isCheckingLogin}, loginCheckCompleted={loginCheckCompleted}");
        
        // 重置登录检查状态
        isCheckingLogin = false;
        loginCheckCompleted = false;
        
        // 重新开始登录检查
        StartLoginCheck();
        
        // 主动检查当前登录状态
        CheckCurrentLoginStatus();
        
        // 设置登录面板为失败状态，显示重试按钮
        if (loginPanel != null && loginPanel.activeInHierarchy)
        {
            SetLoginPanelState(false);
            // Debug.Log("[GameStart] 登录面板已设置为失败状态，显示重试按钮");
        }
        
        // Debug.Log("[GameStart] ===== TapTap登出回调处理完成 =====");
    }
    
    private void OnDestroy()
    {
        // 清理事件监听器
        if (tapTapLoginManager != null)
        {
            tapTapLoginManager.OnLoginSuccess.RemoveListener(OnTapTapLoginSuccess);
            tapTapLoginManager.OnSDKReady.RemoveListener(OnTapTapSDKReady);
            tapTapLoginManager.OnLoginFailed.RemoveListener(OnTapTapLoginFailed);
            tapTapLoginManager.OnLoginCancelled.RemoveListener(OnTapTapLoginCancelled);
            tapTapLoginManager.OnLogout.RemoveListener(OnTapTapLogout);
        }
    }
    
    /// <summary>
    /// 验证场景配置（编辑器中显示帮助信息）
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(gameplaySceneName))
        {
            // 检查场景是否在Build Settings中
            bool sceneInBuildSettings = false;
            
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (sceneName == gameplaySceneName && scene.enabled)
                {
                    sceneInBuildSettings = true;
                    break;
                }
            }
            
            if (!sceneInBuildSettings)
            {
                Debug.LogWarning($"场景 '{gameplaySceneName}' 未添加到Build Settings中，或已被禁用！", this);
            }
        }
#endif
    }

} 