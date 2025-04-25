using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    // 定义比较操作的枚举
    public enum SatietyCompareMode
    {
        LessThan,           // 小于
        LessThanOrEqual,    // 小于等于
        GreaterThan,        // 大于
        GreaterThanOrEqual  // 大于等于
    }
    
    [Category("Pet AI")]
    [Description("检查宠物饱腹度是否满足指定条件")]
    public class CheckPetSatiety : ConditionTask<CharacterController2D>
    {
        [Tooltip("饱腹度阈值")]
        public BBParameter<int> satietyThreshold = 20;
        
        [Tooltip("比较方式")]
        public SatietyCompareMode compareMode = SatietyCompareMode.LessThanOrEqual;
        
        protected override string info
        {
            get 
            { 
                string compareSymbol = GetCompareSymbol();
                return string.Format("宠物饱腹度 {0} {1}", compareSymbol, satietyThreshold); 
            }
        }

        // 获取比较符号的辅助方法
        private string GetCompareSymbol()
        {
            switch (compareMode)
            {
                case SatietyCompareMode.LessThan:
                    return "<";
                case SatietyCompareMode.LessThanOrEqual:
                    return "<=";
                case SatietyCompareMode.GreaterThan:
                    return ">";
                case SatietyCompareMode.GreaterThanOrEqual:
                    return ">=";
                default:
                    return "?";
            }
        }

        protected override bool OnCheck()
        {
            if (agent == null)
                return false;
            
            // 根据选择的比较方式进行判断
            switch (compareMode)
            {
                case SatietyCompareMode.LessThan:
                    return agent.Satiety < satietyThreshold.value;
                    
                case SatietyCompareMode.LessThanOrEqual:
                    return agent.Satiety <= satietyThreshold.value;
                
                case SatietyCompareMode.GreaterThan:
                    return agent.Satiety > satietyThreshold.value;
                
                case SatietyCompareMode.GreaterThanOrEqual:
                    return agent.Satiety >= satietyThreshold.value;
                
                default:
                    return false;
            }
        }
    }
} 