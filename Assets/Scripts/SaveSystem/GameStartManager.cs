using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0414 // 禁用"字段被赋值但从未使用"警告

/// <summary>
/// GameStart场景管理器 - 处理开始游戏界面和存档显示
/// </summary>
public class GameStartManager : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button startGameButton;          // 开始游戏按钮（永远显示）
    [SerializeField] private GameObject saveInfoPanel;        // 存档信息面板（有存档时显示）
    [SerializeField] private Button deleteSaveButton;         // 删除存档按钮（在存档信息面板内）
    [SerializeField] private Text saveInfoText;               // 存档信息显示文本（在存档信息面板内）
    
    [Header("场景设置")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplayScene;        // Gameplay场景资源（拖拽配置）
#else
    [SerializeField] private Object gameplayScene;            // 运行时使用Object类型
#endif
    
    [Header("文本设置")]
    [SerializeField] private string saveInfoFormat = "{0}";
    
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
        
        Debug.Log($"存档信息已更新: 宠物数量={saveInfo.petCount}, 爱心货币={saveInfo.heartCurrency}");
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
        
        Debug.Log("隐藏存档信息面板 - 无存档状态");
    }
    
    /// <summary>
    /// 开始游戏按钮点击事件
    /// </summary>
    private void OnStartGameClicked()
    {
        //Debug.Log("开始游戏按钮被点击");
        
        // 禁用按钮防止重复点击
        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }
        
        // 加载游戏场景
        LoadGameplayScene();
    }
    
    /// <summary>
    /// 删除存档按钮点击事件
    /// </summary>
    private void OnDeleteSaveClicked()
    {
        //Debug.Log("删除存档按钮被点击");
        
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
            Debug.Log("存档删除成功");
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
                Debug.Log($"[Toast消息] {message}");
            }
        }
        catch (System.Exception e)
        {
            // 如果出现任何错误，优雅地处理
            Debug.LogWarning($"显示Toast消息时出错: {e.Message}，消息内容: {message}");
        }
    }
    
    /// <summary>
    /// 加载游戏场景
    /// </summary>
    private void LoadGameplayScene()
    {
        if (gameplayScene == null)
        {
            Debug.LogError("Gameplay场景未配置！请在Inspector中拖拽场景文件到gameplayScene字段");
            
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
            string sceneName = GetSceneName();
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new Exception("无法获取场景名称");
            }
            
            Debug.Log($"正在加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
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
        if (gameplayScene == null) return null;
        
#if UNITY_EDITOR
        return gameplayScene.name;
#else
        return gameplayScene.name;
#endif
    }
    
    /// <summary>
    /// 验证场景配置（编辑器中显示帮助信息）
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (gameplayScene != null)
        {
            // 检查场景是否在Build Settings中
            string scenePath = AssetDatabase.GetAssetPath(gameplayScene);
            bool sceneInBuildSettings = false;
            
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath && scene.enabled)
                {
                    sceneInBuildSettings = true;
                    break;
                }
            }
            
            if (!sceneInBuildSettings)
            {
                Debug.LogWarning($"场景 '{gameplayScene.name}' 未添加到Build Settings中，或已被禁用！", this);
            }
        }
#endif
    }

} 