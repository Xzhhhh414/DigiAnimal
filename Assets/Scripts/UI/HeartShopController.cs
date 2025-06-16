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
    
    [Header("Toast提示文本")]
    [SerializeField] private string maxPetsMessage = "宠物数量已达上限（{0}/{1}），无法购买更多宠物";
    [SerializeField] private string insufficientFundsMessage = "爱心货币不足，需要{0}个爱心，当前只有{1}个";
    [SerializeField] private string purchaseSuccessMessage = "成功购买{0}！";
    [SerializeField] private string purchaseFailedMessage = "购买失败，请稍后重试";
    
    // 私有变量
    private List<PetConfigData> shopPets = new List<PetConfigData>();
    private List<MonoBehaviour> shopItemInstances = new List<MonoBehaviour>(); // 动态生成的商品实例
    private PetConfigData selectedPetForPurchase;
    private const int MAX_PETS = 4;                                  // 最大宠物数量
    
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
        InitializeShop();
        SetupEventListeners();
        
        // 初始时隐藏商店面板
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        
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
            Debug.Log($"加载了{shopPets.Count}个商店宠物");
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
                         var shopItem = itemObj.GetComponent<MonoBehaviour>();
            
                         if (shopItem != null)
             {
                 // 使用反射调用Initialize方法
                 var initializeMethod = shopItem.GetType().GetMethod("Initialize");
                 if (initializeMethod != null)
                 {
                     initializeMethod.Invoke(shopItem, new object[] { pet, (System.Action<PetConfigData>)OnShopItemClicked });
                     shopItemInstances.Add(shopItem);
                 }
                 else
                 {
                     Debug.LogError("HeartShopItem组件缺少Initialize方法！");
                     DestroyImmediate(itemObj);
                 }
             }
             else
             {
                 Debug.LogError("HeartShopItem预制体缺少组件！");
                 DestroyImmediate(itemObj);
             }
        }
        
        Debug.Log($"动态生成了{shopItemInstances.Count}个商品项");
    }
    
    /// <summary>
    /// 打开商店
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel == null) return;
        
        // 刷新商店数据
        LoadShopPets();
        UpdateShopDisplay();
        
        // 显示商店面板
        shopPanel.SetActive(true);
        
        // 播放打开动画
        shopPanel.transform.localScale = Vector3.zero;
        shopPanel.transform.DOScale(Vector3.one, animationDuration)
            .SetEase(animationEase);
    }
    
    /// <summary>
    /// 关闭商店
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel == null) return;
        
        // 播放关闭动画
        shopPanel.transform.DOScale(Vector3.zero, animationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                shopPanel.SetActive(false);
                
                // 如果购买确认面板还在显示，也关闭它
                if (purchaseConfirmPanel != null && purchaseConfirmPanel.activeSelf)
                {
                    HidePurchaseConfirm();
                }
            });
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
            ShowToast(string.Format(maxPetsMessage, GetCurrentPetCount(), MAX_PETS));
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
            ShowToast(string.Format(insufficientFundsMessage, requiredCurrency, currentCurrency));
            return;
        }
        
        // 再次检查宠物数量限制
        if (!CanPurchaseMorePets())
        {
            ShowToast(string.Format(maxPetsMessage, GetCurrentPetCount(), MAX_PETS));
            return;
        }
        
        // 执行购买
        bool purchaseSuccess = await ExecutePurchase(selectedPetForPurchase);
        
        if (purchaseSuccess)
        {
            ShowToast(string.Format(purchaseSuccessMessage, selectedPetForPurchase.petName));
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
                newPet.PetIntroduction = pet.description;
                newPet.Energy = (int)pet.baseEnergy;
                newPet.Satiety = (int)pet.baseSatiety;
                
                Debug.Log($"成功购买宠物: {pet.petName}");
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
                // 使用反射调用RefreshPriceDisplay方法
                var refreshMethod = item.GetType().GetMethod("RefreshPriceDisplay");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(item, null);
                }
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
} 