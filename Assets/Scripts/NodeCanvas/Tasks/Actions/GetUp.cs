using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("触发宠物的起床动画")]
    public class GetUp : ActionTask<CharacterController2D>
    {
        protected override string info
        {
            get { return "触发起床动画"; }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法触发起床动画");
                EndAction(false);
                return;
            }
            
            // 直接调用CharacterController2D的GetUp方法
            agent.GetUp();
            
            // 任务完成
            EndAction(true);
        }
    }
} 