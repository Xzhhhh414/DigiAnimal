using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("查找标签为Bed的游戏对象")]
    public class FindBed : ActionTask
    {
        // 选择是查找已使用的还是未使用的对象
        [Tooltip("True=查找已使用的床(IsUsing=true), False=查找未使用的床(IsUsing=false)")]
        public BBParameter<bool> findIsUsing = false;
        
        [BlackboardOnly]
        public BBParameter<GameObject> saveGameObjectTo;
        
        protected override string info
        {
            get 
            { 
                string stateText = findIsUsing.value ? "已使用" : "未使用";
                return string.Format("寻找{0}的床", stateText); 
            }
        }

        protected override void OnExecute()
        {
            // 找到所有标签为Bed的游戏对象
            GameObject[] allBeds = GameObject.FindGameObjectsWithTag("Bed");
            
            if (allBeds.Length == 0)
            {
                Debug.LogWarning("场景中没有找到标签为Bed的游戏对象!");
                EndAction(false); // 找不到任何对象，返回失败
                return;
            }
            
            // 筛选出符合使用状态的床
            List<GameObject> matchingBeds = new List<GameObject>();
            
            foreach (GameObject obj in allBeds)
            {
                BedController bedController = obj.GetComponent<BedController>();
                if (bedController != null)
                {
                    // 检查使用状态是否匹配
                    if (bedController.IsUsing == findIsUsing.value)
                    {
                        matchingBeds.Add(obj);
                    }
                }
            }
            
            if (matchingBeds.Count == 0)
            {
                string stateText = findIsUsing.value ? "已使用" : "未使用";
                Debug.Log("没有" + stateText + "的床");
                EndAction(false); // 找不到符合条件的对象，返回失败
                return;
            }
            
            // 随机选择一个符合条件的床
            int randomIndex = Random.Range(0, matchingBeds.Count);
            GameObject selectedBed = matchingBeds[randomIndex];
            
            // 保存到黑板变量
            saveGameObjectTo.value = selectedBed;
            
            // 成功找到对象，返回成功
            EndAction(true);
        }
    }
} 