using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 选中电视机信息面板UI控制器
/// </summary>
public class SelectedTVInfo : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Text tvNameText;
    [SerializeField] private Image tvImage; 
    
    [Header("控制按钮")]
    [SerializeField] private Button powerButton;
    [SerializeField] private Text powerButtonText;
    
    [Header("按钮文本配置")]
    [SerializeField] private string turnOnButtonText = "开启";   // 电视机关闭时显示的按钮文本
    [SerializeField] private string turnOffButtonText = "关闭";  // 电视机开启时显示的按钮文本
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Vector3 showScale = Vector3.one;
    [SerializeField] private Vector3 hideScale = new Vector3(0.8f, 0.8f, 0.8f);
    
    // 当前选中的电视机
    private TVController currentTV;
    
    #region Unity 生命周期
    
    private void Awake()
    {
        // 初始化UI状态 - 保持GameObject激活但面板不可见
        transform.localScale = Vector3.zero;  // 设置为完全不可见
    }
    
    private void Start()
    {
        // 设置按钮事件
        if (powerButton != null)
            powerButton.onClick.AddListener(OnPowerButtonClicked);
        
        // 监听电视机选中/取消选中事件
        EventManager.Instance.AddListener<TVController>(CustomEventType.TVSelected, OnTVSelected);
        EventManager.Instance.AddListener<TVController>(CustomEventType.TVUnselected, OnTVUnselected);
        
        // 监听其他家具选中事件，用于关闭电视机面板
        EventManager.Instance.AddListener<PlantController>(CustomEventType.PlantSelected, OnOtherFurnitureSelected);
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodSelected, OnOtherFurnitureSelected);
        EventManager.Instance.AddListener<SpeakerController>(CustomEventType.SpeakerSelected, OnOtherFurnitureSelected);
        EventManager.Instance.AddListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
    }
    
    private void OnDestroy()
    {
        // 移除事件监听
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<TVController>(CustomEventType.TVSelected, OnTVSelected);
            EventManager.Instance.RemoveListener<TVController>(CustomEventType.TVUnselected, OnTVUnselected);
            EventManager.Instance.RemoveListener<PlantController>(CustomEventType.PlantSelected, OnOtherFurnitureSelected);
            EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodSelected, OnOtherFurnitureSelected);
            EventManager.Instance.RemoveListener<SpeakerController>(CustomEventType.SpeakerSelected, OnOtherFurnitureSelected);
            EventManager.Instance.RemoveListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 电视机被选中
    /// </summary>
    private void OnTVSelected(TVController tv)
    {
        currentTV = tv;
        ShowTVInfo();
    }
    
    /// <summary>
    /// 电视机取消选中
    /// </summary>
    private void OnTVUnselected(TVController tv)
    {
        if (currentTV == tv)
        {
            HideTVInfo();
        }
    }
    
    /// <summary>
    /// 其他家具被选中时关闭电视机面板
    /// </summary>
    private void OnOtherFurnitureSelected<T>(T furniture)
    {
        if (currentTV != null)
        {
            HideTVInfo();
        }
    }
    
    /// <summary>
    /// 宠物被选中时关闭电视机面板
    /// </summary>
    private void OnPetSelected(PetController2D pet)
    {
        if (currentTV != null)
        {
            HideTVInfo();
        }
    }
    
    #endregion
    
    #region UI显示控制
    
    /// <summary>
    /// 显示电视机信息面板
    /// </summary>
    private void ShowTVInfo()
    {
        if (currentTV == null) return;
        
        // 更新UI内容
        UpdateUI();
        
        // 播放显示动画
        transform.DOScale(showScale, animationDuration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// 隐藏电视机信息面板
    /// </summary>
    private void HideTVInfo()
    {
        currentTV = null;
        
        // 播放隐藏动画 - 缩放到0来隐藏
        transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
    }
    
    /// <summary>
    /// 更新UI显示内容
    /// </summary>
    private void UpdateUI()
    {
        if (currentTV == null) return;
        
        // 更新电视机名称
        if (tvNameText != null)
            tvNameText.text = currentTV.FurnitureName;
        
        // 更新电视机图标
        if (tvImage != null)
        {
            Sprite icon = currentTV.GetIcon();
            if (icon != null)
            {
                tvImage.sprite = icon;
                tvImage.color = Color.white;
            }
            else
            {
                tvImage.color = Color.clear;
            }
        }
        
        // 更新按钮文本
        UpdatePowerButton();
    }
    
    /// <summary>
    /// 更新开关按钮
    /// </summary>
    private void UpdatePowerButton()
    {
        if (currentTV == null || powerButtonText == null) return;
        
        // 根据电视机状态设置按钮文本（使用可配置的文本）
        powerButtonText.text = currentTV.IsOn ? turnOffButtonText : turnOnButtonText;
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 开关按钮点击
    /// </summary>
    private void OnPowerButtonClicked()
    {
        if (currentTV == null) return;
        
        currentTV.TogglePower();
        
        // 更新UI显示
        UpdateUI();
        
        //Debug.Log($"[SelectedTVInfo] 电视机开关切换: {(currentTV.IsOn ? "开启" : "关闭")}");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 强制刷新UI（用于外部调用）
    /// </summary>
    public void RefreshUI()
    {
        if (currentTV != null)
        {
            UpdateUI();
        }
    }
    
    /// <summary>
    /// 检查是否显示中
    /// </summary>
    public bool IsShowing => currentTV != null && transform.localScale.x > 0.1f;
    
    #endregion
}
