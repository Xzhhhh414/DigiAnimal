using UnityEngine;

/// <summary>
/// 电视机控制器 - 管理电视机的选择、开关状态等功能
/// </summary>
public class TVController : MonoBehaviour, ISelectableFurniture, ISpawnableFurniture
{
    [Header("电视机基本信息")]
    [SerializeField] private string tvName = "电视机";
    [SerializeField] private Sprite tvIcon;
    [SerializeField] private string configId = "tv_basic";
    [SerializeField] private string saveDataId = "";  // 默认家具标识符

    [Header("视觉特效")]
    [SerializeField] private GameObject screenEffectPrefab;  // 屏幕特效预制体
    [SerializeField] private Transform effectSpawnPoint;    // 特效生成位置（可选，默认为电视机位置）
    
    // 电视机状态
    private bool isOn = false;  // 开关状态

    // 特效管理
    private GameObject currentScreenEffect; // 当前播放的屏幕特效实例

    // 家具ID和位置信息
    private string furnitureId;

    #region ISelectableFurniture 实现
    
    public string FurnitureType => "TV";
    public bool IsSelected { get; set; }
    public GameObject GameObject => gameObject;
    
    public void OnSelected()
    {
        IsSelected = true;
        // 触发电视机选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.TVSelected, this);
    }
    
    public void OnDeselected()
    {
        IsSelected = false;
        // 触发电视机取消选中事件
        EventManager.Instance.TriggerEvent(CustomEventType.TVUnselected, this);
    }
    
    public Sprite GetIcon()
    {
        return tvIcon;
    }
    
    #endregion

    #region ISpawnableFurniture 实现
    
    public string FurnitureId 
    { 
        get => furnitureId; 
        set => furnitureId = value; 
    }
    
    public FurnitureType SpawnableFurnitureType => global::FurnitureType.TV;
    
    public string FurnitureName 
    { 
        get => tvName; 
        set => tvName = value; 
    }
    
    public Vector3 Position 
    { 
        get => transform.position; 
        set => transform.position = value; 
    }
    
    public void InitializeFromSaveData(object saveData)
    {
        if (saveData is TVSaveData tvData)
        {
            LoadFromSaveData(tvData);
        }
    }
    
    public object GetSaveData()
    {
        return new TVSaveData(
            furnitureId,
            configId,
            saveDataId,  // 添加 saveDataId 参数
            transform.position,
            isOn
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
    
    private void Start()
    {
        // 生成ID（如果还没有的话）
        GenerateFurnitureId();
        
        // 根据初始状态设置特效
        if (isOn)
        {
            CreateScreenEffect();
        }
    }
    
    private void OnDestroy()
    {
        // 确保在对象销毁时清理特效
        DestroyScreenEffect();
    }
    
    #endregion

    #region 电视机控制
    
    /// <summary>
    /// 切换电视机开关状态
    /// </summary>
    public void TogglePower()
    {
        if (isOn)
        {
            TurnOff();
        }
        else
        {
            TurnOn();
        }
    }
    
    /// <summary>
    /// 开启电视机
    /// </summary>
    public void TurnOn()
    {
        isOn = true;
        
        // 创建屏幕特效
        CreateScreenEffect();
        
        //Debug.Log($"[TVController] 电视机开启: {tvName}");
    }
    
    /// <summary>
    /// 关闭电视机
    /// </summary>
    public void TurnOff()
    {
        isOn = false;
        
        // 销毁屏幕特效
        DestroyScreenEffect();
        
        //Debug.Log($"[TVController] 电视机关闭: {tvName}");
    }
    
    /// <summary>
    /// 获取电视机开关状态
    /// </summary>
    public bool IsOn => isOn;
    
    #endregion

    #region 特效管理
    
    /// <summary>
    /// 创建屏幕特效
    /// </summary>
    private void CreateScreenEffect()
    {
        if (screenEffectPrefab == null) return;
        
        // 如果已经有特效在播放，先销毁
        if (currentScreenEffect != null)
        {
            DestroyScreenEffect();
        }
        
        // 确定特效生成位置
        Transform spawnTransform = effectSpawnPoint != null ? effectSpawnPoint : transform;
        
        // 创建特效实例
        currentScreenEffect = Instantiate(screenEffectPrefab, spawnTransform.position, spawnTransform.rotation, spawnTransform);
        
        //Debug.Log($"[TVController] 创建屏幕特效: {currentScreenEffect.name}");
    }
    
    /// <summary>
    /// 销毁屏幕特效
    /// </summary>
    private void DestroyScreenEffect()
    {
        if (currentScreenEffect != null)
        {
            //Debug.Log($"[TVController] 销毁屏幕特效: {currentScreenEffect.name}");
            Destroy(currentScreenEffect);
            currentScreenEffect = null;
        }
    }
    
    /// <summary>
    /// 检查是否有特效在播放
    /// </summary>
    public bool HasScreenEffect => currentScreenEffect != null;
    
    #endregion

    #region 存档系统
    
    /// <summary>
    /// 从存档数据加载
    /// </summary>
    public void LoadFromSaveData(TVSaveData saveData)
    {
        furnitureId = saveData.tvId;
        configId = saveData.configId;
        saveDataId = saveData.saveDataId;  // 加载 saveDataId
        transform.position = saveData.position;
        
        // 恢复开关状态
        if (saveData.isOn)
        {
            // 延迟一帧再开启，确保所有组件都已初始化
            StartCoroutine(DelayedTurnOn());
        }
        else
        {
            isOn = false;
            DestroyScreenEffect();
        }
        
        //Debug.Log($"[TVController] 加载存档数据: ID={furnitureId}, 状态={saveData.isOn}");
    }
    
    private System.Collections.IEnumerator DelayedTurnOn()
    {
        yield return null; // 等待一帧
        TurnOn();
    }
    
    #endregion
}
