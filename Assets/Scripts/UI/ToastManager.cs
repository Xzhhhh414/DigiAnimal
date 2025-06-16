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
    
    [Header("自适应大小设置")]
    [SerializeField] private float minWidth = 200f; // 最小宽度
    [SerializeField] private float maxWidth = 800f; // 最大宽度
    [SerializeField] private float paddingHorizontal = 50f; // 水平内边距（左右各一半）
    [SerializeField] private float fixedHeight = 175f; // 固定高度
    
    private CanvasGroup canvasGroup;
    private RectTransform toastRectTransform; // Toast根对象的RectTransform
    private bool isShowing = false; // 当前是否正在显示Toast
    private Tween hideDelayTweener; // 隐藏延迟动画的引用
    
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
        
        // 如果正在显示其他Toast，先停止之前的动画
        if (isShowing && currentToastObject != null)
        {
            // 停止所有相关的动画
            DOTween.Kill(canvasGroup);
            if (hideDelayTweener != null && hideDelayTweener.IsActive())
            {
                hideDelayTweener.Kill();
                hideDelayTweener = null;
            }
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
        
        // 根据文本长度调整Toast大小
        AdjustToastSize(message);
        
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
                // 显示完成后，等待一段时间再淡出（重置计时器）
                hideDelayTweener = DOVirtual.DelayedCall(toastDuration, () => {
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
        
        // 获取Toast根对象的RectTransform
        toastRectTransform = currentToastObject.GetComponent<RectTransform>();
        
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
    /// 根据文本长度调整Toast大小
    /// </summary>
    /// <param name="message">要显示的文本</param>
    private void AdjustToastSize(string message)
    {
        if (toastText == null || toastRectTransform == null) return;
        
        // 强制更新文本布局
        Canvas.ForceUpdateCanvases();
        
        // 获取文本的首选宽度
        float preferredWidth = toastText.preferredWidth;
        
        // 计算Toast的总宽度（文本宽度 + 水平内边距）
        float totalWidth = preferredWidth + paddingHorizontal * 2;
        
        // 限制在最小和最大宽度之间
        totalWidth = Mathf.Clamp(totalWidth, minWidth, maxWidth);
        
        // 设置Toast根对象的大小
        toastRectTransform.sizeDelta = new Vector2(totalWidth, fixedHeight);
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
            toastRectTransform = null;
            canvasGroup = null;
        }
        
        // 清理延迟动画引用
        if (hideDelayTweener != null)
        {
            hideDelayTweener = null;
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
            // 停止所有相关动画
            DOTween.Kill(canvasGroup);
            if (hideDelayTweener != null && hideDelayTweener.IsActive())
            {
                hideDelayTweener.Kill();
                hideDelayTweener = null;
            }
            HideToastAnimation();
        }
    }
} 