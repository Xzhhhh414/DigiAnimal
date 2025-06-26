using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 宠物消息管理器 - 控制宠物头像+对话框的显示
/// </summary>
public class PetMessageManager : MonoBehaviour
{
    [Header("PetMessage对象引用")]
    [SerializeField] private GameObject petMessageObject; // PetMessage预制体实例
    [SerializeField] private Image petAvatarImage; // 宠物头像显示组件
    [SerializeField] private Text messageText; // 消息文本组件
    
    [Header("动画设置")]
    [SerializeField] private float messageDuration = 3f; // 消息显示时长
    [SerializeField] private float fadeInDuration = 0.4f; // 淡入时长
    [SerializeField] private float fadeOutDuration = 0.4f; // 淡出时长
    [SerializeField] private float slideUpDistance = 30f; // 向上滑动距离
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isShowing = false; // 当前是否正在显示消息
    
    // 气泡控制相关
    private PetController2D currentPet; // 当前显示消息的宠物
    private PetNeedType currentBubbleType = PetNeedType.None; // 当前显示的气泡类型
    
    // 单例模式
    private static PetMessageManager _instance;
    public static PetMessageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PetMessageManager>();
                
                if (_instance == null)
                {
                    Debug.LogError("场景中未找到PetMessageManager实例！");
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        Debug.Log($"[PetMessageManager] Awake开始执行 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 场景特定的单例初始化
        if (_instance == null)
        {
            _instance = this;
            Debug.Log($"[PetMessageManager] 单例初始化完成（场景特定） - Scene: {gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // 检查现有实例是否来自不同场景
            if (_instance.gameObject.scene != this.gameObject.scene)
            {
                Debug.Log($"[PetMessageManager] 检测到跨场景实例冲突，替换为当前场景实例 - 旧场景: {_instance.gameObject.scene.name}, 新场景: {gameObject.scene.name}");
                // 清理旧实例的引用
                var oldInstance = _instance;
                _instance = this;
                // 销毁旧实例
                if (oldInstance != null)
                {
                    Destroy(oldInstance.gameObject);
                }
            }
            else
            {
                Debug.Log($"[PetMessageManager] 销毁同场景重复实例 - Scene: {gameObject.scene.name}");
                Destroy(gameObject);
                return;
            }
        }
        
        // 延迟初始化，等待场景完全加载
        StartCoroutine(DelayedInitialize());
        
        Debug.Log($"[PetMessageManager] Awake执行完成 - Scene: {gameObject.scene.name}");
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[PetMessageManager] OnDestroy被调用 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 只有当前实例是静态引用时才清除
        if (_instance == this)
        {
            Debug.Log($"[PetMessageManager] 清除静态引用 - Scene: {gameObject.scene.name}");
            _instance = null;
        }
    }
    
    /// <summary>
    /// 延迟初始化PetMessageManager
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialize()
    {
        // 等待几帧，确保场景完全加载
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // 自动查找组件（如果没有在Inspector中设置）
        if (petMessageObject == null)
        {
            // 尝试在Canvas下查找名为PetMessage的对象
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform petMessageTransform = canvas.transform.Find("PetMessage");
                if (petMessageTransform != null)
                {
                    petMessageObject = petMessageTransform.gameObject;
                }
                // 在Start场景中找不到是正常的，不输出日志
            }
            // 在Start场景中找不到Canvas是正常的，不输出日志
        }
        
        if (petMessageObject != null)
        {
            InitializeComponents();
        }
                    // 在Start场景中未找到对象是正常的，不输出日志
    }
    
    /// <summary>
    /// 初始化组件
    /// </summary>
    private void InitializeComponents()
        {
            // 自动查找子组件
            if (petAvatarImage == null)
            {
                petAvatarImage = petMessageObject.GetComponentInChildren<Image>();
            if (petAvatarImage == null)
                {
                Debug.LogWarning("PetMessageManager: 未找到Image组件！");
                }
            }
            
            if (messageText == null)
            {
                messageText = petMessageObject.GetComponentInChildren<Text>();
            if (messageText == null)
                {
                Debug.LogWarning("PetMessageManager: 未找到Text组件！");
                }
            }
            
            // 获取或添加CanvasGroup组件
            canvasGroup = petMessageObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = petMessageObject.AddComponent<CanvasGroup>();
            }
            
            // 获取RectTransform
            rectTransform = petMessageObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
            }
            
