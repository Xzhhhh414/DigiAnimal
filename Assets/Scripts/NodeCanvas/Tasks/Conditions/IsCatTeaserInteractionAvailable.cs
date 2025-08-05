using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Conditions
{
    [Category("Pet AI")]
    [Description("检查逗猫棒的互动列表是否为空（是否可以开始新的互动）")]
    public class IsCatTeaserInteractionAvailable : ConditionTask<PetController2D>
    {
        protected override string info
        {
            get { return "逗猫棒互动是否可用"; }
        }
        
        protected override bool OnCheck()
        {
            if (agent == null)
            {
                return false;
            }
            
            // 检查是否有活跃的逗猫棒
            if (!CatTeaserController.HasActiveCatTeaser)
            {
                return false;
            }
            
            CatTeaserController catTeaser = CatTeaserController.CurrentInstance;
            if (catTeaser == null)
            {
                return false;
            }
            
            // 检查互动列表是否为空
            bool isAvailable = catTeaser.IsInteractionListEmpty;
            
            // Debug.Log($"[{agent.PetDisplayName}] 逗猫棒互动可用性检查: {isAvailable} (互动中宠物数量: {catTeaser.InteractingPetCount})");
            
            return isAvailable;
        }
    }
}