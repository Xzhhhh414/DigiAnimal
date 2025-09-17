using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

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
    
    [Header("场景设置")]
    [SerializeField] private string gameplaySceneName = "Gameplay";        // Gameplay场景名称
    
    [Header("过渡动画")]
    [SerializeField] private GameObject transitionOverlayPrefab;  // 过渡动画预制体
    
    [Header("文本设置")]
    [SerializeField] private string saveInfoFormat = "{0}";
    
    // 过渡动画控制器
    // private TransitionController transitionController;
    
    private void Start()
    {
        InitializeUI();
        RefreshSaveInfo();
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
        
        // 启用删除存档按钮
        if (deleteSaveButton != null)
        {
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
        
        // 隐藏存档信息面板
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
        // 删除存档按钮被点击
        
        // 可以添加确认对话框
        if (ShowDeleteConfirmation())
        {
            DeleteSave();
        }
    }
    
    /// <summary>
    /// 显示删除确认对话框
    /// </summary>
    private bool ShowDeleteConfirmation()
    {
        // 这里可以实现一个确认对话框
        // 暂时直接返回true，实际项目中应该显示UI对话框
        return true;
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