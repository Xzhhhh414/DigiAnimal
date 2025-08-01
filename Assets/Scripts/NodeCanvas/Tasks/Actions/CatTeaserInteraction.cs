using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物与逗猫棒互动的行为")]
    public class CatTeaserInteraction : ActionTask<PetController2D>
    {
        [SerializeField] private BBParameter<float> interactionDuration = 5f;
        [SerializeField] private BBParameter<float> arrivalThreshold = 0.5f; // 到达判定距离
        
        private float startTime;
        private bool hasStarted = false;
        private bool hasArrived = false;
        private CatTeaserController targetCatTeaser;
        private NavMeshAgent navAgent;
        
        protected override string info
        {
            get { return string.Format("逗猫棒互动 (持续时间: {0}秒)", interactionDuration.value); }
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
                Initialize();
            }
        }
        
        protected override void OnUpdate()
        {
            if (agent == null || targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            // 如果还没到达，检查是否到达
            if (!hasArrived)
            {
                CheckArrival();
                return;
            }
            
            // 检查是否到达互动持续时间
            if (Time.time - startTime >= interactionDuration.value)
            {
                EndInteraction();
                return;
            }
        }
        
        private void Initialize()
        {
            hasStarted = true;
            targetCatTeaser = CatTeaserController.CurrentInstance;
            navAgent = agent.GetComponent<NavMeshAgent>();
            
            if (targetCatTeaser == null)
            {
                EndAction(false);
                return;
            }
            
            Debug.Log($"宠物 {agent.PetDisplayName} 开始逗猫棒互动流程");
        }
        
        private void CheckArrival()
        {
            if (targetCatTeaser == null) return;
            
            // 检查距离是否足够近
            float distance = Vector3.Distance(agent.transform.position, targetCatTeaser.Position);
            
            // 也检查NavMeshAgent是否已经停止移动
            bool navStopped = (navAgent == null || navAgent.velocity.magnitude < 0.1f);
            
            if (distance <= arrivalThreshold.value || navStopped)
            {
                StartInteraction();
            }
        }
        
        private void StartInteraction()
        {
            hasArrived = true;
            startTime = Time.time;
            
            // 停止寻路
            if (navAgent != null)
            {
                navAgent.ResetPath();
            }
            
            // 触发开始互动动画
            Animator animator = agent.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(AnimationStrings.startCatTeaserTrigger);
            }
            
            // 通知逗猫棒开始互动
            if (targetCatTeaser != null)
            {
                targetCatTeaser.OnPetStartInteraction(agent);
            }
            
            Debug.Log($"宠物 {agent.PetDisplayName} 开始与逗猫棒互动，持续时间: {interactionDuration.value}秒");
        }
        
        private void EndInteraction()
        {
            if (agent != null)
            {
                // 触发结束互动动画
                Animator animator = agent.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(AnimationStrings.endCatTeaserTrigger);
                }
                
                // 结束互动状态
                agent.IsCatTeasering = false;
                
                Debug.Log($"宠物 {agent.PetDisplayName} 结束与逗猫棒互动");
            }
            
            // 通知逗猫棒结束互动
            if (targetCatTeaser != null)
            {
                targetCatTeaser.OnPetEndInteraction(agent);
            }
            
            EndAction(true);
        }
        
        protected override void OnStop()
        {
            // 如果任务被中途停止，清理状态
            if (agent != null && hasStarted)
            {
                agent.IsCatTeasering = false;
                
                // 停止寻路
                if (navAgent != null)
                {
                    navAgent.ResetPath();
                }
            }
            
            // 通知逗猫棒结束互动
            if (targetCatTeaser != null)
            {
                targetCatTeaser.OnPetEndInteraction(agent);
            }
            
            hasStarted = false;
            hasArrived = false;
        }
        
        protected override void OnPause()
        {
            // 暂停时不做特殊处理
        }
    }
}