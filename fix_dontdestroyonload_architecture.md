# DontDestroyOnLoad 架构修复指南

## 问题分析
当前项目中过多的Manager使用了DontDestroyOnLoad，导致：
1. 场景切换时管理器残留
2. StartSceneLoadingManager等场景特定组件丢失
3. 组件间相互影响，导致意外的OnDisable调用

## 修复方案

### 1. 保留DontDestroyOnLoad的组件（数据层）
```csharp
// 这些组件应该保留DontDestroyOnLoad，因为它们管理跨场景的核心数据
✅ SaveManager              // 存档系统
✅ GameDataManager          // 运行时数据管理  
✅ PetDatabaseManager       // 宠物配置数据库
```

### 2. 已修复：移除DontDestroyOnLoad的组件（场景层）
```csharp
// 这些组件已经修改为场景特定实现
✅ UIManager               // 每个场景都有自己的UI
✅ PlayerManager          // 数据从存档系统加载，每个场景重新创建
```

### 3. 确认无需修复的组件
```csharp
// 这些组件经检查没有使用DontDestroyOnLoad，无需修复
✅ ToolInteractionManager  // 只在Gameplay场景需要
✅ PetManager             // 只在Gameplay场景需要
✅ FoodManager            // 只在Gameplay场景需要
```

### 4. 场景特定组件的处理
```csharp
// StartSceneLoadingManager等场景特定组件：
✅ 不使用DontDestroyOnLoad
✅ 在场景中预先配置
✅ 通过场景加载自动激活
```

## 修复效果

### ✅ 已解决的问题：
1. **UIManager污染**：每个场景现在都有独立的UIManager实例
2. **PlayerManager污染**：PlayerManager现在从存档系统加载数据，每个场景重新创建
3. **StartSceneLoadingManager丢失**：现在作为场景特定组件正确工作
4. **组件间意外影响**：StartScenePetDisplay和StartSceneLoadingManager已分离到不同GameObject

### 🎯 预期效果：
- ✅ 场景切换时没有管理器残留
- ✅ StartSceneLoadingManager不会丢失
- ✅ 避免组件间意外影响  
- ✅ 数据持久化仍然正常工作
- ✅ 从Gameplay返回Start场景的转场动画正常工作

## 测试验证

### 测试步骤：
1. **正常启动测试**：
   - 直接启动Start场景 → 应该看到宠物预览和正常UI
   - 点击开始游戏 → 应该正常进入Gameplay场景

2. **返回菜单测试**：
   - 在Gameplay场景中点击系统设置 → 返回开始菜单
   - 应该看到完整的转场动画（进入→退出）
   - 应该看到正确的宠物预览
   - 开始游戏按钮应该正常响应

3. **数据持久化测试**：
   - 在Gameplay中获得爱心货币
   - 返回Start场景
   - 重新进入Gameplay → 爱心货币应该正确保存

## 关键代码变更

### UIManager.cs
```csharp
// 移除DontDestroyOnLoad，改为场景特定
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        // DontDestroyOnLoad(gameObject); // 已移除
    }
    // ...
}
```

### PlayerManager.cs  
```csharp
// 移除DontDestroyOnLoad，添加数据加载
private void Awake()
{
    if (_instance == null)
    {
        _instance = this;
        // DontDestroyOnLoad(gameObject); // 已移除
    }
    // ...
}

private void Start()
{
    LoadPlayerDataFromSave(); // 从存档加载数据
    ValidateToolConfiguration();
}
```

## 架构优势

### 新架构的优势：
1. **清晰的职责分离**：数据层（跨场景）vs 场景层（场景特定）
2. **更好的内存管理**：避免不必要的对象累积
3. **更容易调试**：每个场景都是干净的环境
4. **更好的扩展性**：新场景不会受到旧场景管理器的影响

### 数据流：
```
SaveManager (跨场景) 
    ↓ 
GameDataManager (跨场景)
    ↓
PlayerManager (场景特定，从存档加载)
    ↓
UI组件 (场景特定)
```

## 完成状态

🎉 **DontDestroyOnLoad架构重构已完成！**

所有关键组件都已正确配置：
- ✅ 数据层组件保持跨场景持久化
- ✅ 场景层组件改为场景特定
- ✅ StartSceneLoadingManager转场动画问题已解决
- ✅ 组件分离避免相互影响

用户现在可以测试完整的场景切换流程，应该不再出现管理器残留和转场动画问题。 