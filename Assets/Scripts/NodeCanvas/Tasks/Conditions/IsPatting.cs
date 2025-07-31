using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查宠物是否正在被摸摸")]
    public class IsPatting : ConditionTask<PetController2D>
    {
        protected override string info
        {
            get { return "宠物正在被摸摸?"; }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
            {
                return false; 
            }

            // 检查宠物是否正在被摸摸
            return agent.IsPatting;
        }
    }
}