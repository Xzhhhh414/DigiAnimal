using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检测周围是否有逗猫棒，并有几率被吸引")]
    public class DetectCatTeaser : ConditionTask<PetController2D>
    {
        [Name("检测半径")]
        public BBParameter<float> detectionRadius = 5f;
        
        [Name("吸引几率")]
        public BBParameter<float> attractionChance = 0.3f;
        
        [Name("检测间隔")]
        public BBParameter<float> checkInterval = 2f;
        
        private float lastCheckTime = 0f;
        
        protected override string info
        {
            get { return string.Format("检测逗猫棒 (半径:{0}, 几率:{1}, 间隔:{2}s)", detectionRadius.value, attractionChance.value, checkInterval.value); }
        }
        
        protected override bool OnCheck()
        {
            if (agent == null)
            {
                return false;
            }
            
            // 检查时间间隔
            if (Time.time - lastCheckTime < checkInterval.value)
            {
                return false;
            }
            
            // 如果宠物正在进行其他活动或厌倦，不检测逗猫棒
            if (agent.IsSleeping || agent.IsEating || agent.IsPatting || agent.IsAttracted || agent.IsCatTeasering)
            {
                return false;
            }
            
            // 如果宠物处于厌倦状态，不会被逗猫棒吸引
            if (agent.IsBored)
            {
                return false;
            }
            
            // 更新最后检查时间
            lastCheckTime = Time.time;
            
            // 检查是否有活跃的逗猫棒
            if (!CatTeaserController.HasActiveCatTeaser)
            {
                return false;
            }
            
            CatTeaserController catTeaser = CatTeaserController.CurrentInstance;
            if (catTeaser == null)
            {
                return false;
            }
            
            // 检查距离
            if (!catTeaser.IsInRange(agent.transform.position, detectionRadius.value))
            {
                return false;
            }
            
            // 几率判断
            if (Random.Range(0f, 1f) > attractionChance.value)
            {
                return false;
            }
            
            Debug.Log($"宠物 {agent.PetDisplayName} 检测到逗猫棒并被吸引！");
            return true;
        }
    }
}