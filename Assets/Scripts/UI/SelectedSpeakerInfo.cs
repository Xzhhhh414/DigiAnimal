using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 选中音响信息面板UI控制器
/// </summary>
public class SelectedSpeakerInfo : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Text speakerNameText;
    [SerializeField] private Text currentTrackText;
    [SerializeField] private Image speakerImage;
    
    [Header("音乐控制按钮")]
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    
    [Header("播放/暂停按钮图标")]
    [SerializeField] private Image playPauseIcon;
    [SerializeField] private Sprite playIcon;      // 播放图标（暂停状态时显示）
    [SerializeField] private Sprite pauseIcon;     // 暂停图标（播放状态时显示）
    
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Vector3 showScale = Vector3.one;
    [SerializeField] private Vector3 hideScale = new Vector3(0.8f, 0.8f, 0.8f);
    
    // 当前选中的音响
    private SpeakerController currentSpeaker;
    
    #region Unity 生命周期
    
    private void Awake()
    {
        // 初始化UI状态 - 保持GameObject激活但面板不可见
        transform.localScale = Vector3.zero;  // 设置为完全不可见
    }
    
    private void Start()
    {
        // 设置按钮事件
        if (playPauseButton != null)
            playPauseButton.onClick.AddListener(OnPlayPauseButtonClicked);
            
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousButtonClicked);
            
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
        
        // 监听音响选中/取消选中事件
        EventManager.Instance.AddListener<SpeakerController>(CustomEventType.SpeakerSelected, OnSpeakerSelected);
        EventManager.Instance.AddListener<SpeakerController>(CustomEventType.SpeakerUnselected, OnSpeakerUnselected);
        
        // 监听其他家具选中事件，用于关闭音响面板
        EventManager.Instance.AddListener<PlantController>(CustomEventType.PlantSelected, OnOtherFurnitureSelected);
        EventManager.Instance.AddListener<FoodController>(CustomEventType.FoodSelected, OnOtherFurnitureSelected);
        EventManager.Instance.AddListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
    }
    
    private void OnDestroy()
    {
        // 移除事件监听
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener<SpeakerController>(CustomEventType.SpeakerSelected, OnSpeakerSelected);
            EventManager.Instance.RemoveListener<SpeakerController>(CustomEventType.SpeakerUnselected, OnSpeakerUnselected);
            EventManager.Instance.RemoveListener<PlantController>(CustomEventType.PlantSelected, OnOtherFurnitureSelected);
            EventManager.Instance.RemoveListener<FoodController>(CustomEventType.FoodSelected, OnOtherFurnitureSelected);
            EventManager.Instance.RemoveListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 音响被选中
    /// </summary>
    private void OnSpeakerSelected(SpeakerController speaker)
    {
        currentSpeaker = speaker;
        ShowSpeakerInfo();
    }
    
    /// <summary>
    /// 音响取消选中
    /// </summary>
    private void OnSpeakerUnselected(SpeakerController speaker)
    {
        if (currentSpeaker == speaker)
        {
            HideSpeakerInfo();
        }
    }
    
    /// <summary>
    /// 其他家具被选中时关闭音响面板
    /// </summary>
    private void OnOtherFurnitureSelected<T>(T furniture)
    {
        if (currentSpeaker != null)
        {
            HideSpeakerInfo();
        }
    }
    
    /// <summary>
    /// 宠物被选中时关闭音响面板
    /// </summary>
    private void OnPetSelected(PetController2D pet)
    {
        if (currentSpeaker != null)
        {
            HideSpeakerInfo();
        }
    }
    
    #endregion
    
    #region UI显示控制
    
    /// <summary>
    /// 显示音响信息面板
    /// </summary>
    private void ShowSpeakerInfo()
    {
        if (currentSpeaker == null) return;
        
        // 更新UI内容
        UpdateUI();
        
        // 播放显示动画
        transform.DOScale(showScale, animationDuration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// 隐藏音响信息面板
    /// </summary>
    private void HideSpeakerInfo()
    {
        currentSpeaker = null;
        
        // 播放隐藏动画 - 缩放到0来隐藏
        transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack);
    }
    
    /// <summary>
    /// 更新UI显示内容
    /// </summary>
    private void UpdateUI()
    {
        if (currentSpeaker == null) return;
        
        // 更新音响名称
        if (speakerNameText != null)
            speakerNameText.text = currentSpeaker.FurnitureName;
        
        // 更新当前曲目
        if (currentTrackText != null)
            currentTrackText.text = currentSpeaker.GetCurrentTrackName();
        
        // 更新音响图标
        if (speakerImage != null)
        {
            Sprite icon = currentSpeaker.GetIcon();
            if (icon != null)
            {
                speakerImage.sprite = icon;
                speakerImage.color = Color.white;
            }
            else
            {
                speakerImage.color = Color.clear;
            }
        }
        
        // 更新播放/暂停按钮图标
        UpdatePlayPauseButton();
    }
    
    /// <summary>
    /// 更新播放/暂停按钮
    /// </summary>
    private void UpdatePlayPauseButton()
    {
        if (currentSpeaker == null || playPauseIcon == null) return;
        
        // 根据播放状态设置图标
        if (currentSpeaker.IsPlaying)
        {
            // 正在播放，显示暂停图标
            playPauseIcon.sprite = pauseIcon;
        }
        else
        {
            // 暂停状态，显示播放图标
            playPauseIcon.sprite = playIcon;
        }
    }
    
    #endregion
    
    #region 按钮事件
    
    /// <summary>
    /// 播放/暂停按钮点击
    /// </summary>
    private void OnPlayPauseButtonClicked()
    {
        if (currentSpeaker == null) return;
        
        currentSpeaker.TogglePlayPause();
        
        // 更新按钮显示
        UpdatePlayPauseButton();
        
        Debug.Log($"[SelectedSpeakerInfo] 播放/暂停: {(currentSpeaker.IsPlaying ? "播放" : "暂停")}");
    }
    
    /// <summary>
    /// 上一首按钮点击
    /// </summary>
    private void OnPreviousButtonClicked()
    {
        if (currentSpeaker == null) return;
        
        currentSpeaker.PreviousTrack();
        
        // 更新UI显示
        UpdateUI();
        
        Debug.Log($"[SelectedSpeakerInfo] 切换到上一首: {currentSpeaker.GetCurrentTrackName()}");
    }
    
    /// <summary>
    /// 下一首按钮点击
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (currentSpeaker == null) return;
        
        currentSpeaker.NextTrack();
        
        // 更新UI显示
        UpdateUI();
        
        Debug.Log($"[SelectedSpeakerInfo] 切换到下一首: {currentSpeaker.GetCurrentTrackName()}");
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 强制刷新UI（用于外部调用）
    /// </summary>
    public void RefreshUI()
    {
        if (currentSpeaker != null)
        {
            UpdateUI();
        }
    }
    
    /// <summary>
    /// 检查是否显示中
    /// </summary>
    public bool IsShowing => currentSpeaker != null && transform.localScale.x > 0.1f;
    
    #endregion
}
