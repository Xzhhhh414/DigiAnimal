using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("改变指定游戏对象的使用状态")]
    public class ChangeObjUsingState : ActionTask
    {
        [RequiredField]
        public BBParameter<GameObject> targetObject;
        
        public BBParameter<bool> setIsUsing = true;
        
        [Tooltip("无论操作本身成功与否，该节点将以此状态结束")]
        public CompactStatus finishStatus = CompactStatus.Success;
        
        protected override string info
        {
            get { return string.Format("设置 {0} 的使用状态为 {1}", targetObject, setIsUsing); }
        }

        protected override void OnExecute()
        {
            if (targetObject.value == null)
            {
                Debug.LogWarning("目标对象为空，无法设置使用状态");
                EndAction(finishStatus == CompactStatus.Success);
                return;
            }
            
            // 尝试获取BedController组件
            BedController bedController = targetObject.value.GetComponent<BedController>();
            if (bedController != null)
            {
                // 设置床的使用状态
                bedController.IsUsing = setIsUsing.value;
                //Debug.Log($"设置床 {targetObject.value.name} 的使用状态为 {setIsUsing.value}");
                EndAction(finishStatus == CompactStatus.Success);
                return;
            }
            
            // 尝试获取FoodController组件
            FoodController foodController = targetObject.value.GetComponent<FoodController>();
            if (foodController != null)
            {
                // 设置食物的使用状态
                foodController.IsUsing = setIsUsing.value;
                //Debug.Log($"设置食物 {targetObject.value.name} 的使用状态为 {setIsUsing.value}");
                EndAction(finishStatus == CompactStatus.Success);
                return;
            }
            
            // 如果既不是床也不是食物，则报错
            Debug.LogWarning($"目标对象 {targetObject.value.name} 既没有BedController也没有FoodController组件，无法设置使用状态");
            EndAction(finishStatus == CompactStatus.Success);
        }
    }
} 