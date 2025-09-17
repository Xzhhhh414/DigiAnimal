# DigiAnimal iOS 灵动岛和主屏幕 Widget 集成指南

## 概述

本指南详细说明如何使用Unity的iOS数据桥接系统在Xcode中开发灵动岛(Dynamic Island)和主屏幕Widget功能。

## 系统架构

```
Unity Game
    ↓ (IOSDataBridge)
App Group Shared Container
    ↓ (UserDefaults + Images)
iOS Widget Extension
    ↓ (WidgetKit)
Dynamic Island & Home Screen Widget
```

## Unity 端准备工作

### 1. 数据桥接系统

已完成的Unity组件：
- `IOSDataBridge.cs` - 核心数据桥接管理器
- `UnityIOSBridge.mm` - iOS原生插件

### 2. 数据结构

Unity会导出以下JSON数据到iOS：

```json
{
    "dynamicIslandEnabled": true,
    "selectedPetId": "pet_001",
            "selectedPetData": {
            "petId": "pet_001",
            "petName": "棕色猫猫",
            "prefabName": "Pet_CatBrown",
            "energy": 85,
            "satiety": 70,
            "introduction": "天下第一好猫~",
            "lastUpdateTime": "2024-01-01 12:00:00"
        },
    "lastUpdateTime": "2024-01-01 12:00:00"
}
```

### 3. 图片资源

iOS端使用独立的图片资源：
- Unity端不导出图片文件
- iOS端根据 `prefabName` 字段映射到对应的本地图片资源
- iOS项目中需要准备对应的宠物图片素材

## Xcode 项目配置

### 1. App Group 设置

1. 在Xcode中选择主项目Target
2. 进入 "Signing & Capabilities"
3. 添加 "App Groups" capability
4. 创建新的App Group，例如：`group.com.yourcompany.digianimal`

### 2. Widget Extension 创建

1. 在Xcode中添加新Target
2. 选择 "Widget Extension"
3. 命名为 `DigiAnimalWidget`
4. 确保同样添加App Group capability

### 3. Unity Build Settings

在Unity的iOS Build Settings中：
1. 设置正确的Bundle Identifier
2. 确保`IOSDataBridge`中的`appGroupIdentifier`与Xcode中的一致

## iOS Widget 开发

### 1. Widget Data Model

创建 `WidgetData.swift`：

```swift
import Foundation

struct WidgetData: Codable {
    let dynamicIslandEnabled: Bool
    let selectedPetId: String
    let selectedPetData: PetData?
    let lastUpdateTime: String
}

struct PetData: Codable {
    let petId: String
    let petName: String
    let prefabName: String
    let energy: Int
    let satiety: Int
    let introduction: String
    let lastUpdateTime: String
}
```

### 2. Shared Data Manager

创建 `SharedDataManager.swift`：

```swift
import Foundation
import UIKit

class SharedDataManager {
    static let shared = SharedDataManager()
    
    private let appGroupIdentifier = "group.com.yourcompany.digianimal"
    private let widgetDataKey = "WidgetData"
    
    private init() {}
    
    private var sharedDefaults: UserDefaults? {
        return UserDefaults(suiteName: appGroupIdentifier)
    }
    
    func getWidgetData() -> WidgetData? {
        guard let sharedDefaults = sharedDefaults,
              let jsonString = sharedDefaults.string(forKey: widgetDataKey),
              let jsonData = jsonString.data(using: .utf8) else {
            return nil
        }
        
        do {
            let widgetData = try JSONDecoder().decode(WidgetData.self, from: jsonData)
            return widgetData
        } catch {
            print("Failed to decode widget data: \(error)")
            return nil
        }
    }
    
    func getPetImage(for prefabName: String) -> UIImage? {
        // 根据prefabName映射到本地图片资源
        // 例如：Pet_CatBrown -> "cat_brown_icon"
        let imageName = mapPrefabNameToImageName(prefabName)
        return UIImage(named: imageName)
    }
    
    private func mapPrefabNameToImageName(_ prefabName: String) -> String {
        switch prefabName {
        case "Pet_CatBrown":
            return "cat_brown_icon"
        case "Pet_CatBlack":
            return "cat_black_icon"
        case "Pet_CatWhite":
            return "cat_white_icon"
        case "Pet_CatGrey":
            return "cat_grey_icon"
        default:
            return "default_pet_icon"
        }
    }
    
    func getAppGroupContainerURL() -> URL? {
        return FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: appGroupIdentifier)
    }
}
```

