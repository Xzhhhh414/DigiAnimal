using UnityEngine;

/// <summary>
/// 统一的家具选中管理器
/// 处理所有家具物件（食物、植物、音响等）的选中逻辑
/// </summary>
public class FurnitureManager : MonoBehaviour
{
    // 单例模式
    public static FurnitureManager Instance { get; private set; }
    
    // 当前选中的家具
    private ISelectableFurniture selectedFurniture;
    
    void Awake()
    {
        // 场景特定的单例初始化 - 移除DontDestroyOnLoad，与ToolInteractionManager保持一致
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // 检查现有实例是否来自不同场景
            if (Instance.gameObject.scene != this.gameObject.scene)
            {
                // 清理旧实例的引用
                var oldInstance = Instance;
                Instance = this;
                // 销毁旧实例
                if (oldInstance != null)
                {
                    Destroy(oldInstance.gameObject);
                }
            }
            else
            {
                // 同场景重复实例，销毁自己
                Destroy(gameObject);
                return;
            }
        }
    }
    
    void Start()
    {
        // Debug.Log("FurnitureManager 初始化完成");
        
        // 监听宠物选中事件，实现互斥
        EventManager.Instance.AddListener<PetController2D>(CustomEventType.PetSelected, OnPetSelected);
    }
    
    void OnDestroy()
    {
        // 只有当前实例是静态引用时才清除
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 当宠物被选中时，取消家具选中
    /// </summary>
    private void OnPetSelected(PetController2D pet)
    {
        if (selectedFurniture != null)
        {
            UnselectCurrentFurniture();
        }
    }
    
    /// <summary>
    /// 选中家具
    /// </summary>
    public void SelectFurniture(ISelectableFurniture furniture)
    {
        if (furniture == null) return;
        
        // 如果之前有选中的家具，先完全取消选中（包括触发取消选中事件）
        if (selectedFurniture != null)
        {
            string oldFurnitureType = selectedFurniture.FurnitureType;
            selectedFurniture.OnDeselected();
            
            // 触发旧家具的取消选中事件，确保UI正确关闭
            switch (oldFurnitureType)
            {
                case "Food":
                    EventManager.Instance.TriggerEvent(CustomEventType.FoodUnselected);
                    break;
                case "Plant":
                    EventManager.Instance.TriggerEvent(CustomEventType.PlantUnselected);
                    break;
                case "Speaker":
                    EventManager.Instance.TriggerEvent(CustomEventType.SpeakerUnselected, selectedFurniture as SpeakerController);
                    break;
                case "TV":
                    EventManager.Instance.TriggerEvent(CustomEventType.TVUnselected, selectedFurniture as TVController);
                    break;
                // 后续可以添加更多家具类型
            }
        }
        
        // 取消宠物的选中状态
        UnselectPet();
        
        // 选中新的家具
        selectedFurniture = furniture;
        selectedFurniture.IsSelected = true;
        selectedFurniture.OnSelected();
        
        // 根据家具类型触发对应的选中事件和显示对应UI
        switch (furniture.FurnitureType)
        {
            case "Food":
                EventManager.Instance.TriggerEvent(CustomEventType.FoodSelected, furniture as FoodController);
                break;
            case "Plant":
                EventManager.Instance.TriggerEvent(CustomEventType.PlantSelected, furniture as PlantController);
                break;
            case "Speaker":
                EventManager.Instance.TriggerEvent(CustomEventType.SpeakerSelected, furniture as SpeakerController);
                break;
            case "TV":
                EventManager.Instance.TriggerEvent(CustomEventType.TVSelected, furniture as TVController);
                break;
            // 后续可以添加更多家具类型
        }
        
        // Debug.Log($"选中家具: {furniture.FurnitureName} (类型: {furniture.FurnitureType})");
    }
    
    /// <summary>
    /// 取消当前家具选中
    /// </summary>
    public void UnselectCurrentFurniture()
    {
        if (selectedFurniture != null)
        {
            string furnitureType = selectedFurniture.FurnitureType;
            ISelectableFurniture oldFurniture = selectedFurniture; // 保存引用用于事件参数
            selectedFurniture.OnDeselected();
            selectedFurniture = null;
            switch (furnitureType)
            {
                case "Food":
                    EventManager.Instance.TriggerEvent(CustomEventType.FoodUnselected);
                    break;
                case "Plant":
                    EventManager.Instance.TriggerEvent(CustomEventType.PlantUnselected);
                    break;
                case "Speaker":
                    EventManager.Instance.TriggerEvent(CustomEventType.SpeakerUnselected, oldFurniture as SpeakerController);
                    break;
                case "TV":
                    EventManager.Instance.TriggerEvent(CustomEventType.TVUnselected, oldFurniture as TVController);
                    break;
                // 后续可以添加更多家具类型
            }
            
            // Debug.Log($"取消选中家具 (类型: {furnitureType})");
        }
    }
    
    /// <summary>
    /// 取消宠物的选中状态
    /// </summary>
    private void UnselectPet()
    {
        EventManager.Instance.TriggerEvent(CustomEventType.PetUnselected);
    }
    
    /// <summary>
    /// 获取当前选中的家具
    /// </summary>
    public ISelectableFurniture GetSelectedFurniture()
    {
        return selectedFurniture;
    }
    
    /// <summary>
    /// 获取当前选中的特定类型家具
    /// </summary>
    public T GetSelectedFurniture<T>() where T : class, ISelectableFurniture
    {
        return selectedFurniture as T;
    }
    
    /// <summary>
    /// 检查点击位置是否在家具上
    /// </summary>
    public bool HandleFurnitureClick(Vector2 screenPosition)
    {
        // 从屏幕坐标发射射线
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // Debug.Log($"FurnitureManager: 射线检测结果 - hit.collider: {hit.collider?.name}");
        
        // 如果点击到了物体
        if (hit.collider != null)
        {
            ISelectableFurniture clickedFurniture = hit.collider.GetComponent<ISelectableFurniture>();
            // Debug.Log($"FurnitureManager: 点击的对象 {hit.collider.name}, ISelectableFurniture: {clickedFurniture != null}");
            
            if (clickedFurniture != null)
            {
                // 如果点击的是已选中的家具，取消选中
                if (selectedFurniture == clickedFurniture)
                {
                    // Debug.Log("FurnitureManager: 点击已选中的家具，取消选中");
                    UnselectCurrentFurniture();
                    return true;
                }
                
                // 选中新的家具
                // Debug.Log($"FurnitureManager: 选中新家具 {clickedFurniture.FurnitureName}");
                SelectFurniture(clickedFurniture);
                return true;
            }
        }
        
        return false; // 没有点击到家具
    }
    
    void Update()
    {
        // 处理点击事件
        if (Input.GetMouseButtonDown(0))
        {
            // Debug.Log("FurnitureManager 检测到点击");
            
            // 检查是否点击在UI上
            if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            {
                // Debug.Log("FurnitureManager: 点击在UI上，忽略");
                return;
            }
            
            // 检查是否在工具使用状态
            if (ToolInteractionManager.Instance != null && ToolInteractionManager.Instance.IsUsingTool)
            {
                // Debug.Log("FurnitureManager: 正在使用工具，忽略");
                return;
            }
            
            Vector2 mousePosition = Input.mousePosition;
            // Debug.Log($"FurnitureManager: 鼠标位置 {mousePosition}");
            
            // 检查是否点击到家具
            bool clickedOnFurniture = HandleFurnitureClick(mousePosition);
            // Debug.Log($"FurnitureManager: 是否点击到家具 {clickedOnFurniture}");
            
            // 如果没有点击到家具，且有选中的家具，则取消选中
            if (!clickedOnFurniture && selectedFurniture != null)
            {
                // Debug.Log("FurnitureManager: 点击空白处，取消家具选中");
                UnselectCurrentFurniture();
            }
            

        }
    }
}