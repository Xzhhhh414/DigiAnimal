using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("判断宠物是否处于自由活动状态")]
    public class IsPetFreeRoaming : ConditionTask<CharacterController2D>
    {
        protected override string info
        {
            get { return "宠物是否自由活动中"; }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
                return false;
                
            // 直接返回宠物的自由活动状态
            return agent.GetIsFreeRoaming();
        }
    }
} 