using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 初次游戏宠物选择管理器 - 处理初次游戏时的宠物选择界面
/// 挂载位置：Start场景 -> GameStartManager对象下
/// 功能：检测是否初次游戏，控制宠物选择界面的显示/隐藏，处理宠物创建和场景切换
/// 配置要求：UI Canvas、宠物选择面板、3个宠物按钮、名字输入框、确认按钮、目标场景(SceneAsset)
/// </summary>
public class FirstTimePetSelectionManager : MonoBehaviour
{
    [Header("UI引用配置")]
    [SerializeField] private Canvas uiCanvas;                     // UI画布引用（拖拽Canvas对象）
    [SerializeField] private GameObject petSelectionPanel;        // 宠物选择面板（拖拽ChoosePet_Newlayer对象）
    [SerializeField] private Button[] petButtons = new Button[3]; // 三个宠物按钮（拖拽PetBtn_1, PetBtn_2, PetBtn_3）
    [SerializeField] private InputField petNameInput;             // 宠物名字输入框（拖拽NameInputField）
    [SerializeField] private Button confirmButton;                // 确认选择按钮
    [SerializeField] private GameObject petSelectionIndicator;    // 宠物选择指示器（类似工具包的选择框）
    
    [Header("场景切换")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset gameplayScene;   // 游戏场景引用
#endif
    
    // 当前选中的宠物索引
    private int selectedPetIndex = -1;
    
    // 是否是初次选择
    private bool isFirstTimeSelection = false;
    
    // 从数据库加载的初始宠物配置
    private List<PetConfigData> starterPets = new List<PetConfigData>();
    
    // 运行时场景名称（从SceneAsset获取）
    private string gameplaySceneName;
    
    private void Start()
    {
        // 验证UI引用
        if (!ValidateUIReferences())
        {
            Debug.LogError("FirstTimePetSelectionManager: UI引用配置不完整，请检查Inspector配置");
            return;
        }
        
        // 更新场景名称
        UpdateSceneName();
        
        // 加载宠物配置
        LoadPetConfigs();
        
        InitializeUI();
        
        // 初始时隐藏宠物选择面板，等待GameStartManager调用
        HidePetSelectionPanel();
        // 初始化完成
    }
    
    /// <summary>
    /// 更新场景名称（从SceneAsset获取）
    /// </summary>
    private void UpdateSceneName()
    {
#if UNITY_EDITOR
        if (gameplayScene != null)
        {
            string scenePath = UnityEditor.AssetDatabase.GetAssetPath(gameplayScene);
            gameplaySceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
#endif
        
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            gameplaySceneName = "Gameplay"; // 默认值
            Debug.LogWarning("FirstTimePetSelectionManager: 游戏场景未配置，使用默认值 'Gameplay'");
        }
        else
        {
            // 目标场景已设置
        }
    }
    
    /// <summary>
    /// 从数据库加载宠物配置
    /// </summary>
    private void LoadPetConfigs()
    {
        if (PetDatabaseManager.Instance != null && PetDatabaseManager.Instance.IsDatabaseLoaded())
        {
            starterPets = PetDatabaseManager.Instance.GetStarterPets();
            
            if (starterPets.Count < 3)
            {
                Debug.LogError($"FirstTimePetSelectionManager: 初始宠物配置不足，需要至少3个，当前只有{starterPets.Count}个");
                // 如果数据库配置不足，使用临时配置作为后备
                CreateTemporaryPetConfigs();
            }
            else
            {
                // 成功从数据库加载宠物配置
            }
        }
        else
        {
            Debug.LogError("FirstTimePetSelectionManager: 宠物数据库未加载，使用临时配置");
            // 数据库未加载时使用临时配置
            CreateTemporaryPetConfigs();
        }
    }
    
    /// <summary>
    /// 创建临时宠物配置（等待数据库系统编译完成后移除）
    /// </summary>
    private void CreateTemporaryPetConfigs()
    {
        starterPets = new List<PetConfigData>();
        
        // 临时创建三个宠物配置
        var catBrown = new PetConfigData
        {
            petId = "cat_brown_001",
            petName = "小猫咪",
            petPrefab = Resources.Load<GameObject>("Pet/Pet_CatBrown"),
            description = "一只可爱的棕色小猫咪",
            petType = PetType.Cat,
            isStarterPet = true,
            baseEnergy = 80f,
            baseSatiety = 70f
        };
        
        var dog = new PetConfigData
        {
            petId = "dog_001",
            petName = "小狗狗",
            petPrefab = Resources.Load<GameObject>("Pet/Pet_Dog"),
            description = "一只忠诚的小狗狗",
            petType = PetType.Dog,
            isStarterPet = true,
            baseEnergy = 85f,
            baseSatiety = 75f
        };
        
        var catWhite = new PetConfigData
        {
            petId = "cat_white_001",
            petName = "小白猫",
            petPrefab = Resources.Load<GameObject>("Pet/Pet_CatWhite"),
            description = "一只优雅的白色小猫咪",
            petType = PetType.Cat,
            isStarterPet = true,
            baseEnergy = 75f,
            baseSatiety = 65f
        };
        
        starterPets.Add(catBrown);
        starterPets.Add(dog);
        starterPets.Add(catWhite);
        
                    // 使用临时配置
    }
    
    /// <summary>
    /// 验证UI引用是否配置完整
    /// </summary>
    private bool ValidateUIReferences()
    {
        if (uiCanvas == null)
        {
            Debug.LogError("FirstTimePetSelectionManager: 缺少Canvas引用");
            return false;
        }
        
        if (petSelectionPanel == null)
        {
            Debug.LogError("FirstTimePetSelectionManager: 缺少宠物选择面板引用");
            return false;
        }
        
        if (petButtons == null || petButtons.Length != 3)
        {
            Debug.LogError("FirstTimePetSelectionManager: 宠物按钮配置不正确，需要3个按钮");
            return false;
        }
        
        if (petNameInput == null)
        {
            Debug.LogError("FirstTimePetSelectionManager: 缺少名字输入框引用");
            return false;
        }
        
        if (confirmButton == null)
        {
            Debug.LogError("FirstTimePetSelectionManager: 缺少确认按钮引用");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置宠物按钮事件
        for (int i = 0; i < petButtons.Length; i++)
        {
            if (petButtons[i] != null)
            {
                int index = i; // 闭包变量
                petButtons[i].onClick.AddListener(() => OnPetButtonClicked(index));
            }
        }
        
        // 设置确认按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false; // 初始不可点击
        }
        
        // 设置输入框事件
        if (petNameInput != null)
        {
            petNameInput.onValueChanged.AddListener(OnNameInputChanged);
            petNameInput.characterLimit = 10; // 限制名字长度
        }
        
        // 初始化选择指示器
        if (petSelectionIndicator != null)
        {
            petSelectionIndicator.SetActive(false); // 初始隐藏
        }
        
        // 设置宠物按钮头像
        UpdatePetButtonImages();
    }
    
    /// <summary>
    /// 检查是否是初次游戏
    /// </summary>
    private void CheckIfFirstTime()
    {
        // 检查是否有存档
        SaveFileInfo saveInfo = SaveManager.Instance.GetSaveFileInfo();
        
        if (saveInfo == null || !saveInfo.exists || saveInfo.petCount == 0)
        {
            // 没有存档或没有宠物，显示宠物选择界面
            isFirstTimeSelection = true;
            ShowPetSelectionPanel();
        }
        else
        {
            // 已有存档，隐藏宠物选择界面
            isFirstTimeSelection = false;
            HidePetSelectionPanel();
        }
    }
    
    /// <summary>
    /// 更新宠物按钮头像
    /// </summary>
    private void UpdatePetButtonImages()
    {
        if (starterPets == null || petButtons == null) return;
        
        for (int i = 0; i < petButtons.Length && i < starterPets.Count; i++)
        {
            if (petButtons[i] != null && starterPets[i].headIconImage != null)
            {
                // 获取按钮的Image组件
                Image buttonImage = petButtons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = starterPets[i].headIconImage;
                    // 设置宠物按钮头像
                }
                else
                {
                    Debug.LogWarning($"宠物按钮 {i} 没有Image组件");
                }
            }
        }
    }
    
