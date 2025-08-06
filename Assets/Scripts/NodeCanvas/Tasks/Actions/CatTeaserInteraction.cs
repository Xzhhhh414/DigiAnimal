using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物与逗猫棒互动的行为")]
    public class CatTeaserInteraction : ActionTask<PetController2D>
    {
        [Name("互动持续时间")]
        public BBParameter<float> interactionDuration = 5f;
        
        private float startTime;
        private bool hasStarted = false;
        private CatTeaserController targetCatTeaser;
        
        protected override string info
        {
            get 
            { 
                int heartReward = GetCatTeaserHeartReward();
                return string.Format("逗猫棒互动 (持续时间: {0}秒, 爱心: {1})", interactionDuration.value, heartReward); 
            }
        }
        
        protected override void OnExecute()
        {
            if (agent == null)
            {
                EndAction(false);
                return;
            }
            
            // 检查是否有活跃的逗猫棒
            if (!CatTeaserController.HasActiveCatTeaser)
            {
                EndAction(false);
                return;
            }
            
            if (!hasStarted)
            {
                StartInteraction();
            }
        }
        
        protected override void OnUpdate()
        {
            if (agent == null || targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            // 检查是否到达互动持续时间
            if (Time.time - startTime >= interactionDuration.value)
            {
                EndInteraction();
                return;
            }
        }
        
        private void StartInteraction()
        {
            hasStarted = true;
            startTime = Time.time;
            targetCatTeaser = CatTeaserController.CurrentInstance;
            
            if (targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            // 调用PetController2D的StartCatTeaser方法
            agent.StartCatTeaser();
            
            // 注意：宠物已在AttractedByCatTeaser阶段加入互动列表，这里不再重复加入
            
            // 通知工具交互管理器更新为互动时的指令文本（传入宠物对象以支持占位符替换）
            if (ToolInteractionManager.Instance != null)
            {
                ToolInteractionManager.Instance.UpdateToInteractingInstructionText("逗猫棒", agent);
            }
            
            Debug.Log($"宠物 {agent.PetDisplayName} 开始与逗猫棒互动，持续时间: {interactionDuration.value}秒");
        }
        
        private void EndInteraction()
        {
            // 检查是否进入厌倦状态（类似TryToyInteraction的逻辑）
            bool willBeBored = Random.Range(0f, 1f) < agent.BoredomChance;
            if (willBeBored)
            {
                // 进入厌倦状态
                agent.SetBored(true);
                Debug.Log($"宠物 {agent.PetDisplayName} 在与逗猫棒互动后感到厌倦了，需要 {agent.BoredomRecoveryRemaining} 分钟恢复");
            }
            
            if (agent != null)
            {
                // 调用PetController2D的EndCatTeaser方法
                agent.EndCatTeaser();
                
                // 结束互动状态
                agent.IsCatTeasering = false;
                
                Debug.Log($"宠物 {agent.PetDisplayName} 结束与逗猫棒互动");
            }
            
            // 通知逗猫棒结束互动
            if (targetCatTeaser != null)
            {
                targetCatTeaser.OnPetEndInteraction(agent);
            }
            
            // 开始通用的互动结束阶段（显示结束文本、给予奖励、延迟后回到工具背包）
            if (ToolInteractionManager.Instance != null)
            {
                ToolInteractionManager.Instance.StartInteractionEndingPhase("逗猫棒", agent);
            }
            
            EndAction(true);
        }
        
        protected override void OnStop()
        {
            // 如果任务被中途停止，清理状态
            if (agent != null && hasStarted)
            {
                agent.IsCatTeasering = false;
            }
            
            // 通知逗猫棒结束互动
            if (targetCatTeaser != null)
            {
                targetCatTeaser.OnPetEndInteraction(agent);
            }
            
            hasStarted = false;
        }
        
        protected override void OnPause()
        {
            // 暂停时不做特殊处理
        }
        
        /// <summary>
        /// 从PlayerManager中获取逗猫棒工具的爱心奖励数值
        /// </summary>
        /// <returns>爱心奖励数量</returns>
        private int GetCatTeaserHeartReward()
        {
            // 在编辑器模式下（非运行时），直接返回默认值，避免警告
            if (!Application.isPlaying)
            {
                return 0; // 编辑器显示用的默认值
            }
            
            if (PlayerManager.Instance == null)
            {
             //   Debug.LogWarning("CatTeaserInteraction: PlayerManager.Instance为空，使用默认爱心奖励");
                return 0; // 运行时默认奖励
            }
            
            ToolInfo[] tools = PlayerManager.Instance.GetTools();
            if (tools == null)
            {
                Debug.LogWarning("CatTeaserInteraction: 未找到工具配置，使用默认爱心奖励");
                return 1; // 默认奖励
            }
            
            // 查找逗猫棒工具
            foreach (ToolInfo tool in tools)
            {
                if (tool != null && tool.toolName == "逗猫棒")
                {
                    return tool.heartReward;
                }
            }
            
            Debug.LogWarning("CatTeaserInteraction: 未找到逗猫棒工具配置，使用默认爱心奖励");
            return 1; // 默认奖励
        }
    }
}