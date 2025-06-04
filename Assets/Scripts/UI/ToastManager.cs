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
    [SerializeField] private GameObject toastMessagePrefab; // ToastMessage预制体
    [SerializeField] private Canvas targetCanvas; // 目标Canvas
    
    // 运行时创建的对象
    private GameObject currentToastObject; // 当前显示的Toast对象
    private Text toastText; // 当前Toast对象的Text组件
    
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
        
        // 自动查找Canvas（如果没有在Inspector中设置）
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("ToastManager: 未找到Canvas！请确保场景中有Canvas组件。");
            }
        }
        
        // 如果没有设置prefab，尝试从Resources加载
        if (toastMessagePrefab == null)
        {
            toastMessagePrefab = Resources.Load<GameObject>("UI/ToastMessage");
            if (toastMessagePrefab == null)
            {
                Debug.LogWarning("ToastManager: 未找到ToastMessage预制体！请在Inspector中设置或确保Resources/UI/ToastMessage.prefab存在。");
            }
        }
    }
    
    /// <summary>
    /// 显示Toast消息
    /// </summary>
    /// <param name="message">消息内容</param>
    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        if (toastMessagePrefab == null || targetCanvas == null)
        {
            Debug.LogError("ToastManager: ToastMessage预制体或Canvas未设置！请检查设置。");
            Debug.Log($"Toast (Fallback): {message}");
            return;
        }
        
        // 如果正在显示其他Toast，先隐藏之前的
        if (isShowing && currentToastObject != null)
        {
            DOTween.Kill(canvasGroup);
            DestroyCurrentToast();
        }
        
        // 创建新的Toast对象
        CreateToastObject();
        
        if (currentToastObject == null || toastText == null)
        {
            Debug.LogError("ToastManager: 创建Toast对象失败！");
            Debug.Log($"Toast (Fallback): {message}");
            return;
        }
        
        // 设置文本内容
        toastText.text = message;
        
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
    /// 创建Toast对象
    /// </summary>
    private void CreateToastObject()
    {
        if (toastMessagePrefab == null || targetCanvas == null) return;
        
        // 实例化Toast对象
        currentToastObject = Instantiate(toastMessagePrefab, targetCanvas.transform);
        
        // 获取组件
        toastText = currentToastObject.GetComponentInChildren<Text>();
        if (toastText == null)
        {
            toastText = currentToastObject.GetComponent<Text>();
        }
        
        // 获取或添加CanvasGroup组件
        canvasGroup = currentToastObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = currentToastObject.AddComponent<CanvasGroup>();
        }
        
        // 设置初始状态
        canvasGroup.alpha = 0;
        currentToastObject.SetActive(true);
    }
    
    /// <summary>
    /// 销毁当前Toast对象
    /// </summary>
    private void DestroyCurrentToast()
    {
        if (currentToastObject != null)
        {
            Destroy(currentToastObject);
            currentToastObject = null;
            toastText = null;
            canvasGroup = null;
        }
        isShowing = false;
    }
    
    /// <summary>
    /// 隐藏Toast动画
    /// </summary>
    private void HideToastAnimation()
    {
        if (canvasGroup == null) return;
        
        // 淡出动画
        canvasGroup.DOFade(0, fadeOutDuration)
            .OnComplete(() => {
                // 动画完成后销毁对象
                DestroyCurrentToast();
            });
    }
    
    /// <summary>
    /// 手动隐藏Toast
    /// </summary>
    public void HideToast()
    {
        if (isShowing && canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
            HideToastAnimation();
        }
    }
} 