using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedFoodInfo : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image foodImage;           // 食物图片
    [SerializeField] private Text foodName;             // 食物名称文本
    [SerializeField] private GameObject[] starImages;   // 美味度星星数组，应包含5个星星图像对象
    [SerializeField] private Text foodStatusText;       // 食物状态文本（空盘/可用）
    
    // 状态显示文本
    [Header("状态文本配置")]
    [SerializeField] private string emptyText = "被吃光啦";   // 空盘状态显示文本
    [SerializeField] private string availableText = "可供食用"; // 可用状态显示文本
    
    private FoodController currentFood;                 // 当前显示的食物
    private CanvasGroup canvasGroup;                    // 用于控制面板显示/隐藏
    
    void Awake()
    {
        // 获取CanvasGroup组件，如果没有则添加
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始状态隐藏面板
        HidePanel();
        
        // 注册事件（需要先定义食物选择的事件类型）
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodSelected, OnFoodSelected);
        EventManager.Instance.AddListener(CustomEventType.FoodUnselected, OnFoodUnselected);
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodStatusChanged, OnFoodStatusChanged);
    }
    
    void OnDestroy()
    {
        // 取消注册事件
        EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodSelected, OnFoodSelected);
        EventManager.Instance.RemoveListener(CustomEventType.FoodUnselected, OnFoodUnselected);
        EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodStatusChanged, OnFoodStatusChanged);
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
    
    // 显示面板
    private void ShowPanel()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    // 隐藏面板
    private void HidePanel()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
} 