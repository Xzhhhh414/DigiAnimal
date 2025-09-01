using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// DefaultFurnitureConfig 相关类已移动到单独的脚本文件 DefaultFurnitureConfigAsset.cs



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
    
    [Header("输入限制")]
    [SerializeField] private int maxNameLength = 10;                  // 宠物名字最大显示长度（中文=2，英文=1）
    
    [Header("Toast提示文本")]
    [SerializeField] private string emptyNameMessage = "宠物名字不能为空";
    [SerializeField] private string nameTooLongMessage = "宠物名字过长，当前长度：{0}，最大长度：{1}";
    
    [Header("场景切换")]
    [SerializeField] private string gameplaySceneName = "Gameplay";   // 游戏场景名称
    
    [Header("默认家具配置")]
    [SerializeField] private DefaultFurnitureConfigAsset defaultFurnitureConfig; // ScriptableObject配置文件
    
    // 当前选中的宠物索引
    private int selectedPetIndex = -1;
    
    // 是否是初次选择
    private bool isFirstTimeSelection = false;
    
    // 从数据库加载的初始宠物配置
    private List<PetConfigData> starterPets = new List<PetConfigData>();
    

    
    private void Start()
    {
        // 验证UI引用
        if (!ValidateUIReferences())
        {
            Debug.LogError("FirstTimePetSelectionManager: UI引用配置不完整，请检查Inspector配置");
            return;
        }
        
        // 验证场景名称
        ValidateSceneName();
        
        // 加载宠物配置
        LoadPetConfigs();
        
        InitializeUI();
        
        // 初始时隐藏宠物选择面板，等待GameStartManager调用
        HidePetSelectionPanel();
        // 初始化完成
    }
    
    /// <summary>
    /// 验证场景名称
    /// </summary>
    private void ValidateSceneName()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            gameplaySceneName = "Gameplay"; // 默认值
            Debug.LogWarning("FirstTimePetSelectionManager: 游戏场景名称未配置，使用默认值 'Gameplay'");
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
            introduction = "一只可爱的棕色小猫咪",
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
            introduction = "一只忠诚的小狗狗",
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
            introduction = "一只优雅的白色小猫咪",
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
        
        // 设置输入框事件（不设置characterLimit，使用自定义长度验证）
        if (petNameInput != null)
        {
            petNameInput.onValueChanged.AddListener(OnNameInputChanged);
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
        
        // 更新宠物按钮头像
        UpdatePetButtonImages();
        
        // 默认选择第一个宠物
        if (starterPets != null && starterPets.Count > 0)
        {
            OnPetButtonClicked(0); // 自动选择第一个宠物
        }
        else
        {
            // 如果没有宠物配置，重置选择状态
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
        }
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
    /// 计算字符串的显示长度（中文字符=2，英文字符=1）
    /// </summary>
    /// <param name="text">要计算的字符串</param>
    /// <returns>显示长度</returns>
    private int GetDisplayLength(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
            
        int length = 0;
        foreach (char c in text)
        {
            // 判断是否为中文字符（基本汉字范围）
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                length += 2; // 中文字符占2个长度
            }
            else
            {
                length += 1; // 其他字符占1个长度
            }
        }
        return length;
    }
    
    /// <summary>
    /// 验证名字输入（不再自动截断，允许用户继续输入）
    /// </summary>
    private void ValidateNameInput(string name)
    {
        // 现在不再自动截断输入，允许用户继续输入
        // 验证将在确认时进行并显示Toast提示
        
        // 检查是否包含特殊字符（可选）
        // 这里可以添加更多验证逻辑
    }
    
    /// <summary>
    /// 截断字符串到指定的显示长度
    /// </summary>
    /// <param name="text">原始字符串</param>
    /// <param name="maxDisplayLength">最大显示长度</param>
    /// <returns>截断后的字符串</returns>
    private string TruncateToDisplayLength(string text, int maxDisplayLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        int currentLength = 0;
        int charIndex = 0;
        
        foreach (char c in text)
        {
            int charLength = (c >= 0x4E00 && c <= 0x9FFF) ? 2 : 1;
            
            if (currentLength + charLength > maxDisplayLength)
            {
                break;
            }
            
            currentLength += charLength;
            charIndex++;
        }
        
        return text.Substring(0, charIndex);
    }
    
    /// <summary>
    /// 获取名字验证错误消息
    /// </summary>
    /// <param name="name">宠物名字</param>
    /// <returns>错误消息，如果验证通过则返回null</returns>
    private string GetNameValidationMessage(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return emptyNameMessage;
        }
        
        int nameLength = GetDisplayLength(name);
        if (nameLength > maxNameLength)
        {
            return string.Format(nameTooLongMessage, nameLength, maxNameLength);
        }
        
        return null; // 验证通过
    }
    
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    /// <param name="message">提示消息</param>
    private void ShowToast(string message)
    {
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(message);
        }
        else
        {
            Debug.LogWarning($"未找到ToastManager，消息：{message}");
        }
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
        
        // 验证名字长度并显示Toast提示
        string validationMessage = GetNameValidationMessage(petName);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            ShowToast(validationMessage);
            return;
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
            newPetData.introduction = petConfig.introduction; // 设置宠物介绍
            newPetData.position = Vector3.zero; // 使用默认位置，由PetSpawner统一管理
            
            // 从配置设置初始属性
            newPetData.energy = (int)petConfig.baseEnergy;
            newPetData.satiety = (int)petConfig.baseSatiety;
            
            // 添加到存档
            saveData.petsData.Add(newPetData);
            
            // 设置新玩家的默认系统设置和默认家具
            if (saveData.petsData.Count == 1) // 如果这是第一个宠物（新账号）
            {
                // 系统设置
                saveData.playerData.dynamicIslandEnabled = true; // 默认开启灵动岛
                saveData.playerData.selectedDynamicIslandPetId = newPetData.petId; // 默认选择第一个宠物
                saveData.playerData.lockScreenWidgetEnabled = false; // 锁屏小组件默认关闭
                saveData.playerData.selectedLockScreenPetId = newPetData.petId; // 默认选择第一个宠物
                
                // 创建默认家具数据（新账号）
                CreateDefaultFurnitureData(saveData);
                
                // Debug.Log($"[FirstTimePetSelectionManager] 设置新玩家默认系统设置和家具");
            }
            else
            {
                // 老账号：检查并补充缺失的默认家具
                SupplementMissingDefaultFurniture(saveData);
                
                // Debug.Log($"[FirstTimePetSelectionManager] 为老账号补充缺失的默认家具");
            }
            
            // 保存前的数据检查
            // Debug.Log($"[FirstTimePetSelectionManager] 保存前数据检查 - 植物:{saveData.worldData?.plants?.Count ?? -1}, 食物:{saveData.worldData?.foods?.Count ?? -1}");
            
            // 将修改后的数据设置回SaveManager
            SaveManager.Instance.SetCurrentSaveData(saveData);
            
            // 保存存档
            bool saveSuccess = SaveManager.Instance.Save();
            
            if (saveSuccess)
            {
                // 宠物创建成功
                success = true;
                // Debug.Log("[FirstTimePetSelectionManager] 存档保存成功");
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
                canConfirm = !string.IsNullOrEmpty(name); // 移除长度限制检查，允许点击确认
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
    /// 公共方法：为现有账号补充缺失的默认家具
    /// 这个方法应该在游戏启动时调用，用于为老账号添加新版本中的默认家具
    /// </summary>
    public static void SupplementMissingDefaultFurnitureForExistingAccount()
    {
        // 检查是否有存档
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[FirstTimePetSelectionManager] SaveManager未初始化，无法补充默认家具");
            return;
        }
        
        SaveData saveData = SaveManager.Instance.LoadSave();
        if (saveData == null || saveData.petsData == null || saveData.petsData.Count == 0)
        {
            //Debug.Log("[FirstTimePetSelectionManager] 无存档或无宠物数据，跳过补充默认家具");
            return;
        }
        
        // 查找场景中的FirstTimePetSelectionManager实例来获取默认家具配置
        FirstTimePetSelectionManager instance = FindObjectOfType<FirstTimePetSelectionManager>();
        if (instance == null)
        {
            //Debug.Log("[FirstTimePetSelectionManager] 未找到FirstTimePetSelectionManager实例，跳过补充默认家具");
            return;
        }
        
        // 调用实例方法进行补充
        bool hasChanges = instance.SupplementMissingDefaultFurnitureInternal(saveData);
        
        if (hasChanges)
        {
            // 保存修改后的存档
            SaveManager.Instance.SetCurrentSaveData(saveData);
            bool saveSuccess = SaveManager.Instance.Save();
            
            if (saveSuccess)
            {
                //Debug.Log("[FirstTimePetSelectionManager] 默认家具补充完成并保存成功");
            }
            else
            {
                //Debug.LogError("[FirstTimePetSelectionManager] 默认家具补充保存失败");
            }
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
    
    /// <summary>
    /// 创建默认家具数据（新账号使用）
    /// </summary>
    private void CreateDefaultFurnitureData(SaveData saveData)
    {
        // 确保worldData存在
        if (saveData.worldData == null)
        {
            saveData.worldData = new WorldSaveData();
        }
        
        // 确保plants列表存在
        if (saveData.worldData.plants == null)
        {
            saveData.worldData.plants = new List<PlantSaveData>();
        }
        
        // 确保foods列表存在
        if (saveData.worldData.foods == null)
        {
            saveData.worldData.foods = new List<FoodSaveData>();
        }
        
        // 确保speakers列表存在
        if (saveData.worldData.speakers == null)
        {
            saveData.worldData.speakers = new List<SpeakerSaveData>();
        }
        
        // 确保tvs列表存在
        if (saveData.worldData.tvs == null)
        {
            saveData.worldData.tvs = new List<TVSaveData>();
        }
        
        // 根据配置创建家具数据
        var furnitureConfigList = GetDefaultFurnitureList();
        //Debug.Log($"[FirstTimePetSelectionManager] 获取到 {furnitureConfigList?.Count ?? 0} 个默认家具配置");
        
        if (furnitureConfigList != null && furnitureConfigList.Count > 0)
        {
            foreach (var furnitureConfig in furnitureConfigList)
            {
                CreateFurnitureByConfigId(saveData, furnitureConfig);
            }
        }
        else
        {
            Debug.LogError("[FirstTimePetSelectionManager] 无法加载默认家具配置！请检查DefaultFurnitureConfig.asset文件是否存在且配置正确");
            // 不创建任何默认家具，让开发者知道配置有问题
        }
        
        // Debug.Log($"[FirstTimePetSelectionManager] 创建默认家具数据完成 - 植物:{saveData.worldData.plants.Count}, 食物:{saveData.worldData.foods.Count}");
    }
    
    /// <summary>
    /// 根据ConfigId创建对应的家具数据
    /// </summary>
    private void CreateFurnitureByConfigId(SaveData saveData, DefaultFurnitureConfig config)
    {
        if (string.IsNullOrEmpty(config.furnitureConfigId))
        {
            Debug.LogWarning("[FirstTimePetSelectionManager] 家具ConfigId为空，跳过创建");
            return;
        }
        
        //Debug.Log($"[FirstTimePetSelectionManager] 正在创建家具: DefaultId={config.saveDataId}, ConfigId={config.furnitureConfigId}, Position={config.position}");
        
        string furnitureId = GenerateUniqueFurnitureId(saveData);
        
        // 根据ConfigId判断家具类型并创建对应的存档数据
        if (config.furnitureConfigId.ToLower().Contains("plant"))
        {
            // 创建植物数据
            // 参数顺序：id, configId, saveDataId, health, position, cost, recovery
            PlantSaveData plantData = new PlantSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, 100, config.position, 0, 25);
            plantData.lastHealthUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            saveData.worldData.plants.Add(plantData);
            //Debug.Log($"[FirstTimePetSelectionManager] 创建植物数据: 默认植物 (ConfigId: {config.furnitureConfigId}, DefaultId: {config.saveDataId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("food"))
        {
            // 创建食物数据
            // 参数顺序：id, type, config, defId, empty, pos, tastyValue, satietyValue
            FoodSaveData foodData = new FoodSaveData(furnitureId, "猫粮", config.furnitureConfigId, config.saveDataId, false, config.position, 3, 25);
            saveData.worldData.foods.Add(foodData);
            //Debug.Log($"[FirstTimePetSelectionManager] 创建食物数据: 猫粮 (ConfigId: {config.furnitureConfigId}, DefaultId: {config.saveDataId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("speaker"))
        {
            // 创建音响数据
            // 参数顺序：id, configId, saveDataId, position, trackIndex, pauseTime, playing
            SpeakerSaveData speakerData = new SpeakerSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, config.position, 0, 0f, false);
            saveData.worldData.speakers.Add(speakerData);
            //Debug.Log($"[FirstTimePetSelectionManager] 创建音响数据: 音响 (ConfigId: {config.furnitureConfigId}, DefaultId: {config.saveDataId}) at {config.position}");
        }
        else if (config.furnitureConfigId.ToLower().Contains("tv"))
        {
            // 创建电视机数据
            // 参数顺序：id, configId, saveDataId, position, isOn
            TVSaveData tvData = new TVSaveData(furnitureId, config.furnitureConfigId, config.saveDataId, config.position, false);
            saveData.worldData.tvs.Add(tvData);
            //Debug.Log($"[FirstTimePetSelectionManager] 创建电视机数据: 电视机 (ConfigId: {config.furnitureConfigId}, DefaultId: {config.saveDataId}) at {config.position}");
        }
        else
        {
            Debug.LogWarning($"[FirstTimePetSelectionManager] 未知的家具类型: {config.furnitureConfigId}");
        }
    }
    
    /// <summary>
    /// 生成唯一的家具ID
    /// </summary>
    private string GenerateUniqueFurnitureId(SaveData saveData)
    {
        string id = $"furniture_{saveData.nextFurnitureIdCounter}";
        saveData.nextFurnitureIdCounter++;
        return id;
    }
    
    /// <summary>
    /// 为老账号补充缺失的默认家具（私有方法，用于创建新宠物时调用）
    /// </summary>
    private void SupplementMissingDefaultFurniture(SaveData saveData)
    {
        SupplementMissingDefaultFurnitureInternal(saveData);
    }
    
    /// <summary>
    /// 为老账号补充缺失的默认家具的内部实现
    /// </summary>
    /// <param name="saveData">存档数据</param>
    /// <returns>是否有添加新家具</returns>
    private bool SupplementMissingDefaultFurnitureInternal(SaveData saveData)
    {
        var furnitureConfigList = GetDefaultFurnitureList();
        if (furnitureConfigList == null || furnitureConfigList.Count == 0)
        {
            //Debug.Log("[FirstTimePetSelectionManager] 没有配置默认家具列表，跳过补充");
            return false;
        }
        
        // 确保worldData和相关列表存在
        EnsureWorldDataExists(saveData);
        
        int addedCount = 0;
        
        // 遍历每个默认家具配置
        foreach (var defaultConfig in furnitureConfigList)
        {
            if (string.IsNullOrEmpty(defaultConfig.furnitureConfigId))
                continue;
                
            // 检查是否已存在该saveDataId的默认家具
            if (!HasFurnitureWithDefaultId(saveData, defaultConfig.saveDataId))
            {
                // 缺失该默认家具，需要补充
                CreateFurnitureByConfigId(saveData, defaultConfig);
                addedCount++;
                
                //Debug.Log($"[FirstTimePetSelectionManager] 为老账号补充家具: {defaultConfig.saveDataId} (ConfigId: {defaultConfig.furnitureConfigId}) at {defaultConfig.position}");
            }
        }
        
        if (addedCount > 0)
        {
            //Debug.Log($"[FirstTimePetSelectionManager] 老账号补充完成，共添加 {addedCount} 个家具");
            return true;
        }
        else
        {
            //Debug.Log("[FirstTimePetSelectionManager] 老账号无需补充家具");
            return false;
        }
    }
    
    /// <summary>
    /// 检查存档中是否已存在指定saveDataId的默认家具
    /// </summary>
    private bool HasFurnitureWithDefaultId(SaveData saveData, string saveDataId)
    {
        if (string.IsNullOrEmpty(saveDataId))
            return false;
            
        // 检查植物
        if (saveData.worldData?.plants != null)
        {
            foreach (var plant in saveData.worldData.plants)
            {
                if (!string.IsNullOrEmpty(plant.saveDataId) && plant.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查食物
        if (saveData.worldData?.foods != null)
        {
            foreach (var food in saveData.worldData.foods)
            {
                if (!string.IsNullOrEmpty(food.saveDataId) && food.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查音响
        if (saveData.worldData?.speakers != null)
        {
            foreach (var speaker in saveData.worldData.speakers)
            {
                if (!string.IsNullOrEmpty(speaker.saveDataId) && speaker.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        // 检查电视机
        if (saveData.worldData?.tvs != null)
        {
            foreach (var tv in saveData.worldData.tvs)
            {
                if (!string.IsNullOrEmpty(tv.saveDataId) && tv.saveDataId == saveDataId)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取默认家具配置列表（供其他类使用）
    /// </summary>
    public List<DefaultFurnitureConfig> GetDefaultFurnitureList()
    {
        // 首先尝试从Inspector配置的ScriptableObject加载
        if (defaultFurnitureConfig != null)
        {
            var items = defaultFurnitureConfig.GetDefaultFurnitureItems();
            if (items != null && items.Count > 0)
            {
                //Debug.Log($"[FirstTimePetSelectionManager] 从Inspector配置获取到 {items.Count} 个默认家具配置");
                return items;
            }
        }
        
        // 如果Inspector配置无效，尝试从Resources加载
        Debug.LogWarning("[FirstTimePetSelectionManager] Inspector配置无效，尝试从Resources加载");
        
        DefaultFurnitureConfigAsset resourceConfig = Resources.Load<DefaultFurnitureConfigAsset>("Data/DefaultFurnitureConfig");
        if (resourceConfig != null)
        {
            var items = resourceConfig.GetDefaultFurnitureItems();
            if (items != null && items.Count > 0)
            {
                //Debug.Log($"[FirstTimePetSelectionManager] 从Resources获取到 {items.Count} 个默认家具配置");
                return items;
            }
        }
        
        // 如果都失败了，返回空列表并记录错误
        Debug.LogError("[FirstTimePetSelectionManager] 无法从任何源加载默认家具配置！");
        Debug.LogError("请检查：1. Inspector中是否正确配置了DefaultFurnitureConfig引用 2. Resources/Data/DefaultFurnitureConfig.asset是否存在");
        return new List<DefaultFurnitureConfig>();
    }
    
    /// <summary>
    /// 确保WorldData和相关列表存在
    /// </summary>
    private void EnsureWorldDataExists(SaveData saveData)
    {
        // 确保worldData存在
        if (saveData.worldData == null)
        {
            saveData.worldData = new WorldSaveData();
        }
        
        // 确保各种家具列表存在
        if (saveData.worldData.plants == null)
        {
            saveData.worldData.plants = new List<PlantSaveData>();
        }
        
        if (saveData.worldData.foods == null)
        {
            saveData.worldData.foods = new List<FoodSaveData>();
        }
        
        if (saveData.worldData.speakers == null)
        {
            saveData.worldData.speakers = new List<SpeakerSaveData>();
        }
        
        if (saveData.worldData.tvs == null)
        {
            saveData.worldData.tvs = new List<TVSaveData>();
        }
    }
} 