using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("触发宠物的睡眠动画")]
    public class Sleep : ActionTask<CharacterController2D>
    {
        protected override string info
        {
            get { return "触发睡眠动画"; }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法触发睡眠动画");
                EndAction();
                return;
            }
            
            // 直接调用CharacterController2D的Sleep方法
            agent.Sleep();
            
            // 任务完成
            EndAction();
        }
    }
} 