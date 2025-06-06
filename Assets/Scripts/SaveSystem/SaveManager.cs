using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 存档管理器 - 负责游戏存档的保存、加载、删除
/// </summary>
public class SaveManager : MonoBehaviour
{
    // 单例模式
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject saveManagerObj = new GameObject("SaveManager");
                    _instance = saveManagerObj.AddComponent<SaveManager>();
                    DontDestroyOnLoad(saveManagerObj);
                }
            }
            return _instance;
        }
    }
    
    [Header("存档设置")]
    [SerializeField] private string saveFileName = "save.json";
    
    // 存档路径
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
    
    // 当前存档数据
    private SaveData currentSaveData;
    
    // 下一个宠物ID计数器
    private static int nextPetIdCounter = 1;
    
    // 事件
    public event System.Action<SaveData> OnSaveLoaded;
    public event System.Action OnSaveDeleted;
    public event System.Action<bool> OnSaveOperation; // true=保存成功, false=保存失败
    
    private void Awake()
    {
        // 单例模式设置
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 启动时检查是否有存档
        if (HasSaveFile())
        {
            // Debug.Log($"发现存档文件: {SaveFilePath}");
        }
        else
        {
            // Debug.Log("未发现存档文件，将创建新存档");
        }
    }
    
    #region 公共API
    
    /// <summary>
    /// 检查是否存在存档文件
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }
    
    /// <summary>
    /// 获取存档文件信息
    /// </summary>
    public SaveFileInfo GetSaveFileInfo()
    {
        if (!HasSaveFile())
            return null;
            
        try
        {
            FileInfo fileInfo = new FileInfo(SaveFilePath);
            string jsonContent = File.ReadAllText(SaveFilePath);
            SaveData tempData = JsonUtility.FromJson<SaveData>(jsonContent);
            
            return new SaveFileInfo
            {
                exists = true,
                lastModified = fileInfo.LastWriteTime,
                fileSize = fileInfo.Length,
                saveTime = tempData?.lastSaveTime ?? "未知",
                petCount = tempData?.petsData?.Count ?? 0,
                heartCurrency = tempData?.playerData?.heartCurrency ?? 0
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"读取存档信息失败: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 异步加载存档
    /// </summary>
    public async Task<SaveData> LoadSaveAsync()
    {
        if (!HasSaveFile())
        {
            // Debug.Log("没有存档文件，创建新存档");
            currentSaveData = CreateNewSave();
            return currentSaveData;
        }
        
        try
        {
            // Debug.Log($"开始加载存档: {SaveFilePath}");
            string jsonContent = await File.ReadAllTextAsync(SaveFilePath);
            
            currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);
            
            if (currentSaveData == null)
            {
                Debug.LogError("存档数据解析失败，创建新存档");
                currentSaveData = CreateNewSave();
            }
            else
            {
                // 更新宠物ID计数器
                UpdatePetIdCounter();
                // Debug.Log($"存档加载成功! 宠物数量: {currentSaveData.petsData.Count}, 爱心货币: {currentSaveData.playerData.heartCurrency}");
            }
            
            OnSaveLoaded?.Invoke(currentSaveData);
            return currentSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载存档失败: {e.Message}");
            currentSaveData = CreateNewSave();
            return currentSaveData;
        }
    }
    
    /// <summary>
    /// 同步加载存档（用于需要立即结果的场景）
    /// </summary>
    public SaveData LoadSave()
    {
        if (!HasSaveFile())
        {
            // Debug.Log("没有存档文件，创建新存档");
            currentSaveData = CreateNewSave();
            return currentSaveData;
        }
        
        try
        {
            // Debug.Log($"开始同步加载存档: {SaveFilePath}");
            string jsonContent = File.ReadAllText(SaveFilePath);
            
            currentSaveData = JsonUtility.FromJson<SaveData>(jsonContent);
            
            if (currentSaveData == null)
            {
                Debug.LogError("存档数据解析失败，创建新存档");
                currentSaveData = CreateNewSave();
            }
            else
            {
                UpdatePetIdCounter();
                // Debug.Log($"存档加载成功! 宠物数量: {currentSaveData.petsData.Count}, 爱心货币: {currentSaveData.playerData.heartCurrency}");
            }
            
            OnSaveLoaded?.Invoke(currentSaveData);
            return currentSaveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载存档失败: {e.Message}");
            currentSaveData = CreateNewSave();
            return currentSaveData;
        }
    }
    
    /// <summary>
    /// 异步保存存档
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("没有数据需要保存");
            return false;
        }
        
        try
        {
            // 更新保存时间
            currentSaveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string jsonContent = JsonUtility.ToJson(currentSaveData, true);
            await File.WriteAllTextAsync(SaveFilePath, jsonContent);
            
            // Debug.Log($"存档保存成功: {SaveFilePath}");
            OnSaveOperation?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"保存存档失败: {e.Message}");
            OnSaveOperation?.Invoke(false);
            return false;
        }
    }
    
    /// <summary>
    /// 同步保存存档
    /// </summary>
    public bool Save()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("没有数据需要保存");
            return false;
        }
        
        try
        {
            // 更新保存时间
            currentSaveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string jsonContent = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(SaveFilePath, jsonContent);
            
            // Debug.Log($"存档保存成功: {SaveFilePath}");
            OnSaveOperation?.Invoke(true);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"保存存档失败: {e.Message}");
            OnSaveOperation?.Invoke(false);
            return false;
        }
    }
    
    /// <summary>
    /// 删除存档
    /// </summary>
    public bool DeleteSave()
    {
        try
        {
            if (HasSaveFile())
            {
                File.Delete(SaveFilePath);
                Debug.Log("存档已删除");
            }
            
            currentSaveData = null;
            nextPetIdCounter = 1;
            OnSaveDeleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"删除存档失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取当前存档数据
    /// </summary>
    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }
    
    /// <summary>
    /// 设置当前存档数据
    /// </summary>
    public void SetCurrentSaveData(SaveData saveData)
    {
        currentSaveData = saveData;
    }
    
    /// <summary>
    /// 获取下一个宠物ID
    /// </summary>
    public static int GetNextPetId()
    {
        return nextPetIdCounter++;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 创建新的存档数据
    /// </summary>
    private SaveData CreateNewSave()
    {
        SaveData newSave = new SaveData();
        
        // 创建默认宠物（如果需要的话）
        // TODO: 这里可以根据需要添加默认宠物
        
        return newSave;
    }
    
    /// <summary>
    /// 更新宠物ID计数器
    /// </summary>
    private void UpdatePetIdCounter()
    {
        int maxId = 0;
        
        if (currentSaveData?.petsData != null)
        {
            foreach (var petData in currentSaveData.petsData)
            {
                // 从petId中提取数字部分 (pet_001 -> 1)
                if (petData.petId.StartsWith("pet_"))
                {
                    string idStr = petData.petId.Substring(4);
                    if (int.TryParse(idStr, out int id))
                    {
                        maxId = Mathf.Max(maxId, id);
                    }
                }
            }
        }
        
        nextPetIdCounter = maxId + 1;
        // Debug.Log($"更新宠物ID计数器: {nextPetIdCounter}");
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("打印存档路径")]
    public void DebugSavePath()
    {
        Debug.Log($"存档路径: {SaveFilePath}");
        Debug.Log($"存档存在: {HasSaveFile()}");
    }
    
    [ContextMenu("打印当前存档")]
    public void DebugCurrentSave()
    {
        if (currentSaveData != null)
        {
            string json = JsonUtility.ToJson(currentSaveData, true);
            Debug.Log($"当前存档数据:\n{json}");
        }
        else
        {
            Debug.Log("当前没有存档数据");
        }
    }
    
    /// <summary>
    /// 调试：打印存档中的位置信息
    /// </summary>
    [ContextMenu("调试：打印存档位置信息")]
    public void DebugPrintPetPositions()
    {
        if (currentSaveData?.petsData != null)
        {
            Debug.Log($"=== 存档位置信息 (共{currentSaveData.petsData.Count}只宠物) ===");
            foreach (var petData in currentSaveData.petsData)
            {
                Debug.Log($"宠物 {petData.petId} ({petData.displayName}): 位置={petData.position}");
            }
        }
        else
        {
            Debug.Log("当前没有存档数据或宠物数据为空");
        }
    }
    
    #endregion
}

/// <summary>
/// 存档文件信息
/// </summary>
[Serializable]
public class SaveFileInfo
{
    public bool exists;
    public DateTime lastModified;
    public long fileSize;
    public string saveTime;
    public int petCount;
    public int heartCurrency;
}