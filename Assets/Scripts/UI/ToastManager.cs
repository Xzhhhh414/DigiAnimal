using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Toast消息管理器 - 控制Canvas下固定的ToastMessage对象
/// </summary>
public class ToastManager : MonoBehaviour
{
    [Header("Toast对象引用")]
    [SerializeField] private GameObject toastMessageObject; // Canvas下的固定ToastMessage对象
    [SerializeField] private Text toastText; // ToastMessage对象下的Text组件
    
    [Header("动画设置")]
    [SerializeField] private float toastDuration = 2f; // Toast显示时长
    [SerializeField] private float fadeInDuration = 0.3f; // 淡入时长
    [SerializeField] private float fadeOutDuration = 0.3f; // 淡出时长
    
    private CanvasGroup canvasGroup;
    private bool isShowing = false; // 当前是否正在显示Toast
    
    // 单例模式
    private static ToastManager _instance;
    public static ToastManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ToastManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("场景中未找到ToastManager实例！");
                }
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
        
        // 自动查找组件（如果没有在Inspector中设置）
        if (toastMessageObject == null)
        {
            // 尝试在Canvas下查找名为ToastMessage的对象
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform toastTransform = canvas.transform.Find("ToastMessage");
                if (toastTransform != null)
                {
                    toastMessageObject = toastTransform.gameObject;
                }
            }
        }
        
        if (toastText == null && toastMessageObject != null)
        {
            toastText = toastMessageObject.GetComponentInChildren<Text>();
        }
        
        // 获取或添加CanvasGroup组件
        if (toastMessageObject != null)
        {
            canvasGroup = toastMessageObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = toastMessageObject.AddComponent<CanvasGroup>();
            }
            
            // 初始状态隐藏
            HideToastImmediate();
        }
    }
    
    /// <summary>
    /// 显示Toast消息
    /// </summary>
    /// <param name="message">消息内容</param>
    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        if (toastMessageObject == null || toastText == null)
        {
            Debug.LogError("ToastManager: ToastMessage对象或Text组件未找到！请检查设置。");
            Debug.Log($"Toast (Fallback): {message}");
            return;
        }
        
        // 如果正在显示其他Toast，先停止之前的动画
        if (isShowing)
        {
            DOTween.Kill(canvasGroup);
        }
        
        // 设置文本内容
        toastText.text = message;
        
        // 确保对象激活
        toastMessageObject.SetActive(true);
        
        // 开始显示动画
        ShowToastAnimation();
    }
    
    /// <summary>
    /// 显示Toast动画
    /// </summary>
    private void ShowToastAnimation()
    {
        isShowing = true;
        
        // 设置初始状态
        canvasGroup.alpha = 0;
        
        // 淡入动画
        canvasGroup.DOFade(1, fadeInDuration)
            .OnComplete(() => {
                // 显示完成后，等待一段时间再淡出
                DOVirtual.DelayedCall(toastDuration, () => {
                    HideToastAnimation();
                });
            });
    }
    
    /// <summary>
    /// 隐藏Toast动画
    /// </summary>
    private void HideToastAnimation()
    {
        // 淡出动画
        canvasGroup.DOFade(0, fadeOutDuration)
            .OnComplete(() => {
                // 动画完成后隐藏对象
                toastMessageObject.SetActive(false);
                isShowing = false;
            });
    }
    
    /// <summary>
    /// 立即隐藏Toast（无动画）
    /// </summary>
    private void HideToastImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
        }
        if (toastMessageObject != null)
        {
            toastMessageObject.SetActive(false);
        }
        isShowing = false;
    }
    
    /// <summary>
    /// 手动隐藏Toast
    /// </summary>
    public void HideToast()
    {
        if (isShowing)
        {
            DOTween.Kill(canvasGroup);
            HideToastAnimation();
        }
    }
} 