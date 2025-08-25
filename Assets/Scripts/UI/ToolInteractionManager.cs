using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 工具交互管理器 - 管理工具使用状态和交互
/// </summary>
public class ToolInteractionManager : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private ToolUsePanelController toolUsePanel;
    
    [Header("Toast消息配置")]
    [SerializeField] private string cannotInteractMessage = "{PetName} 对玩具感到厌倦，需要休息一下";
    [SerializeField] private string sleepingInteractionMessage = "{PetName} 正在睡觉呢，别打扰ta的美梦~";
    [SerializeField] private string eatingInteractionMessage = "{PetName} 正在专心吃饭，现在可不想玩玩具";
    [SerializeField] private string pattingInteractionMessage = "{PetName} 正在被摸摸呢，请等ta享受完再来";
    [SerializeField] private string attractedInteractionMessage = "{PetName} 被逗猫棒吸引了，无法进行其他互动";
    [SerializeField] private string catTeaserInteractionMessage = "{PetName} 正在玩逗猫棒呢，请等ta玩完再来";
    [SerializeField] private string cannotPlaceObjectMessage = "无法放置{ToolName}，请选择空旷处";
    [SerializeField] private string catTeaserExistsMessage = "已经有一个逗猫棒在使用中，请等它消失后再试";
    [SerializeField] private string otherFailureMessage = "{PetName} 现在无法进行玩具互动";
    
    [Header("结束阶段设置")]
    [SerializeField] private float endingPhaseDuration = 2f; // 结束阶段展示时间（秒）
    
    //[Header("文本替换符号说明")]
    //[SerializeField] [TextArea(2, 3)] private string symbolHelp = "可用符号:\n{PetName} - 宠物名字\n{ToolName} - 工具名字\n{HeartReward} - 爱心奖励数量";
    
    // 单例模式
    private static ToolInteractionManager _instance;
    public static ToolInteractionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ToolInteractionManager>();
                
                if (_instance == null)
                {
                    Debug.LogWarning("场景中未找到ToolInteractionManager实例！请确保场景中有ToolInteractionManager组件。");
                    return null;
                }
            }
            return _instance;
        }
    }
    
    // 当前使用的工具索引(-1表示未使用工具)
    private int currentToolIndex = -1;
    
    // 当前选中的工具信息
    private ToolInfo CurrentTool => GetCurrentTool();
    
    // 工具使用状态
    private bool isUsingTool = false;
    
    /// <summary>
    /// 获取当前是否正在使用工具
    /// </summary>
    public bool IsUsingTool => isUsingTool;
    
    // 交互间隔控制（防止过快连续点击）
    private float lastInteractionTime = 0f;
    private float interactionCooldown = 0.3f; // 0.3秒交互间隔
    

    
    /// <summary>
    /// 获取工具信息数组（供其他脚本访问）
    /// </summary>
    public ToolInfo[] GetTools()
    {
        if (PlayerManager.Instance != null)
        {
            return PlayerManager.Instance.GetTools();
        }
        
        Debug.LogError("ToolInteractionManager: PlayerManager.Instance为空！");
        return new ToolInfo[0];
    }
    
    /// <summary>
    /// 获取当前选中的工具信息
    /// </summary>
    private ToolInfo GetCurrentTool()
    {
        if (PlayerManager.Instance != null && currentToolIndex >= 0)
        {
            return PlayerManager.Instance.GetTool(currentToolIndex);
        }
        return null;
    }
    
    private void Awake()
    {
        //Debug.Log($"[ToolInteractionManager] Awake开始执行 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 场景特定的单例初始化 - 移除DontDestroyOnLoad
        if (_instance == null)
        {
            _instance = this;
            //Debug.Log($"[ToolInteractionManager] 单例初始化完成（场景特定） - Scene: {gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // 检查现有实例是否来自不同场景
            if (_instance.gameObject.scene != this.gameObject.scene)
            {
                //Debug.Log($"[ToolInteractionManager] 检测到跨场景实例冲突，替换为当前场景实例 - 旧场景: {_instance.gameObject.scene.name}, 新场景: {gameObject.scene.name}");
                // 清理旧实例的引用
                var oldInstance = _instance;
                _instance = this;
                // 销毁旧实例
                if (oldInstance != null)
                {
                    Destroy(oldInstance.gameObject);
                }
            }
            else
            {
                //Debug.Log($"[ToolInteractionManager] 销毁同场景重复实例 - Scene: {gameObject.scene.name}");
                Destroy(gameObject);
                return;
            }
        }
        
        //Debug.Log($"[ToolInteractionManager] Awake执行完成 - Scene: {gameObject.scene.name}");
    }
    
    private void OnDestroy()
    {
        //Debug.Log($"[ToolInteractionManager] OnDestroy被调用 - GameObject: {gameObject.name}, Scene: {gameObject.scene.name}");
        
        // 只有当前实例是静态引用时才清除
        if (_instance == this)
        {
            //Debug.Log($"[ToolInteractionManager] 清除静态引用 - Scene: {gameObject.scene.name}");
            _instance = null;
        }
    }
    
    private void Update()
    {
        // 在工具使用状态下检测点击
        if (isUsingTool && Input.GetMouseButtonDown(0))
        {
            // 检查是否点击在UI上
            if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            {
                return;
            }
            
            // 尝试与宠物交互
            TryInteractWithPet();
        }
    }
    
    private void TryInteractWithPet()
    {
        // 从摄像机发射射线到点击位置
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // 获取点击的世界坐标
        Vector3 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        clickPosition.z = 0; // 确保Z坐标为0
        
        // 检查是否点击到宠物（只对DirectInteraction类型的工具有效）
        if (hit.collider != null && CurrentTool != null && CurrentTool.toolType == ToolType.DirectInteraction)
        {
            PetController2D pet = hit.collider.GetComponent<PetController2D>();
            
            if (pet != null)
            {
                // 与宠物交互
                InteractWithPet(pet);
                return;
            }
        }
        
        // 如果没有点击到宠物，检查是否是可放置物体类型的工具
        if (CurrentTool != null && CurrentTool.toolType == ToolType.PlaceableObject)
        {
            TryPlaceObject(clickPosition);
        }
    }
    
    /// <summary>
    /// 启动工具使用
    /// </summary>
    public void StartToolUse(int toolIndex)
    {
        // 验证PlayerManager
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("ToolInteractionManager: PlayerManager.Instance为空！");
            return;
        }
        
        // 验证工具索引
        if (toolIndex < 0 || toolIndex >= PlayerManager.Instance.GetToolCount())
        {
            Debug.LogError($"工具索引无效: {toolIndex}");
            return;
        }
        
        // 设置当前工具
        currentToolIndex = toolIndex;
        isUsingTool = true;
        
        // 显示工具使用面板
        if (toolUsePanel != null)
        {
            // 设置使用说明文本
            if (CurrentTool != null)
            {
                toolUsePanel.SetInstructionText(CurrentTool.useInstruction);
            }
            
            // 显示面板
            toolUsePanel.ShowPanel();
        }
        else
        {
            Debug.LogError("ToolInteractionManager: toolUsePanel引用为空！请在Inspector中设置ToolUsePanelController引用");
        }
        
        // 进入工具使用模式，隐藏其他UI界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.EnterToolUseMode();
        }
        else
        {
            Debug.LogWarning("ToolInteractionManager: UIManager.Instance为空");
        }
    }
    
    /// <summary>
    /// 判断工具是否是基于互动的（需要宠物互动才完成，奖励在互动时发放）
    /// </summary>
    /// <param name="toolName">工具名称</param>
    /// <returns>是否是基于互动的工具</returns>
    private bool IsInteractionBasedTool(string toolName)
    {
        switch (toolName)
        {
            case "逗猫棒":
            case "玩具老鼠":
                return true; // 这些工具需要宠物互动才完成，奖励在互动完成时发放
            default:
                return false; // 其他工具放置即完成，立即发放奖励
        }
    }
    
    /// <summary>
    /// 更新工具使用面板为互动时的指令文本
    /// </summary>
    /// <param name="toolName">需要更新文本的工具名称</param>
    /// <param name="pet">参与交互的宠物（用于占位符替换）</param>
    public void UpdateToInteractingInstructionText(string toolName, PetController2D pet = null)
    {
        // 只有当前选中的工具匹配时才更新文本
        if (CurrentTool != null && CurrentTool.toolName == toolName)
        {
            var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
            if (toolUsePanel != null && !string.IsNullOrEmpty(CurrentTool.interactingInstruction))
            {
                string interactingText = CurrentTool.interactingInstruction;
                
                // 替换占位符
                interactingText = interactingText.Replace("{ToolName}", CurrentTool.toolName);
                interactingText = interactingText.Replace("{HeartReward}", CurrentTool.heartReward.ToString());
                
                // 替换宠物名称占位符（如果有宠物）
                if (pet != null)
                {
                    interactingText = interactingText.Replace("{PetName}", pet.PetDisplayName);
                }
                
                toolUsePanel.SetInstructionText(interactingText);
                // Debug.Log($"更新工具 '{toolName}' 为互动时的说明文本: {interactingText}");
            }
        }
    }
    
    /// <summary>
    /// 更新工具使用面板为直接交互成功后的指令文本，并隐藏取消按钮
    /// </summary>
    /// <param name="toolName">需要更新文本的工具名称</param>
    /// <param name="pet">参与交互的宠物（用于占位符替换）</param>
    public void UpdateToInteractedInstructionText(string toolName, PetController2D pet = null)
    {
        // 只有当前选中的工具匹配时才更新文本
        if (CurrentTool != null && CurrentTool.toolName == toolName)
        {
            var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
            if (toolUsePanel != null)
            {
                // 更新文本（支持占位符替换）
                if (!string.IsNullOrEmpty(CurrentTool.interactedInstruction))
                {
                    string interactedText = CurrentTool.interactedInstruction;
                    
                    // 替换占位符
                    interactedText = interactedText.Replace("{ToolName}", CurrentTool.toolName);
                    interactedText = interactedText.Replace("{HeartReward}", CurrentTool.heartReward.ToString());
                    
                    // 替换宠物名称占位符（如果有宠物）
                    if (pet != null)
                    {
                        interactedText = interactedText.Replace("{PetName}", pet.PetDisplayName);
                    }
                    
                    toolUsePanel.SetInstructionText(interactedText);
                    //Debug.Log($"更新工具 '{toolName}' 为交互成功后的说明文本: {interactedText}");
                }
                
                // 隐藏取消按钮
                toolUsePanel.HideCancelButton();
            }
        }
    }
    
    /// <summary>
    /// 恢复工具使用面板的原始指令文本
    /// </summary>
    /// <param name="toolName">需要恢复文本的工具名称</param>
    public void RestoreOriginalInstructionText(string toolName)
    {
        // 只有当前选中的工具匹配时才恢复文本
        if (CurrentTool != null && CurrentTool.toolName == toolName)
        {
            var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
            if (toolUsePanel != null && !string.IsNullOrEmpty(CurrentTool.useInstruction))
            {
                toolUsePanel.SetInstructionText(CurrentTool.useInstruction);
                //Debug.Log($"恢复工具 '{toolName}' 的原始使用说明文本");
            }
        }
    }
    
    /// <summary>
    /// 取消工具使用
    /// </summary>
    public void CancelToolUse()
    {
        isUsingTool = false;
        currentToolIndex = -1;
        
        // 隐藏工具使用面板
        if (toolUsePanel != null)
        {
            toolUsePanel.HidePanel();
        }
        
        // 关闭工具包选择界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseToolkit();
        }
        
        // 退出工具使用模式，恢复其他UI界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ExitToolUseMode();
        }
    }
    
    /// <summary>
    /// 完成工具使用后回到工具背包界面
    /// </summary>
    public void ReturnToToolkit()
    {
        isUsingTool = false;
        currentToolIndex = -1;
        
        // 隐藏工具使用面板
        if (toolUsePanel != null)
        {
            toolUsePanel.HidePanel();
        }
        
        // 退出工具使用模式
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ExitToolUseMode();
        }
        
        // 重新打开工具包选择界面（而不是关闭）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenToolkit();
        }
    }
    
    /// <summary>
    /// 开始通用的玩具互动结束阶段（显示结束文本、给予奖励、延迟后回到工具背包）
    /// </summary>
    /// <param name="toolName">玩具名称</param>
    /// <param name="pet">参与互动的宠物（可为null，用于放置类玩具）</param>
    public void StartInteractionEndingPhase(string toolName, PetController2D pet = null)
    {
        if (CurrentTool == null || CurrentTool.toolName != toolName)
        {
            Debug.LogWarning($"StartInteractionEndingPhase: 当前工具不匹配 '{toolName}'");
            ReturnToToolkit(); // 直接回到工具背包
            return;
        }
        
        // 开始结束阶段的协程
        StartCoroutine(InteractionEndingCoroutine(pet));
    }
    
    /// <summary>
    /// 开始放置类玩具的无互动结束阶段（显示无互动文本、延迟后回到工具背包）
    /// </summary>
    /// <param name="toolName">玩具名称</param>
    public void StartNoInteractionEndingPhase(string toolName)
    {
        if (CurrentTool == null || CurrentTool.toolName != toolName)
        {
            Debug.LogWarning($"StartNoInteractionEndingPhase: 当前工具不匹配 '{toolName}'");
            ReturnToToolkit(); // 直接回到工具背包
            return;
        }
        
        // 开始无互动结束阶段的协程
        StartCoroutine(NoInteractionEndingCoroutine());
    }
    
    /// <summary>
    /// 玩具互动结束阶段的协程
    /// </summary>
    /// <param name="pet">参与互动的宠物（可为null）</param>
    private System.Collections.IEnumerator InteractionEndingCoroutine(PetController2D pet)
    {
        // 1. 给予爱心奖励
        int heartReward = CurrentTool.heartReward;
        if (PlayerManager.Instance != null && heartReward > 0)
        {
            PlayerManager.Instance.AddHeartCurrency(heartReward);
            
            // 显示爱心获得提示
            var heartManager = UnityEngine.Object.FindObjectOfType<HeartMessageManager>();
            if (heartManager != null && pet != null)
            {
                heartManager.ShowHeartGainMessage(pet, heartReward);
            }
        }
        
        // 2. 更新为通用结束文本
        var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
        if (toolUsePanel != null && !string.IsNullOrEmpty(CurrentTool.endingInstruction))
        {
            // 替换文本中的爱心奖励占位符
            string endingText = CurrentTool.endingInstruction.Replace("{HeartReward}", heartReward.ToString());
            // 替换工具名称占位符
            endingText = endingText.Replace("{ToolName}", CurrentTool.toolName);
            // 替换宠物名称占位符（如果有宠物）
            if (pet != null)
            {
                endingText = endingText.Replace("{PetName}", pet.PetDisplayName);
            }
            
            toolUsePanel.SetInstructionText(endingText);
            // Debug.Log($"显示玩具 '{CurrentTool.toolName}' 的结束文本: {endingText}");
        }
        
        // 3. 等待配置的结束阶段时间
        yield return new WaitForSeconds(endingPhaseDuration);
        
        // 4. 回到工具背包界面
        ReturnToToolkit();
    }
    
    /// <summary>
    /// 放置类玩具无互动结束阶段的协程
    /// </summary>
    private System.Collections.IEnumerator NoInteractionEndingCoroutine()
    {
        // 1. 更新为无互动文本（不给予奖励）
        var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
        if (toolUsePanel != null && !string.IsNullOrEmpty(CurrentTool.noInteractionInstruction))
        {
            // 替换文本中的占位符
            string noInteractionText = CurrentTool.noInteractionInstruction;
            
            // 替换工具名称占位符
            noInteractionText = noInteractionText.Replace("{ToolName}", CurrentTool.toolName);
            
            toolUsePanel.SetInstructionText(noInteractionText);
            //Debug.Log($"显示玩具 '{CurrentTool.toolName}' 的无互动结束文本: {noInteractionText}");
        }
        
        // 2. 等待配置的结束阶段时间（与成功互动时间一致）
        yield return new WaitForSeconds(endingPhaseDuration);
        
        // 3. 回到工具背包界面
        ReturnToToolkit();
    }
    
    /// <summary>
    /// 与宠物交互
    /// </summary>
    private void InteractWithPet(PetController2D pet)
    {
        if (pet == null || CurrentTool == null) return;
        
        // 检查交互间隔
        if (Time.time - lastInteractionTime < interactionCooldown)
        {
            return; // 还在冷却期内，忽略这次交互
        }
        
        // 更新最后交互时间
        lastInteractionTime = Time.time;
        
        // 获取当前工具的爱心奖励数量
        int heartReward = CurrentTool.heartReward;
        
        // 记录交互前的厌倦状态
        bool wasBoredBefore = pet.IsBored;
        
        // 尝试玩具互动（传入奖励值而不是固定的1.0f）
        int interactionResult = pet.TryToyInteraction(heartReward);
        
        switch (interactionResult)
        {
            case 0: // 成功
            // 检查本次交互是否让宠物进入厌倦状态
            bool becameBored = !wasBoredBefore && pet.IsBored;
            
            // 启动具体的玩具互动表现
            pet.StartToyInteraction(CurrentTool.toolName);
            
            // 成功互动不再显示消息气泡（根据用户需求简化UI）
            
            // 更新为交互成功后的文本并隐藏取消按钮（传入宠物对象以支持占位符替换）
            UpdateToInteractedInstructionText(CurrentTool.toolName, pet);
            
            // 直接交互成功后，停止接受进一步的点击操作
            isUsingTool = false;
            
            // 注意：爱心奖励将在互动结束阶段统一发放，不在这里发放
                break;
                
            case 1: // 厌倦状态
                string boredMessage = ReplaceTextSymbols(cannotInteractMessage, pet, 0);
                ShowPetMessage(pet, boredMessage, PetNeedType.Indifferent);
                break;
                
            case 2: // 正在睡觉
                string sleepingMessage = ReplaceTextSymbols(sleepingInteractionMessage, pet, 0);
                ShowPetMessage(pet, sleepingMessage, PetNeedType.None);
                break;
                
            case 3: // 正在吃饭
                string eatingMessage = ReplaceTextSymbols(eatingInteractionMessage, pet, 0);
                ShowPetMessage(pet, eatingMessage, PetNeedType.None);
                break;
                
            case 4: // 正在被摸摸
                string pattingMessage = ReplaceTextSymbols(pattingInteractionMessage, pet, 0);
                ShowPetMessage(pet, pattingMessage, PetNeedType.None);
                break;
                
            case 5: // 被逗猫棒吸引
                string attractedMessage = ReplaceTextSymbols(attractedInteractionMessage, pet, 0);
                ShowPetMessage(pet, attractedMessage, PetNeedType.None);
                break;
                
            case 6: // 正在与逗猫棒互动
                string catTeaserMessage = ReplaceTextSymbols(catTeaserInteractionMessage, pet, 0);
                ShowPetMessage(pet, catTeaserMessage, PetNeedType.None);
                break;
                
            case 7: // 正在与玩具老鼠互动
                ShowPetMessage(pet, "我正在和玩具老鼠玩耍，请稍后再来~", PetNeedType.None);
                break;
                
            default: // 其他失败情况
                string failureMessage = ReplaceTextSymbols(otherFailureMessage, pet, 0);
                ShowPetMessage(pet, failureMessage, PetNeedType.Indifferent);
                break;
        }
        
        // 不再自动关闭工具使用界面，保持在玩具使用状态
        // CancelToolUse(); // 注释掉这行
    }
    
    /// <summary>
    /// 显示宠物消息
    /// </summary>
    private void ShowPetMessage(PetController2D pet, string message)
    {
        var petMessageManager = FindObjectOfType<PetMessageManager>();
        if (petMessageManager != null)
        {
            petMessageManager.ShowPetMessage(pet, message);
        }
        else
        {
            Debug.LogWarning("未找到PetMessageManager！请在场景中添加PetMessageManager组件以显示宠物消息。");
        }
    }
    
    /// <summary>
    /// 显示宠物消息（指定气泡类型）
    /// </summary>
    private void ShowPetMessage(PetController2D pet, string message, PetNeedType bubbleType)
    {
        var petMessageManager = FindObjectOfType<PetMessageManager>();
        if (petMessageManager != null)
        {
            petMessageManager.ShowPetMessage(pet, message, bubbleType);
        }
        else
        {
            Debug.LogWarning("未找到PetMessageManager！请在场景中添加PetMessageManager组件以显示宠物消息。");
        }
    }
    
    /// <summary>
    /// 替换文本中的符号
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <param name="pet">宠物对象</param>
    /// <param name="heartReward">爱心奖励数量</param>
    /// <returns>替换后的文本</returns>
    private string ReplaceTextSymbols(string text, PetController2D pet, int heartReward)
    {
        string result = text;
        
        // 替换宠物名字
        if (pet != null)
        {
            result = result.Replace("{PetName}", pet.PetDisplayName);
        }
        
        // 替换工具名字
        if (CurrentTool != null)
        {
            result = result.Replace("{ToolName}", CurrentTool.toolName);
        }
        
        // 替换爱心奖励数量
        result = result.Replace("{HeartReward}", heartReward.ToString());
        
        return result;
    }
    
    /// <summary>
    /// 尝试放置可放置物体类型的工具
    /// </summary>
    /// <param name="position">放置位置</param>
    private void TryPlaceObject(Vector3 position)
    {
        if (CurrentTool == null) return;
        
        // 检查交互间隔
        if (Time.time - lastInteractionTime < interactionCooldown)
        {
            return; // 还在冷却期内，忽略这次交互
        }
        
        // 更新最后交互时间
        lastInteractionTime = Time.time;
        
        // 获取当前工具的检测半径
        float detectionRadius = GetToolDetectionRadius();
        
        // 根据工具类型进行特定检查
        switch (CurrentTool.toolName)
        {
            case "逗猫棒":
                if (CatTeaserController.HasActiveCatTeaser)
                {
                    ShowGeneralMessage(catTeaserExistsMessage);
                    return;
                }
                
                if (!CatTeaserController.CanPlaceAt(position, detectionRadius))
                {
                    ShowGeneralMessage(ReplaceToolNameInMessage(cannotPlaceObjectMessage));
                    return;
                }
                break;
                
            case "玩具老鼠":
                // TODO: 添加玩具老鼠的特定检查逻辑
                // 可以检查是否已有玩具老鼠、位置是否合适等
                break;
                
            default:
                // 通用的可放置物体检查
                if (!CanPlaceObjectAt(position, detectionRadius))
                {
                    ShowGeneralMessage(ReplaceToolNameInMessage(cannotPlaceObjectMessage));
                    return;
                }
                break;
        }
        
        // 放置物体
        PlaceObject(position);
    }
    
    /// <summary>
    /// 在指定位置放置可放置物体
    /// </summary>
    /// <param name="position">放置位置</param>
    private void PlaceObject(Vector3 position)
    {
        if (CurrentTool == null || CurrentTool.toolPrefab == null)
        {
            Debug.LogError($"工具 '{CurrentTool?.toolName}' 没有设置预制体！请在PlayerManager中配置toolPrefab字段。");
            ShowGeneralMessage($"{CurrentTool?.toolName ?? "工具"}创建失败，请联系开发者");
            return;
        }
        
        // 使用工具配置中的预制体
        GameObject placedObj = Instantiate(CurrentTool.toolPrefab, position, Quaternion.identity);
        
        // 根据工具类型添加特定组件或进行特定设置
        switch (CurrentTool.toolName)
        {
            case "逗猫棒":
                // 确保逗猫棒有正确的控制器组件
                CatTeaserController catController = placedObj.GetComponent<CatTeaserController>();
                if (catController == null)
                {
                    catController = placedObj.AddComponent<CatTeaserController>();
                }
                break;
                
            case "玩具老鼠":
                // TODO: 为玩具老鼠添加对应的控制器组件
                // ToyMouseController mouseController = placedObj.GetComponent<ToyMouseController>();
                // if (mouseController == null)
                // {
                //     mouseController = placedObj.AddComponent<ToyMouseController>();
                // }
                break;
        }
        
        // 获得爱心奖励（仅限于放置即完成的工具，交互型工具在互动完成时发放）
        if (PlayerManager.Instance != null && !IsInteractionBasedTool(CurrentTool.toolName))
        {
            int heartReward = CurrentTool.heartReward;
            PlayerManager.Instance.AddHeartCurrency(heartReward);
            
            // 可放置物体放置成功的爱心奖励不需要特定位置显示
            // 爱心货币已在上方添加，UI会自动更新显示
        }
        
                //Debug.Log($"{CurrentTool.toolName}已放置在位置: {position}");

        // 更新工具使用面板的提示文本为放置后的说明，并隐藏取消按钮
        var toolUsePanel = FindObjectOfType<ToolUsePanelController>();
        if (toolUsePanel != null)
        {
            // 更新文本
            if (!string.IsNullOrEmpty(CurrentTool.placedInstruction))
            {
                toolUsePanel.SetInstructionText(CurrentTool.placedInstruction);
            }
            
            // 隐藏取消按钮
            toolUsePanel.HideCancelButton();
        }
        
        // 放置成功后，停止接受进一步的放置操作，但保持面板显示给用户看到放置成功的提示
        isUsingTool = false;
    }
    
    /// <summary>
    /// 通用的检查位置是否可以放置物体
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="checkRadius">检测半径</param>
    /// <returns>是否可以放置</returns>
    private bool CanPlaceObjectAt(Vector3 position, float checkRadius)
    {
        // 检查位置是否有阻挡物
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, checkRadius);
        
        foreach (var collider in colliders)
        {
            // 检查layer是否为Pet(宠物)或Scene(场景阻挡物)
            int layerMask = collider.gameObject.layer;
            string layerName = LayerMask.LayerToName(layerMask);
            
            if (layerName == "Pet" || layerName == "Scene")
            {
                return false;
            }
            
            // 如果有其他重要的游戏物体，也不能放置
            if (collider.GetComponent<FoodController>() != null)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 显示通用消息（不需要宠物对象）
    /// </summary>
    /// <param name="message">要显示的消息</param>
    private void ShowGeneralMessage(string message)
    {
        // 使用ToastManager显示提示消息
        if (ToastManager.Instance != null)
        {
            ToastManager.Instance.ShowToast(message);
        }
        else
        {
            // 如果ToastManager不可用，回退到Debug.Log
           //Debug.Log("游戏提示: " + message);
            Debug.LogWarning("ToastManager未找到，无法显示Toast提示。");
        }
    }
    
    /// <summary>
    /// 替换通用消息中的工具名称占位符
    /// </summary>
    /// <param name="message">包含{ToolName}占位符的消息</param>
    /// <returns>替换后的消息</returns>
    private string ReplaceToolNameInMessage(string message)
    {
        if (CurrentTool != null)
        {
            return message.Replace("{ToolName}", CurrentTool.toolName);
        }
        return message.Replace("{ToolName}", "工具");
    }
    
    /// <summary>
    /// 获取当前工具的检测半径（基于prefab的collider）
    /// </summary>
    /// <returns>检测半径</returns>
    private float GetToolDetectionRadius()
    {
        if (CurrentTool == null || CurrentTool.toolPrefab == null)
        {
            Debug.LogError("ToolInteractionManager: 无法获取工具prefab！请确保工具配置了预制体。");
            return 1f; // 默认半径
        }
        
        // 获取prefab上的所有Collider2D组件
        Collider2D[] colliders = CurrentTool.toolPrefab.GetComponentsInChildren<Collider2D>();
        
        if (colliders.Length == 0)
        {
            Debug.LogError($"ToolInteractionManager: 工具prefab '{CurrentTool.toolName}' 没有Collider2D组件！请在prefab上添加碰撞体。");
            return 1f; // 默认半径
        }
        
        float maxRadius = 0f;
        
        // 遍历所有collider，找到最大的检测范围
        foreach (var collider in colliders)
        {
            if (collider == null || !collider.enabled) continue;
            
            float currentRadius = GetColliderRadius(collider);
            if (currentRadius > maxRadius)
            {
                maxRadius = currentRadius;
            }
        }
        
        // 如果没有找到有效的半径，返回默认值
        if (maxRadius <= 0f)
        {
            Debug.LogError($"ToolInteractionManager: 无法从工具prefab '{CurrentTool.toolName}' 获取有效半径！请检查碰撞体配置。");
            return 1f; // 默认半径
        }
        
        return maxRadius;
    }
    
    /// <summary>
    /// 根据Collider2D类型获取等效半径
    /// </summary>
    /// <param name="collider">碰撞体组件</param>
    /// <returns>等效半径</returns>
    private float GetColliderRadius(Collider2D collider)
    {
        if (collider == null) return 0f;
        
        // 根据不同类型的Collider2D计算半径
        switch (collider)
        {
            case CircleCollider2D circle:
                // 圆形碰撞体直接返回半径
                return circle.radius * Mathf.Max(collider.transform.localScale.x, collider.transform.localScale.y);
                
            case BoxCollider2D box:
                // 矩形碰撞体返回对角线的一半作为等效半径
                Vector2 size = box.size;
                size.x *= collider.transform.localScale.x;
                size.y *= collider.transform.localScale.y;
                return Mathf.Sqrt(size.x * size.x + size.y * size.y) * 0.5f;
                
            case CapsuleCollider2D capsule:
                // 胶囊碰撞体返回最大尺寸的一半
                Vector2 capsuleSize = capsule.size;
                capsuleSize.x *= collider.transform.localScale.x;
                capsuleSize.y *= collider.transform.localScale.y;
                return Mathf.Max(capsuleSize.x, capsuleSize.y) * 0.5f;
                
            default:
                // 其他类型的碰撞体使用bounds计算等效半径
                Bounds bounds = collider.bounds;
                Vector3 size3D = bounds.size;
                return Mathf.Sqrt(size3D.x * size3D.x + size3D.y * size3D.y) * 0.5f;
        }
    }
} 