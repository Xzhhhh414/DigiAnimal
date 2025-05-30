using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 爱心商店按钮控制器 - 显示当前爱心货币数量并处理商店入口
/// </summary>
public class HeartShopButtonController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button shopButton;                    // 商店按钮
    [SerializeField] private Text heartCountText;                  // 爱心数量文本（Legacy Text）
    
    // 当前显示的爱心数量
    private int currentDisplayedCount = 0;
    
    private void Awake()
    {
        // 自动查找组件（如果没有在Inspector中设置）
        if (shopButton == null)
            shopButton = GetComponent<Button>();
            
        if (heartCountText == null)
            heartCountText = GetComponentInChildren<Text>();
    }
    
    private void Start()
    {
        // 设置按钮点击事件
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }
        
        // 订阅PlayerManager的货币变化事件
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            
            // 初始化显示当前货币数量
            UpdateHeartCount(PlayerManager.Instance.HeartCurrency);
        }
        else
        {
            Debug.LogWarning("HeartShopButtonController: PlayerManager.Instance为空，无法获取爱心货币信息");
            UpdateHeartCount(0);
        }
    }
    
    private void OnDestroy()
    {
        // 取消订阅事件（只有在PlayerManager仍然有效时才取消订阅）
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
    }
    
    /// <summary>
    /// 货币变化事件处理
    /// </summary>
    /// <param name="newAmount">新的货币数量</param>
    private void OnCurrencyChanged(int newAmount)
    {
        UpdateHeartCount(newAmount);
    }
    
    /// <summary>
    /// 更新爱心数量显示
    /// </summary>
    /// <param name="newCount">新的数量</param>
    private void UpdateHeartCount(int newCount)
    {
        currentDisplayedCount = newCount;
        UpdateDisplayText();
    }
    
    /// <summary>
    /// 更新显示文本
    /// </summary>
    private void UpdateDisplayText()
    {
        if (heartCountText != null)
        {
            heartCountText.text = currentDisplayedCount.ToString();
        }
    }
    
    /// <summary>
    /// 商店按钮点击事件
    /// </summary>
    private void OnShopButtonClick()
    {
        int currentCurrency = PlayerManager.Instance?.HeartCurrency ?? 0;
        Debug.Log("爱心商店按钮被点击！当前爱心货币: " + currentCurrency);
        
        // TODO: 在这里添加打开商店界面的逻辑
        // 例如：
        // ShopManager.Instance.OpenShop();
        // 或者触发事件：
        // EventManager.Instance.TriggerEvent(CustomEventType.OpenShop);
        
        // 临时提示
        Debug.Log("商店功能开发中...");
    }
    
    /// <summary>
    /// 获取当前显示的爱心数量
    /// </summary>
    public int GetCurrentHeartCount()
    {
        return currentDisplayedCount;
    }
    
    /// <summary>
    /// 手动刷新爱心数量显示
    /// </summary>
    public void RefreshHeartCount()
    {
        if (PlayerManager.Instance != null)
        {
            UpdateHeartCount(PlayerManager.Instance.HeartCurrency);
        }
        else
        {
            // 如果PlayerManager不可用，显示0
            UpdateHeartCount(0);
        }
    }
    
    /// <summary>
    /// 设置按钮可交互性
    /// </summary>
    /// <param name="interactable">是否可交互</param>
    public void SetInteractable(bool interactable)
    {
        if (shopButton != null)
        {
            shopButton.interactable = interactable;
        }
    }
} 