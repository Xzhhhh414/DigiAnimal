using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 爱心商店控制器 - 管理商店界面、商品展示和购买逻辑
/// </summary>
public class HeartShopController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private GameObject shopPanel;                    // 商店主面板
    [SerializeField] private GameObject purchaseConfirmPanel;         // 购买确认面板
    [SerializeField] private GameObject shadowCover;                  // 阴影遮罩
    [SerializeField] private Button closeButton;                     // 关闭按钮
    [SerializeField] private ScrollRect shopScrollRect;              // 商店滑动区域
    
    [Header("动态商品生成")]
    [SerializeField] private GameObject shopItemPrefab;              // 商品项预制体 (HeartShopItem)
    [SerializeField] private Transform shopItemContainer;            // 商品容器 (ScrollView的Content)
    
    [Header("购买确认UI引用")]
    [SerializeField] private Button confirmPurchaseButton;           // 确认购买按钮
    [SerializeField] private Button cancelPurchaseButton;            // 取消购买按钮
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;         // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack;      // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 50); // 隐藏时的位置偏移
    
    [Header("Toast提示文本")]
    [SerializeField] private string maxPetsMessage = "宠物数量已达上限（{0}/{1}），无法购买更多宠物";
    [SerializeField] private string insufficientFundsMessage = "爱心货币不足，需要{0}个爱心，当前只有{1}个";
    [SerializeField] private string purchaseSuccessMessage = "成功购买{0}！";
    [SerializeField] private string purchaseFailedMessage = "购买失败，请稍后重试";
    
    // 私有变量
    private List<PetConfigData> shopPets = new List<PetConfigData>();
    private List<HeartShopItem> shopItemInstances = new List<HeartShopItem>(); // 动态生成的商品实例
    private PetConfigData selectedPetForPurchase;
    private bool isShopOpen = false;                                 // 商店是否打开（内部状态）
    private const int MAX_PETS = 4;                                  // 最大宠物数量
    
    // 动画相关
    private RectTransform shopPanelRect;
    private CanvasGroup shopPanelCanvasGroup;
    private Vector2 shownPosition;
    private Sequence currentAnimation;
    
    // 单例模式
    private static HeartShopController _instance;
    public static HeartShopController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HeartShopController>();
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // 单例初始化
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeAnimationComponents();
        InitializeShop();
        SetupEventListeners();
        
        // 订阅货币变化事件
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅货币变化事件
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
        
        // 清理可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
    
    /// <summary>
    /// 初始化动画组件
    /// </summary>
    private void InitializeAnimationComponents()
    {
        // 获取商店面板的RectTransform
        if (shopPanel != null)
        {
            shopPanelRect = shopPanel.GetComponent<RectTransform>();
            if (shopPanelRect == null)
            {
                Debug.LogError("HeartShopController: 商店面板缺少RectTransform组件！");
            }
            
            // 获取或添加CanvasGroup组件
            shopPanelCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (shopPanelCanvasGroup == null)
            {
                shopPanelCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
            }
            
            // 保存显示位置
            if (shopPanelRect != null)
            {
                shownPosition = shopPanelRect.anchoredPosition;
            }
            
            // 初始状态隐藏(但不禁用游戏对象，只使其不可见和不可交互)
            if (shopPanelCanvasGroup != null && shopPanelRect != null)
            {
                shopPanelCanvasGroup.alpha = 0;
                shopPanelCanvasGroup.interactable = false;
                shopPanelCanvasGroup.blocksRaycasts = false;
                shopPanelRect.anchoredPosition = shownPosition + hiddenPosition;
            }
            
            // 确保面板激活但不可见
            shopPanel.SetActive(true);
        }
        
        // 设置滚动条初始位置到顶部
        if (shopScrollRect != null)
        {
            shopScrollRect.verticalNormalizedPosition = 1f; // 1f = 顶部, 0f = 底部
        }
    }
    
    /// <summary>
    /// 货币变化事件处理
    /// </summary>
    private void OnCurrencyChanged(int newAmount)
    {
        // 刷新所有商品的价格显示状态
        RefreshAllItemsPriceDisplay();
    }
    
    /// <summary>
    /// 初始化商店
    /// </summary>
    private void InitializeShop()
    {
        LoadShopPets();
        UpdateShopDisplay();
    }
    
    /// <summary>
    /// 设置事件监听器
    /// </summary>
    private void SetupEventListeners()
    {
        // 关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
        
        // 购买确认按钮
        if (confirmPurchaseButton != null)
        {
            confirmPurchaseButton.onClick.AddListener(OnConfirmPurchase);
        }
        
        if (cancelPurchaseButton != null)
        {
            cancelPurchaseButton.onClick.AddListener(OnCancelPurchase);
        }
        
        // 商品按钮事件会在动态生成时设置
    }
    
    /// <summary>
    /// 从数据库加载商店宠物
    /// </summary>
    private void LoadShopPets()
    {
        if (PetDatabaseManager.Instance != null && PetDatabaseManager.Instance.IsDatabaseLoaded())
        {
            shopPets = PetDatabaseManager.Instance.GetShopPets();
        }
        else
        {
            Debug.LogError("宠物数据库未加载，无法获取商店宠物");
            shopPets.Clear();
        }
    }
    
    /// <summary>
    /// 更新商店显示
    /// </summary>
    private void UpdateShopDisplay()
    {
        // 清理现有的商品实例
        ClearShopItems();
        
        // 动态生成商品项
        GenerateShopItems();
    }
    
    /// <summary>
    /// 清理现有的商品实例
    /// </summary>
    private void ClearShopItems()
    {
        foreach (var item in shopItemInstances)
        {
            if (item != null && item.gameObject != null)
            {
                DestroyImmediate(item.gameObject);
            }
        }
        shopItemInstances.Clear();
    }
    
    /// <summary>
    /// 动态生成商品项
    /// </summary>
    private void GenerateShopItems()
    {
        if (shopItemPrefab == null || shopItemContainer == null)
        {
            Debug.LogError("商品预制体或容器未设置！");
            return;
        }
        
        foreach (var pet in shopPets)
        {
            // 实例化商品项
            GameObject itemObj = Instantiate(shopItemPrefab, shopItemContainer);
            var shopItem = itemObj.GetComponent<HeartShopItem>();
            
            if (shopItem != null)
            {
                // 直接调用Initialize方法
                shopItem.Initialize(pet, OnShopItemClicked);
                shopItemInstances.Add(shopItem);
            }
            else
            {
                Debug.LogError("HeartShopItem预制体缺少HeartShopItem组件！");
                DestroyImmediate(itemObj);
            }
        }
        
        // 生成商品后，确保滚动条在正确位置
        if (shopScrollRect != null && shopPanel.activeSelf)
        {
            StartCoroutine(EnsureScrollPositionAfterGeneration());
        }
    }
    
    /// <summary>
    /// 切换商店显示状态
    /// </summary>
    public void ToggleShop()
    {
        if (isShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }
    
    /// <summary>
    /// 打开商店
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel == null || isShopOpen) return;
        
        // 设置商店打开状态
        isShopOpen = true;
        
        ShowShopPanel(true);
    }
    
    /// <summary>
    /// 在下一帧重置滚动条位置
    /// </summary>
    private IEnumerator ResetScrollPositionNextFrame()
    {
        // 等待一帧，让UI布局系统完成计算
        yield return null;
        
        // 强制重建布局
        if (shopScrollRect != null)
        {
            // 强制重建布局
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(shopScrollRect.content);
            
            // 等待另一帧确保布局完全完成
            yield return null;
            
            // 重置滚动条位置到顶部
            shopScrollRect.verticalNormalizedPosition = 1f; // 1f = 顶部, 0f = 底部
            
            // 强制更新滚动条
            shopScrollRect.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);
        }
    }
    
    /// <summary>
    /// 确保商品生成后滚动条位置正确
    /// </summary>
    private IEnumerator EnsureScrollPositionAfterGeneration()
    {
        // 等待两帧，确保所有UI元素都已正确布局
        yield return null;
        yield return null;
        
        if (shopScrollRect != null)
        {
            // 强制重建布局
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(shopScrollRect.content);
            
            // 设置滚动条到顶部
            shopScrollRect.verticalNormalizedPosition = 1f;
        }
    }
    
    /// <summary>
    /// 关闭商店
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel == null || !isShopOpen) return;
        
        HideShopPanel(true);
    }
    
    /// <summary>
    /// 显示商店面板
    /// </summary>
    private void ShowShopPanel(bool animated = true)
    {
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 确保面板处于可交互状态
        if (shopPanelCanvasGroup != null)
        {
            shopPanelCanvasGroup.interactable = true;
            shopPanelCanvasGroup.blocksRaycasts = true;
        }
        
        // 刷新商店数据
        LoadShopPets();
        UpdateShopDisplay();
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            if (shopPanelCanvasGroup != null)
            {
                shopPanelCanvasGroup.alpha = 1;
            }
            if (shopPanelRect != null)
            {
                shopPanelRect.anchoredPosition = shownPosition;
            }
            // 等待一帧后重置滚动条位置
            StartCoroutine(ResetScrollPositionNextFrame());
            return;
        }
        
        // 设置初始位置和透明度
        if (shopPanelRect != null)
        {
            shopPanelRect.anchoredPosition = shownPosition + hiddenPosition;
        }
        if (shopPanelCanvasGroup != null)
        {
            shopPanelCanvasGroup.alpha = 0;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡入动画
        if (shopPanelRect != null)
        {
            currentAnimation.Join(shopPanelRect.DOAnchorPos(shownPosition, animationDuration).SetEase(animationEase));
        }
        if (shopPanelCanvasGroup != null)
        {
            currentAnimation.Join(shopPanelCanvasGroup.DOFade(1, animationDuration * 0.7f));
        }
        
        // 动画完成后重置滚动条位置
        currentAnimation.OnComplete(() => {
            StartCoroutine(ResetScrollPositionNextFrame());
        });
        
        // 播放动画
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 隐藏商店面板
    /// </summary>
    private void HideShopPanel(bool animated = true)
    {
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 在动画开始前立即清理动态生成的商品项
        ClearShopItems();
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            if (shopPanelCanvasGroup != null)
            {
                shopPanelCanvasGroup.alpha = 0;
                shopPanelCanvasGroup.interactable = false;
                shopPanelCanvasGroup.blocksRaycasts = false;
            }
            if (shopPanelRect != null)
            {
                shopPanelRect.anchoredPosition = shownPosition + hiddenPosition;
            }
            
            // 重置商店状态
            isShopOpen = false;
            
            // 如果购买确认面板还在显示，也关闭它
            if (purchaseConfirmPanel != null && purchaseConfirmPanel.activeSelf)
            {
                HidePurchaseConfirm();
            }
            
            // 通知底部面板控制器商店已关闭
            NotifyBottomPanelClosed();
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡出动画
        if (shopPanelRect != null)
        {
            currentAnimation.Join(shopPanelRect.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase));
        }
        if (shopPanelCanvasGroup != null)
        {
            currentAnimation.Join(shopPanelCanvasGroup.DOFade(0, animationDuration * 0.7f));
        }
        
        // 动画完成后设置交互状态
        currentAnimation.OnComplete(() => {
            if (shopPanelCanvasGroup != null)
            {
                shopPanelCanvasGroup.interactable = false;
                shopPanelCanvasGroup.blocksRaycasts = false;
            }
            
            // 重置商店状态
            isShopOpen = false;
            
            // 如果购买确认面板还在显示，也关闭它
            if (purchaseConfirmPanel != null && purchaseConfirmPanel.activeSelf)
            {
                HidePurchaseConfirm();
            }
            
            // 通知底部面板控制器商店已关闭
            NotifyBottomPanelClosed();
        });
        
        // 播放动画
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 通知底部面板控制器商店已关闭
    /// </summary>
    private void NotifyBottomPanelClosed()
    {
        BottomPanelController bottomPanel = FindObjectOfType<BottomPanelController>();
        if (bottomPanel != null)
        {
            bottomPanel.OnFunctionPanelClosed(BottomPanelController.FunctionType.HeartShop);
        }
    }
    
    /// <summary>
    /// 商品点击事件
    /// </summary>
    private void OnShopItemClicked(PetConfigData selectedPet)
    {
        if (selectedPet == null) return;
        
        // 检查宠物数量限制
        if (!CanPurchaseMorePets())
        {
            ShowToast(maxPetsMessage.Replace("{0}", GetCurrentPetCount().ToString()).Replace("{1}", MAX_PETS.ToString()));
            return;
        }
        
        // 显示购买确认面板
        ShowPurchaseConfirm(selectedPet);
    }
    
    /// <summary>
    /// 显示购买确认面板
    /// </summary>
    private void ShowPurchaseConfirm(PetConfigData pet)
    {
        selectedPetForPurchase = pet;
        
        if (purchaseConfirmPanel == null) return;
        
        // 显示阴影遮罩
        if (shadowCover != null)
        {
            shadowCover.SetActive(true);
        }
        
        // 隐藏关闭按钮
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }
        
        // 显示购买确认面板
        purchaseConfirmPanel.SetActive(true);
        
        // 播放动画
        purchaseConfirmPanel.transform.localScale = Vector3.zero;
        purchaseConfirmPanel.transform.DOScale(Vector3.one, animationDuration)
            .SetEase(animationEase);
    }
    
    /// <summary>
    /// 隐藏购买确认面板
    /// </summary>
    private void HidePurchaseConfirm()
    {
        if (purchaseConfirmPanel == null) return;
        
        // 播放关闭动画
        purchaseConfirmPanel.transform.DOScale(Vector3.zero, animationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                purchaseConfirmPanel.SetActive(false);
                
                // 隐藏阴影遮罩
                if (shadowCover != null)
                {
                    shadowCover.SetActive(false);
                }
                
                // 显示关闭按钮
                if (closeButton != null)
                {
                    closeButton.gameObject.SetActive(true);
                }
            });
    }
    
    /// <summary>
    /// 确认购买
    /// </summary>
    private async void OnConfirmPurchase()
    {
        if (selectedPetForPurchase == null) return;
        
        // 检查爱心货币是否充足
        int currentCurrency = PlayerManager.Instance?.HeartCurrency ?? 0;
        int requiredCurrency = selectedPetForPurchase.heartCost;
        
        if (currentCurrency < requiredCurrency)
        {
            ShowToast(insufficientFundsMessage.Replace("{0}", requiredCurrency.ToString()).Replace("{1}", currentCurrency.ToString()));
            HidePurchaseConfirm(); // 关闭确认界面
            return;
        }
        
        // 再次检查宠物数量限制
        if (!CanPurchaseMorePets())
        {
            ShowToast(maxPetsMessage.Replace("{0}", GetCurrentPetCount().ToString()).Replace("{1}", MAX_PETS.ToString()));
            HidePurchaseConfirm(); // 关闭确认界面
            return;
        }
        
        // 执行购买
        bool purchaseSuccess = await ExecutePurchase(selectedPetForPurchase);
        
        if (purchaseSuccess)
        {
            ShowToast(purchaseSuccessMessage.Replace("{0}", selectedPetForPurchase.petName ?? "未知宠物"));
        }
        else
        {
            ShowToast(purchaseFailedMessage);
        }
        
        // 隐藏购买确认面板
        HidePurchaseConfirm();
    }
    
    /// <summary>
    /// 取消购买
    /// </summary>
    private void OnCancelPurchase()
    {
        selectedPetForPurchase = null;
        HidePurchaseConfirm();
    }
    
    /// <summary>
    /// 执行购买逻辑
    /// </summary>
    private async System.Threading.Tasks.Task<bool> ExecutePurchase(PetConfigData pet)
    {
        try
        {
            // 扣除爱心货币
            if (!PlayerManager.Instance.SpendHeartCurrency(pet.heartCost))
            {
                Debug.LogError("扣除爱心货币失败");
                return false;
            }
            
            // 创建新宠物
            PetController2D newPet = await PetSpawner.Instance.CreateNewPet(pet.petPrefab.name);
            
            if (newPet != null)
            {
                // 设置宠物属性
                newPet.PetDisplayName = pet.petName;
                newPet.PetIntroduction = pet.introduction;
                newPet.Energy = (int)pet.baseEnergy;
                newPet.Satiety = (int)pet.baseSatiety;
                
                // Debug.Log($"成功购买宠物: {pet.petName}");
                return true;
            }
            else
            {
                // 如果宠物创建失败，退还货币
                PlayerManager.Instance.AddHeartCurrency(pet.heartCost);
                Debug.LogError("宠物创建失败，已退还货币");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"购买宠物时发生错误: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 检查是否可以购买更多宠物
    /// </summary>
    private bool CanPurchaseMorePets()
    {
        return GetCurrentPetCount() < MAX_PETS;
    }
    
    /// <summary>
    /// 获取当前宠物数量
    /// </summary>
    private int GetCurrentPetCount()
    {
        SaveData saveData = SaveManager.Instance?.GetCurrentSaveData();
        return saveData?.petsData?.Count ?? 0;
    }
    
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    private void ShowToast(string message)
    {
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(message);
        }
        else
        {
            Debug.LogWarning($"ToastManager未找到，消息: {message}");
        }
    }
    
    /// <summary>
    /// 刷新所有商品的价格显示状态
    /// </summary>
    private void RefreshAllItemsPriceDisplay()
    {
        foreach (var item in shopItemInstances)
        {
            if (item != null)
            {
                // 直接调用RefreshPriceDisplay方法
                item.RefreshPriceDisplay();
            }
        }
    }
    
    /// <summary>
    /// 获取最大宠物数量
    /// </summary>
    public int GetMaxPets()
    {
        return MAX_PETS;
    }
    
    /// <summary>
    /// 检查商店是否打开
    /// </summary>
    public bool IsShopOpen()
    {
        return isShopOpen;
    }
} 