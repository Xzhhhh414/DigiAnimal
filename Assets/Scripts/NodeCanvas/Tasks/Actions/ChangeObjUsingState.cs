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
            
            BedController bedController = targetObject.value.GetComponent<BedController>();
            if (bedController == null)
            {
                Debug.LogWarning("目标对象没有BedController组件，无法设置使用状态");
                EndAction(finishStatus == CompactStatus.Success);
                return;
            }
            
            // 设置使用状态
            bedController.IsUsing = setIsUsing.value;
            
            // 根据用户设置的finishStatus决定返回成功或失败
            EndAction(finishStatus == CompactStatus.Success);
        }
    }
} 