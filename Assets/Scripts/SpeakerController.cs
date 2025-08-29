using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 音响控制器 - 管理音响的选择、音乐播放等功能
/// </summary>
public class SpeakerController : MonoBehaviour, ISelectableFurniture, ISpawnableFurniture
{
    [Header("音响基本信息")]
    [SerializeField] private string speakerName = "音响";
    [SerializeField] private Sprite speakerIcon;
    [SerializeField] private string configId = "speaker_basic";
    [SerializeField] private string saveDataId = "";  // 默认家具标识符

    [Header("音频设置")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();
    
    [Header("视觉特效")]
    [SerializeField] private GameObject soundWaveEffectPrefab;  // 音浪特效预制体
    [SerializeField] private Transform effectSpawnPoint;       // 特效生成位置（可选，默认为音响位置）
    
    // 播放状态
    private bool isPlaying = false;
    private int currentTrackIndex = 0;
    private float pausedTime = 0f; // 暂停时的播放位置

    // 特效管理
    private GameObject currentSoundWaveEffect; // 当前播放的音浪特效实例

    // 家具ID和位置信息
    private string furnitureId;

    #region ISelectableFurniture 实现
    
    public string FurnitureType => "Speaker";
    public bool IsSelected { get; set; }
    public GameObject GameObject => gameObject;
    
    public void OnSelected()
    {
        IsSelected = true;
        // 触发音响选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.SpeakerSelected, this);
    }
    
    public void OnDeselected()
    {
        IsSelected = false;
        // 触发音响取消选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.SpeakerUnselected, this);
    }
    
    public Sprite GetIcon()
    {
        return speakerIcon;
    }
    
    #endregion

    #region ISpawnableFurniture 实现
    
    public string FurnitureId 
    { 
        get => furnitureId; 
        set => furnitureId = value; 
    }
    
    public FurnitureType SpawnableFurnitureType => global::FurnitureType.Speaker;
    
    public string FurnitureName 
    { 
        get => speakerName; 
        set => speakerName = value; 
    }
    
    public Vector3 Position 
    { 
        get => transform.position; 
        set => transform.position = value; 
    }
    
    public void InitializeFromSaveData(object saveData)
    {
        if (saveData is SpeakerSaveData speakerData)
        {
            LoadFromSaveData(speakerData);
        }
    }
    
    public object GetSaveData()
    {
        return new SpeakerSaveData(
            furnitureId,
            configId,
            saveDataId,  // 添加 saveDataId 参数
            transform.position,
            currentTrackIndex,
            pausedTime,
            isPlaying
        );
    }
    
    public void GenerateFurnitureId()
    {
        if (string.IsNullOrEmpty(furnitureId))
        {
            furnitureId = FurnitureSpawner.Instance.GenerateUniqueFurnitureId();
        }
    }
    
    #endregion

    #region Unity 生命周期
    
    private void Awake()
    {
        // 确保有AudioSource组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // 设置AudioSource基本属性
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }
    
    private void Start()
    {
        // 确保有音乐曲目
        if (musicTracks.Count == 0)
        {
            Debug.LogWarning($"[SpeakerController] 音响 {speakerName} 没有配置音乐曲目！");
        }
        
        // 生成ID（如果还没有的话）
        GenerateFurnitureId();
    }
    
    private void OnDestroy()
    {
        // 确保在对象销毁时清理特效
        DestroySoundWaveEffect();
    }
    
    #endregion

    #region 音乐播放控制
    
    /// <summary>
    /// 播放/暂停音乐
    /// </summary>
    public void TogglePlayPause()
    {
        if (musicTracks.Count == 0) return;
        
        if (isPlaying)
        {
            PauseMusic();
        }
        else
        {
            PlayMusic();
        }
    }
    
    /// <summary>
    /// 播放音乐
    /// </summary>
    public void PlayMusic()
    {
        if (musicTracks.Count == 0) return;
        
        // 设置当前曲目
        audioSource.clip = musicTracks[currentTrackIndex];
        
        // 从暂停位置开始播放
        audioSource.time = pausedTime;
        audioSource.Play();
        
        isPlaying = true;
        
        // 创建音浪特效
        CreateSoundWaveEffect();
        
        //Debug.Log($"[SpeakerController] 开始播放: {GetCurrentTrackName()}");
    }
    
    /// <summary>
    /// 暂停音乐
    /// </summary>
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            pausedTime = audioSource.time;
            audioSource.Pause();
        }
        
        isPlaying = false;
        
        // 销毁音浪特效
        DestroySoundWaveEffect();
        
