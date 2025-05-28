using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("改变宠物的动画参数或触发动画触发器")]
    public class ChangePetPara : ActionTask<PetController2D>
    {
        // 支持的参数类型的枚举
        public enum PetParameterType
        {
            IsSleeping,
            SleepTrigger,
            GetUpTrigger
        }
        
        // 要修改的参数类型
        [Tooltip("选择要修改的宠物参数或触发器")]
        public PetParameterType parameterType = PetParameterType.IsSleeping;
        
        // 要设置的布尔值 (仅用于非触发器类型的参数)
        [Tooltip("设置参数的新值 (仅用于非触发器类型的参数)")]
        [ShowIf("parameterType", (int)PetParameterType.IsSleeping)]
        public BBParameter<bool> newValue;
        
        protected override string info
        {
            get
            {
                string paramName = GetParameterName(parameterType);
                if (parameterType == PetParameterType.IsSleeping)
                {
                    return string.Format("设置宠物参数 {0} = {1}", paramName, newValue);
                }
                else
                {
                    return string.Format("触发宠物动画 {0}", paramName);
                }
            }
        }
        
        // 获取参数的显示名称
        private string GetParameterName(PetParameterType type)
        {
            switch (type)
            {
                case PetParameterType.IsSleeping:
                    return "睡眠状态";
                case PetParameterType.SleepTrigger:
                    return "睡眠触发器";
                case PetParameterType.GetUpTrigger:
                    return "起床触发器";
                default:
                    return type.ToString();
            }
        }
        
        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法设置参数");
                EndAction(false);
                return;
            }
            
            // 获取宠物的Animator组件
            Animator animator = agent.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("宠物没有Animator组件，无法设置动画参数");
                EndAction(false);
                return;
            }
            
            // 根据选择的参数类型设置对应的值
            switch (parameterType)
            {
                case PetParameterType.IsSleeping:
                    agent.IsSleeping = newValue.value;
                    Debug.Log($"设置宠物睡眠状态: {newValue.value}");
                    break;
                    
                case PetParameterType.SleepTrigger:
                    animator.SetTrigger(AnimationStrings.sleepTrigger);
                    Debug.Log("触发宠物睡眠动画");
                    break;
                    
                case PetParameterType.GetUpTrigger:
                    animator.SetTrigger(AnimationStrings.getUpTrigger);
                    Debug.Log("触发宠物起床动画");
                    break;
                    
                // 未来可以在这里添加更多参数类型的支持
            }
            
            // 任务完成
            EndAction(true);
        }
    }
} 