    /// <summary>
    /// 显示宠物选择面板
    /// </summary>
    public void ShowPetSelectionPanel()
    {
        if (petSelectionPanel != null)
        {
            petSelectionPanel.SetActive(true);
        }
        
        // 重置选择状态
        selectedPetIndex = -1;
        
        // 隐藏选择指示器
        if (petSelectionIndicator != null)
        {
            petSelectionIndicator.SetActive(false);
        }
        
        UpdateButtonStates();
        
        // 清空名字输入框
        if (petNameInput != null)
        {
            petNameInput.text = "";
        }
        
        // 更新宠物按钮头像
        UpdatePetButtonImages();
    }
    
    /// <summary>
    /// 隐藏宠物选择面板
    /// </summary>
    public void HidePetSelectionPanel()
    {
        if (petSelectionPanel != null)
        {
            petSelectionPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 宠物按钮点击事件
    /// </summary>
    private void OnPetButtonClicked(int petIndex)
    {
        if (petIndex < 0 || petIndex >= starterPets.Count)
        {
            Debug.LogError($"无效的宠物索引: {petIndex}");
            return;
        }
        
        selectedPetIndex = petIndex;
        
        // 显示选择指示器在选中的宠物按钮上
        if (petSelectionIndicator != null && petButtons[petIndex] != null)
        {
            petSelectionIndicator.SetActive(true);
            petSelectionIndicator.transform.position = petButtons[petIndex].transform.position;
        }
        
        // 更新按钮状态（高亮选中的按钮）
        UpdateButtonStates();
        
        // 设置默认名字
        if (petNameInput != null)
        {
            petNameInput.text = starterPets[petIndex].petName;
        }
        
        // 检查是否可以确认
        UpdateConfirmButton();
        
        // 宠物已选择
    }
    
    /// <summary>
    /// 名字输入框变化事件
    /// </summary>
    private void OnNameInputChanged(string newName)
    {
        // 实时验证名字输入
        ValidateNameInput(newName);
        
        // 更新确认按钮状态
        UpdateConfirmButton();
    }
    
    /// <summary>
    /// 验证名字输入
    /// </summary>
    private void ValidateNameInput(string name)
    {
        if (petNameInput == null) return;
        
        // 去除首尾空格
        string trimmedName = name.Trim();
        
        // 检查长度限制
        if (trimmedName.Length > 10)
        {
            // 截断超出的部分
            petNameInput.text = trimmedName.Substring(0, 10);
        }
        
        // 检查是否包含特殊字符（可选）
        // 这里可以添加更多验证逻辑
    }
    
    /// <summary>
    /// 确认选择按钮点击事件
    /// </summary>
    private void OnConfirmClicked()
    {
        if (selectedPetIndex < 0 || selectedPetIndex >= starterPets.Count)
        {
            Debug.LogError("无效的宠物选择索引");
            return;
        }
        
        string petName = petNameInput != null ? petNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(petName))
        {
            petName = starterPets[selectedPetIndex].petName;
        }
        
        // 禁用按钮防止重复点击
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }
        
        // 创建宠物并开始游戏
        StartCoroutine(CreatePetAndStartGame(starterPets[selectedPetIndex], petName));
    }
    
