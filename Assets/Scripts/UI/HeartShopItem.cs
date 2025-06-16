using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爱心商店商品项 - 管理单个商品的显示和交互
/// </summary>
public class HeartShopItem : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Button itemButton;           // 商品按钮
    [SerializeField] private Image petIcon;               // 宠物头像
    [SerializeField] private Text petNameText;            // 宠物名字
    [SerializeField] private Text priceText;              // 价格文本
    [SerializeField] private Image heartIcon;             // 爱心图标（可选）
    
    // 当前商品的宠物数据
    private PetConfigData petData;
    
    // 点击事件回调
    private System.Action<PetConfigData> onItemClicked;
    
    private void Awake()
    {
        // 自动查找组件（如果没有在Inspector中设置）
        if (itemButton == null)
            itemButton = GetComponent<Button>();
            
        if (petIcon == null)
            petIcon = transform.Find("Icon")?.GetComponent<Image>();
            
        if (petNameText == null)
            petNameText = transform.Find("Name")?.GetComponent<Text>();
            
        if (priceText == null)
            priceText = transform.Find("CostValue")?.GetComponent<Text>();
            
        if (heartIcon == null)
            heartIcon = transform.Find("HeartIcon")?.GetComponent<Image>();
    }
    
    private void Start()
    {
        // 设置按钮点击事件
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(OnItemClick);
        }
    }
    
    /// <summary>
    /// 初始化商品数据
    /// </summary>
    /// <param name="pet">宠物配置数据</param>
    /// <param name="clickCallback">点击回调</param>
    public void Initialize(PetConfigData pet, System.Action<PetConfigData> clickCallback)
    {
        petData = pet;
        onItemClicked = clickCallback;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// 更新显示内容
    /// </summary>
    private void UpdateDisplay()
    {
        if (petData == null) return;
        
        // 设置宠物头像
        if (petIcon != null && petData.headIconImage != null)
        {
            petIcon.sprite = petData.headIconImage;
        }
        
        // 设置宠物名字
        if (petNameText != null)
        {
            petNameText.text = petData.petName;
        }
        
        // 设置价格
        if (priceText != null)
        {
            priceText.text = petData.heartCost.ToString();
        }
    }
    
    /// <summary>
    /// 商品点击事件
    /// </summary>
    private void OnItemClick()
    {
        if (petData != null && onItemClicked != null)
        {
            onItemClicked.Invoke(petData);
        }
    }
    
    /// <summary>
    /// 获取当前宠物数据
    /// </summary>
    public PetConfigData GetPetData()
    {
        return petData;
    }
    
    /// <summary>
    /// 设置商品可交互性
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (itemButton != null)
        {
            itemButton.interactable = interactable;
        }
    }
    
    /// <summary>
    /// 刷新价格显示（用于货币变化时更新显示状态）
    /// </summary>
    public void RefreshPriceDisplay()
    {
        if (petData == null) return;
        
        // 检查玩家是否有足够的货币
        int currentCurrency = PlayerManager.Instance?.HeartCurrency ?? 0;
        bool canAfford = currentCurrency >= petData.heartCost;
        
        // 根据是否买得起来调整显示效果
        if (priceText != null)
        {
            priceText.color = canAfford ? Color.white : Color.red;
        }
        
        // 设置按钮可交互性
        SetInteractable(canAfford);
    }
} 