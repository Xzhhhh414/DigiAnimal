using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // 添加DOTween命名空间

public class SelectedPetInfo : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image petAvatar;           // 宠物头像图片
    [SerializeField] private Text petName;              // 宠物名字文本 (使用Legacy Text)
    [SerializeField] private Slider energySlider;       // 精力值滑动条
    [SerializeField] private Slider satietySlider;      // 饱腹度滑动条
    [SerializeField] private Text preferenceText;       // 宠物偏好文本
    [SerializeField] private Button nameChangeButton;   // 改名按钮
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f; // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 100); // 隐藏时的位置偏移
    
    private PetController2D currentPet;           // 当前显示的宠物
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
        
        // 设置改名按钮事件
        if (nameChangeButton != null)
        {
            nameChangeButton.onClick.AddListener(OnNameChangeButtonClicked);
        }
        
        // 注册事件
        EventManager.Instance.AddListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
        EventManager.Instance.AddListener(CustomEventType.PetUnselected, OnPetUnselected);
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
        EventManager.Instance.RemoveListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
        EventManager.Instance.RemoveListener(CustomEventType.PetUnselected, OnPetUnselected);
    }
    
    // 当宠物被选中时调用
    private void OnPetSelected(PetController2D pet)
    {
        if (pet != null)
        {
            currentPet = pet;
            UpdatePetInfo();
            ShowPanel();
        }
    }
    
    // 当宠物取消选中时调用
    private void OnPetUnselected()
    {
        currentPet = null;
        HidePanel();
    }
    
    // 更新面板信息
    private void UpdatePetInfo()
    {
        if (currentPet == null) return;
        
        // 设置宠物头像
        if (petAvatar != null)
        {
            petAvatar.sprite = currentPet.PetProfileImage;
            petAvatar.preserveAspect = true;
        }
        
        // 设置宠物名字
        if (petName != null)
        {
            petName.text = currentPet.PetDisplayName;
        }
        
        // 设置精力值
        if (energySlider != null)
        {
            energySlider.value = currentPet.Energy / 100f; // 精力值最大为100
        }
        
        // 设置饱腹度
        if (satietySlider != null)
        {
            satietySlider.value = currentPet.Satiety / 100f; // 饱腹度最大为100
        }
        
        // 设置宠物偏好
        if (preferenceText != null)
        {
            preferenceText.text = string.IsNullOrEmpty(currentPet.PetIntroduction) ? "无特殊偏好" : currentPet.PetIntroduction;
        }
    }
    
    void Update()
    {
        // 如果有宠物处于选中状态，持续更新状态值
        if (currentPet != null)
        {
            // 更新精力值
            if (energySlider != null)
            {
                energySlider.value = Mathf.Lerp(energySlider.value, currentPet.Energy / 100f, Time.deltaTime * 5f);
            }
            
            // 更新饱腹度
            if (satietySlider != null)
            {
                satietySlider.value = Mathf.Lerp(satietySlider.value, currentPet.Satiety / 100f, Time.deltaTime * 5f);
            }
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
    
    /// <summary>
    /// 改名按钮点击事件
    /// </summary>
    private void OnNameChangeButtonClicked()
    {
        if (currentPet == null)
        {
            Debug.LogError("当前没有选中的宠物");
            return;
        }
        
        // 暂时禁用改名功能，显示Toast提示
        // TODO: 稍后实现详情界面的改名功能
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast("改名功能开发中，敬请期待！");
        }
        else
        {
            Debug.Log("改名功能暂未实现");
        }
    }
} 