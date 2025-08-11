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
        
        [Name("移动速度")]
        [Tooltip("NavMeshAgent的移动速度，3=walk, 4=run")]
        public BBParameter<float> moveSpeed = 3f;
        
        // 用于缓存NavMeshAgent和PetController2D组件
        private NavMeshAgent navMeshAgent;
        private PetController2D characterController;
        
        // 用于追踪目标位置
        private Vector2 lastTargetPosition;
        private string petName; // 用于日志标识
        
        // 用于检测是否实际移动
        private Vector2 lastAgentPosition;
        private float stuckTimer = 0f;
        private float pathRetryTimer = 0f;
        private const float PATH_RETRY_INTERVAL = 0.5f;
        
        protected override string info {
            get { return string.Format("移动到 {0}", target); }
        }
        
        protected override void OnExecute() {
            // 获取所需组件
            navMeshAgent = agent.GetComponent<NavMeshAgent>();
            characterController = agent.GetComponent<PetController2D>();
            petName = agent.name; // 记录宠物名称，用于日志区分
            
            if (navMeshAgent == null) {
                // Debug.LogError($"[{petName}] MoveToObj需要NavMeshAgent组件");
                EndAction(false);
                return;
            }
            
            if (characterController == null) {
                // Debug.LogWarning($"[{petName}] 未找到PetController2D组件，动画可能无法正确更新");
            }
            
            // 设置NavMeshAgent的移动速度
            navMeshAgent.speed = moveSpeed.value;
            
            // 不直接操作Animator参数，避免每帧/每次启动任务时重新触发进入Movement
            // 由PetController2D根据NavMeshAgent.velocity在Update中统一驱动isMoving
            
            if (target.value == null) {
                // Debug.LogWarning($"[{petName}] 目标对象为空，无法移动");
                return; // 不立即返回失败，等待目标被设置
            }
            
            // 初始化目标位置为2D坐标
            Vector2 targetPosition = new Vector2(target.value.transform.position.x, target.value.transform.position.y);
            lastTargetPosition = targetPosition;
            
            // 记录初始状态
            Vector2 agentPosition = new Vector2(navMeshAgent.transform.position.x, navMeshAgent.transform.position.y);
            lastAgentPosition = agentPosition; // 记录初始位置用于检测卡住
            float distanceToTarget = Vector2.Distance(agentPosition, targetPosition);
            float stoppingDist = navMeshAgent.stoppingDistance;
            
            // Debug.Log($"[{petName}] 开始移动 - 目标:{target.value.name} 位置:{targetPosition} " +
            //          $"当前位置:{agentPosition} 距离:{distanceToTarget} " +
            //          $"停止距离:{stoppingDist} 保持距离:{keepDistance.value}");
            
            // 设置导航目标
            Vector3 destination = new Vector3(targetPosition.x, targetPosition.y, navMeshAgent.transform.position.z);
            navMeshAgent.SetDestination(destination);
            
            // 检查是否已经在目标附近
            if (distanceToTarget <= stoppingDist + keepDistance.value) {
                // Debug.Log($"[{petName}] 已经在目标位置附近，距离:{distanceToTarget} <= {stoppingDist + keepDistance.value}，任务完成");
                EndAction(true);
                return;
            } else {
                // Debug.Log($"[{petName}] 目标不在范围内，需要移动 距离:{distanceToTarget} > {stoppingDist + keepDistance.value}");
            }
            
            // 重置计时器
            stuckTimer = 0f;
            pathRetryTimer = 0f;
        }

        protected override void OnUpdate() {
            if (target.value == null) {
                // Debug.LogWarning($"[{petName}] 目标对象已失效");
                return; // 不立即返回失败，等待目标被设置
            }
            
            // 获取当前位置和速度
            Vector2 agentPosition = new Vector2(navMeshAgent.transform.position.x, navMeshAgent.transform.position.y);
            Vector2 agentVelocity = new Vector2(navMeshAgent.velocity.x, navMeshAgent.velocity.y);
            
            // 获取目标位置（2D坐标）
            Vector2 targetPosition = new Vector2(target.value.transform.position.x, target.value.transform.position.y);
            float distanceToTarget = Vector2.Distance(agentPosition, targetPosition);
            
            // 检测是否实际移动（防止卡住）
            float movementDelta = Vector2.Distance(agentPosition, lastAgentPosition);
            lastAgentPosition = agentPosition;
            
            // 计时器更新
            pathRetryTimer += Time.deltaTime;
            
            // 如果几乎没有移动且不在目标附近，增加卡住计时器
            if (movementDelta < 0.01f && distanceToTarget > navMeshAgent.stoppingDistance + keepDistance.value) {
                stuckTimer += Time.deltaTime;
                // 如果卡住超过1秒，尝试重新计算路径
                if (stuckTimer > 1.0f && pathRetryTimer > PATH_RETRY_INTERVAL) {
                    // Debug.LogWarning($"[{petName}] 检测到卡住，重新计算路径。距离:{distanceToTarget}，移动量:{movementDelta}");
                    navMeshAgent.SetDestination(new Vector3(targetPosition.x, targetPosition.y, navMeshAgent.transform.position.z));
                    stuckTimer = 0f;
                    pathRetryTimer = 0f;
                }
            } else {
                // 如果移动了，重置卡住计时器
                stuckTimer = 0f;
            }
            
            // 记录每5帧一次的移动状态（避免日志过多）
            // if(Time.frameCount % 5 == 0) {
            //     Debug.Log($"[{petName}] 移动中 - 当前位置:{agentPosition} 目标位置:{targetPosition} " +
            //               $"距离:{distanceToTarget} 速度:{agentVelocity.magnitude} " +
            //               $"路径待处理:{navMeshAgent.pathPending} 剩余距离:{navMeshAgent.remainingDistance} " +
            //               $"路径状态:{navMeshAgent.pathStatus}");
            // }
            
            // 如果目标位置发生了显著变化，更新导航目标
            if (Vector2.Distance(lastTargetPosition, targetPosition) > 0.1f) {
                Vector3 destination = new Vector3(targetPosition.x, targetPosition.y, navMeshAgent.transform.position.z);
                navMeshAgent.SetDestination(destination);
                lastTargetPosition = targetPosition;
                // Debug.Log($"[{petName}] 目标位置变化，更新导航目标到:{targetPosition}");
            }
            
            // 速度检查 - 如果宠物应该移动但速度几乎为0，可能是卡住了
            if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance && agentVelocity.magnitude < 0.01f) {
                // Debug.LogWarning($"[{petName}] 可能卡住了！距离:{navMeshAgent.remainingDistance} > {navMeshAgent.stoppingDistance}，但速度:{agentVelocity.magnitude}");
            }
            
            // 检查是否到达目标 - 添加更严格的检查
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + keepDistance.value) {
                // 额外检查实际距离，防止NavMesh路径计算错误的情况
                if (distanceToTarget > navMeshAgent.stoppingDistance + keepDistance.value + 0.5f) {
                    // 如果NavMesh认为已到达但实际距离还很远，说明路径计算有问题
                    // Debug.LogWarning($"[{petName}] 路径计算异常！remainingDistance:{navMeshAgent.remainingDistance}很小，" +
                    //                $"但实际距离:{distanceToTarget}很大，路径状态:{navMeshAgent.pathStatus}，尝试重新计算路径");
                    
                    // 只有在一定间隔后才重试路径计算，避免频繁计算
                    if (pathRetryTimer > PATH_RETRY_INTERVAL) {
                        navMeshAgent.SetDestination(new Vector3(targetPosition.x, targetPosition.y, navMeshAgent.transform.position.z));
                        pathRetryTimer = 0f;
                    }
                    return; // 继续尝试移动，不结束动作
                }
                
                // 正常到达目标
                // Debug.Log($"[{petName}] 判定到达目标位置 - pathPending:{navMeshAgent.pathPending} " +
                //           $"remainingDistance:{navMeshAgent.remainingDistance} <= {navMeshAgent.stoppingDistance + keepDistance.value} " +
                //           $"实际距离:{distanceToTarget} 路径状态:{navMeshAgent.pathStatus}");
                EndAction(true);
                return;
            }
        }

        protected override void OnPause() { OnStop(); }
        protected override void OnStop() {
            // 安全地重置NavMeshAgent路径
            if (navMeshAgent != null && navMeshAgent.gameObject != null && navMeshAgent.gameObject.activeSelf && 
                navMeshAgent.enabled && navMeshAgent.isOnNavMesh) {
                try {
                    // Debug.Log($"[{petName}] 停止移动，重置路径");
                    navMeshAgent.ResetPath();
                } catch {
                    // 忽略销毁时的NavMeshAgent错误
                }
            }
            
            // 重置移动状态（让PetController2D的Update自然处理isMoving的设置）
            // 这里不直接设置isMoving=false，因为PetController2D会根据NavMeshAgent速度自动处理
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