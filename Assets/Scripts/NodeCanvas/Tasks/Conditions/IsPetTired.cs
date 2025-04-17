using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查宠物是否精力不足需要睡觉")]
    public class IsPetTired : ConditionTask<CharacterController2D>
    {
        [Tooltip("精力阈值，当宠物精力值小于等于此值时返回true")]
        public BBParameter<int> energyThreshold = 20;
        
        protected override string info
        {
            get { return string.Format("宠物精力值 <= {0}", energyThreshold); }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
                return false;
                
            // 检查宠物的精力值是否低于阈值
            return agent.Energy <= energyThreshold.value;
        }
    }
} 