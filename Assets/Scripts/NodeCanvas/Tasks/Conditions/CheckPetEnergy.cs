using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    // 定义比较操作的枚举
    public enum EnergyCompareMode
    {
        LessThan,           // 小于
        LessThanOrEqual,    // 小于等于
        GreaterThan,        // 大于
        GreaterThanOrEqual  // 大于等于
    }
    
    [Category("Pet AI")]
    [Description("检查宠物精力值是否满足指定条件")]
    public class CheckPetEnergy : ConditionTask<PetController2D>
    {
        [Tooltip("精力阈值")]
        public BBParameter<int> energyThreshold = 20;
        
        [Tooltip("比较方式")]
        public EnergyCompareMode compareMode = EnergyCompareMode.LessThanOrEqual;
        
        protected override string info
        {
            get 
            { 
                string compareSymbol = GetCompareSymbol();
                return string.Format("宠物精力值 {0} {1}", compareSymbol, energyThreshold); 
            }
        }

        // 获取比较符号的辅助方法
        private string GetCompareSymbol()
        {
            switch (compareMode)
            {
                case EnergyCompareMode.LessThan:
                    return "<";
                case EnergyCompareMode.LessThanOrEqual:
                    return "<=";
                case EnergyCompareMode.GreaterThan:
                    return ">";
                case EnergyCompareMode.GreaterThanOrEqual:
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
                case EnergyCompareMode.LessThan:
                    return agent.Energy < energyThreshold.value;
                    
                case EnergyCompareMode.LessThanOrEqual:
                    return agent.Energy <= energyThreshold.value;
                
                case EnergyCompareMode.GreaterThan:
                    return agent.Energy > energyThreshold.value;
                
                case EnergyCompareMode.GreaterThanOrEqual:
                    return agent.Energy >= energyThreshold.value;
                
                default:
                    return false;
            }
        }
    }
} 