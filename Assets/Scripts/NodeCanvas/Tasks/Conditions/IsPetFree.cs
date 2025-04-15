using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Poki")]
    [Description("判断宠物是否处于自由活动状态")]
    public class IsPetFree : ConditionTask<CharacterController2D>
    {
        protected override string info
        {
            get { return "是否在自由状态"; }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
                return false;
                
            // 直接返回宠物的自由活动状态
            return agent.InFreeMode;
        }
    }
} 