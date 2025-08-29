# 音响功能设置指南

## 概述
音响功能已经完全集成到DigiAnimal的家具系统中，支持音乐播放、暂停、切换曲目等功能。

## 已实现的功能

### 1. 核心脚本
- ✅ **SpeakerController.cs** - 音响控制器，管理音乐播放和选择
- ✅ **SelectedSpeakerInfo.cs** - 音响信息UI面板
- ✅ **SpeakerSaveData** - 音响存档数据结构

### 2. 系统集成
- ✅ **EventManager** - 添加了SpeakerSelected/SpeakerUnselected事件
- ✅ **FurnitureManager** - 支持音响的选择和取消选择
- ✅ **FurnitureSpawner** - 支持音响的动态生成
- ✅ **GameInitializer** - 支持音响的场景加载
- ✅ **GameDataManager** - 支持音响数据的同步保存
- ✅ **FirstTimePetSelectionManager** - 支持新账号创建默认音响

### 3. 接口实现
- ✅ **ISelectableFurniture** - 音响可选择功能
- ✅ **ISpawnableFurniture** - 音响可动态生成功能

## 设置步骤

### 步骤1：创建音响预制体

1. **创建基础GameObject**
   ```
   SpeakerPrefab (GameObject)
   ├── SpeakerModel (可视化模型 - Sprite Renderer或3D模型)
   ├── AudioSource (音频源组件)
   └── Collider2D (用于点击检测)
   ```

2. **添加SpeakerController脚本**
   - 将`SpeakerController.cs`添加到根GameObject
   - 配置以下字段：
     ```csharp
     [Header("音响基本信息")]
     speakerName = "音响"              // 音响显示名称
     speakerIcon = [音响图标精灵]      // UI中显示的图标
     configId = "SpeakerPrefab"        // 配置ID（与FurnitureDatabase中一致）
     
     [Header("音频设置")]
     audioSource = [拖拽AudioSource组件]  // 音频源
     musicTracks = [拖拽音乐AudioClip列表] // 音乐曲目列表
     ```

3. **保存为预制体**
   - 将配置好的GameObject保存为预制体
   - 建议路径：`Assets/Prefabs/Furniture/SpeakerPrefab.prefab`

### 步骤2：配置FurnitureDatabase

1. **打开或创建FurnitureDatabase**
   - 路径：`Assets -> Create -> DigiAnimal -> Furniture Database`

2. **添加音响配置**
   ```
   Furniture Configs:
   - Config Id: "SpeakerPrefab"
   - Prefab: [拖拽SpeakerPrefab预制体]
   ```

### 步骤3：设置UI面板

1. **创建SelectedSpeakerInfo UI**
   - 在Canvas下创建UI面板
   - 添加`SelectedSpeakerInfo.cs`脚本
   - 配置UI组件：
     ```
     [Header("UI组件")]
     canvasGroup = [面板的CanvasGroup]
     speakerNameText = [音响名称文本]
     currentTrackText = [当前曲目文本]
     speakerImage = [音响图标图片]
     
     [Header("音乐控制按钮")]
     playPauseButton = [播放/暂停按钮]
     previousButton = [上一首按钮]
     nextButton = [下一首按钮]
     
     [Header("播放/暂停按钮图标")]
     playPauseIcon = [按钮图标Image组件]
     playIcon = [播放图标精灵]
     pauseIcon = [暂停图标精灵]
     ```

### 步骤4：配置FurnitureSpawner

1. **在GameManager上配置FurnitureSpawner**
   - 确保`FurnitureSpawner`组件已添加到GameManager
   - 配置以下字段：
     ```
     database = [拖拽FurnitureDatabase资产]
     furnitureParent = [拖拽家具容器Transform]
     ```

### 步骤5：配置默认音响（可选）

1. **在FirstTimePetSelectionManager中配置**
   - 打开Start场景的FirstTimePetSelectionManager
   - 在`defaultFurnitureList`中添加：
     ```
     Default Furniture List:
     - Furniture Config Id: "SpeakerPrefab"
     - Position: Vector3(x, y, 0) // 音响的默认位置
     ```

## 使用方法

### 玩家操作
1. **选择音响** - 点击场景中的音响
2. **播放/暂停** - 点击播放/暂停按钮
3. **切换曲目** - 点击上一首/下一首按钮
4. **关闭面板** - 点击空白处或选择其他物体

### 音响功能特性
- ✅ 支持多首音乐曲目
- ✅ 播放/暂停功能（支持从暂停位置继续）
- ✅ 上一首/下一首切换
- ✅ 曲目名称显示（使用AudioClip文件名）
- ✅ 播放状态持久化（保存到存档）
- ✅ 与其他家具互斥选择

## 音频文件要求

### 支持的音频格式
- **.wav** - 推荐，无压缩，质量最好
- **.mp3** - 常用格式，文件较小
- **.ogg** - Unity推荐格式，平衡质量和大小

### 导入设置建议
```
Audio Clip Import Settings:
- Load Type: Compressed In Memory (适合背景音乐)
- Compression Format: Vorbis (跨平台兼容)
- Quality: 70% (平衡质量和文件大小)
```

## 调试信息

### 日志输出
音响系统会输出以下调试信息：
- `[SpeakerController] 开始播放: [曲目名]`
- `[SpeakerController] 暂停播放: [曲目名] (位置: X.Xs)`
- `[SpeakerController] 切换到下一首: [曲目名]`

### 常见问题
1. **音响无法播放** - 检查AudioSource组件和musicTracks列表
2. **UI不显示** - 检查SelectedSpeakerInfo的UI组件配置
3. **选择无效** - 检查Collider2D组件设置
4. **存档问题** - 检查configId是否与FurnitureDatabase一致

## 扩展功能建议

### 可添加的功能
- 🔄 **随机播放模式**
- 🔄 **单曲循环模式**
- 🔄 **音量控制**
- 🔄 **播放进度条**
- 🔄 **播放列表管理**
- 🔄 **音响外观变化**（根据播放状态）

这些功能可以基于现有的架构轻松扩展！
