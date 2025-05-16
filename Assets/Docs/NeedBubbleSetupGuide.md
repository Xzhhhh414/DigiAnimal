# 宠物需求气泡系统设置指南

本指南介绍如何为宠物设置需求气泡系统，让宠物在需要睡觉或吃东西时能够通过气泡表达。

## 步骤一：准备气泡精灵资源

1. 准备两个图片资源：
   - **气泡框**：通用的气泡背景
   - **状态图标**：
     - 睡觉图标（例如：一个Z字符或床的图标）
     - 食物图标（例如：一个骨头或食物碗的图标）

2. 将这些精灵导入到项目中，放在合适的资源文件夹，如 `Assets/Sprites/UI/Bubbles/`

## 步骤二：设置宠物预制体结构

1. 选中宠物预制体，在其下添加一个新的空对象作为气泡：
   ```
   Pet (主宠物对象)
   ├── Sprite (宠物精灵)
   └── NeedBubble (新添加的空对象)
       ├── BubbleFrame (SpriteRenderer - 气泡框)
       └── StatusIcon (SpriteRenderer - 状态图标)
   ```

2. 设置气泡位置：
   - 将`NeedBubble`放置在宠物头顶适当位置
   - 设置合适的本地坐标，例如 `(0, 1.5, 0)`

3. 添加组件：
   - 为`BubbleFrame`添加SpriteRenderer组件，设置气泡框精灵
   - 为`StatusIcon`添加SpriteRenderer组件，初始不设置精灵（由代码动态设置）

## 步骤三：添加NeedBubbleController组件

1. 选中`NeedBubble`对象
2. 添加`NeedBubbleController`组件
3. 在Inspector中设置以下参数：
   - **Bubble Object**：拖拽整个`NeedBubble`对象
   - **Status Icon Renderer**：拖拽`StatusIcon`的SpriteRenderer组件
   - **Hungry Icon**：拖拽食物图标精灵
   - **Tired Icon**：拖拽睡觉图标精灵
   - **Float Speed**：设置浮动动画速度，推荐值为`1.0`
   - **Float Amount**：设置浮动幅度，推荐值为`0.1`

## 步骤四：配置CharacterController2D

1. 选中宠物对象
2. 在CharacterController2D组件中，找到"需求气泡设置"部分
3. 设置以下参数：
   - **Need Bubble Controller**：拖拽上面添加的NeedBubbleController组件
   - **Hungry Threshold**：设置饥饿阈值（饱腹度低于此值时显示饥饿气泡），默认值为`25`
   - **Tired Threshold**：设置疲劳阈值（精力值低于此值时显示疲劳气泡），默认值为`30`

## 步骤五：测试系统

1. 进入游戏预览模式
2. 等待宠物的饱腹度或精力值降至阈值以下
3. 观察气泡是否正确出现并有浮动效果
4. 测试当宠物吃食物或睡觉时，气泡是否正确消失

## 扩展添加新的需求类型

如需添加新的需求类型：

1. 在`PetNeedType`枚举中添加新类型
2. 为新类型准备对应的图标精灵
3. 在`NeedBubbleController`中：
   - 添加新的图标字段
   - 在`ShowNeed`方法的switch语句中添加新的case
   - 在优先级字典中设置新类型的优先级
4. 在`CharacterController2D`中添加相应的监测和显示逻辑

## 注意事项

- 确保所有精灵的Pivot设置正确，以保证气泡位置准确
- 气泡图像应设为适当的Sorting Layer和Order in Layer，确保它显示在宠物上方
- 如果预期气泡在不同尺寸的宠物上使用，可考虑将气泡位置设为相对于宠物大小的位置 