using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查宠物是否不处于任何特殊行为中 (如睡觉、吃饭等)")]
    public class IsPetAvailable : ConditionTask<PetController2D>
    {
        protected override string info
        {
            get { return "宠物是否空闲?"; } // 更新info信息以匹配新的类名和含义
        }

        protected override bool OnCheck()
        {
            if (agent == null)
            {
                return false; 
            }

            // 检查宠物是否不在睡觉并且不在吃东西
            if (agent.IsSleeping)
            {
                return false; // 正在睡觉，不空闲
            }

            if (agent.IsEating)
            { 
                return false; // 正在吃东西，不空闲
            }

            // 如果还有其他特殊行为，在此处添加检查
            // 例如: if (agent.IsPlayingWithToy) return false;

            return true; // 宠物空闲，不在任何已定义的特殊行为中
        }
    }
} 