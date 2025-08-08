using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查玩具老鼠的互动列表是否为空（是否可以开始新的互动）")]
    public class IsToyMouseInteractionAvailable : ConditionTask<PetController2D>
    {
        protected override string info
        {
            get { return "玩具老鼠互动是否可用"; }
        }
        
        protected override bool OnCheck()
        {
            if (agent == null)
            {
                return false;
            }
            
            // 检查是否有活跃的玩具老鼠
            if (!ToyMouseController.HasActiveToyMouse)
            {
                return false;
            }
            
            ToyMouseController toyMouse = ToyMouseController.CurrentInstance;
            if (toyMouse == null)
            {
                return false;
            }
            
            // 检查互动列表是否为空
            bool isAvailable = toyMouse.IsInteractionListEmpty;
            
            // Debug.Log($"[{agent.PetDisplayName}] 玩具老鼠互动可用性检查: {isAvailable} (互动中宠物数量: {toyMouse.InteractingPetCount})");
            
            return isAvailable;
        }
    }
}