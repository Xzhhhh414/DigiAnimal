using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 爱心货币获得提示管理器
/// </summary>
public class HeartMessageManager : MonoBehaviour
{
    [Header("预制体设置")]
    [SerializeField] private GameObject heartMessagePrefab; // HeartMessage预制体
    [SerializeField] private Canvas targetCanvas; // 目标Canvas
    
    [Header("显示位置设置")]
    [SerializeField] private Vector2 offsetFromPet = new Vector2(100, 50); // 相对宠物的偏移量（UI坐标）
    
    [Header("动画设置")]
    [SerializeField] private float displayDuration = 1.5f; // 显示时长（减少至1.5秒，比对话更短）
    [SerializeField] private float fadeInDuration = 0.2f; // 淡入时长（加快淡入）
    [SerializeField] private float fadeOutDuration = 0.8f; // 淡出时长
    [SerializeField] private float moveUpDistance = 80f; // 向上移动距离（增加飘动效果）
    [SerializeField] private float scalePopDuration = 0.15f; // 弹出缩放时长
    [SerializeField] private float maxScaleEffect = 1.3f; // 最大缩放倍数
    
    [Header("伤害数字效果")]
    [SerializeField] private AnimationCurve moveUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 向上移动曲线
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 缩放动画曲线
    
    // 单例模式
    private static HeartMessageManager _instance;
    public static HeartMessageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HeartMessageManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("场景中未找到HeartMessageManager实例！");
                }
            }
            return _instance;
        }
    }
    
    // 活跃的爱心消息列表
    private List<HeartMessageInstance> activeMessages = new List<HeartMessageInstance>();
    
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
        
        // 自动查找Canvas
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("HeartMessageManager: 未找到Canvas！");
            }
        }
    }
    
    private void Update()
    {
        // 更新所有活跃消息的位置
        UpdateActiveMessagesPosition();
        
        // 清理已销毁的消息
        CleanupDestroyedMessages();
    }
    
    /// <summary>
    /// 显示爱心获得提示
    /// </summary>
    /// <param name="pet">宠物对象</param>
    /// <param name="heartAmount">获得的爱心数量</param>
    public void ShowHeartGainMessage(PetController2D pet, int heartAmount)
    {
        if (pet == null)
        {
            Debug.LogWarning("HeartMessageManager: 宠物对象为空");
            return;
        }
        
        if (heartMessagePrefab == null)
        {
            Debug.LogWarning("HeartMessageManager: heartMessagePrefab未设置，请在Inspector中拖拽HeartMessage预制体");
            return;
        }
        
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("HeartMessageManager: 未找到Canvas！");
                return;
            }
        }
        
        // 创建爱心消息实例
        GameObject messageObj = Instantiate(heartMessagePrefab, targetCanvas.transform);
        
        // 获取组件
        RectTransform rectTransform = messageObj.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = messageObj.GetComponent<CanvasGroup>();
        
        // 查找Text组件 - 运行时动态查找
        Text messageText = messageObj.GetComponentInChildren<Text>();
        if (messageText == null)
        {
            // 如果预制体本身就有Text组件
            messageText = messageObj.GetComponent<Text>();
        }
        
        if (rectTransform == null)
        {
            Debug.LogError("HeartMessage预制体缺少RectTransform组件！");
            Destroy(messageObj);
            return;
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = messageObj.AddComponent<CanvasGroup>();
        }
        
        // 设置消息文本
        if (messageText != null)
        {
            messageText.text = $"+{heartAmount}";
        }
        else
        {
            Debug.LogWarning("HeartMessage预制体中未找到Text组件，无法显示文本。建议在预制体中添加Text子对象");
        }
        
        // 计算初始位置
        Vector2 uiPosition = WorldToUIPosition(pet.transform.position);
        Vector2 targetPosition = uiPosition + offsetFromPet;
        rectTransform.anchoredPosition = targetPosition;
        
        // 创建消息实例数据
        HeartMessageInstance messageInstance = new HeartMessageInstance
        {
            messageObject = messageObj,
            pet = pet,
            rectTransform = rectTransform,
            canvasGroup = canvasGroup,
            originalPosition = targetPosition,
            isDestroyed = false
        };
        
        // 添加到活跃列表
        activeMessages.Add(messageInstance);
        
        // 播放显示动画
        PlayShowAnimation(messageInstance);
    }
    
    /// <summary>
    /// 播放显示动画
    /// </summary>
    private void PlayShowAnimation(HeartMessageInstance messageInstance)
    {
        var rectTransform = messageInstance.rectTransform;
        var canvasGroup = messageInstance.canvasGroup;
        var startPosition = messageInstance.originalPosition;
        
        // 设置初始状态
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
        rectTransform.anchoredPosition = startPosition;
        
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 第一阶段：快速弹出效果
        sequence.Append(canvasGroup.DOFade(1f, fadeInDuration));
        sequence.Join(rectTransform.DOScale(maxScaleEffect, scalePopDuration).SetEase(Ease.OutBack));
        
        // 第二阶段：回到正常大小并开始向上飘动
        sequence.Append(rectTransform.DOScale(1f, scalePopDuration * 0.5f).SetEase(Ease.InBack));
        sequence.Join(rectTransform.DOAnchorPosY(startPosition.y + moveUpDistance * 0.3f, scalePopDuration * 0.5f).SetEase(Ease.OutQuart));
        
        // 第三阶段：持续向上飘动
        float remainingDuration = displayDuration - fadeInDuration - scalePopDuration * 1.5f;
        if (remainingDuration > 0)
        {
            sequence.AppendInterval(remainingDuration * 0.3f); // 短暂停留
            sequence.Append(rectTransform.DOAnchorPosY(startPosition.y + moveUpDistance, remainingDuration * 0.7f).SetEase(Ease.OutQuart));
        }
        
        // 第四阶段：淡出效果
        sequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.OutQuart));
        
        // 动画完成后销毁
        sequence.OnComplete(() => {
            if (messageInstance.messageObject != null)
            {
                Destroy(messageInstance.messageObject);
                messageInstance.isDestroyed = true;
            }
        });
    }
    
    /// <summary>
    /// 更新活跃消息的位置
    /// </summary>
    private void UpdateActiveMessagesPosition()
    {
        foreach (var messageInstance in activeMessages)
        {
            if (messageInstance.isDestroyed || messageInstance.pet == null || messageInstance.rectTransform == null)
                continue;
                
            // 将宠物世界坐标转换为UI坐标
            Vector2 uiPosition = WorldToUIPosition(messageInstance.pet.transform.position);
            Vector2 targetPosition = uiPosition + offsetFromPet;
            
            // 更新位置（保持Y轴的动画效果）
            Vector2 currentPos = messageInstance.rectTransform.anchoredPosition;
            float yOffset = currentPos.y - messageInstance.originalPosition.y;
            messageInstance.rectTransform.anchoredPosition = new Vector2(targetPosition.x, targetPosition.y + yOffset);
            
            // 更新原始位置记录
            messageInstance.originalPosition = targetPosition;
        }
    }
    
    /// <summary>
    /// 清理已销毁的消息
    /// </summary>
    private void CleanupDestroyedMessages()
    {
        activeMessages.RemoveAll(msg => msg.isDestroyed || msg.messageObject == null);
    }
    
    /// <summary>
    /// SendMessage兼容的方法 - 供ToolInteractionManager调用
    /// </summary>
    /// <param name="data">包含宠物和爱心数量的数组</param>
    public void ShowHeartGainMessageForPet(object[] data)
    {
        if (data != null && data.Length >= 2 && data[0] is PetController2D && data[1] is int)
        {
            ShowHeartGainMessage((PetController2D)data[0], (int)data[1]);
        }
    }
    
    /// <summary>
    /// 将世界坐标转换为UI坐标
    /// </summary>
    private Vector2 WorldToUIPosition(Vector3 worldPosition)
    {
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        Vector2 uiPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, targetCanvas.worldCamera, out uiPosition);
            
        return uiPosition;
    }
}

/// <summary>
/// 爱心消息实例数据
/// </summary>
[System.Serializable]
public class HeartMessageInstance
{
    public GameObject messageObject;
    public PetController2D pet;
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup;
    public Vector2 originalPosition;
    public bool isDestroyed;
} 