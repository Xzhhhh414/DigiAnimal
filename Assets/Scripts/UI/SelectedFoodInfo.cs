using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // 添加DOTween命名空间

public class SelectedFoodInfo : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image foodImage;           // 食物图片
    [SerializeField] private Text foodName;             // 食物名称文本
    [SerializeField] private GameObject[] starImages;   // 美味度星星数组，应包含5个星星图像对象
    [SerializeField] private Text foodStatusText;       // 食物状态文本（空盘/可用）
    [SerializeField] private Button refillButton;       // 添加食物按钮
    
    // 状态显示文本
    [Header("状态文本配置")]
    [SerializeField] private string emptyText = "被吃光啦";   // 空盘状态显示文本
    [SerializeField] private string availableText = "满满一碗"; // 可用状态显示文本
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 100); // 隐藏时的位置偏移
    
    private FoodController currentFood;                 // 当前显示的食物
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
        
        // 初始状态隐藏面板
        HidePanel(false); // 不使用动画立即隐藏
        
        // 注册事件（需要先定义食物选择的事件类型）
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodSelected, OnFoodSelected);
        EventManager.Instance.AddListener(CustomEventType.FoodUnselected, OnFoodUnselected);
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodStatusChanged, OnFoodStatusChanged);
        
        // 添加按钮点击事件
        if (refillButton != null)
        {
            refillButton.onClick.AddListener(OnRefillButtonClicked);
        }
    }
    
    void OnDestroy()
    {
        // 清理可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 取消注册事件
        EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodSelected, OnFoodSelected);
        EventManager.Instance.RemoveListener(CustomEventType.FoodUnselected, OnFoodUnselected);
        EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodStatusChanged, OnFoodStatusChanged);
        
        // 移除按钮点击事件
        if (refillButton != null)
        {
            refillButton.onClick.RemoveListener(OnRefillButtonClicked);
        }
    }
    
    // 当食物被选中时调用
    private void OnFoodSelected(FoodController food)
    {
        if (food != null)
        {
            currentFood = food;
            UpdateFoodInfo();
            ShowPanel();
        }
    }
    
    // 当食物取消选中时调用
    private void OnFoodUnselected()
    {
        currentFood = null;
        HidePanel();
    }
    
    // 当食物状态改变时调用
    private void OnFoodStatusChanged(FoodController food)
    {
        if (food == currentFood && currentFood != null)
        {
            UpdateFoodInfo();
        }
    }
    
    // 添加食物按钮点击事件处理
    private void OnRefillButtonClicked()
    {
        if (currentFood != null && currentFood.IsEmpty)
        {
            // 调用食物控制器的填满方法
            currentFood.RefillFood();
            
            // 更新UI显示
            UpdateFoodInfo();
        }
    }
    
    // 更新面板信息
    private void UpdateFoodInfo()
    {
        if (currentFood == null) return;
        
        // 设置食物图片
        if (foodImage != null)
        {
            // 使用FoodController中的FoodIcon属性
            foodImage.sprite = currentFood.FoodIcon;
            foodImage.preserveAspect = true;
        }
        
        // 设置食物名称
        if (foodName != null)
        {
            // 获取游戏对象名称，去除Clone后缀
            string objName = currentFood.gameObject.name;
            if (objName.EndsWith("(Clone)"))
            {
                objName = objName.Substring(0, objName.Length - 7);
            }
            foodName.text = objName;
        }
        
        // 设置美味度星星
        UpdateStars(currentFood.Tasty);
        
        // 更新食物状态文本
        UpdateFoodStatus(currentFood.IsEmpty);
        
        // 更新添加按钮显示状态
        UpdateRefillButtonVisibility(currentFood.IsEmpty);
    }
    
    // 更新美味度星星显示
    private void UpdateStars(int deliciousness)
    {
        if (starImages == null || starImages.Length == 0) return;
        
        // 确保美味度值在有效范围内
        deliciousness = Mathf.Clamp(deliciousness, 1, 5);
        
        // 激活相应数量的星星
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].SetActive(i < deliciousness);
            }
        }
    }
    
    // 更新食物状态文本
    private void UpdateFoodStatus(bool isEmpty)
    {
        if (foodStatusText == null) return;
        
        // 只更新文本内容，不再区分颜色
        foodStatusText.text = isEmpty ? emptyText : availableText;
    }
    
    // 更新添加按钮显示状态
    private void UpdateRefillButtonVisibility(bool isEmpty)
    {
        if (refillButton != null)
        {
            // 只有在食物为空盘状态时显示添加按钮
            refillButton.gameObject.SetActive(isEmpty);
        }
    }
    
    // 显示面板（带动画）
    private void ShowPanel(bool animated = true)
    {
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        // 确保面板处于可交互状态
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = shownPosition;
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 如果当前是隐藏状态，先设置初始位置
        if (canvasGroup.alpha < 0.1f)
        {
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
        }
        
        // 添加移动和淡入动画
        currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition, animationDuration).SetEase(animationEase))
                       .Join(canvasGroup.DOFade(1, animationDuration * 0.7f));
        
        // 播放动画
        currentAnimation.Play();
    }
    
    // 隐藏面板（带动画）
    private void HidePanel(bool animated = true)
    {
        // 停止当前可能正在运行的动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
        
        if (!animated)
        {
            // 不使用动画，直接设置最终状态
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            rectTransform.anchoredPosition = shownPosition + hiddenPosition;
            return;
        }
        
        // 创建动画序列
        currentAnimation = DOTween.Sequence();
        
        // 添加移动和淡出动画（使用与显示相同的缓动效果）
        currentAnimation.Join(rectTransform.DOAnchorPos(shownPosition + hiddenPosition, animationDuration).SetEase(animationEase))
                       .Join(canvasGroup.DOFade(0, animationDuration * 0.7f));
        
        // 动画完成后设置交互状态
        currentAnimation.OnComplete(() => {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
        
        // 播放动画
        currentAnimation.Play();
    }
} 