    /// <summary>
    /// 创建宠物并开始游戏
    /// </summary>
    private IEnumerator CreatePetAndStartGame(PetConfigData petConfig, string petName)
    {
        // 开始创建宠物
        
        // 确保存档系统已初始化
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager未初始化");
            
            // 重新启用确认按钮
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
            yield break;
        }
        
        bool success = false;
        
        try
        {
            // 加载当前存档或创建新存档
            SaveData saveData = SaveManager.Instance.LoadSave();
            
            // 创建新宠物数据
            PetSaveData newPetData = new PetSaveData(petConfig.petPrefab?.name ?? "Unknown");
            newPetData.displayName = petName;
            newPetData.position = Vector3.zero; // 使用默认位置，由PetSpawner统一管理
            
            // 从配置设置初始属性
            newPetData.energy = (int)petConfig.baseEnergy;
            newPetData.satiety = (int)petConfig.baseSatiety;
            
            // 添加到存档
            saveData.petsData.Add(newPetData);
            
            // 保存存档
            bool saveSuccess = SaveManager.Instance.Save();
            
            if (saveSuccess)
            {
                // 宠物创建成功
                success = true;
            }
            else
            {
                Debug.LogError("保存存档失败");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建宠物时出错: {e.Message}");
        }
        
        if (success)
        {
            // 等待一帧确保保存完成
            yield return null;
            
            // 确保UIManager存在（只在真正进入游戏时创建）
            EnsureUIManagerExists();
            
            // 在场景切换前清理
            CleanupBeforeSceneTransition();
            
            // 切换到游戏场景
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            // 重新启用确认按钮
            if (confirmButton != null)
            {
                confirmButton.interactable = true;
            }
        }
    }
    
    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        for (int i = 0; i < petButtons.Length; i++)
        {
            if (petButtons[i] != null)
            {
                // 可以在这里添加视觉反馈，比如改变颜色或添加边框
                // 暂时只是设置按钮的交互状态
                ColorBlock colors = petButtons[i].colors;
                
                if (i == selectedPetIndex)
                {
                    // 选中状态 - 使用选中颜色
                    colors.normalColor = colors.selectedColor;
                }
                else
                {
                    // 未选中状态 - 使用正常颜色
                    colors.normalColor = Color.white;
                }
                
                petButtons[i].colors = colors;
            }
        }
    }
    
    /// <summary>
    /// 更新确认按钮状态
    /// </summary>
    private void UpdateConfirmButton()
    {
        if (confirmButton != null)
        {
            // 检查是否已选择宠物且输入了名字
            bool canConfirm = selectedPetIndex >= 0 && selectedPetIndex < starterPets.Count;
            
            if (canConfirm && petNameInput != null)
            {
                string name = petNameInput.text.Trim();
                canConfirm = !string.IsNullOrEmpty(name);
            }
            
            confirmButton.interactable = canConfirm;
        }
    }
    

    
    /// <summary>
    /// 公共方法：强制显示宠物选择界面（供其他脚本调用）
    /// </summary>
    public void ForceShowPetSelection()
    {
        isFirstTimeSelection = true;
        ShowPetSelectionPanel();
    }
    
    /// <summary>
    /// 检查是否应该显示宠物选择界面
    /// </summary>
    public bool ShouldShowPetSelection()
    {
        return isFirstTimeSelection;
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
            Debug.LogWarning("FirstTimePetSelectionManager: 未找到UIManager预制体，请确保Resources/Prefab/Manager/UIManager.prefab存在");
        }
    }
    
    /// <summary>
    /// 场景切换前的清理工作
    /// </summary>
    private void CleanupBeforeSceneTransition()
    {
        // 清理场景切换前的对象
        
        // 确保FirstTimePetSelectionManager对象不会跟随到下一个场景
        // 由于FirstTimePetSelectionManager没有使用DontDestroyOnLoad，理论上应该会被自动销毁
    }
} 