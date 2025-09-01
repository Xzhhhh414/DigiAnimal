# 动态NavMesh烘焙系统设置指南

## 概述
动态NavMesh烘焙系统允许在家具动态生成后重新烘焙NavMesh，确保宠物AI能正确避开新生成的家具。

**注意：** 本系统使用NavMeshPlus插件的NavMeshSurface组件，支持2D NavMesh烘焙。

## 系统组件

### 1. DynamicNavMeshManager.cs
- **功能**：管理NavMesh的动态烘焙
- **位置**：`Assets/Scripts/Navigation/DynamicNavMeshManager.cs`
- **特点**：
  - 单例模式，全局访问
  - 支持延迟烘焙，避免频繁操作
  - 自动为家具添加NavMeshObstacle组件
  - 烘焙完成后自动刷新所有NavMeshAgent

### 2. GameInitializer.cs 集成
- **修改**：在家具生成完成后调用NavMesh烘焙
- **触发时机**：所有家具（植物、食物、音响、电视机）生成完成后

## 设置步骤

### 第1步：确保场景有NavMeshSurface组件
1. 在Gameplay场景中找到NavMesh GameObject（已存在）
2. 确认它有NavMeshSurface组件（来自NavMeshPlus插件）
3. 配置参数：
   - **Agent Type ID**: 0 (Cat)
   - **Collect Objects**: All
   - **Layer Mask**: 64 (或适当的层级)
   - **Use Geometry**: Render Meshes
   - **Default Area**: Walkable
   - **Ignore NavMesh Agent**: 勾选
   - **Ignore NavMesh Obstacle**: 取消勾选（重要！让家具能阻挡路径）

### 第2步：添加DynamicNavMeshManager组件

#### 推荐方案：添加到GameManager prefab（支持运行时家具移动）
1. 找到GameManager prefab或场景中的GameManager GameObject
2. 直接添加DynamicNavMeshManager组件
3. 配置参数：
   - **Nav Mesh Surface**: 留空（推荐使用自动查找）
   - **Auto Find Nav Mesh Surface**: 勾选（自动查找，推荐）
   - **Bake Delay**: 1秒（推荐）
   - **Enable Debug Logs**: 勾选（用于调试）
   - **Hide NavMesh Logs**: 勾选（隐藏NavMeshPlus插件的详细日志，推荐）

**优势：**
- 支持跨场景持久化
- 适合运行时家具移动功能
- 整个游戏过程中都可用

#### 替代方案：添加到GameInitializer prefab（仅初始化时使用）
1. 找到GameInitializer prefab或场景中的GameInitializer GameObject
2. 直接添加DynamicNavMeshManager组件
3. 配置参数同上

**适用场景：** 如果只需要在游戏初始化时烘焙NavMesh，不支持运行时移动家具

#### 方案3：独立GameObject
1. 在Gameplay场景创建空GameObject，命名为"DynamicNavMeshManager"
2. 添加DynamicNavMeshManager组件
3. 配置参数同上

**推荐使用方案1（GameManager）**，特别是如果计划支持运行时家具移动功能。

### 第3步：配置家具预制体
确保家具预制体有适当的碰撞体：

#### 植物预制体
- 添加BoxCollider或其他Collider组件
- 不要设为Trigger
- 系统会自动添加NavMeshObstacle

#### 食物预制体
- 添加BoxCollider或其他Collider组件
- 不要设为Trigger
- 系统会自动添加NavMeshObstacle

#### 音响预制体
- 添加BoxCollider或其他Collider组件
- 不要设为Trigger
- 系统会自动添加NavMeshObstacle

#### 电视机预制体
- 添加BoxCollider或其他Collider组件
- 不要设为Trigger
- 系统会自动添加NavMeshObstacle

## 工作流程

### 游戏启动时
1. **GameInitializer** 开始初始化
2. 清理场景预置的家具
3. 从存档数据生成家具
4. 调用 **DynamicNavMeshManager.RequestNavMeshBake()**
5. 系统为所有家具添加NavMeshObstacle
6. 重新烘焙NavMesh
7. 刷新所有NavMeshAgent的路径

### 运行时动态添加家具
```csharp
// 生成新家具后调用
DynamicNavMeshManager.Instance.RequestNavMeshBake();
```

### 运行时移动家具
```csharp
// 移动家具位置后调用
DynamicNavMeshManager.Instance.RequestNavMeshBakeForFurnitureMove();
```

### 场景切换时的处理
如果GameManager跨场景，需要在进入新场景时刷新NavMeshSurface引用：
```csharp
// 在场景加载完成后调用
DynamicNavMeshManager.Instance.RefreshNavMeshSurface();
```

## 智能障碍物检测

DynamicNavMeshManager会智能分析每个家具对象，决定是否应该作为NavMesh障碍物：

### 检测规则

**简单直接的判断逻辑：只根据碰撞体的`isTrigger`属性决定**