### 3. Widget View

创建主屏幕Widget视图：

```swift
import WidgetKit
import SwiftUI

struct DigiAnimalWidgetEntryView: View {
    var entry: Provider.Entry
    
    var body: some View {
        ZStack {
            // 背景
            LinearGradient(
                gradient: Gradient(colors: [Color.blue.opacity(0.3), Color.purple.opacity(0.3)]),
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            
            VStack(spacing: 8) {
                // 宠物头像
                if let petData = entry.widgetData?.selectedPetData,
                   let petImage = SharedDataManager.shared.getPetImage(for: petData.prefabName) {
                    Image(uiImage: petImage)
                        .resizable()
                        .aspectRatio(contentMode: .fit)
                        .frame(width: 50, height: 50)
                        .clipShape(Circle())
                } else {
                    Image(systemName: "cat.fill")
                        .font(.system(size: 30))
                        .foregroundColor(.gray)
                }
                
                // 宠物信息
                if let petData = entry.widgetData?.selectedPetData {
                    VStack(spacing: 4) {
                        Text(petData.petName)
                            .font(.caption)
                            .fontWeight(.semibold)
                            .lineLimit(1)
                        
                        HStack(spacing: 12) {
                            // 精力值
                            HStack(spacing: 2) {
                                Image(systemName: "bolt.fill")
                                    .font(.caption2)
                                    .foregroundColor(.yellow)
                                Text("\(petData.energy)")
                                    .font(.caption2)
                            }
                            
                            // 饱腹度
                            HStack(spacing: 2) {
                                Image(systemName: "heart.fill")
                                    .font(.caption2)
                                    .foregroundColor(.red)
                                Text("\(petData.satiety)")
                                    .font(.caption2)
                            }
                        }
                    }
                } else {
                    Text("DigiAnimal")
                        .font(.caption)
                        .fontWeight(.semibold)
                }
            }
            .padding(8)
        }
        .containerBackground(for: .widget) {
            Color.clear
        }
    }
}
```

### 4. Dynamic Island 配置

对于支持Dynamic Island的设备，在Widget配置中添加：

