using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("使用宠物自身速度，在2D游戏中移动到指定的游戏对象位置")]
    public class MoveToObj : ActionTask<Transform>
    {
        [RequiredField]
        public BBParameter<GameObject> target;
        public BBParameter<float> keepDistance = 0.1f;
        
        // 用于缓存NavMeshAgent和CharacterController2D组件
        private NavMeshAgent navAgent;
        private CharacterController2D characterController;
        
        // 用于追踪目标位置
        private Vector2 lastTargetPosition;
        
        protected override string info {
            get { return string.Format("移动到 {0}", target); }
        }
        
        protected override void OnExecute() {
            // 获取所需组件
            navAgent = agent.GetComponent<NavMeshAgent>();
            characterController = agent.GetComponent<CharacterController2D>();
            
            if (navAgent == null) {
                Debug.LogError("MoveToObj需要NavMeshAgent组件");
                EndAction(false);
                return;
            }
            
            if (characterController == null) {
                Debug.LogWarning("未找到CharacterController2D组件，动画可能无法正确更新");
            }
            
            if (target.value == null) {
                Debug.LogWarning("目标对象为空，无法移动");
                return; // 不立即返回失败，等待目标被设置
            }
            
            // 初始化目标位置为2D坐标
            Vector2 targetPosition = new Vector2(target.value.transform.position.x, target.value.transform.position.y);
            lastTargetPosition = targetPosition;
            
            // 设置导航目标
            Vector3 destination = new Vector3(targetPosition.x, targetPosition.y, agent.position.z);
            navAgent.SetDestination(destination);
            
            // 检查是否已经在目标附近
            if (Vector2.Distance(new Vector2(agent.position.x, agent.position.y), targetPosition) <= navAgent.stoppingDistance + keepDistance.value) {
                Debug.Log("已经在目标位置附近，任务完成");
                EndAction(true);
                return;
            }
        }

        protected override void OnUpdate() {
            if (target.value == null) {
                Debug.LogWarning("目标对象已失效");
                return; // 不立即返回失败，等待目标被设置
            }
            
            // 获取目标位置（2D坐标）
            Vector2 targetPosition = new Vector2(target.value.transform.position.x, target.value.transform.position.y);
            
            // 如果目标位置发生了显著变化，更新导航目标
            if (Vector2.Distance(lastTargetPosition, targetPosition) > 0.1f) {
                Vector3 destination = new Vector3(targetPosition.x, targetPosition.y, agent.position.z);
                navAgent.SetDestination(destination);
                lastTargetPosition = targetPosition;
            }
            
            // 检查是否到达目标 - 只有这种情况才返回成功
            if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance + keepDistance.value) {
                Debug.Log("成功到达目标位置");
                EndAction(true);
                return;
            }
            
            // 其他情况下继续移动，不返回任何状态
        }

        protected override void OnPause() { OnStop(); }
        protected override void OnStop() {
            if (navAgent != null && navAgent.gameObject.activeSelf) {
                navAgent.ResetPath();
            }
        }

        public override void OnDrawGizmosSelected() {
            if (target.value != null) {
                // 在2D平面上绘制目标范围
                Vector3 targetPos = target.value.transform.position;
                Gizmos.DrawWireSphere(targetPos, keepDistance.value);
            }
        }
    }
} 