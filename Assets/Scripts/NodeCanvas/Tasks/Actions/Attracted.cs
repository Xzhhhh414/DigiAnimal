using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物被逗猫棒吸引的行为")]
    public class Attracted : ActionTask<PetController2D>
    {
        [SerializeField] private BBParameter<float> attractedDuration = 3f;
        
        private float startTime;
        private bool hasStarted = false;
        private CatTeaserController targetCatTeaser;
        
        protected override string info
        {
            get { return string.Format("被吸引 (持续时间: {0}秒)", attractedDuration.value); }
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
            
            // 设置被吸引状态
            agent.IsAttracted = true;
            
            // 显示兴奋气泡
            agent.ShowEmotionBubble(PetNeedType.Happy);
            
            // 通知逗猫棒有宠物被吸引
            targetCatTeaser.OnPetAttracted(agent);
            
            Debug.Log($"宠物 {agent.PetDisplayName} 开始被逗猫棒吸引，持续时间: {attractedDuration.value}秒");
        }
        
        private void EndAttraction()
        {
            if (agent != null)
            {
                // 隐藏兴奋气泡
                agent.HideEmotionBubble(PetNeedType.Happy);
                
                // 结束被吸引状态
                agent.IsAttracted = false;
                
                // 开始与逗猫棒互动状态
                agent.IsCatTeasering = true;
                
                // 开始寻路到逗猫棒位置
                if (targetCatTeaser != null)
                {
                    NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
                    if (navAgent != null)
                    {
                        navAgent.SetDestination(targetCatTeaser.Position);
                        Debug.Log($"宠物 {agent.PetDisplayName} 开始寻路到逗猫棒位置");
                    }
                }
                
                Debug.Log($"宠物 {agent.PetDisplayName} 结束被吸引状态，开始寻路");
            }
            
            EndAction(true);
        }
        
        protected override void OnStop()
        {
            // 如果任务被中途停止，清理状态
            if (agent != null && hasStarted)
            {
                agent.HideEmotionBubble(PetNeedType.Happy);
                agent.IsAttracted = false;
            }
            
            hasStarted = false;
        }
        
        protected override void OnPause()
        {
            // 暂停时不做特殊处理
        }
    }
}