1. **触发器碰撞体**（`isTrigger = true`）：
   - 被识别为可行走区域，不会作为NavMesh障碍物
   - 例如：CatFood的BoxCollider2D设为触发器用于点击交互

2. **非触发器碰撞体**（`isTrigger = false`）：
   - 被识别为障碍物，会阻挡宠物导航
   - 例如：植物、音响、电视等实体家具

3. **无碰撞体对象**：
   - 默认作为可行走区域，不会添加NavMeshObstacle

### 处理结果

- **可行走对象**：移除NavMeshObstacle组件，允许宠物通过
- **障碍物对象**：添加NavMeshObstacle组件，在NavMesh中创建洞

## API参考

### DynamicNavMeshManager主要方法

```csharp
// 请求烘焙（推荐，有延迟防抖）
DynamicNavMeshManager.Instance.RequestNavMeshBake();

// 立即烘焙（不推荐频繁使用）
DynamicNavMeshManager.Instance.BakeNavMeshImmediate();

// 检查是否正在烘焙
bool isBaking = DynamicNavMeshManager.Instance.IsBaking;

// 检查是否需要更新
bool needUpdate = DynamicNavMeshManager.Instance.IsNavMeshUpdateNeeded();
```

## 调试和测试

### 调试日志
启用 **Enable Debug Logs** 后，系统会输出详细日志：
- 烘焙开始/完成
- 准备的家具对象数量
- NavMeshAgent刷新数量

**注意：** 如果您不想看到NavMeshPlus插件的详细烘焙日志（如"Walkable Bounds"、"Sources"等），请勾选 **Hide NavMesh Logs** 选项。这些日志对普通使用来说过于详细，但在深度调试NavMesh问题时可能有用。

### 调试NavMesh障碍物问题

如果发现动态创建的家具没有正确作为NavMesh障碍物，请检查以下调试日志：

1. **障碍物检测日志**：
   ```
   [DynamicNavMeshManager] Plant_01(Clone) 被标记为可行走区域，跳过障碍物设置
   [DynamicNavMeshManager] Speaker(Clone) 添加了NavMeshObstacle - Size: (1.0, 1.0, 1.0)
   ```

2. **烘焙后验证日志**：
   ```
   [DynamicNavMeshManager] 场景中共有 3 个NavMeshObstacle组件
   [DynamicNavMeshManager] 障碍物 Speaker(Clone): Size=(1.0, 1.0, 1.0), Carving=true
   ```

3. **常见问题排查**：
   - **触发器设置**：确认`isTrigger`属性是否正确设置
   - **NavMeshObstacle尺寸**：检查Size是否合理（不能为零）
   - **Carving属性**：必须为true才能在NavMesh中创建洞
   - **烘焙时机**：NavMeshObstacle在NavMesh烘焙完成后才能生效
   - **碰撞体冲突**：系统不会强制添加3D碰撞体，避免与现有2D碰撞体冲突

### 编辑器测试
在DynamicNavMeshManager组件的右键菜单中：
- **测试NavMesh烘焙**：运行时测试烘焙功能
- **显示NavMesh信息**：显示当前NavMeshSurface配置

### 可视化调试
1. 在Scene视图中启用Navigation显示
2. Window → AI → Navigation
3. 在Navigation窗口点击"Show NavMesh"
4. 蓝色区域为可行走区域，家具周围应该有洞

## 性能考虑

### 烘焙频率控制
- 使用延迟烘焙（默认1秒）
- 避免频繁调用烘焙
- 烘焙期间会阻止重复烘焙请求

### 优化建议
- 只在必要时烘焙（家具生成/移除时）
- 考虑将家具分批生成，减少烘焙次数
- 对于不影响寻路的小装饰品，可以不添加NavMeshObstacle

## 故障排除

### 常见问题

#### 1. NavMesh没有更新
- 检查DynamicNavMeshManager是否正确设置
- 确认NavMeshSurface引用正确
- 查看调试日志确认烘焙是否执行

#### 2. 家具没有阻挡效果
- 检查家具是否有Collider组件
- 确认NavMeshObstacle是否正确添加
- 检查NavMeshObstacle的carving是否启用

#### 3. 宠物AI行为异常
- 确认NavMeshAgent在有效的NavMesh上
- 检查路径刷新是否成功
- 使用NavMeshAgentHelper进行安全操作

#### 4. 性能问题
- 减少烘焙频率
- 检查是否有不必要的重复烘焙
- 考虑分批处理家具生成

## 扩展功能

### 自定义烘焙条件
可以修改 `IsNavMeshUpdateNeeded()` 方法来添加更复杂的烘焙条件判断。

### 区域烘焙
如果需要只烘焙特定区域，可以调整NavMeshSurface的Size和Center参数。

### 多Agent支持
当前配置支持Cat Agent Type，如果需要其他类型的Agent，需要在NavMeshSurface中配置相应的Agent Type ID。
