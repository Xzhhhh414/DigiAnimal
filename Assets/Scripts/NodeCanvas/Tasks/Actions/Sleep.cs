using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("触发宠物的睡眠动画")]
    public class Sleep : ActionTask<PetController2D>
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
                EndAction(false);
                return;
            }
            
            // 直接调用PetController2D的Sleep方法
            agent.Sleep();
            
            // 任务完成
            EndAction(true);
        }
    }
} 