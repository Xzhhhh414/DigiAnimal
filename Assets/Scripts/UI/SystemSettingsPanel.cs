using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 系统设置面板控制器
/// </summary>
public class SystemSettingsPanel : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Button backToMenuButton;           // 返回开始菜单按钮
    [SerializeField] private Button closePanelButton;           // 关闭面板按钮
    
    [Header("功能开关")]
    [SerializeField] private ToggleSwitchController dynamicIslandToggle;  // 灵动岛开关
    [SerializeField] private GameObject dynamicIslandSettingsGroup;       // 灵动岛设置组
    
    [Header("灵动岛设置")]
    [SerializeField] private Dropdown petSelectionDropdown;     // 宠物选择下拉菜单
    [SerializeField] private Text selectedPetNameText;          // 选中宠物名称显示
    
    [Header("下拉菜单高度设置")]
    [SerializeField] private float dropdownItemHeight = 50f;    // 单个选项高度
    [SerializeField] private float dropdownItemSpacing = 2f;    // 选项间距
    [SerializeField] private float dropdownPadding = 15f;       // 上下内边距
    [SerializeField] private int maxVisibleItems = 4;           // 最大可见选项数量
    
    [Header("面板高度设置")]
    [SerializeField] private float panelHeightWhenOff = 300f;   // 灵动岛关闭时的面板高度
    [SerializeField] private float panelHeightWhenOn = 500f;    // 灵动岛开启时的面板高度
    [SerializeField] private float panelResizeAnimationDuration = 0.4f; // 面板大小调整动画时长
    
    [Header("动画设置")]
    [SerializeField] private float settingsGroupAnimationDuration = 0.3f;
    [SerializeField] private Ease settingsGroupAnimationEase = Ease.OutQuart;
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 50); // 隐藏时的位置偏移
    
    [Header("场景切换设置")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset startScene;        // Start场景资源（拖拽配置）
#else
    [SerializeField] private Object startScene;                       // 运行时使用Object类型
#endif
    [SerializeField] private GameObject transitionOverlayPrefab;       // 过渡动画预制体
    
    // 设置数据
    private SystemSettingsData settingsData;
    private Vector2 shownPosition; // 显示时的位置
    private Sequence currentAnimation; // 当前动画序列
    
    // 状态管理
    private bool isUpdatingUI = false; // 防止UI更新时触发事件
    
    private void Start()
    {
        InitializePanel();
        SetupEventListeners();
        
        // 延迟加载设置，确保SaveManager初始化完成
        StartCoroutine(LoadSettingsDelayed());
        
        // 移除全局数据变化事件监听，改为在面板显示时主动刷新
        // SubscribeToDataChangeEvents();
    }
    
    /// <summary>
    /// 延迟加载设置
    /// </summary>
    private IEnumerator LoadSettingsDelayed()
    {
        // 等待SaveManager初始化
        while (SaveManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 确保存档已加载
        if (SaveManager.Instance.GetCurrentSaveData() == null)
        {
            SaveManager.Instance.LoadSave();
        }
        
        // 再等待一帧确保数据完全同步
        yield return null;
        
        LoadSettings();
        UpdateUI();
    }
    
    /// <summary>
    /// 初始化面板
    /// </summary>
    private void InitializePanel()
    {
        // 保存场景中配置的显示位置
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            shownPosition = rectTransform.anchoredPosition;
            // Debug.Log($"[SystemSettingsPanel] 保存场景配置的显示位置: {shownPosition}");
        }
        
        // 确保设置组初始状态正确
        if (dynamicIslandSettingsGroup != null)
        {
            dynamicIslandSettingsGroup.SetActive(false);
        }
        
        // 注意：不在这里调用SetPanelToHiddenState()
        // 因为BottomPanelController的InitializeSystemSettingsPanel()会处理初始隐藏状态
    }
    
    /// <summary>
    /// 设置事件监听器
    /// </summary>
    private void SetupEventListeners()
    {
        // 返回菜单按钮
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
        }
        
        // 关闭面板按钮
        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(OnClosePanelClicked);
        }
        
        // 灵动岛开关
        if (dynamicIslandToggle != null)
        {
            dynamicIslandToggle.OnValueChanged += OnDynamicIslandToggleChanged;
        }
        
        // 宠物选择下拉菜单
        if (petSelectionDropdown != null)
        {
            petSelectionDropdown.onValueChanged.AddListener(OnPetSelectionChanged);
        }
    }
    
    /// <summary>
    /// 加载设置数据
    /// </summary>
    private void LoadSettings()
    {
        // 从存档数据加载设置
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData != null)
        {
            settingsData = new SystemSettingsData();
            settingsData.dynamicIslandEnabled = saveData.playerData.dynamicIslandEnabled;
            settingsData.selectedDynamicIslandPetId = saveData.playerData.selectedDynamicIslandPetId;
            settingsData.lockScreenWidgetEnabled = saveData.playerData.lockScreenWidgetEnabled;
            settingsData.selectedLockScreenPetId = saveData.playerData.selectedLockScreenPetId;
            
            // Debug.Log($"[SystemSettingsPanel] 从存档加载设置: 灵动岛={settingsData.dynamicIslandEnabled}, 选中宠物={settingsData.selectedDynamicIslandPetId}");
            
            // 检查是否是老账号（没有系统设置数据）
            if (string.IsNullOrEmpty(settingsData.selectedDynamicIslandPetId) && saveData.petsData != null && saveData.petsData.Count > 0)
            {
                // Debug.Log("[SystemSettingsPanel] 检测到老账号，设置默认系统设置");
                settingsData.dynamicIslandEnabled = true;
                settingsData.selectedDynamicIslandPetId = saveData.petsData[0].petId;
                
                // 立即保存默认设置
                SaveSettings();
            }
        }
        else
        {
            // 如果没有存档数据，使用默认设置
            settingsData = new SystemSettingsData();
            Debug.LogWarning("[SystemSettingsPanel] 存档数据不可用，使用默认设置");
        }
        
        // 更新UI显示（包括设置组可见性）
        UpdateUI();
        
        // 初始化宠物选择下拉菜单（这个需要在UpdateUI之后，因为需要settingsData）
        PopulatePetDropdown();
    }
    
    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (settingsData == null)
        {
            Debug.LogWarning("[SystemSettingsPanel] UpdateUI: settingsData为空，跳过UI更新");
            return;
        }
        
        // Debug.Log($"[SystemSettingsPanel] UpdateUI: 灵动岛={settingsData.dynamicIslandEnabled}");
        
        // 设置更新标志，防止事件触发
        isUpdatingUI = true;
        
        try
        {
            // 设置灵动岛开关状态
            if (dynamicIslandToggle != null)
            {
                dynamicIslandToggle.SetValue(settingsData.dynamicIslandEnabled, false);
            }
            
            // 更新面板高度（不需要动画，因为是初始化）
            AdjustPanelHeight(settingsData.dynamicIslandEnabled, false);
            
            // 更新设置组显示状态
            UpdateSettingsGroupVisibility(settingsData.dynamicIslandEnabled, false);
            
            // 更新宠物选择
            UpdateSelectedPetDisplay();
        }
        finally
        {
            // 清除更新标志
            isUpdatingUI = false;
        }
        
        // Debug.Log("[SystemSettingsPanel] UpdateUI 完成");
    }
    
    /// <summary>
    /// 填充宠物选择下拉菜单
    /// </summary>
    private void PopulatePetDropdown()
    {
        if (petSelectionDropdown == null) return;
        
        petSelectionDropdown.ClearOptions();
        
        // 获取当前拥有的宠物
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        
        // 调试信息
        // Debug.Log($"[SystemSettingsPanel] SaveManager.Instance: {SaveManager.Instance != null}");
        // Debug.Log($"[SystemSettingsPanel] SaveData: {saveData != null}");
        // Debug.Log($"[SystemSettingsPanel] PetsData: {saveData?.petsData != null}");
        // Debug.Log($"[SystemSettingsPanel] PetsData Count: {saveData?.petsData?.Count ?? 0}");
        
        if (saveData?.petsData == null || saveData.petsData.Count == 0)
        {
            Debug.LogWarning("[SystemSettingsPanel] 没有找到宠物数据！");
            petSelectionDropdown.options.Add(new Dropdown.OptionData("暂无宠物"));
            petSelectionDropdown.interactable = false;
            
            // 调整下拉菜单高度
            AdjustDropdownHeight(1);
            return;
        }
        
        // Debug.Log($"[SystemSettingsPanel] 找到 {saveData.petsData.Count} 只宠物");
        
        // 添加宠物选项
        foreach (var petData in saveData.petsData)
        {
            string displayName = string.IsNullOrEmpty(petData.displayName) ? petData.prefabName : petData.displayName;
            petSelectionDropdown.options.Add(new Dropdown.OptionData(displayName));
            // Debug.Log($"[SystemSettingsPanel] 添加宠物选项: {displayName} (ID: {petData.petId})");
        }
        
        // 设置当前选中的宠物
        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(settingsData.selectedDynamicIslandPetId))
        {
            for (int i = 0; i < saveData.petsData.Count; i++)
            {
                if (saveData.petsData[i].petId == settingsData.selectedDynamicIslandPetId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }
        else
        {
            // 如果没有选中的宠物ID（老账号兼容），默认选择第一个宠物
            if (saveData.petsData.Count > 0)
            {
                settingsData.selectedDynamicIslandPetId = saveData.petsData[0].petId;
                // Debug.Log($"[SystemSettingsPanel] 老账号兼容：自动选择第一个宠物 {saveData.petsData[0].petId}");
            }
        }
        
        // 设置下拉菜单值时防止触发事件
        isUpdatingUI = true;
        petSelectionDropdown.value = selectedIndex;
        isUpdatingUI = false;
        
        petSelectionDropdown.interactable = true;
        
        // 调整下拉菜单高度
        AdjustDropdownHeight(saveData.petsData.Count);
        
        // 强制刷新下拉菜单，确保Template更新
        petSelectionDropdown.RefreshShownValue();
    }
    
    /// <summary>
    /// 动态调整下拉菜单高度
    /// </summary>
    /// <param name="itemCount">选项数量</param>
    private void AdjustDropdownHeight(int itemCount)
    {
        if (petSelectionDropdown == null) return;
        
        // 获取Template组件
        RectTransform template = petSelectionDropdown.template;
        if (template == null) return;
        
        // 计算需要的高度
        float itemHeight = dropdownItemHeight;
        float spacing = dropdownItemSpacing;
        float padding = dropdownPadding;
        
        // 限制最大显示数量为4个
        int displayCount = Mathf.Min(itemCount, maxVisibleItems);
        float totalHeight = (itemHeight * displayCount) + (spacing * Mathf.Max(0, displayCount - 1)) + padding;
        
        // 设置Template高度
        Vector2 templateSize = template.sizeDelta;
        templateSize.y = totalHeight;
        template.sizeDelta = templateSize;
        
        // 查找并设置Viewport和Content的高度
        Transform viewport = template.Find("Viewport");
        if (viewport != null)
        {
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
            {
                // 设置Viewport的边距，实现真正的上下padding
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = new Vector2(0, padding / 2f);      // 下边距
                viewportRect.offsetMax = new Vector2(0, -padding / 2f);     // 上边距（负值）
            }
            
            Transform content = viewport.Find("Content");
            if (content != null)
            {
                RectTransform contentRect = content.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    // 计算Content的实际内容高度（不包含padding）
                    float contentItemsHeight = (itemHeight * itemCount) + (spacing * Mathf.Max(0, itemCount - 1));
                    
                    // 如果选项超过最大可见数量，Content需要更高以支持滚动
                    if (itemCount > maxVisibleItems)
                    {
                        Vector2 contentSize = contentRect.sizeDelta;
                        contentSize.y = contentItemsHeight;
                        contentRect.sizeDelta = contentSize;
                        
                        // Content应该从顶部开始
                        contentRect.anchorMin = new Vector2(0, 1);
                        contentRect.anchorMax = new Vector2(1, 1);
                        contentRect.pivot = new Vector2(0.5f, 1);
                        contentRect.anchoredPosition = new Vector2(0, 0);
                    }
                    else
                    {
                        // 选项不多时，Content填满Viewport（会自动应用padding）
                        contentRect.anchorMin = Vector2.zero;
                        contentRect.anchorMax = Vector2.one;
                        contentRect.offsetMin = Vector2.zero;
                        contentRect.offsetMax = Vector2.zero;
                        contentRect.pivot = new Vector2(0.5f, 0.5f);
                    }
                }
            }
        }
        
        // Debug.Log($"[SystemSettingsPanel] 下拉菜单高度调整: {itemCount}个选项, Template高度={totalHeight}px");
    }
    
    /// <summary>
    /// 更新选中宠物的显示
    /// </summary>
    private void UpdateSelectedPetDisplay()
    {
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.petsData == null || saveData.petsData.Count == 0)
        {
            if (selectedPetNameText != null)
                selectedPetNameText.text = "暂无宠物";
            return;
        }
        
        // 找到选中的宠物数据
        PetSaveData selectedPet = null;
        if (!string.IsNullOrEmpty(settingsData.selectedDynamicIslandPetId))
        {
            selectedPet = saveData.petsData.Find(p => p.petId == settingsData.selectedDynamicIslandPetId);
        }
        
        if (selectedPet == null && saveData.petsData.Count > 0)
        {
            // 如果没有找到选中的宠物或selectedDynamicIslandPetId为空，选择第一个宠物
            selectedPet = saveData.petsData[0];
            settingsData.selectedDynamicIslandPetId = selectedPet.petId;
            
            // 立即保存这个默认选择
            SaveSettings();
            
            // Debug.Log($"[SystemSettingsPanel] 自动选择第一个宠物作为默认选择: {selectedPet.petId}");
        }
        
        if (selectedPet != null)
        {
            // 更新名称显示
            if (selectedPetNameText != null)
            {
                string displayName = string.IsNullOrEmpty(selectedPet.displayName) ? selectedPet.prefabName : selectedPet.displayName;
                selectedPetNameText.text = displayName;
            }
        }
    }
    
    /// <summary>
    /// 更新设置组显示状态
    /// </summary>
    /// <param name="show">是否显示</param>
    /// <param name="animate">是否播放动画</param>
    private void UpdateSettingsGroupVisibility(bool show, bool animate = true)
    {
        if (dynamicIslandSettingsGroup == null)
        {
            Debug.LogError("[SystemSettingsPanel] dynamicIslandSettingsGroup为空！");
            return;
        }
        
        // Debug.Log($"[SystemSettingsPanel] UpdateSettingsGroupVisibility: show={show}, animate={animate}");
        
        // 先调整面板高度
        AdjustPanelHeight(show, animate);
        
        if (show)
        {
            dynamicIslandSettingsGroup.SetActive(true);
            
            // 获取或添加CanvasGroup
            CanvasGroup canvasGroup = dynamicIslandSettingsGroup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dynamicIslandSettingsGroup.AddComponent<CanvasGroup>();
            }
            
            if (animate)
            {
                // 淡入动画
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, settingsGroupAnimationDuration)
                    .SetEase(settingsGroupAnimationEase);
            }
            else
            {
                // 立即显示，确保alpha为1
                canvasGroup.alpha = 1f;
            }
        }
        else
        {
            if (animate)
            {
                // 淡出动画
                CanvasGroup canvasGroup = dynamicIslandSettingsGroup.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dynamicIslandSettingsGroup.AddComponent<CanvasGroup>();
                }
                
                canvasGroup.DOFade(0f, settingsGroupAnimationDuration)
                    .SetEase(settingsGroupAnimationEase)
                    .OnComplete(() => dynamicIslandSettingsGroup.SetActive(false));
            }
            else
            {
                dynamicIslandSettingsGroup.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 调整面板高度
    /// </summary>
    /// <param name="isExpanded">是否展开（灵动岛开启）</param>
    /// <param name="animate">是否播放动画</param>
    private void AdjustPanelHeight(bool isExpanded, bool animate = true)
    {
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null) return;
        
        float targetHeight = isExpanded ? panelHeightWhenOn : panelHeightWhenOff;
        
        if (animate)
        {
            // 动画调整高度
            panelRect.DOSizeDelta(new Vector2(panelRect.sizeDelta.x, targetHeight), panelResizeAnimationDuration)
                .SetEase(settingsGroupAnimationEase);
        }
        else
        {
            // 立即调整高度
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, targetHeight);
        }
        
        // Debug.Log($"[SystemSettingsPanel] 面板高度调整: {(isExpanded ? "展开" : "收缩")} -> {targetHeight}px");
    }
    
    /// <summary>
    /// 显示面板（从BottomPanelController调用）
    /// </summary>
    public void ShowPanel(bool animated = true)
    {
        // 如果settingsData为空，说明数据还没有加载完成，需要等待
        if (settingsData == null)
        {
            //Debug.LogWarning("[SystemSettingsPanel] ShowPanel被调用但settingsData为空，启动等待加载机制");
            StartCoroutine(WaitForDataAndShowPanel(animated));
            return;
        }
        
        // 数据已加载，直接显示面板
        ShowPanelInternal(animated);
        
        // 主动刷新宠物列表，确保显示最新数据
        RefreshPetListOnShow();
    }
    
    /// <summary>
    /// 等待数据加载完成后显示面板
    /// </summary>
    private IEnumerator WaitForDataAndShowPanel(bool animated)
    {
        // 等待SaveManager初始化
        while (SaveManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 确保存档已加载
        if (SaveManager.Instance.GetCurrentSaveData() == null)
        {
            SaveManager.Instance.LoadSave();
            yield return null; // 等待一帧
        }
        
        // 如果settingsData还是空，手动加载设置
        if (settingsData == null)
        {
            // Debug.Log("[SystemSettingsPanel] 手动加载设置数据");
            LoadSettings();
        }
        
        // 再次检查settingsData
        if (settingsData == null)
        {
            Debug.LogError("[SystemSettingsPanel] 无法加载设置数据，使用默认设置");
            settingsData = new SystemSettingsData();
        }
        
        // 现在可以安全地显示面板了
        ShowPanelInternal(animated);
        
        // 主动刷新宠物列表，确保显示最新数据
        RefreshPetListOnShow();
    }
    
    /// <summary>
    /// 内部显示面板方法
    /// </summary>
    private void ShowPanelInternal(bool animated = true)
    {
        // 停止当前动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 确保面板可交互
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // 定义UI状态同步方法
        System.Action syncUIState = () => {
            if (settingsData != null)
            {
                // Debug.Log($"[SystemSettingsPanel] ShowPanelInternal完成，确保设置组状态正确: {settingsData.dynamicIslandEnabled}");
                
                // 设置更新标志，防止事件触发
                isUpdatingUI = true;
                
                try
                {
                    // 确保Toggle状态正确
                    if (dynamicIslandToggle != null)
                    {
                        dynamicIslandToggle.SetValue(settingsData.dynamicIslandEnabled, false);
                    }
                    
                    // 确保设置组可见性正确
                    UpdateSettingsGroupVisibility(settingsData.dynamicIslandEnabled, false);
                    
                    // 确保面板高度正确
                    AdjustPanelHeight(settingsData.dynamicIslandEnabled, false);
                }
                finally
                {
                    isUpdatingUI = false;
                }
            }
            else
            {
                Debug.LogWarning("[SystemSettingsPanel] ShowPanelInternal完成但settingsData为空，无法同步UI状态");
            }
        };
        
        if (!animated)
        {
            // 立即显示
            canvasGroup.alpha = 1f;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = shownPosition;
            }
            
            // 立即执行UI状态同步
            syncUIState();
            return;
        }
        
        // 设置初始隐藏状态
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        }
        canvasGroup.alpha = 0f;
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        if (rectTransform != null)
        {
            currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition, settingsGroupAnimationDuration).SetEase(settingsGroupAnimationEase));
        }
        
        currentAnimation.Join(canvasGroup.DOFade(1f, settingsGroupAnimationDuration * 0.7f));
        
        // 确保设置组状态正确（在动画完成后）
        currentAnimation.OnComplete(() => syncUIState());
        
        // 添加备用同步机制，防止动画回调失败
        StartCoroutine(EnsureUIStateSyncAfterDelay(syncUIState));
        
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 备用UI状态同步机制，防止动画回调失败
    /// </summary>
    private IEnumerator EnsureUIStateSyncAfterDelay(System.Action syncAction)
    {
        // 等待动画时长加上一点缓冲时间
        yield return new WaitForSeconds(settingsGroupAnimationDuration + 0.1f);
        
        // 如果动画还在进行中，说明可能有问题，强制执行同步
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            Debug.LogWarning("[SystemSettingsPanel] 动画超时，强制执行UI状态同步");
            syncAction?.Invoke();
        }
    }
    
    /// <summary>
    /// 隐藏面板（从BottomPanelController调用）
    /// </summary>
    public void HidePanel(bool animated = true)
    {
        // 停止当前动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (!animated)
        {
            // 立即隐藏
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = shownPosition + hiddenPosition;
            }
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        if (rectTransform != null)
        {
            currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition + hiddenPosition, settingsGroupAnimationDuration).SetEase(settingsGroupAnimationEase));
        }
        
        currentAnimation.Join(canvasGroup.DOFade(0f, settingsGroupAnimationDuration * 0.7f));
        
        // 动画完成后设置交互状态
        currentAnimation.OnComplete(() => {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // 动画完成后隐藏GameObject
            gameObject.SetActive(false);
        });
        
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 面板显示时刷新宠物列表
    /// </summary>
    private void RefreshPetListOnShow()
    {
        //Debug.Log("[SystemSettingsPanel] 面板显示，主动刷新宠物列表");
        
        // 刷新宠物下拉菜单
        PopulatePetDropdown();
        
        // 验证当前选中的宠物是否仍然存在
        ValidateSelectedPet();
    }
    
    /// <summary>
    /// 验证当前选中的宠物是否仍然存在
    /// </summary>
    private void ValidateSelectedPet()
    {
        if (settingsData == null || string.IsNullOrEmpty(settingsData.selectedDynamicIslandPetId))
            return;
            
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.petsData == null || saveData.petsData.Count == 0)
            return;
            
        // 检查当前选中的宠物是否还存在
        bool petExists = saveData.petsData.Exists(p => p.petId == settingsData.selectedDynamicIslandPetId);
        
        if (!petExists)
        {
            //Debug.Log($"[SystemSettingsPanel] 当前选中的宠物 {settingsData.selectedDynamicIslandPetId} 不再存在，自动选择第一个宠物");
            
            // 选择第一个可用的宠物
            settingsData.selectedDynamicIslandPetId = saveData.petsData[0].petId;
            
            // 更新UI显示
            UpdateSelectedPetDisplay();
            
            // 更新下拉菜单选择
            if (petSelectionDropdown != null)
            {
                isUpdatingUI = true;
                petSelectionDropdown.value = 0;
                isUpdatingUI = false;
            }
            
            // 保存设置
            SaveSettings();
        }
    }
    
    #region 事件处理
    
    /// <summary>
    /// 返回开始菜单按钮点击
    /// </summary>
    private void OnBackToMenuClicked()
    {
        // Debug.Log("[SystemSettingsPanel] 返回开始菜单按钮被点击");
        
        // 保存系统设置
        SaveSettings();
        
        // 使用GameDataManager同步所有运行时数据到存档
        if (GameDataManager.Instance != null)
        {
            // Debug.Log("[SystemSettingsPanel] 同步游戏数据到存档...");
            GameDataManager.Instance.SyncToSave(true); // immediate = true，立即保存
        }
        else
        {
            Debug.LogWarning("[SystemSettingsPanel] GameDataManager未找到，尝试直接保存存档");
            
            // 如果GameDataManager不可用，直接保存存档
            if (SaveManager.Instance != null)
            {
                bool saveSuccess = SaveManager.Instance.Save();
                if (saveSuccess)
                {
                    // Debug.Log("[SystemSettingsPanel] 存档保存成功");
                }
                else
                {
                    Debug.LogError("[SystemSettingsPanel] 存档保存失败！");
                }
            }
            else
            {
                Debug.LogError("[SystemSettingsPanel] SaveManager也未找到，无法保存游戏数据！");
            }
        }
        
        // 播放过渡动画并切换场景
        StartTransitionToStartScene();
    }
    
    /// <summary>
    /// 开始过渡到开始菜单场景
    /// </summary>
    private void StartTransitionToStartScene()
    {
        if (transitionOverlayPrefab == null)
        {
            Debug.LogWarning("[SystemSettingsPanel] 过渡动画预制体未配置，直接加载场景");
            LoadStartScene();
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
                    enterCompleteEvent.AddListener(LoadStartScene);
                }
                
                // Debug.Log("[SystemSettingsPanel] 已设置过渡动画立即播放");
            }
            else
            {
                Debug.LogError("[SystemSettingsPanel] 过渡动画预制体缺少TransitionController组件");
                Destroy(overlayInstance);
                LoadStartScene();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SystemSettingsPanel] 创建过渡动画失败: {e.Message}");
            LoadStartScene();
        }
    }
    
    /// <summary>
    /// 加载开始菜单场景
    /// </summary>
    private void LoadStartScene()
    {
        //Debug.Log("[SystemSettingsPanel] LoadStartScene() 开始执行");
        
        if (startScene == null)
        {
            Debug.LogError("[SystemSettingsPanel] Start场景未配置！请在Inspector中拖拽场景文件到startScene字段");
            Debug.LogWarning("[SystemSettingsPanel] 使用默认场景名称 'Start'");
            
            // 设置返回标志，让Start场景知道这是从Gameplay返回的
            GameState.IsReturningFromGameplay = true;
            //Debug.Log("[SystemSettingsPanel] 已设置 GameState.IsReturningFromGameplay = true (使用默认场景名)");
            
            SceneManager.LoadScene("Start");
            return;
        }
        
        try
        {
            string sceneName = GetStartSceneName();
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new System.Exception("无法获取场景名称");
            }
            
            //Debug.Log($"[SystemSettingsPanel] 准备加载开始菜单场景: {sceneName}");
            
            // 设置返回标志，让Start场景知道这是从Gameplay返回的
            GameState.IsReturningFromGameplay = true;
            //Debug.Log("[SystemSettingsPanel] 已设置 GameState.IsReturningFromGameplay = true");
            
            //Debug.Log($"[SystemSettingsPanel] 开始加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SystemSettingsPanel] 加载场景失败: {e.Message}");
            Debug.LogWarning("[SystemSettingsPanel] 使用默认场景名称 'Start'");
            
            // 设置返回标志，让Start场景知道这是从Gameplay返回的
            GameState.IsReturningFromGameplay = true;
            //Debug.Log("[SystemSettingsPanel] 已设置 GameState.IsReturningFromGameplay = true (异常情况)");
            
            SceneManager.LoadScene("Start");
        }
    }
    
    /// <summary>
    /// 获取开始场景名称
    /// </summary>
    private string GetStartSceneName()
    {
        if (startScene == null) return null;
        
#if UNITY_EDITOR
        return startScene.name;
#else
        return startScene.name;
#endif
    }
    
    /// <summary>
    /// 关闭面板按钮点击
    /// </summary>
    private void OnClosePanelClicked()
    {
        // 保存设置
        SaveSettings();
        
        // 直接隐藏面板，并通知BottomPanelController更新状态
        HidePanel(true);
        
        // 通知BottomPanelController更新按钮状态（不要让它再次关闭面板）
        BottomPanelController bottomPanel = FindObjectOfType<BottomPanelController>();
        if (bottomPanel != null)
        {
            bottomPanel.OnFunctionPanelClosed(BottomPanelController.FunctionType.Settings);
        }
    }
    
    /// <summary>
    /// 灵动岛开关状态改变
    /// </summary>
    /// <param name="isOn">开关状态</param>
    private void OnDynamicIslandToggleChanged(bool isOn)
    {
        // 如果正在更新UI，忽略事件
        if (isUpdatingUI)
        {
            // Debug.Log($"[SystemSettingsPanel] 正在更新UI，忽略Toggle事件: {isOn}");
            return;
        }
        
        // 确保settingsData已初始化
        if (settingsData == null)
        {
            Debug.LogWarning("[SystemSettingsPanel] settingsData为空，正在重新加载...");
            LoadSettings(); // 重新调用LoadSettings方法
            
            // 重新加载后，如果settingsData还是空，使用默认设置
            if (settingsData == null)
            {
                Debug.LogError("[SystemSettingsPanel] 重新加载后settingsData仍为空，使用默认设置");
                settingsData = new SystemSettingsData();
            }
            
            // 现在继续处理toggle事件
        }
        
        // Debug.Log($"[SystemSettingsPanel] 灵动岛开关改变: {settingsData.dynamicIslandEnabled} -> {isOn}");
        
        settingsData.dynamicIslandEnabled = isOn;
        
        // 更新设置组可见性和面板高度
        UpdateSettingsGroupVisibility(isOn, true);
        AdjustPanelHeight(isOn, true);
        
        // 立即保存设置
        SaveSettings();
    }
    
    /// <summary>
    /// 宠物选择改变
    /// </summary>
    /// <param name="selectedIndex">选中的索引</param>
    private void OnPetSelectionChanged(int selectedIndex)
    {
        // 如果正在更新UI，忽略事件
        if (isUpdatingUI)
        {
            // Debug.Log($"[SystemSettingsPanel] 正在更新UI，忽略下拉菜单事件: {selectedIndex}");
            return;
        }
        
        // 确保settingsData已初始化
        if (settingsData == null)
        {
            Debug.LogWarning("[SystemSettingsPanel] settingsData为空，正在重新加载...");
            LoadSettings(); // 重新调用LoadSettings方法
            
            // 重新加载后，如果settingsData还是空，使用默认设置
            if (settingsData == null)
            {
                Debug.LogError("[SystemSettingsPanel] 重新加载后settingsData仍为空，使用默认设置");
                settingsData = new SystemSettingsData();
            }
            
            // 现在继续处理选择事件
        }
        
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.petsData == null || selectedIndex < 0 || selectedIndex >= saveData.petsData.Count)
            return;
        
        // Debug.Log($"[SystemSettingsPanel] 宠物选择改变: {settingsData.selectedDynamicIslandPetId} -> {saveData.petsData[selectedIndex].petId}");
        
        settingsData.selectedDynamicIslandPetId = saveData.petsData[selectedIndex].petId;
        UpdateSelectedPetDisplay();
        
        // 立即保存设置
        SaveSettings();
    }
    
    #endregion
    
    /// <summary>
    /// 保存设置
    /// </summary>
    private void SaveSettings()
    {
        if (settingsData == null)
        {
            Debug.LogWarning("[SystemSettingsPanel] settingsData为空，无法保存设置");
            return;
        }
        
        // 保存到存档数据
        var saveData = SaveManager.Instance?.GetCurrentSaveData();
        if (saveData?.playerData != null)
        {
            // Debug.Log($"[SystemSettingsPanel] 保存前状态: 灵动岛={saveData.playerData.dynamicIslandEnabled}, 选中宠物={saveData.playerData.selectedDynamicIslandPetId}");
            
            saveData.playerData.dynamicIslandEnabled = settingsData.dynamicIslandEnabled;
            saveData.playerData.selectedDynamicIslandPetId = settingsData.selectedDynamicIslandPetId;
            saveData.playerData.lockScreenWidgetEnabled = settingsData.lockScreenWidgetEnabled;
            saveData.playerData.selectedLockScreenPetId = settingsData.selectedLockScreenPetId;
            
            // Debug.Log($"[SystemSettingsPanel] 保存后状态: 灵动岛={saveData.playerData.dynamicIslandEnabled}, 选中宠物={saveData.playerData.selectedDynamicIslandPetId}");
            
            // 触发存档保存
            bool saveSuccess = SaveManager.Instance.Save();
            
            if (saveSuccess)
            {
                // Debug.Log($"[SystemSettingsPanel] 设置已成功保存到存档: 灵动岛={settingsData.dynamicIslandEnabled}, 选中宠物={settingsData.selectedDynamicIslandPetId}");
            }
            else
            {
                Debug.LogError("[SystemSettingsPanel] 存档保存失败！");
            }
        }
        else
        {
            Debug.LogError("[SystemSettingsPanel] 存档数据不可用，无法保存设置");
            Debug.LogError($"[SystemSettingsPanel] SaveManager.Instance: {SaveManager.Instance != null}");
            Debug.LogError($"[SystemSettingsPanel] CurrentSaveData: {SaveManager.Instance?.GetCurrentSaveData() != null}");
            Debug.LogError($"[SystemSettingsPanel] PlayerData: {SaveManager.Instance?.GetCurrentSaveData()?.playerData != null}");
        }
    }
    
    /// <summary>
    /// 强制同步UI状态
    /// </summary>
    [ContextMenu("强制同步UI状态")]
    public void ForceSyncUIState()
    {
        // Debug.Log("[SystemSettingsPanel] 强制同步UI状态");
        if (settingsData != null)
        {
            UpdateUI();
        }
        else
        {
            LoadSettings();
        }
    }
    
    /// <summary>
    /// 测试方法：诊断保存和加载问题
    /// </summary>
    [ContextMenu("诊断系统设置保存加载")]
    public void DiagnoseSystemSettings()
    {
        // Debug.Log("=== 系统设置诊断开始 ===");
        
        // 检查SaveManager
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager.Instance 为空！");
            return;
        }
        
        //Debug.Log("SaveManager.Instance 正常");
        
        // 检查当前存档数据
        var saveData = SaveManager.Instance.GetCurrentSaveData();
        if (saveData == null)
        {
            Debug.LogError("当前存档数据为空！");
            return;
        }
        
        //Debug.Log("当前存档数据正常");
        
        // 检查玩家数据
        if (saveData.playerData == null)
        {
            Debug.LogError("玩家数据为空！");
            return;
        }
        
        //Debug.Log("玩家数据正常");
        
        // 输出当前系统设置
        // Debug.Log($"当前系统设置:");
        //Debug.Log($"  - 灵动岛开启: {saveData.playerData.dynamicIslandEnabled}");
        //Debug.Log($"  - 选中宠物ID: '{saveData.playerData.selectedDynamicIslandPetId}'");
        //Debug.Log($"  - 锁屏小组件开启: {saveData.playerData.lockScreenWidgetEnabled}");
        //Debug.Log($"  - 锁屏小组件宠物ID: '{saveData.playerData.selectedLockScreenPetId}'");
        
        // 检查宠物数据
        if (saveData.petsData != null && saveData.petsData.Count > 0)
        {
            Debug.Log($"宠物数据: 共{saveData.petsData.Count}只宠物");
            for (int i = 0; i < saveData.petsData.Count; i++)
            {
                Debug.Log($"  - 宠物{i}: ID='{saveData.petsData[i].petId}', 名称='{saveData.petsData[i].displayName}'");
            }
        }
        else
        {
            Debug.LogWarning("没有宠物数据！");
        }
        
        // 检查settingsData
        if (settingsData != null)
        {
            Debug.Log($"内存中的设置数据:");
            Debug.Log($"  - 灵动岛开启: {settingsData.dynamicIslandEnabled}");
            Debug.Log($"  - 选中宠物ID: '{settingsData.selectedDynamicIslandPetId}'");
            Debug.Log($"  - 锁屏小组件开启: {settingsData.lockScreenWidgetEnabled}");
            Debug.Log($"  - 锁屏小组件宠物ID: '{settingsData.selectedLockScreenPetId}'");
        }
        else
        {
            Debug.LogWarning("内存中的设置数据为空！");
        }
        
        // Debug.Log("=== 系统设置诊断结束 ===");
    }
    
    /// <summary>
    /// 验证场景配置（编辑器中显示帮助信息）
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (startScene != null)
        {
            // 检查场景是否在Build Settings中
            string scenePath = AssetDatabase.GetAssetPath(startScene);
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
                Debug.LogWarning($"[SystemSettingsPanel] 场景 '{startScene.name}' 未添加到Build Settings中，或已被禁用！正在自动修复...", this);
                
                // 自动添加场景到Build Settings
                var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                
                // 检查是否已存在但被禁用
                bool sceneExists = false;
                for (int i = 0; i < scenes.Count; i++)
                {
                    if (scenes[i].path == scenePath)
                    {
                        scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                        sceneExists = true;
                        break;
                    }
                }
                
                // 如果场景不存在，添加到列表开头（作为第一个场景）
                if (!sceneExists)
                {
                    scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                }
                
                // 更新Build Settings
                EditorBuildSettings.scenes = scenes.ToArray();
                // Debug.Log($"[SystemSettingsPanel] 已自动将场景 '{startScene.name}' 添加到Build Settings中", this);
            }
        }
#endif
    }
    
    private void OnDestroy()
    {
        // 清理事件监听器
        if (dynamicIslandToggle != null)
        {
            dynamicIslandToggle.OnValueChanged -= OnDynamicIslandToggleChanged;
        }
        
        // 清理动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
}

/// <summary>
/// 系统设置数据
/// </summary>
[System.Serializable]
public class SystemSettingsData
{
    public bool dynamicIslandEnabled = false;
    public string selectedDynamicIslandPetId = "";
    public bool lockScreenWidgetEnabled = false;
    public string selectedLockScreenPetId = "";
    
    public SystemSettingsData()
    {
        dynamicIslandEnabled = false;
        selectedDynamicIslandPetId = "";
        lockScreenWidgetEnabled = false;
        selectedLockScreenPetId = "";
    }
} 