            // 初始状态隐藏
            HideMessageImmediate();
    }
    
    /// <summary>
    /// 尝试重新初始化组件
    /// </summary>
    private void TryReinitialize()
    {
        // 重新查找PetMessage对象
        if (petMessageObject == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform petMessageTransform = canvas.transform.Find("PetMessage");
                if (petMessageTransform != null)
                {
                    petMessageObject = petMessageTransform.gameObject;
                }
            }
        }
        
        // 如果找到了PetMessage对象，初始化组件
        if (petMessageObject != null)
        {
            InitializeComponents();
        }
    }
    
    /// <summary>
    /// 显示宠物消息
    /// </summary>
    /// <param name="pet">宠物对象</param>
    /// <param name="message">消息内容</param>
    public void ShowPetMessage(PetController2D pet, string message)
    {
        // 根据消息内容判断气泡类型
        PetNeedType bubbleType = PetNeedType.None;
        if (message.Contains("厌倦") || message.Contains("休息"))
        {
            bubbleType = PetNeedType.Indifferent;
        }
        else if (message.Contains("开心") || message.Contains("爱心货币"))
        {
            bubbleType = PetNeedType.Happy;
        }
        
        ShowPetMessage(pet, message, bubbleType);
    }
    
    /// <summary>
    /// 显示宠物消息（指定气泡类型）
    /// </summary>
    /// <param name="pet">宠物对象</param>
    /// <param name="message">消息内容</param>
    /// <param name="bubbleType">要显示的气泡类型</param>
    public void ShowPetMessage(PetController2D pet, string message, PetNeedType bubbleType)
    {
        // Debug.Log($"ShowPetMessage 被调用: pet={pet?.name}, message={message}");
        
        if (string.IsNullOrEmpty(message) || pet == null)
        {
            Debug.LogWarning("ShowPetMessage: 消息为空或宠物为空");
            return;
        }
        
        // 如果组件未初始化，尝试重新初始化
        if (petMessageObject == null || messageText == null)
        {
            TryReinitialize();
            
            // 如果重新初始化后仍然失败，则输出警告并返回
            if (petMessageObject == null || messageText == null)
            {
                Debug.LogWarning("PetMessageManager: PetMessage对象或组件未找到，可能当前场景不支持宠物消息显示。");
            return;
            }
        }
        
        // Debug.Log("开始显示宠物消息...");
        
        // 如果正在显示其他消息，先停止之前的动画和气泡
        if (isShowing)
        {
            // Debug.Log("停止之前的动画");
            DOTween.Kill("PetMessageSequence"); // 使用ID杀死显示序列
            DOTween.Kill("PetMessageHideSequence"); // 使用ID杀死隐藏序列
            DOTween.Kill(canvasGroup);
            DOTween.Kill(rectTransform);
            
            // 隐藏之前的气泡
            if (currentPet != null && currentBubbleType != PetNeedType.None)
            {
                currentPet.HideEmotionBubble(currentBubbleType);
            }
        }
        
        // 保存当前宠物信息
        currentPet = pet;
        
        // 保存当前气泡类型
        currentBubbleType = bubbleType;
        
        // 显示对应的气泡
        if (bubbleType != PetNeedType.None)
        {
            currentPet.ShowEmotionBubble(bubbleType);
            // Debug.Log($"显示气泡类型: {bubbleType}");
        }
        
        // 设置宠物头像
        if (petAvatarImage != null && pet.PetProfileImage != null)
        {
            petAvatarImage.sprite = pet.PetProfileImage;
            // Debug.Log($"设置宠物头像: {pet.PetProfileImage.name}");
        }
        else
        {
            // Debug.LogWarning($"无法设置头像: petAvatarImage={petAvatarImage != null}, PetProfileImage={pet.PetProfileImage != null}");
        }
        
        // 设置消息文本
        messageText.text = message;
        // Debug.Log($"设置消息文本: {message}");
        
        // 确保对象激活
        petMessageObject.SetActive(true);
        // Debug.Log("激活PetMessage对象");
        
        // 开始显示动画
        ShowMessageAnimation();
    }
    
    /// <summary>
    /// 显示消息动画
    /// </summary>
    private void ShowMessageAnimation()
    {
        isShowing = true;
        
        // 设置初始状态
        canvasGroup.alpha = 0;
        rectTransform.anchoredPosition = originalPosition + Vector2.down * slideUpDistance;
        
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 同时进行淡入和向上滑动
        sequence.Append(canvasGroup.DOFade(1, fadeInDuration));
        sequence.Join(rectTransform.DOAnchorPos(originalPosition, fadeInDuration).SetEase(Ease.OutBack));
        
        // 显示完成后，等待一段时间再隐藏
        sequence.AppendInterval(messageDuration);
        sequence.AppendCallback(() => HideMessageAnimation());
        
        // 为序列设置ID，方便管理
        sequence.SetId("PetMessageSequence");
    }
    
    /// <summary>
    /// 隐藏消息动画
    /// </summary>
    private void HideMessageAnimation()
    {
        // 立即开始气泡的淡出动画，与对话同步
        if (currentPet != null && currentBubbleType != PetNeedType.None)
        {
            currentPet.HideEmotionBubble(currentBubbleType);
            // Debug.Log($"开始隐藏气泡类型: {currentBubbleType}");
        }
        
        // 创建隐藏动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 同时进行淡出和向上滑动
        sequence.Append(canvasGroup.DOFade(0, fadeOutDuration));
        sequence.Join(rectTransform.DOAnchorPos(originalPosition + Vector2.up * slideUpDistance, fadeOutDuration).SetEase(Ease.InBack));
        
        // 为隐藏序列设置ID
        sequence.SetId("PetMessageHideSequence");
        
        // 动画完成后隐藏对象并重置状态
        sequence.OnComplete(() => {
            petMessageObject.SetActive(false);
            
            // 重置状态
            isShowing = false;
            currentPet = null;
            currentBubbleType = PetNeedType.None;
        });
    }
    
    /// <summary>
    /// 立即隐藏消息（无动画）
    /// </summary>
    private void HideMessageImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
        }
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
        if (petMessageObject != null)
        {
            petMessageObject.SetActive(false);
        }
        
        // 隐藏气泡
        if (currentPet != null && currentBubbleType != PetNeedType.None)
        {
            currentPet.HideEmotionBubble(currentBubbleType);
        }
        
        // 重置状态
        isShowing = false;
        currentPet = null;
        currentBubbleType = PetNeedType.None;
    }
    
    /// <summary>
    /// 手动隐藏消息
    /// </summary>
    public void HideMessage()
    {
        if (isShowing)
        {
            DOTween.Kill("PetMessageSequence");
            DOTween.Kill("PetMessageHideSequence");
            DOTween.Kill(canvasGroup);
            DOTween.Kill(rectTransform);
            HideMessageAnimation();
        }
    }
} 