        //Debug.Log($"[SpeakerController] 暂停播放: {GetCurrentTrackName()} (位置: {pausedTime:F1}s)");
    }
    
    /// <summary>
    /// 停止音乐
    /// </summary>
    public void StopMusic()
    {
        audioSource.Stop();
        isPlaying = false;
        pausedTime = 0f;
        
        // 销毁音浪特效
        DestroySoundWaveEffect();
        
        //Debug.Log($"[SpeakerController] 停止播放: {GetCurrentTrackName()}");
    }
    
    /// <summary>
    /// 切换到上一首
    /// </summary>
    public void PreviousTrack()
    {
        if (musicTracks.Count == 0) return;
        
        bool wasPlaying = isPlaying;
        
        // 停止当前播放
        if (isPlaying)
        {
            StopMusic();
        }
        
        // 切换到上一首
        currentTrackIndex = (currentTrackIndex - 1 + musicTracks.Count) % musicTracks.Count;
        pausedTime = 0f; // 重置播放位置
        
        //Debug.Log($"[SpeakerController] 切换到上一首: {GetCurrentTrackName()}");
        
        // 如果之前在播放，继续播放新歌
        if (wasPlaying)
        {
            PlayMusic();
        }
    }
    
    /// <summary>
    /// 切换到下一首
    /// </summary>
    public void NextTrack()
    {
        if (musicTracks.Count == 0) return;
        
        bool wasPlaying = isPlaying;
        
        // 停止当前播放
        if (isPlaying)
        {
            StopMusic();
        }
        
        // 切换到下一首
        currentTrackIndex = (currentTrackIndex + 1) % musicTracks.Count;
        pausedTime = 0f; // 重置播放位置
        
        //Debug.Log($"[SpeakerController] 切换到下一首: {GetCurrentTrackName()}");
        
        // 如果之前在播放，继续播放新歌
        if (wasPlaying)
        {
            PlayMusic();
        }
    }
    
    /// <summary>
    /// 获取当前曲目名称
    /// </summary>
    public string GetCurrentTrackName()
    {
        if (musicTracks.Count == 0 || currentTrackIndex >= musicTracks.Count)
            return "无曲目";
            
        AudioClip currentClip = musicTracks[currentTrackIndex];
        return currentClip != null ? currentClip.name : "未知曲目";
    }
    
    /// <summary>
    /// 获取播放状态
    /// </summary>
    public bool IsPlaying => isPlaying;
    
    /// <summary>
    /// 获取当前曲目索引
    /// </summary>
    public int CurrentTrackIndex => currentTrackIndex;
    
    /// <summary>
    /// 获取总曲目数量
    /// </summary>
    public int TotalTracks => musicTracks.Count;
    
    #endregion

    #region 存档系统
    
    /// <summary>
    /// 从存档数据加载
    /// </summary>
    public void LoadFromSaveData(SpeakerSaveData saveData)
    {
        furnitureId = saveData.speakerId;
        configId = saveData.configId;
        saveDataId = saveData.saveDataId;  // 加载 saveDataId
        transform.position = saveData.position;
        currentTrackIndex = Mathf.Clamp(saveData.currentTrackIndex, 0, musicTracks.Count - 1);
        pausedTime = saveData.pausedTime;
        
        // 恢复播放状态
        if (saveData.wasPlaying && musicTracks.Count > 0)
        {
            // 延迟一帧再开始播放，确保所有组件都已初始化
            StartCoroutine(DelayedPlayMusic());
        }
        
        //Debug.Log($"[SpeakerController] 加载存档数据: ID={furnitureId}, 曲目={currentTrackIndex}, 播放={saveData.wasPlaying}");
    }
    
    private System.Collections.IEnumerator DelayedPlayMusic()
    {
        yield return null; // 等待一帧
        if (musicTracks.Count > currentTrackIndex)
        {
            PlayMusic();
        }
    }
    
    #endregion

    #region 编辑器支持
    
    private void OnValidate()
    {
        // 确保索引在有效范围内
        if (musicTracks.Count > 0)
        {
            currentTrackIndex = Mathf.Clamp(currentTrackIndex, 0, musicTracks.Count - 1);
        }
    }
    
    #endregion

    #region 特效管理
    
    /// <summary>
    /// 创建音浪特效
    /// </summary>
    private void CreateSoundWaveEffect()
    {
        if (soundWaveEffectPrefab == null) return;
        
        // 如果已经有特效在播放，先销毁
        if (currentSoundWaveEffect != null)
        {
            DestroySoundWaveEffect();
        }
        
        // 确定特效生成位置
        Transform spawnTransform = effectSpawnPoint != null ? effectSpawnPoint : transform;
        
        // 创建特效实例
        currentSoundWaveEffect = Instantiate(soundWaveEffectPrefab, spawnTransform.position, spawnTransform.rotation, spawnTransform);
        
        //Debug.Log($"[SpeakerController] 创建音浪特效: {currentSoundWaveEffect.name}");
    }
    
    /// <summary>
    /// 销毁音浪特效
    /// </summary>
    private void DestroySoundWaveEffect()
    {
        if (currentSoundWaveEffect != null)
        {
            //Debug.Log($"[SpeakerController] 销毁音浪特效: {currentSoundWaveEffect.name}");
            Destroy(currentSoundWaveEffect);
            currentSoundWaveEffect = null;
        }
    }
    
    /// <summary>
    /// 检查是否有特效在播放
    /// </summary>
    public bool HasSoundWaveEffect => currentSoundWaveEffect != null;
    
    #endregion
}
