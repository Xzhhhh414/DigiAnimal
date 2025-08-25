using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SelectedPlantInfo : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image plantImage;           // 植物图片
    [SerializeField] private Text plantName;             // 植物名称文本
    [SerializeField] private Text plantStatusText;       // 植物状态文本
    [SerializeField] private Slider healthSlider;        // 健康度滑动条
    [SerializeField] private Button wateringButton;      // 浇水按钮
    
    // 状态显示文本配置
    [Header("状态文本配置")]
    [SerializeField] private string healthyText = "生机盎然";      // 健康状态显示文本
    [SerializeField] private string thirstyText = "需要浇水";      // 缺水状态显示文本
    [SerializeField] private string witheredText = "快要枯萎";     // 枯萎状态显示文本
    

    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 100); // 隐藏时的位置偏移
    
    private PlantController currentPlant;               // 当前显示的植物
    private CanvasGroup canvasGroup;                    // 用于控制面板显示/隐藏
    private RectTransform rectTransform;                // UI面板的RectTransform
    private Vector2 shownPosition;                      // 显示时的位置
    private Sequence currentAnimation;                  // 当前运行的动画序列
    
    void Awake()
    {
        // 获取组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        // 保存原始位置作为显示位置
        shownPosition = rectTransform.anchoredPosition;
        
        // 初始化为隐藏状态
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        
        // 绑定浇水按钮事件
        if (wateringButton != null)
        {
            wateringButton.onClick.AddListener(OnWateringButtonClicked);
        }
    }
    
    /// <summary>
    /// 显示植物信息面板
    /// </summary>
    /// <param name="plant">要显示的植物</param>
    public void ShowPlantInfo(PlantController plant)
    {
        if (plant == null) return;
        
        currentPlant = plant;
        UpdatePlantInfo();
        
        // 停止之前的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
        
        // 不需要特殊处理，UpdatePlantInfo()会正确设置按钮状态
        
        // 在动画开始前立即设置交互状态，与SelectedFoodInfo保持一致
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        // 显示面板动画
        currentAnimation = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(shownPosition, animationDuration).SetEase(animationEase))
            .Join(canvasGroup.DOFade(1f, animationDuration));
    }
    
    /// <summary>
    /// 隐藏植物信息面板
    /// </summary>
    public void HidePlantInfo()
    {
        // 停止之前的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
        
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // 隐藏面板动画
        currentAnimation = DOTween.Sequence()
            .Append(rectTransform.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase))
            .Join(canvasGroup.DOFade(0f, animationDuration))
            .OnComplete(() =>
            {
                currentPlant = null;
            });
    }
    
    /// <summary>
    /// 更新植物信息显示
    /// </summary>
    private void UpdatePlantInfo()
    {
        if (currentPlant == null) return;
        
        // 更新植物名称
        if (plantName != null)
        {
            plantName.text = currentPlant.PlantName;
        }
        
        // 更新植物图片（使用植物图标）
        if (plantImage != null && currentPlant.PlantIcon != null)
        {
            plantImage.sprite = currentPlant.PlantIcon;
        }
        
        // 更新健康度滑动条
        if (healthSlider != null)
        {
            healthSlider.value = currentPlant.HealthLevel / 100f;
        }
        
        // 更新植物状态文本
        if (plantStatusText != null)
        {
            plantStatusText.text = GetStateDisplayText(currentPlant.CurrentState);
        }
        
        // 更新浇水按钮
        UpdateWateringButton();
    }
    
    /// <summary>
    /// 获取状态显示文本
    /// </summary>
    private string GetStateDisplayText(PlantController.PlantState state)
    {
        switch (state)
        {
            case PlantController.PlantState.Healthy:
                return healthyText;
            case PlantController.PlantState.Thirsty:
                return thirstyText;
            case PlantController.PlantState.Withered:
                return witheredText;
            default:
                return "未知状态";
        }
    }
    
    /// <summary>
    /// 更新浇水按钮状态
    /// </summary>
    private void UpdateWateringButton()
    {
        if (currentPlant == null || wateringButton == null) return;
        
        // 健康度不是最大时显示按钮，否则隐藏按钮
        bool needsWatering = currentPlant.HealthLevel < 100;
        
        // 简单的显示/隐藏逻辑，和食物按钮一致
        wateringButton.gameObject.SetActive(needsWatering);
        
    }
    
    /// <summary>
    /// 浇水按钮点击事件
    /// </summary>
    private void OnWateringButtonClicked()
    {
        if (currentPlant == null) return;
        
        // 尝试带视觉效果的浇水
        bool success = currentPlant.TryWateringWithEffects(
            onWateringStart: OnWateringStart,
            onWateringComplete: OnWateringComplete
        );
        
        if (!success)
        {
            // 浇水失败（通常是因为植物已经满血）
            // 不显示失败提示，只播放震动效果
        }
    }
    
    /// <summary>
    /// 浇水开始回调
    /// </summary>
    private void OnWateringStart()
    {
        // 浇水期间隐藏浇水按钮
        if (wateringButton != null)
        {
            wateringButton.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 浇水完成回调
    /// </summary>
    private void OnWateringComplete()
    {
        // 更新UI显示（包括按钮状态）
        UpdatePlantInfo();
        
        // 播放浇水成功动画效果
        PlayWateringSuccessEffect();
    }
    
    /// <summary>
    /// 播放浇水成功效果
    /// </summary>
    private void PlayWateringSuccessEffect()
    {
        // 健康度滑动条闪烁效果
        if (healthSlider != null)
        {
            healthSlider.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 0.5f);
        }
        
        // 植物图片闪烁效果
        if (plantImage != null)
        {
            plantImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5, 0.5f);
        }
    }
    
    

    
    /// <summary>
    /// 定期更新植物信息（用于实时显示健康度变化）
    /// </summary>
    void Update()
    {
        // 如果面板正在显示且有当前植物，定期更新信息
        if (currentPlant != null && canvasGroup.alpha > 0.5f)
        {
            // 每0.5秒更新一次，避免过于频繁
            if (Time.frameCount % 30 == 0) // 假设60FPS，每0.5秒更新一次
            {
                UpdatePlantInfo();
            }
        }
    }
    
    /// <summary>
    /// 面板是否正在显示
    /// </summary>
    public bool IsShowing
    {
        get { return canvasGroup.alpha > 0.5f; }
    }
    
    /// <summary>
    /// 获取当前显示的植物
    /// </summary>
    public PlantController CurrentPlant
    {
        get { return currentPlant; }
    }
}