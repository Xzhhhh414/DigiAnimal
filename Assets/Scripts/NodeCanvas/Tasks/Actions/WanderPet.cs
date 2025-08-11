using UnityEngine;
using NodeCanvas.Framework;
using ParadoxNotion.Design;


using NavMeshAgent = UnityEngine.AI.NavMeshAgent;
using NavMesh = UnityEngine.AI.NavMesh;
using NavMeshHit = UnityEngine.AI.NavMeshHit;

namespace NodeCanvas.Tasks.Actions
{

    [Category("Pet AI")]
    [Description("使宠物在导航网格上随机散步")]
    public class WanderPet : ActionTask<NavMeshAgent>
    {
        [Tooltip("与每个漫步点保持的距离")]
        public BBParameter<float> keepDistance = 0.1f;
        [Tooltip("漫步点不能比这个距离更近")]
        public BBParameter<float> minWanderDistance = 5;
        [Tooltip("漫步点不能比这个距离更远")]
        public BBParameter<float> maxWanderDistance = 20;
        [Tooltip("如果启用，将永远保持漫步。如果不启用，则只执行一次漫步")]
        public bool repeat = true;
        
        [Name("移动速度")]
        [Tooltip("NavMeshAgent的移动速度，3=walk, 4=run")]
        public BBParameter<float> moveSpeed = 3f; // 散步默认用走的
        
        // 用于追踪是否已找到有效位置
        private bool foundValidPosition = false;

        protected override void OnExecute() {
            // 设置NavMeshAgent的移动速度
            agent.speed = moveSpeed.value;
            
            // 尝试寻找漫步点并设置路径
            foundValidPosition = AttemptToFindWanderPoint();
        }

        protected override void OnUpdate() {
            // 如果还没找到有效位置，继续尝试
            if (!foundValidPosition) {
                foundValidPosition = AttemptToFindWanderPoint();
                return;
            }
            
            // 检查是否到达目的地
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + keepDistance.value) {
                // 如果设置了重复模式，找新的漫步点
                if (repeat) {
                    foundValidPosition = AttemptToFindWanderPoint();
                    // 注意：在repeat模式下不结束任务，继续寻找新的点
                } else {
                    // 非重复模式，且已到达目标点，返回成功
                    EndAction(true);
                }
            }
            // 如果尚未到达目标，继续移动
        }
        
        // 尝试找到一个有效的漫步点并设置导航路径
        // 返回是否成功找到
        bool AttemptToFindWanderPoint() {
            // 计算距离范围
            var min = minWanderDistance.value;
            var max = maxWanderDistance.value;
            min = Mathf.Clamp(min, 0.01f, max);
            max = Mathf.Clamp(max, min, max);
            
            // 尝试10次寻找合适的点
            for (int i = 0; i < 10; i++) {
                var wanderPos = agent.transform.position;
                // 确保点不会太近
                int safetyCounter = 0;
                while ((wanderPos - agent.transform.position).magnitude < min && safetyCounter < 30) {
                    wanderPos = (Random.insideUnitSphere * max) + agent.transform.position;
                    safetyCounter++;
                }
                
                // 在NavMesh上采样最近的点
                NavMeshHit hit;
                if (NavMesh.SamplePosition(wanderPos, out hit, agent.height * 2, NavMesh.AllAreas)) {
                    // 设置导航目标
                    if (agent.SetDestination(hit.position)) {
                        //Debug.Log($"宠物找到了新的散步点 距离: {Vector3.Distance(agent.transform.position, hit.position)}");
                        return true;
                    }
                }
            }
            
            // 10次尝试都失败，记录错误
            //Debug.LogWarning("无法找到有效的NavMesh散步点");
            return false;
        }

        protected override void OnPause() { OnStop(); }
        protected override void OnStop() {
            // 安全地重置NavMeshAgent路径
            if (agent != null && agent.gameObject != null && agent.gameObject.activeSelf && 
                agent.enabled && agent.isOnNavMesh) {
                try {
                    agent.ResetPath();
                } catch {
                    // 忽略销毁时的NavMeshAgent错误
                }
            }
        }
    }
}