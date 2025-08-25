# 统一家具选中系统

## 架构说明

### 核心组件
- **`ISelectableFurniture.cs`** - 家具接口，定义标准行为
- **`FurnitureManager.cs`** - 统一管理所有家具选中逻辑
- **`FoodController.cs`** - 食物控制器，实现家具接口
- **`PlantController.cs`** - 植物控制器，实现家具接口

### 已移除的组件
- ~~`FoodManager.cs`~~ - 已删除，功能合并到FurnitureManager
- ~~`PlantManager.cs`~~ - 已删除，功能合并到FurnitureManager

## 使用方法

### 场景设置
1. 在场景中添加 `FurnitureManager` 组件
2. 确保 `CameraController` 引用了 `FurnitureManager`（替代旧的FoodManager）

### 添加新家具类型
```csharp
public class YourFurnitureController : MonoBehaviour, ISelectableFurniture
{
    [SerializeField] private string furnitureName = "你的家具";
    [SerializeField] private Sprite furnitureIcon;
    [SerializeField] public bool isSelected = false;
    
    private PixelOutlineManager pixelOutlineManager;
    
    void Awake()
    {
        pixelOutlineManager = GetComponent<PixelOutlineManager>();
    }
    
    // 实现接口
    public string FurnitureType => "YourType";
    public string FurnitureName => furnitureName;
    public bool IsSelected { get => isSelected; set => isSelected = value; }
    public GameObject GameObject => gameObject;
    
    public void OnSelected()
    {
        if (pixelOutlineManager != null)
            pixelOutlineManager.SetOutlineActive(true);
        // 显示UI等...
    }
    
    public void OnDeselected()
    {
        isSelected = false;
        if (pixelOutlineManager != null)
            pixelOutlineManager.SetOutlineActive(false);
        // 关闭UI等...
    }
    
    public Sprite GetIcon() => furnitureIcon;
}
```

### 在FurnitureManager中注册新类型
在`FurnitureManager.cs`的switch语句中添加：
```csharp
case "YourType":
    // 处理选中/取消选中事件
    break;
```

## 特性
- ✅ 统一的选中逻辑
- ✅ 与宠物选中互斥
- ✅ 点击空白处取消选中
- ✅ 易于扩展新家具类型
- ✅ 类型安全的接口设计

## 注意事项
- 家具预制体必须有 `BoxCollider2D` 组件用于点击检测
- 家具预制体必须有 `PixelOutlineManager` 组件用于选中描边
- 每个家具类型的 `FurnitureType` 必须唯一