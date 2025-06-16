using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 宠物信息编辑弹窗
/// 功能：允许玩家修改宠物的名字和简介
/// </summary>
public class PetInfoEditDialog : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private InputField nameInputField;        // 宠物名字输入框
    [SerializeField] private InputField introInputField;       // 宠物简介输入框
    [SerializeField] private Button confirmButton;             // 确认按钮
    [SerializeField] private Button cancelButton;              // 取消按钮
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;   // 动画持续时间
    [SerializeField] private Ease animationEase = Ease.OutBack; // 动画缓动效果
    
    [Header("输入限制")]
    [SerializeField] private int maxNameLength = 10;           // 名字最大显示长度（中文=2，英文=1）
    [SerializeField] private int maxIntroLength = 50;          // 简介最大显示长度（中文=2，英文=1）
    
    [Header("Toast提示文本")]
    [SerializeField] private string emptyNameMessage = "宠物名字不能为空";
    [SerializeField] private string nameTooLongMessage = "宠物名字过长，当前长度：{0}，最大长度：{1}";
    [SerializeField] private string introTooLongMessage = "宠物简介过长，当前长度：{0}，最大长度：{1}";
    
    // 私有变量
    private PetController2D currentPet;                        // 当前编辑的宠物
    private System.Action<string, string> onConfirmCallback;   // 确认回调
    private CanvasGroup canvasGroup;                           // 用于控制显示/隐藏
    private RectTransform rectTransform;                       // UI变换组件
    private Sequence currentAnimation;                         // 当前动画序列
    
    // 原始输入值（用于取消时恢复）
    private string originalName;
    private string originalIntroduction;
    
    void Awake()
    {
        // 获取组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        // 设置按钮事件
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        // 设置输入框事件监听（不设置characterLimit，使用自定义长度验证）
        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);
        }
        
        if (introInputField != null)
        {
            introInputField.onValueChanged.AddListener(OnIntroInputChanged);
        }
        
        // 初始状态隐藏
        gameObject.SetActive(false);
    }
    
    void OnDestroy()
    {
        // 清理动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }
    }
    
    /// <summary>
    /// 打开弹窗
    /// </summary>
    /// <param name="pet">要编辑的宠物</param>
    /// <param name="onConfirm">确认回调</param>
    public void OpenDialog(PetController2D pet, System.Action<string, string> onConfirm)
    {
        if (pet == null)
        {
            Debug.LogError("PetInfoEditDialog: 宠物参数为空");
            return;
        }
        
        currentPet = pet;
        onConfirmCallback = onConfirm;
        
        // 保存原始值
        originalName = pet.PetDisplayName;
        originalIntroduction = pet.PetIntroduction;
        
        // 设置输入框初始值
        if (nameInputField != null)
        {
            nameInputField.text = originalName;
        }
        
        if (introInputField != null)
        {
            introInputField.text = string.IsNullOrEmpty(originalIntroduction) ? "" : originalIntroduction;
        }
        
        // 显示弹窗
        ShowDialog();
        
        // 验证初始状态
        ValidateInput();
    }
    
    /// <summary>
    /// 显示弹窗（带动画）
    /// </summary>
    private void ShowDialog()
    {
        gameObject.SetActive(true);
        
        // 停止当前动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
        
        // 设置初始状态
        canvasGroup.alpha = 0;
        rectTransform.localScale = Vector3.one * 0.8f;
        
        // 创建显示动画
        currentAnimation = DOTween.Sequence();
        currentAnimation.Join(canvasGroup.DOFade(1, animationDuration))
                       .Join(rectTransform.DOScale(Vector3.one, animationDuration).SetEase(animationEase));
        
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 隐藏弹窗（带动画）
    /// </summary>
    private void HideDialog()
    {
        // 停止当前动画
        if (currentAnimation != null && currentAnimation.IsActive())
        {
            currentAnimation.Kill();
        }
        
        // 创建隐藏动画
        currentAnimation = DOTween.Sequence();
        currentAnimation.Join(canvasGroup.DOFade(0, animationDuration * 0.7f))
                       .Join(rectTransform.DOScale(Vector3.one * 0.8f, animationDuration * 0.7f).SetEase(Ease.InBack));
        
        // 动画完成后隐藏GameObject
        currentAnimation.OnComplete(() => {
            gameObject.SetActive(false);
        });
        
        currentAnimation.Play();
    }
    
    /// <summary>
    /// 确认按钮点击事件
    /// </summary>
    private void OnConfirmClicked()
    {
        string newName = nameInputField != null ? nameInputField.text.Trim() : originalName;
        string newIntro = introInputField != null ? introInputField.text.Trim() : originalIntroduction;
        
        // 验证输入并显示Toast提示
        string validationMessage = GetValidationMessage(newName, newIntro);
        if (!string.IsNullOrEmpty(validationMessage))
        {
            ShowToast(validationMessage);
            return;
        }
        
        // 调用回调
        onConfirmCallback?.Invoke(newName, newIntro);
        
        // 隐藏弹窗
        HideDialog();
    }
    
    /// <summary>
    /// 取消按钮点击事件
    /// </summary>
    private void OnCancelClicked()
    {
        // 直接隐藏弹窗，不保存任何更改
        HideDialog();
    }
    
    /// <summary>
    /// 名字输入变化事件
    /// </summary>
    private void OnNameInputChanged(string value)
    {
        ValidateInput();
    }
    
    /// <summary>
    /// 简介输入变化事件
    /// </summary>
    private void OnIntroInputChanged(string value)
    {
        ValidateInput();
    }
    
    /// <summary>
    /// 计算字符串的显示长度（中文字符=2，英文字符=1）
    /// </summary>
    /// <param name="text">要计算的字符串</param>
    /// <returns>显示长度</returns>
    private int GetDisplayLength(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
            
        int length = 0;
        foreach (char c in text)
        {
            // 判断是否为中文字符（基本汉字范围）
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                length += 2; // 中文字符占2个长度
            }
            else
            {
                length += 1; // 其他字符占1个长度
            }
        }
        return length;
    }
    
    /// <summary>
    /// 验证输入内容（仅用于更新按钮状态，不再阻止输入）
    /// </summary>
    private bool ValidateInput()
    {
        // 现在总是返回true，允许用户继续输入
        // 确认按钮始终可点击，验证在点击时进行
        if (confirmButton != null)
        {
            confirmButton.interactable = true;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取验证错误消息
    /// </summary>
    /// <param name="name">宠物名字</param>
    /// <param name="intro">宠物简介</param>
    /// <returns>错误消息，如果验证通过则返回null</returns>
    private string GetValidationMessage(string name, string intro)
    {
        // 验证名字
        if (string.IsNullOrEmpty(name))
        {
            return emptyNameMessage;
        }
        
        int nameLength = GetDisplayLength(name);
        if (nameLength > maxNameLength)
        {
            return string.Format(nameTooLongMessage, nameLength, maxNameLength);
        }
        
        // 验证简介长度（简介可以为空）
        int introLength = GetDisplayLength(intro);
        if (introLength > maxIntroLength)
        {
            return string.Format(introTooLongMessage, introLength, maxIntroLength);
        }
        
        return null; // 验证通过
    }
    
    /// <summary>
    /// 显示Toast提示
    /// </summary>
    /// <param name="message">提示消息</param>
    private void ShowToast(string message)
    {
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(message);
        }
        else
        {
            Debug.LogWarning($"未找到ToastManager，消息：{message}");
        }
    }
    
    /// <summary>
    /// 检查是否有未保存的更改
    /// </summary>
    private bool HasUnsavedChanges()
    {
        string currentName = nameInputField != null ? nameInputField.text.Trim() : originalName;
        string currentIntro = introInputField != null ? introInputField.text.Trim() : originalIntroduction;
        
        return currentName != originalName || currentIntro != (originalIntroduction ?? "");
    }
} 