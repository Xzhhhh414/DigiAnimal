# DigiAnimal 存档系统使用说明

## 概述

本存档系统为DigiAnimal项目提供完整的数据持久化解决方案，支持宠物数据、玩家数据的保存和加载，使用Resources系统进行资源管理。

## 系统架构

### 核心组件

1. **SaveManager** - 存档管理器
   - 负责存档的保存、加载、删除
   - 支持异步操作，不阻塞主线程
   - 自动管理存档文件路径

2. **GameDataManager** - 运行时数据管理器
   - 管理运行时游戏状态
   - 自动保存机制（货币变化立即保存，位置每秒保存）
   - 处理应用程序生命周期事件

3. **PetSpawner** - 宠物生成器
   - 使用Resources.Load加载宠物预制体
   - 根据存档数据创建宠物实例
   - 支持动态创建和移除宠物

4. **GameInitializer** - 游戏初始化器
   - 在Gameplay场景启动时自动初始化存档系统
   - 加载存档并创建宠物
   - 处理首次游戏的默认宠物创建

5. **GameStartManager** - 开始界面管理器
   - 显示存档信息
   - 提供删除存档功能
   - 处理场景切换

## 数据结构

### SaveData - 主存档数据
```csharp
public class SaveData
{
    public PlayerSaveData playerData;      // 玩家数据
    public List<PetSaveData> petsData;     // 宠物列表
    public WorldSaveData worldData;        // 世界数据（预留）
    public int saveVersion;                // 存档版本
    public string lastSaveTime;            // 最后保存时间
}
```

### PetSaveData - 宠物存档数据
```csharp
public class PetSaveData
{
    public string petId;                   // 唯一ID（pet_001格式）
    public string prefabName;              // 预制体名称（Pet_CatBrown格式）
    public string displayName;             // 显示名称
    public string introduction;            // 介绍
    public int energy;                     // 精力值
    public int satiety;                    // 饱腹度
    public Vector3 position;               // 位置
    // 注意：不保存当前状态（isSleeping、isEating）
    // 重新登录后这些状态会重置为默认值
    public bool isBored;                   // 厌倦状态
    public float lastBoredomTime;          // 上次厌倦时间
}
```

## 使用方法

### 1. 设置宠物预制体

1. 将宠物预制体放置在 `Assets/Resources/Prefab/Pet/` 目录下
2. 预制体命名规则：`Pet_宠物名字.prefab`
3. 确保预制体包含 `PetController2D` 组件

### 2. 场景设置

#### Gameplay场景
1. 创建空GameObject，命名为"GameInitializer"
2. 添加 `GameInitializer` 组件
3. 配置设置：
   - Auto Initialize On Start: true
   - Create Default Pet If Empty: true
   - Default Pet Prefab Name: "Pet_CatBrown"

#### GameStart场景（可选）
1. 创建UI界面包含：
   - 开始游戏按钮
   - 删除存档按钮
   - 存档信息显示文本
2. 添加 `GameStartManager` 组件并关联UI元素

### 3. 代码集成

#### 获取存档信息
```csharp
SaveFileInfo info = SaveManager.Instance.GetSaveFileInfo();
if (info != null && info.exists)
{
    Debug.Log($"存档存在，宠物数量: {info.petCount}");
}
```

#### 创建新宠物
```csharp
PetController2D newPet = await PetSpawner.Instance.CreateNewPet("Pet_CatBrown", Vector3.zero);
if (newPet != null)
{
    newPet.PetDisplayName = "我的小猫";
    newPet.PetIntroduction = "一只可爱的棕色小猫";
}
```

#### 手动保存
```csharp
// 异步保存
await SaveManager.Instance.SaveAsync();

// 同步保存
SaveManager.Instance.Save();

// 强制同步数据
GameDataManager.Instance.SyncToSave(true);
```

## 自动保存机制

1. **立即保存**：爱心货币变化时立即保存
2. **定时保存**：宠物位置每秒保存一次
3. **生命周期保存**：应用暂停/失去焦点/退出时强制保存所有数据
4. **宠物数据变化**：精力值、饱腹度变化时异步保存
5. **状态重置**：睡觉、吃饭等当前状态不保存，重新登录后重置为默认值

## 存档文件

- 位置：`Application.persistentDataPath/save.json`
- 格式：JSON格式，便于调试和修改
- 编码：UTF-8

## 调试功能

### SaveManager
- `[ContextMenu] 打印存档路径` - 显示存档文件路径
- `[ContextMenu] 打印当前存档` - 显示当前存档数据

### GameDataManager
- `[ContextMenu] 强制同步数据` - 立即同步所有数据
- `[ContextMenu] 打印活跃宠物` - 显示当前活跃宠物列表

### PetSpawner
- `[ContextMenu] 打印生成的宠物` - 显示生成的宠物列表
- `[ContextMenu] 打印预制体缓存` - 显示预制体缓存状态

### GameStartManager
- `[ContextMenu] 创建测试存档` - 创建包含测试数据的存档

## 错误处理

1. **预制体缺失**：系统会记录错误日志并跳过该宠物
2. **存档损坏**：自动创建新的空存档
3. **异步操作失败**：通过事件通知和日志记录错误

## 扩展点

1. **新数据类型**：在 `WorldSaveData` 中添加玩具、家具等数据
2. **新宠物属性**：在 `PetSaveData` 中添加自定义属性字典
3. **多存档支持**：修改 `SaveManager` 支持多个存档槽
4. **云存档**：扩展保存方法支持云同步

## 注意事项

1. 确保在使用前所有单例管理器已初始化
2. 宠物预制体必须包含 `PetController2D` 组件
3. 宠物预制体必须放在 `Assets/Resources/Prefab/Pet/` 目录下
4. 移动端测试时注意 `Application.persistentDataPath` 的权限
5. 大量宠物时注意内存使用和加载性能

## 版本兼容性

- Unity 2021.3 LTS+
- 支持 DOTween（用于动画系统）
- 支持移动端平台 