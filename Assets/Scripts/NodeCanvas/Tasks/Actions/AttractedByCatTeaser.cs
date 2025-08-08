using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物被逗猫棒吸引的专用行为")]
    public class AttractedByCatTeaser : ActionTask<PetController2D>
    {
        [Name("逗猫棒吸引持续时间")]
        public BBParameter<float> attractedDuration = 3f;
        
        [BlackboardOnly]
        public BBParameter<GameObject> saveGameObjectTo;
        
        private float startTime;
        private bool hasStarted = false;
        private bool hasCompletedNormally = false; // 标记是否正常完成
        private bool isInInteractionList = false; // 标记是否已加入互动列表
        private CatTeaserController targetCatTeaser;
        
        protected override string info
        {
            get 
            { 
                return string.Format("被逗猫棒吸引 (持续时间: {0}秒)", attractedDuration.value); 
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
                StartAttraction();
            }
        }
        
        protected override void OnUpdate()
        {
            if (agent == null || targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            // 检查是否到达持续时间
            if (Time.time - startTime >= attractedDuration.value)
            {
                EndAttraction();
                return;
            }
        }
        
        private void StartAttraction()
        {
            hasStarted = true;
            startTime = Time.time;
            targetCatTeaser = CatTeaserController.CurrentInstance;
            
            if (targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            // 尝试加入逗猫棒的互动列表（限制单宠物）
            bool canInteract = targetCatTeaser.OnPetStartInteraction(agent);
            if (!canInteract)
            {
                // 如果无法加入互动列表（已有其他宠物在互动），结束当前行为
                // Debug.Log($"宠物 {agent.PetDisplayName} 无法与逗猫棒互动，已有其他宠物在互动");
                EndAction(false);
                return;
            }
            
            // 标记宠物已成功加入互动列表
            isInInteractionList = true;
            
            // 设置被吸引状态
            agent.IsAttracted = true;
            
            // 显示好奇气泡
            agent.ShowEmotionBubble(PetNeedType.Curious);
            
            // 保存交互位置点到黑板变量（如果有InteractPos则使用，否则使用逗猫棒本身）
            Transform interactPos = targetCatTeaser.InteractPos;
            if (interactPos != null)
            {
                saveGameObjectTo.value = interactPos.gameObject;
                // Debug.Log($"宠物 {agent.PetDisplayName} 将移动到逗猫棒的交互位置点");
            }
            else
            {
                saveGameObjectTo.value = targetCatTeaser.gameObject;
                // Debug.Log($"宠物 {agent.PetDisplayName} 将移动到逗猫棒中心位置（未找到InteractPos）");
            }
            
            // Debug.Log($"宠物 {agent.PetDisplayName} 开始被逗猫棒吸引，持续时间: {attractedDuration.value}秒");
        }
        
        private void EndAttraction()
        {
            if (agent != null)
            {
                // 隐藏好奇气泡
                agent.HideEmotionBubble(PetNeedType.Curious);
                
                // 结束被吸引状态
                agent.IsAttracted = false;
                
                // 开始逗猫棒互动状态，准备进入下一个阶段
                agent.IsCatTeasering = true;
                
                // Debug.Log($"宠物 {agent.PetDisplayName} 结束被吸引状态，准备与逗猫棒互动");
            }
            
            // 标记为正常完成，避免在OnStop中清理互动列表
            hasCompletedNormally = true;
            EndAction(true);
        }
        
        protected override void OnStop()
        {
            // 只有在被中途停止且宠物在互动列表中时才清理状态（正常完成不清理互动列表）
            if (agent != null && hasStarted && !hasCompletedNormally && isInInteractionList)
            {
                agent.HideEmotionBubble(PetNeedType.Curious);
                agent.IsAttracted = false;
                
                // 从逗猫棒的互动列表中移除宠物
                if (targetCatTeaser != null)
                {
                    targetCatTeaser.OnPetEndInteraction(agent);
                    // Debug.Log($"宠物 {agent.PetDisplayName} 被中途停止，从逗猫棒互动列表中移除");
                }
            }
            
            // 重置状态
            hasStarted = false;
            hasCompletedNormally = false;
            isInInteractionList = false;
        }
        
        protected override void OnPause()
        {
            // 暂停时不做特殊处理
        }
    }
}