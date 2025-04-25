using NodeCanvas.Framework;
using ParadoxNotion.Design;
using ParadoxNotion;
using UnityEngine;
using System.Collections.Generic;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("查找标签为Food的游戏对象")]
    public class FindFood : ActionTask
    {
        // 选择是查找已空盘还是可食用的食物
        [Tooltip("True=查找已空盘的食物(IsEmpty=true), False=查找可食用的食物(IsEmpty=false)")]
        public BBParameter<bool> findEmpty = false;
        
        // 是否查找正在使用中的食物
        [Tooltip("True=查找正在使用中的食物, False=查找未使用的食物")]
        public BBParameter<bool> findIsUsing = false;
        
        [BlackboardOnly]
        public BBParameter<GameObject> saveGameObjectTo;
        
        protected override string info
        {
            get 
            { 
                string stateText = findEmpty.value ? "已空盘" : "可食用";
                string usingText = findIsUsing.value ? "正在使用" : "未使用";
                return string.Format("寻找{0}且{1}的食物", stateText, usingText); 
            }
        }

        protected override void OnExecute()
        {
            // 找到所有标签为Food的游戏对象
            GameObject[] allFoods = GameObject.FindGameObjectsWithTag("Food");
            
            if (allFoods.Length == 0)
            {
                Debug.LogWarning("场景中没有找到标签为Food的游戏对象!");
                EndAction(false); // 找不到任何对象，返回失败
                return;
            }
            
            // 筛选出符合条件的食物
            List<GameObject> matchingFoods = new List<GameObject>();
            
            foreach (GameObject obj in allFoods)
            {
                FoodController foodController = obj.GetComponent<FoodController>();
                if (foodController != null)
                {
                    // 检查食物的空盘状态和使用状态是否同时匹配
                    if (foodController.IsEmpty == findEmpty.value && 
                        foodController.IsUsing == findIsUsing.value)
                    {
                        matchingFoods.Add(obj);
                    }
                }
            }
            
            if (matchingFoods.Count == 0)
            {
                string stateText = findEmpty.value ? "已空盘" : "可食用";
                string usingText = findIsUsing.value ? "正在使用" : "未使用";
                Debug.Log($"没有{stateText}且{usingText}的食物");
                EndAction(false); // 找不到符合条件的对象，返回失败
                return;
            }
            
            // 随机选择一个符合条件的食物
            int randomIndex = Random.Range(0, matchingFoods.Count);
            GameObject selectedFood = matchingFoods[randomIndex];
            
            // 保存到黑板变量
            saveGameObjectTo.value = selectedFood;
            
            // 成功找到对象，返回成功
            EndAction(true);
        }
    }
} 