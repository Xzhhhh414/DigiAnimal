using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("处理宠物被摸摸的完整互动流程")]
    public class Patting : ActionTask<PetController2D>
    {
        [Tooltip("摸摸互动持续时间（秒）")]
        public BBParameter<float> pattingDuration = 3.0f;
        
        protected override string info
        {
            get { return string.Format("摸摸互动 ({0}秒)", pattingDuration); }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法执行摸摸互动");
                EndAction(false);
                return;
            }
            
            // 开始摸摸互动
            StartPatting();
        }
        
        protected override void OnUpdate()
        {
            // 检查是否已经达到持续时间
            if (elapsedTime >= pattingDuration.value)
            {
                // 结束摸摸互动
                EndPatting();
                EndAction(true);
            }
        }
        
        protected override void OnStop()
        {
            // 如果任务被中断，确保清理状态
            if (agent != null && agent.IsPatting)
            {
                EndPatting();
            }
        }
        
        /// <summary>
        /// 开始摸摸互动
        /// </summary>
        private void StartPatting()
        {
            // 调用PetController2D的StartPatting方法（只负责动画）
            agent.StartPatting();
            
            // Debug.Log($"{agent.gameObject.name} BT开始摸摸互动，持续时间: {pattingDuration.value}秒");
        }
        
        /// <summary>
        /// 结束摸摸互动
        /// </summary>
        private void EndPatting()
        {
            // 重置宠物状态
            agent.IsPatting = false;
            
            // 调用PetController2D的EndPatting方法（负责动画）
            agent.EndPatting();
            
            // Debug.Log($"{agent.gameObject.name} BT结束摸摸互动");
        }
    }
}