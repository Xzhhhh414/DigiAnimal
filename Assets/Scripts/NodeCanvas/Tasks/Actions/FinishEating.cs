using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物结束吃食物的动作，并恢复饱腹度")]
    public class FinishEating : ActionTask<CharacterController2D>
    {
        [Tooltip("要吃的食物对象")]
        [RequiredField]
        public BBParameter<GameObject> targetFood;
        
        [Tooltip("吃完后是否将食物标记为空盘")]
        public BBParameter<bool> setFoodEmpty = true;
        
        protected override string info
        {
            get { return "结束吃食物" + (setFoodEmpty.value ? "并清空食盘" : ""); }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法结束吃食物");
                EndAction(false);
                return;
            }
            
            if (targetFood.value == null)
            {
                Debug.LogWarning("食物对象为空，无法结束吃食物");
                EndAction(false);
                return;
            }
            
            // 获取食物控制器
            FoodController foodController = targetFood.value.GetComponent<FoodController>();
            if (foodController == null)
            {
                Debug.LogWarning("目标对象没有FoodController组件，无法完成吃食物");
                EndAction(false);
                return;
            }
            
            // 如果食物已空盘不处理
            if (foodController.IsEmpty)
            {
                Debug.LogWarning("食物已经被吃光了，无法完成吃食物");
                EndAction(false);
                return;
            }
            
            // 调用宠物的FinishEating方法来恢复饱腹度
            agent.FinishEating();
            
            // 设置食物是否为空盘
            if (setFoodEmpty.value)
            {
                foodController.SetEmpty();
            }
            
            // 在这里可以触发结束吃食物的动画（如果有的话）
            // agent.GetComponent<Animator>().SetTrigger("StopEatTrigger");
            
            // 任务完成
            EndAction(true);
        }
    }
} 