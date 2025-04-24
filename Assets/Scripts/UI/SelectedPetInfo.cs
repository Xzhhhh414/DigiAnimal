using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedPetInfo : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image petAvatar;           // 宠物头像图片
    [SerializeField] private Text petName;              // 宠物名字文本 (使用Legacy Text)
    [SerializeField] private Slider energySlider;       // 精力值滑动条
    [SerializeField] private Slider satietySlider;      // 饱腹度滑动条
    [SerializeField] private Text preferenceText;       // 宠物偏好文本
    
    private CharacterController2D currentPet;           // 当前显示的宠物
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
        
        // 注册事件
        EventManager.Instance.AddListener<CharacterController2D>(CustomEventType.PetSelected, OnPetSelected);
        EventManager.Instance.AddListener(CustomEventType.PetUnselected, OnPetUnselected);
    }
    
    void OnDestroy()
    {
        // 取消注册事件
        EventManager.Instance.RemoveListener<CharacterController2D>(CustomEventType.PetSelected, OnPetSelected);
        EventManager.Instance.RemoveListener(CustomEventType.PetUnselected, OnPetUnselected);
    }
    
    // 当宠物被选中时调用
    private void OnPetSelected(CharacterController2D pet)
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
            preferenceText.text = string.IsNullOrEmpty(currentPet.Preference) ? "无特殊偏好" : currentPet.Preference;
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