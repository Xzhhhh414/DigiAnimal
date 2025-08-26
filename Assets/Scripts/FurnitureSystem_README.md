# 家具生成系统 (FurnitureSpawner System)

## 📋 系统概述

家具生成系统是一个统一的家具管理框架，用于动态创建和管理游戏中的所有家具物件（植物、食物、装饰品等）。

## 🏗️ 系统架构

### 核心组件

1. **ISpawnableFurniture** - 可生成家具接口
2. **FurnitureDatabase** - 家具数据库配置
3. **FurnitureSpawner** - 家具生成管理器
4. **FurnitureDatabaseManager** - 数据库访问管理器

### 设计优势

- ✅ **统一管理**：所有家具通过同一套系统管理
- ✅ **动态创建**：根据存档数据动态生成，避免场景预置
- ✅ **可扩展性**：易于添加新的家具类型
- ✅ **存档一致性**：解决场景切换时的数据匹配问题

## 🔧 配置步骤

### 1. 创建家具数据库

1. 在Project窗口右键 → `Create` → `Game` → `Furniture Database`
2. 命名为 `FurnitureDatabase`
3. 配置家具项：

```csharp
// 示例配置
configId: "default"
furnitureType: Plant
displayName: "盆栽植物"
prefab: [植物预制体]
defaultPosition: (2, 0, 0)
spawnByDefault: true
maxInstances: 1
```

### 2. 设置数据库管理器

1. 在场景中创建空对象，命名为 `FurnitureDatabaseManager`
2. 添加 `FurnitureDatabaseManager` 组件
3. 将创建的数据库拖拽到 `Database` 字段

### 3. 设置家具生成器

1. 在场景中创建空对象，命名为 `FurnitureSpawner`
2. 添加 `FurnitureSpawner` 组件
3. 配置 `Furniture Container`（可选，会自动创建）

## 💻 使用方法

### 让家具支持动态生成

让你的家具类实现 `ISpawnableFurniture` 接口：

```csharp
public class PlantController : MonoBehaviour, ISelectableFurniture, ISpawnableFurniture
{
    // ISpawnableFurniture 实现
    public string FurnitureId { get; set; }
    public FurnitureType SpawnableFurnitureType => global::FurnitureType.Plant;
    public Vector3 Position { get; set; }
    public GameObject GameObject => gameObject;
    
    public void InitializeFromSaveData(object saveData) { /* 实现 */ }
    public object GetSaveData() { /* 实现 */ }
    public void GenerateFurnitureId() { /* 实现 */ }
}
```

### 程序化创建家具

```csharp
// 从存档数据创建
var furniture = await FurnitureSpawner.Instance.SpawnFurnitureFromSaveData(
    FurnitureType.Plant, 
    plantSaveData
);

// 创建默认家具
var defaultFurniture = await FurnitureSpawner.Instance.CreateDefaultFurniture(
    config, 
    position
);

// 创建所有默认家具（新账号）
await FurnitureSpawner.Instance.SpawnDefaultFurniture();
```

## 🔄 集成到游戏初始化

系统已集成到 `GameInitializer` 中：

1. **存档加载时**：调用 `SpawnFurniture()` 根据存档数据创建家具
2. **新账号时**：调用 `CreateDefaultFurniture()` 创建默认家具
3. **场景切换**：自动清理预置家具，重新生成

## 📁 文件结构

```
Assets/Scripts/
├── ISpawnableFurniture.cs          # 可生成家具接口
├── FurnitureDatabase.cs            # 家具数据库系统
├── FurnitureSpawner.cs             # 家具生成管理器
├── PlantController.cs              # 植物控制器（已实现接口）
└── SaveSystem/
    └── GameInitializer.cs          # 游戏初始化（已集成）
```

## 🎯 新账号默认家具

新账号进入游戏时，系统会：

1. 检测是否为新账号（无宠物且无植物数据）
2. 自动创建数据库中标记为 `spawnByDefault: true` 的家具
3. 使用配置的默认位置和属性

## 🔍 调试功能

- 在 `FurnitureSpawner` 中启用 `enableDebugLog` 查看详细日志
- 使用 `GetAllFurniture()` 检查当前生成的家具
- 通过 `GetFurnitureByType()` 按类型筛选家具

## 🚀 扩展指南

### 添加新家具类型

1. 在 `FurnitureType` 枚举中添加新类型
2. 创建家具控制器实现 `ISpawnableFurniture`
3. 在数据库中配置预制体
4. 在 `GameInitializer.SpawnFurniture()` 中添加生成逻辑

### 自定义生成逻辑

继承或扩展 `FurnitureSpawner` 类，重写相关方法实现自定义生成逻辑。

---

## ⚠️ 注意事项

1. **移除场景预置**：使用此系统后，应从场景中移除预置的家具对象
2. **数据库配置**：确保数据库管理器在场景加载前初始化
3. **存档兼容性**：修改家具结构时注意存档数据的向后兼容性
4. **性能考虑**：大量家具生成时使用异步方法避免卡顿