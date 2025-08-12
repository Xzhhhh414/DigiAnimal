using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物与玩具老鼠互动的行为")]
    public class ToyMouseInteraction : ActionTask<PetController2D>
    {
        [Name("互动持续时间")]
        public BBParameter<float> interactionDuration = 4f;
        
        private float startTime;
        private bool hasStarted = false;
        private ToyMouseController targetToyMouse;
        
        protected override string info
        {
            get 
            { 
                int heartReward = GetToyMouseHeartReward();
                return string.Format("玩具老鼠互动 (持续时间: {0}秒, 爱心: {1})", interactionDuration.value, heartReward); 
            }
        }
        
        protected override void OnExecute()
        {
            if (agent == null)
            {
                EndAction(false);
                return;
            }
            
            // 检查是否有活跃的玩具老鼠
            if (!ToyMouseController.HasActiveToyMouse)
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
            if (agent == null || targetToyMouse == null)
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
            targetToyMouse = ToyMouseController.CurrentInstance;
            
            if (targetToyMouse == null)
            {
                EndAction(false);
                return;
            }
            
            // 逗猫棒吸引阶段已加入互动列表，这里避免重复加入失败
            // 若未加入（比如直接跳到此节点），再尝试一次加入
            if (targetToyMouse != null)
            {
                targetToyMouse.OnPetStartInteraction(agent);
            }
            
            // 设置宠物状态（先清理残留吸引状态）
            agent.IsAttracted = false;
            agent.IsPlayingMouse = true;
            
            // 停止路径，避免残余位移影响互动动画
            var nav = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null && nav.isOnNavMesh)
            {
                try { 
                    nav.isStopped = true; 
                    nav.ResetPath(); 
                } catch { }
            }
            
            // 调用PetController2D的StartPlayMouse方法（确保老鼠外观已被隐藏，避免重复表现）
            // Debug.Log($"[ToyMouseInteraction] 准备调用 agent.StartPlayMouse()，宠物: {agent.name}");
            // Debug.Log($"[ToyMouseInteraction] 调用前状态 - IsPlayingMouse: {agent.IsPlayingMouse}");
            agent.StartPlayMouse();
            // Debug.Log($"[ToyMouseInteraction] 调用后状态 - IsPlayingMouse: {agent.IsPlayingMouse}");
            
            // 通知工具交互管理器更新为互动时的指令文本（传入宠物对象以支持占位符替换）
            if (ToolInteractionManager.Instance != null)
            {
                ToolInteractionManager.Instance.UpdateToInteractingInstructionText("玩具老鼠", agent);
            }
            
            // Debug.Log($"宠物 {agent.PetDisplayName} 开始与玩具老鼠互动，持续时间: {interactionDuration.value}秒");
        }
        
        private void EndInteraction()
        {
            // 检查是否进入厌倦状态（类似TryToyInteraction的逻辑）
            bool willBeBored = Random.Range(0f, 1f) < agent.BoredomChance;
            if (willBeBored)
            {
                // 进入厌倦状态
                agent.SetBored(true);
                // Debug.Log($"宠物 {agent.PetDisplayName} 在与玩具老鼠互动后感到厌倦了，需要 {agent.BoredomRecoveryRemaining} 分钟恢复");
            }
            
            if (agent != null)
            {
                // 调用PetController2D的EndPlayMouse方法
                agent.EndPlayMouse();
                
                // 结束互动状态
                agent.IsPlayingMouse = false;
                
                // Debug.Log($"宠物 {agent.PetDisplayName} 结束与玩具老鼠互动");
            }
            
            // 通知玩具老鼠结束互动
            if (targetToyMouse != null)
            {
                targetToyMouse.OnPetEndInteraction(agent);
            }
            
            // 恢复导航
            var nav = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null && nav.isOnNavMesh)
            {
                try { nav.isStopped = false; } catch { }
            }

            // 开始通用的互动结束阶段（显示结束文本、给予奖励、延迟后回到工具背包）
            if (ToolInteractionManager.Instance != null)
            {
                ToolInteractionManager.Instance.StartInteractionEndingPhase("玩具老鼠", agent);
            }
            
            EndAction(true);
        }
        
        protected override void OnStop()
        {
            // 如果任务被中途停止，清理状态
            if (agent != null && hasStarted)
            {
                agent.IsPlayingMouse = false;
            }
            
            // 通知玩具老鼠结束互动
            if (targetToyMouse != null)
            {
                targetToyMouse.OnPetEndInteraction(agent);
            }
            
            hasStarted = false;
        }
        
        protected override void OnPause()
        {
            // 暂停时不做特殊处理
        }
        
        /// <summary>
        /// 从PlayerManager中获取玩具老鼠工具的爱心奖励数值
        /// </summary>
        /// <returns>爱心奖励数量</returns>
        private int GetToyMouseHeartReward()
        {
            // 在编辑器模式下（非运行时），直接返回默认值，避免警告
            if (!Application.isPlaying)
            {
                return 0; // 编辑器显示用的默认值
            }
            
            if (PlayerManager.Instance == null)
            {
                return 0; // 运行时默认奖励
            }
            
            ToolInfo[] tools = PlayerManager.Instance.GetTools();
            if (tools == null)
            {
                Debug.LogWarning("ToyMouseInteraction: 未找到工具配置，使用默认爱心奖励");
                return 1; // 默认奖励
            }
            
            // 查找玩具老鼠工具
            foreach (ToolInfo tool in tools)
            {
                if (tool != null && tool.toolName == "玩具老鼠")
                {
                    return tool.heartReward;
                }
            }
            
            Debug.LogWarning("ToyMouseInteraction: 未找到玩具老鼠工具配置，使用默认爱心奖励");
            return 1; // 默认奖励
        }
    }
}