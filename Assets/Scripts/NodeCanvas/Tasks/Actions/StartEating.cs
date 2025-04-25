using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物开始吃食物的动作")]
    public class StartEating : ActionTask<CharacterController2D>
    {
        [Tooltip("要吃的食物对象")]
        [RequiredField]
        public BBParameter<GameObject> targetFood;
        
        protected override string info
        {
            get { return "开始吃食物"; }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                Debug.LogWarning("宠物不存在，无法开始吃食物");
                EndAction(false);
                return;
            }
            
            if (targetFood.value == null)
            {
                Debug.LogWarning("食物对象为空，无法开始吃食物");
                EndAction(false);
                return;
            }
            
            // 获取食物控制器
            FoodController foodController = targetFood.value.GetComponent<FoodController>();
            if (foodController == null)
            {
                Debug.LogWarning("目标对象没有FoodController组件，无法吃食物");
                EndAction(false);
                return;
            }
            
            // 如果食物已被标记为空盘，无法开始吃
            if (foodController.IsEmpty)
            {
                Debug.LogWarning("食物已经被吃光了，无法开始吃");
                EndAction(false);
                return;
            }
            
            // 调用宠物的Eating方法开始吃食物
            agent.Eating(targetFood.value);
            
            Debug.Log($"{agent.PetDisplayName} 开始吃食物 {targetFood.value.name}");
            
            // 任务完成
            EndAction(true);
        }
    }
} 