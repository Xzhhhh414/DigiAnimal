# HeartMessage爱心获得提示系统设置指南 v2.0

## 新功能特性 🎉
- **可配置奖励**：每个工具可设置不同的爱心奖励数量
- **伤害数字效果**：仿伤害数字的动画表现（弹出、向上飘动、淡出）
- **更快响应**：提示显示时间比对话更短，提供更好的用户体验
- **智能查找**：自动在预制体中查找Text组件，无需手动配置

## 快速故障排除

如果交互后没有出现HeartMessage，请按以下步骤检查：

### 1. 检查必需组件
运行游戏后，按J键执行自动配置检查，查看Console输出：
- ✅ 表示配置正确
- ❌ 表示配置错误，需要修复
- ⚠️ 表示建议改进，不影响基本功能

### 2. 配置工具奖励（新功能！）
在PlayerManager的工具配置中：
1. 选择PlayerManager对象
2. 在Inspector中找到Tools数组
3. 为每个工具设置**Heart Reward**值（默认为1）
4. 不同工具可以设置不同的奖励数量

### 3. 创建HeartMessage预制体（重要！）
**这是最关键的步骤，必须手动创建：**

1. 在场景的Canvas下创建UI对象：
   - 右键Canvas → UI → Panel，重命名为"HeartMessage"
   - 删除Panel的Image组件（我们只需要RectTransform）

2. 添加必需组件：
   - 确保有RectTransform（自动有）
   - 添加CanvasGroup组件（Component → Layout → Canvas Group）

3. 添加文字显示：
   - 右键HeartMessage → UI → Text，重命名为"MessageText"
   - 设置Text内容为"+1"（实际显示时会被动态替换）
   - 设置字体大小24-32，颜色为红色或金色
   - 设置Alignment为Center
   - **推荐**：设置字体为粗体，增强视觉效果

4. 保存为预制体：
   - 将HeartMessage拖拽到Project窗口，保存为预制体
   - 然后从场景中删除这个临时对象

### 4. 配置HeartMessageManager
1. 在场景中创建空GameObject，命名为"HeartMessageManager"
2. 添加HeartMessageManager脚本
3. 在Inspector中设置：
   - **Heart Message Prefab**: 拖拽刚创建的预制体
   - **Target Canvas**: 拖拽场景中的Canvas
   - **Offset From Pet**: 设置为(100, 50)
   - **动画参数**: 可根据喜好调整

### 5. 测试系统
1. 运行游戏
2. 按J键检查配置（查看Console输出）
3. 按H键测试爱心消息显示
4. 或者使用正常的宠物交互测试
5. 观察不同工具是否显示不同的奖励数量

## 预制体结构示例
```
HeartMessage (RectTransform + CanvasGroup)
└── MessageText (Text) - 动态显示"+X"（X为工具奖励数量）
```

## 动画效果说明

### 伤害数字风格动画
1. **快速弹出**：从0缩放到1.3倍，产生弹出效果
2. **回弹正常**：缩放回1.0倍，开始向上飘动
3. **持续上升**：继续向上移动，模拟飘浮效果
4. **淡出消失**：在1.5秒内淡出（比对话更快）

### 动画时序
- **总时长**：约1.5秒（比PetMessage的2.4秒更短）
- **淡入**：0.2秒
- **弹出**：0.15秒
- **淡出**：0.8秒
- **同步性**：与对话气泡同时开始，但更早结束

## 常见问题解决

### 问题1：Console显示"❌ HeartMessage预制体未设置"
**解决**：确保在HeartMessageManager的Inspector中拖拽了预制体

### 问题2：Console显示"❌ 预制体缺少RectTransform组件"
**解决**：预制体必须是UI对象，不能是普通GameObject

### 问题3：Console显示"❌ 场景中未找到Canvas"
**解决**：场景必须有Canvas，可以创建UI → Canvas

### 问题4：Console显示"❌ 未找到主摄像机"
**解决**：确保场景中的Camera标签设置为"MainCamera"

### 问题5：创建了对象但看不到
**检查**：
- Canvas的Render Mode是否正确
- HeartMessage的位置是否在屏幕范围内
- CanvasGroup的Alpha是否为0（应该由动画控制）

### 问题6：显示的数字不对
**检查**：
- PlayerManager中工具的Heart Reward设置
- ToolInteractionManager的消息配置是否包含{HeartReward}符号

## 配置参数说明

### HeartMessageManager配置
- **Display Duration**: 显示时长（推荐1.5秒）
- **Fade In Duration**: 淡入时长（推荐0.2秒）
- **Fade Out Duration**: 淡出时长（推荐0.8秒）
- **Move Up Distance**: 向上飘动距离（推荐80像素）
- **Scale Pop Duration**: 弹出效果时长（推荐0.15秒）
- **Max Scale Effect**: 最大缩放倍数（推荐1.3）
- **Offset From Pet**: 相对宠物的偏移量（推荐100, 50）

### ToolInfo配置（新增）
- **Heart Reward**: 使用该工具成功交互时获得的爱心数量
- 不同工具可以设置不同数值（如高级工具给更多爱心）

## 符号替换系统

在ToolInteractionManager的消息中支持：
- **{PetName}** - 宠物名字
- **{ToolName}** - 工具名字  
- **{HeartReward}** - 爱心奖励数量（新增）

## 调试工具使用
- **H键**：快速测试爱心消息
- **J键**：检查系统配置
- 或在HeartMessageTester组件上右键选择Context Menu选项

## 注意事项
1. 确保场景中有Camera.main
2. 确保Canvas的Render Mode设置正确
3. 预制体的CanvasGroup组件是必需的
4. 系统会自动管理多个爱心提示的显示
5. 提示会跟随宠物移动，包括动画过程中
6. 预制体不是默认存在的，必须手动创建！
7. **新**：HeartMessage的显示时间比PetMessage更短，提供更好的层次感 