```swift
import ActivityKit

@available(iOS 16.1, *)
struct DigiAnimalLiveActivity: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: DigiAnimalLiveActivityAttributes.self) { context in
            // Dynamic Island紧凑视图
            VStack {
                HStack {
                    if let petImage = SharedDataManager.shared.getPetImage(for: context.state.prefabName) {
                        Image(uiImage: petImage)
                            .resizable()
                            .frame(width: 20, height: 20)
                            .clipShape(Circle())
                    }
                    
                    Text(context.state.petName)
                        .font(.caption2)
                        .fontWeight(.medium)
                }
            }
            .padding(.horizontal, 8)
            
        } dynamicIsland: { context in
            DynamicIsland {
                // Expanded view
                DynamicIslandExpandedRegion(.leading) {
                    if let petImage = SharedDataManager.shared.getPetImage(for: context.state.prefabName) {
                        Image(uiImage: petImage)
                            .resizable()
                            .frame(width: 40, height: 40)
                            .clipShape(Circle())
                    }
                }
                
                DynamicIslandExpandedRegion(.trailing) {
                    VStack(alignment: .trailing, spacing: 4) {
                        HStack {
                            Image(systemName: "bolt.fill")
                                .foregroundColor(.yellow)
                            Text("\(context.state.energy)")
                        }
                        
                        HStack {
                            Image(systemName: "heart.fill")
                                .foregroundColor(.red)
                            Text("\(context.state.satiety)")
                        }
                    }
                    .font(.caption2)
                }
                
                DynamicIslandExpandedRegion(.center) {
                    VStack {
                        Text(context.state.petName)
                            .font(.caption)
                            .fontWeight(.semibold)
                        
                        Text(context.state.introduction)
                            .font(.caption2)
                            .foregroundColor(.secondary)
                            .lineLimit(2)
                    }
                }
                
            } compactLeading: {
                if let petImage = SharedDataManager.shared.getPetImage(for: context.state.prefabName) {
                    Image(uiImage: petImage)
                        .resizable()
                        .frame(width: 20, height: 20)
                        .clipShape(Circle())
                }
                
            } compactTrailing: {
                Text("\(context.state.energy)")
                    .font(.caption2)
                    .fontWeight(.medium)
                
            } minimal: {
                if let petImage = SharedDataManager.shared.getPetImage(for: context.state.prefabName) {
                    Image(uiImage: petImage)
                        .resizable()
                        .frame(width: 16, height: 16)
                        .clipShape(Circle())
                }
            }
        }
    }
}

@available(iOS 16.1, *)
struct DigiAnimalLiveActivityAttributes: ActivityAttributes {
    public struct ContentState: Codable, Hashable {
        let petId: String
        let petName: String
        let prefabName: String
        let energy: Int
        let satiety: Int
        let introduction: String
        let lastUpdateTime: String
    }
}
```

## 数据同步流程

### 1. Unity 端数据变化
1. 玩家在系统设置中修改灵动岛设置
2. `SystemSettingsPanel.SaveSettings()` 被调用
3. `IOSDataBridge.ForceSyncNow()` 同步数据到iOS
4. JSON数据保存到App Group的UserDefaults

### 2. iOS 端数据读取
1. Widget或Dynamic Island需要更新时
2. `SharedDataManager.getWidgetData()` 读取JSON数据
3. `SharedDataManager.getPetImage()` 根据prefabName获取本地图片
4. 更新UI显示

## 调试指南

### 1. Unity 端调试

在Unity中使用以下方法调试：

```csharp
// 显示当前iOS数据
IOSDataBridge.Instance.ShowCurrentIOSData();

// 强制同步数据
IOSDataBridge.Instance.ForceSyncNow();
```

### 2. iOS 端调试

在Xcode中检查数据：

```swift
// 检查共享数据
let sharedDefaults = UserDefaults(suiteName: "group.com.yourcompany.digianimal")
if let widgetData = sharedDefaults?.string(forKey: "WidgetData") {
    print("Widget Data: \(widgetData)")
}

// 检查本地图片资源
if let petImage = UIImage(named: "cat_brown_icon") {
    print("Pet image loaded successfully")
} else {
    print("Failed to load pet image")
}
```

## 发布注意事项

### 1. App Store 审核
- 确保App Group ID在开发者账户中正确设置
- Widget功能要有明确的用户价值说明

### 2. 权限配置
- 在Info.plist中添加必要的权限说明
- 确保Background App Refresh已启用

### 3. 测试建议
- 在真实设备上测试Widget功能
- 测试不同iOS版本的兼容性
- 测试长时间运行的数据同步稳定性

## 示例代码仓库

完整的示例代码可以参考：
- Unity端：已集成在当前项目中
- iOS端：需要根据本指南在Xcode中实现

## 技术支持

如果遇到问题，请检查：
1. App Group ID是否一致
2. Unity中的数据是否正确导出
3. iOS端是否正确读取共享数据
4. iOS项目中是否包含对应的宠物图片资源

---

*本指南基于iOS 14.0+和Unity 2021.3+编写* 