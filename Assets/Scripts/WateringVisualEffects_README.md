# 植物浇水视觉效果系统

## 功能概述

实现了完整的植物浇水视觉反馈系统，包括：
1. **水壶浇水动画** - 在植物上方播放水壶浇水序列帧动画
2. **持续Toast提示** - 浇水期间显示"正在浇水ing..."提示
3. **闪亮完成特效** - 浇水完成后播放闪闪亮特效

## 系统架构

### 核心组件
- **`PlantController.cs`** - 植物控制器，统一管理浇水视觉效果
- **`ToastManager.cs`** - Toast管理器，新增持续显示功能
- **`SelectedPlantInfo.cs`** - 植物信息面板，集成视觉效果

### 工作流程
```
用户点击浇水按钮
    ↓
调用 TryWateringWithEffects()
    ↓
显示持续Toast "正在浇水ing..."
    ↓
生成水壶prefab + 播放浇水动画
    ↓
等待浇水持续时间
    ↓
销毁水壶 + 恢复植物健康度
    ↓
播放闪亮特效
    ↓
隐藏持续Toast + 显示成功Toast
```

## 配置说明

### PlantController 配置
```csharp
[Header("浇水视觉效果设置")]
[SerializeField] private GameObject wateringCanPrefab;        // 水壶预制体
[SerializeField] private Vector3 wateringCanOffset = new Vector3(0, 1f, 0); // 水壶偏移
[SerializeField] private float wateringDuration = 2f;         // 浇水持续时间
[SerializeField] private GameObject sparkleEffectPrefab;      // 闪亮特效预制体
[SerializeField] private Vector3 sparkleEffectOffset = Vector3.zero; // 特效偏移
[SerializeField] private float sparkleEffectDuration = 1f;    // 闪亮特效持续时间
```

### 预制体要求

#### 水壶预制体 (WateringCan.prefab)
- **必须组件**：`SpriteRenderer`, `Animator`
- **动画设置**：Entry状态直接连接到浇水动画，自动循环播放
- **层级设置**：代码自动设置为 `sortingOrder = 10`
- **父子关系**：会被设置为植物的子对象

#### 闪亮特效预制体 (SparkleEffect.prefab)
- **必须组件**：`SpriteRenderer`, `Animator`
- **动画设置**：Entry状态直接连接到闪亮动画，自动播放
- **层级设置**：代码自动设置为 `sortingOrder = 15`
- **父子关系**：会被设置为植物的子对象
- **持续时间**：由PlantController配置决定

## 使用方法

### 1. 准备资源
- 准备水壶浇水的序列帧图片
- 准备闪亮特效的序列帧图片
- 在Unity中创建对应的动画和Animator Controller

### 2. 创建预制体
- 创建水壶预制体，只需 `SpriteRenderer` + `Animator`
- 创建闪亮特效预制体，只需 `SpriteRenderer` + `Animator`  
- 动画设置为Entry状态直接播放，无需触发器

### 3. 配置植物
- 在每个植物预制体上配置：
  - `wateringCanPrefab` - 拖入水壶预制体
  - `wateringCanOffset` - 调整水壶相对植物的位置
  - `wateringDuration` - 设置浇水动画持续时间
  - `sparkleEffectPrefab` - 拖入闪亮特效预制体
  - `sparkleEffectOffset` - 调整特效相对植物的位置
  - `sparkleEffectDuration` - 设置闪亮特效持续时间

### 4. UI配置
在 `SelectedPlantInfo` 中配置：
- `wateringInProgressText` - "正在浇水ing..."

## 技术特性

### 简化的特效管理
- **统一生命周期管理**：PlantController统一控制特效的生成和销毁
- **父子对象关系**：特效作为植物的子对象，便于管理和清理
- **自动层级设置**：代码自动设置正确的渲染层级
- **无需额外脚本**：预制体只需基础组件，简化维护

### 异步浇水系统
- 支持回调机制，可以在浇水开始和完成时执行自定义逻辑
- 防止重复点击，浇水期间按钮不可用
- 兼容原有的同步浇水方法

### 持续Toast系统
- 新增 `ShowPersistentToast()` - 显示不自动消失的Toast
- 新增 `HidePersistentToast()` - 手动隐藏持续Toast
- 与原有Toast系统完全兼容

### 视觉层级管理
- 水壶动画：`sortingOrder = 10`
- 闪亮特效：`sortingOrder = 15`
- 确保特效正确显示在植物上方

## 扩展建议

### 音效支持
可以在以下时机添加音效：
- 浇水开始时：水流声
- 浇水完成时：叮叮声或成功音效

### 更多视觉效果
- 水滴粒子效果
- 植物生长动画
- 健康度变化的缓动动画

### 个性化配置
- 不同植物可以有不同的特效
- 根据植物状态调整特效强度
- 支持多种水壶样式

## 注意事项

1. **性能优化**：特效会自动销毁，但要确保动画资源不会过大
2. **层级管理**：确保特效在正确的层级显示
3. **时间同步**：水壶动画时长应该与 `wateringDuration` 匹配
4. **错误处理**：预制体缺失时会有日志提示，但不会影响基础功能

## 调试技巧

- 使用 `Debug.Log` 查看浇水流程
- 检查预制体是否正确配置
- 确认动画触发器名称匹配
- 验证层级设置是否正确