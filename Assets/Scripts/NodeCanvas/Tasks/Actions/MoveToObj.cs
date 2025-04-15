using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using NavMeshAgent = UnityEngine.AI.NavMeshAgent;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("使用宠物自身速度，按NavMesh寻路到指定的游戏对象位置")]
    public class MoveToObj : ActionTask<NavMeshAgent>
    {
        [RequiredField]
        public BBParameter<GameObject> target;
        public BBParameter<float> keepDistance = 0.1f;
        
        // 用于追踪寻路状态
        private Vector3? lastRequest;
        
        protected override string info {
            get { return string.Format("移动到 {0}", target); }
        }

        protected override void OnExecute() {
            if (target.value == null) {
                Debug.LogWarning("目标对象为空，无法移动");
                EndAction(false);
                return;
            }
            
            // 初始化lastRequest，确保第一次会设置目标
            lastRequest = null;
            
            // 检查是否已经在目标附近
            if (Vector3.Distance(agent.transform.position, target.value.transform.position) <= agent.stoppingDistance + keepDistance.value) {
                Debug.Log("已经在目标位置附近，无需移动");
                EndAction(true);
                return;
            }
            
            // 初始设置目标位置
            var targetPos = target.value.transform.position;
            if (!agent.SetDestination(targetPos)) {
                Debug.LogWarning("无法设置导航目标，可能是NavMesh问题");
                EndAction(false);
            }
            
            lastRequest = targetPos;
        }

        protected override void OnUpdate() {
            if (target.value == null) {
                Debug.LogWarning("目标对象已失效");
                EndAction(false);
                return;
            }
            
            // 获取目标位置
            var pos = target.value.transform.position;
            
            // 只有当目标位置变化时才更新路径
            if (lastRequest != pos) {
                if (!agent.SetDestination(pos)) {
                    Debug.LogWarning("更新路径失败");
                    EndAction(false);
                    return;
                }
                lastRequest = pos;
            }
            
            // 检查是否到达目标
            if (!agent.pathPending) {
                if (agent.remainingDistance <= agent.stoppingDistance + keepDistance.value) {
                    // 成功到达目标
                    Debug.Log("成功到达目标位置");
                    EndAction(true);
                    return;
                }
                
                // 检查是否无法到达目标
                if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial || 
                    agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid) {
                    Debug.LogWarning("无法到达目标位置，路径不完整或无效");
                    EndAction(false);
                    return;
                }
            }
            
            // 继续移动，不返回结果
        }

        protected override void OnPause() { OnStop(); }
        protected override void OnStop() {
            if (agent != null && agent.gameObject.activeSelf) {
                agent.ResetPath();
            }
            lastRequest = null;
        }

        public override void OnDrawGizmosSelected() {
            if (target.value != null) {
                Gizmos.DrawWireSphere(target.value.transform.position, keepDistance.value);
            }
        }
    }
} 