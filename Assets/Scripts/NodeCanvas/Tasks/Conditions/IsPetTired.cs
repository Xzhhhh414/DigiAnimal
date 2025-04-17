using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查宠物是否足够疲劳需要睡觉")]
    public class IsPetTired : ConditionTask<CharacterController2D>
    {
        [Tooltip("疲劳阈值，当宠物疲劳值大于等于此值时返回true")]
        public BBParameter<int> fatigueThreshold = 100;
        
        protected override string info
        {
            get { return string.Format("宠物疲劳值 >= {0}", fatigueThreshold); }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
                return false;
                
            // 检查宠物的疲劳值是否达到阈值
            return agent.Fatigue >= fatigueThreshold.value;
        }
    }
} 