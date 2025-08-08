using UnityEngine;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

using NavMeshAgent = UnityEngine.AI.NavMeshAgent;
using NavMesh = UnityEngine.AI.NavMesh;
using NavMeshHit = UnityEngine.AI.NavMeshHit;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("使玩具老鼠在导航网格上随机漫游")]
    public class WanderMouse : ActionTask<NavMeshAgent>
    {
        [Tooltip("与每个漫游点保持的距离")]
        public BBParameter<float> keepDistance = 0.1f;
        [Tooltip("漫游点不能比这个距离更近")]
        public BBParameter<float> minWanderDistance = 1f;
        [Tooltip("漫游点不能比这个距离更远")]
        public BBParameter<float> maxWanderDistance = 3f;
        [Tooltip("如果启用，将永远保持漫游。如果不启用，则只执行一次漫游")]
        public bool repeat = true;
        
        // 用于追踪是否已找到有效位置
        private bool foundValidPosition = false;
        private Vector3 spawnPosition; // 出生位置，用作漫游中心点
        private bool hasSetSpawnPosition = false;

        protected override string info
        {
            get 
            { 
                return string.Format("老鼠漫游 (范围:{0}-{1})", 
                    minWanderDistance.value, maxWanderDistance.value); 
            }
        }

        protected override void OnExecute() 
        {
            // 第一次执行时记录出生位置
            if (!hasSetSpawnPosition)
            {
                spawnPosition = agent.transform.position;
                hasSetSpawnPosition = true;
            }
            
            // 尝试寻找漫游点并设置路径
            foundValidPosition = AttemptToFindWanderPoint();
        }

        protected override void OnUpdate() 
        {
            // 如果还没找到有效位置，继续尝试
            if (!foundValidPosition) 
            {
                foundValidPosition = AttemptToFindWanderPoint();
                return;
            }
            
            // 检查是否到达目的地
            bool reachedDestination = !agent.pathPending && 
                agent.remainingDistance <= agent.stoppingDistance + keepDistance.value;
            
            if (reachedDestination) 
            {
                // 如果设置了重复模式，找新的漫游点
                if (repeat) 
                {
                    foundValidPosition = AttemptToFindWanderPoint();
                    // 注意：在repeat模式下不结束任务，继续寻找新的点
                } 
                else 
                {
                    // 非重复模式，且已到达目标点，返回成功
                    EndAction(true);
                }
            }
            // 如果尚未到达目标，继续移动
        }
        
        // 尝试找到一个有效的漫游点并设置导航路径
        // 返回是否成功找到
        bool AttemptToFindWanderPoint() 
        {
            // 计算距离范围
            var min = minWanderDistance.value;
            var max = maxWanderDistance.value;
            min = Mathf.Clamp(min, 0.01f, max);
            max = Mathf.Clamp(max, min, max);
            
            // 尝试10次寻找合适的点
            for (int i = 0; i < 10; i++) 
            {
                // 在出生位置周围的范围内随机选择一个点
                Vector2 randomDirection = Random.insideUnitCircle.normalized * Random.Range(min, max);
                Vector3 targetPosition = spawnPosition + new Vector3(randomDirection.x, randomDirection.y, 0);
                
                // 在NavMesh上采样最近的点
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPosition, out hit, max, NavMesh.AllAreas)) 
                {
                    // 确保点不会离出生位置太远
                    float distanceFromSpawn = Vector3.Distance(hit.position, spawnPosition);
                    if (distanceFromSpawn <= max)
                    {
                        // 设置导航目标
                        if (agent.SetDestination(hit.position)) 
                        {
                            return true;
                        }
                    }
                }
            }
            
            // 10次尝试都失败，尝试就近找一个点
            NavMeshHit nearHit;
            if (NavMesh.SamplePosition(agent.transform.position, out nearHit, 1f, NavMesh.AllAreas))
            {
                if (agent.SetDestination(nearHit.position))
                {
                    return true;
                }
            }
            
            // 完全失败
            return false;
        }

        protected override void OnPause() { OnStop(); }
        
        protected override void OnStop() 
        {
            // 安全地重置NavMeshAgent路径
            if (agent != null && agent.gameObject != null && agent.gameObject.activeSelf && 
                agent.enabled && agent.isOnNavMesh) 
            {
                try 
                {
                    agent.ResetPath();
                } 
                catch 
                {
                    // 忽略销毁时的NavMeshAgent错误
                }
            }
            
            // 重置状态
            foundValidPosition = false;
            hasSetSpawnPosition = false;
        }
    }
}