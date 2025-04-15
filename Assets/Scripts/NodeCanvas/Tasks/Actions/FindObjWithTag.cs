using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("查找指定Tag且符合使用状态的GameObject")]
    public class FindObjWithTag : ActionTask
    {
        [RequiredField]
        public BBParameter<string> targetTag = new BBParameter<string>("Bed");
        
        // 选择是查找已使用的还是未使用的对象
        [Tooltip("True=查找已使用的对象(IsUsing=true), False=查找未使用的对象(IsUsing=false)")]
        public BBParameter<bool> findIsUsing = false;
        
        [BlackboardOnly]
        public BBParameter<GameObject> saveGameObjectTo;
        
        protected override string info
        {
            get 
            { 
                string stateText = findIsUsing.value ? "已使用" : "未使用";
                return string.Format("寻找{0}的 {1}", stateText, targetTag); 
            }
        }

        protected override void OnExecute()
        {
            // 找到所有指定Tag的游戏对象
            GameObject[] allObjectsWithTag = GameObject.FindGameObjectsWithTag(targetTag.value);
            
            if (allObjectsWithTag.Length == 0)
            {
                Debug.LogWarning("场景中没有找到Tag为 " + targetTag.value + " 的游戏对象!");
                EndAction(false); // 找不到任何对象，返回失败
                return;
            }
            
            // 筛选出符合使用状态的对象
            List<GameObject> matchingObjects = new List<GameObject>();
            
            foreach (GameObject obj in allObjectsWithTag)
            {
                BedController bedController = obj.GetComponent<BedController>();
                if (bedController != null)
                {
                    // 直接比较IsUsing与findIsUsing，保持一致性
                    if (bedController.IsUsing == findIsUsing.value)
                    {
                        matchingObjects.Add(obj);
                    }
                }
            }
            
            if (matchingObjects.Count == 0)
            {
                string stateText = findIsUsing.value ? "已使用" : "未使用";
                Debug.Log("没有" + stateText + "的 " + targetTag.value);
                EndAction(false); // 找不到符合条件的对象，返回失败
                return;
            }
            
            // 随机选择一个符合条件的对象
            int randomIndex = Random.Range(0, matchingObjects.Count);
            GameObject selectedObject = matchingObjects[randomIndex];
            
            // 保存到黑板变量
            saveGameObjectTo.value = selectedObject;
            
            // 成功找到对象，返回成功
            EndAction(true);
        }
    }
} 