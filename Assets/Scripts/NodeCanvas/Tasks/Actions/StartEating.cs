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
        
        [Tooltip("吃食物持续时间(秒)")]
        public BBParameter<float> eatingDuration = 3f;
        
        // 内部变量
        private float eatingElapsedTime = 0f;  // 改名以避免与基类变量冲突
        private float lastSatietyUpdateTime = 0f;
        private int totalSatietyToAdd = 0;
        private int satietyPerSecond = 0;
        private FoodController foodController;
        
        protected override string info
        {
            get { return $"吃食物 ({eatingDuration}秒)"; }
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
            foodController = targetFood.value.GetComponent<FoodController>();
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
            
            // 初始化计时器和状态
            eatingElapsedTime = 0f;
            lastSatietyUpdateTime = 0f;
            
            // 计算食物总恢复的饱腹度值（包括美味度加成）
            totalSatietyToAdd = foodController.SatietyRecoveryValue + (foodController.Tasty - 1) * 5;
            
            // 计算每秒增加的饱腹度
            satietyPerSecond = Mathf.CeilToInt(totalSatietyToAdd / eatingDuration.value);
            
            // 调用宠物的Eating方法开始吃食物动画
            agent.Eating(targetFood.value);
            
            Debug.Log($"{agent.PetDisplayName} 开始吃食物 {targetFood.value.name}，持续{eatingDuration.value}秒，总计恢复{totalSatietyToAdd}点饱腹度");
        }
        
        protected override void OnUpdate()
        {
            // 累计经过时间
            eatingElapsedTime += Time.deltaTime;
            
            // 每隔1秒增加一次饱腹度
            if (eatingElapsedTime - lastSatietyUpdateTime >= 1f)
            {
                lastSatietyUpdateTime += 1f;
                
                // 增加饱腹度
                agent.Satiety += satietyPerSecond;
                
                Debug.Log($"{agent.PetDisplayName} 吃食物中，增加{satietyPerSecond}点饱腹度，当前饱腹度: {agent.Satiety}");
            }
            
            // 检查是否达到持续时间
            if (eatingElapsedTime >= eatingDuration.value)
            {
                // 触发结束吃食物的逻辑
                agent.FinishEating();
                
                Debug.Log($"{agent.PetDisplayName} 已吃完食物 {targetFood.value.name}");
                
                // 注意：不在此处设置食物为空盘状态，该逻辑已移至FinishEating.cs中处理
                
                // 任务完成
                EndAction(true);
            }
        }
    }
} 