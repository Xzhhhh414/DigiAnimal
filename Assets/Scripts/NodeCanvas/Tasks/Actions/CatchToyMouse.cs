using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.AI;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("在抵达玩具老鼠交互点后，将老鼠从场景中隐藏以便播放互动动画")] 
    public class CatchToyMouse : ActionTask<PetController2D>
    {
        [Name("目标对象(来自黑板)")]
        [BlackboardOnly]
        public BBParameter<GameObject> targetObj; // 期望为老鼠的InteractPos或老鼠本体

        [Name("判定抵达半径")]
        public BBParameter<float> arriveRadius = 0.2f;
        
        [Name("路径重试间隔(秒)")]
        public BBParameter<float> repathInterval = 0.5f;
        
        [Name("卡住判定阈值(米)")]
        public BBParameter<float> stuckMoveThreshold = 0.01f;
        
        [Name("移动速度")]
        [Tooltip("NavMeshAgent的移动速度，3=walk, 4=run")]
        public BBParameter<float> moveSpeed = 4f; // 追老鼠默认用跑的

        private ToyMouseController toyMouse;
        private Transform interactPos;
        private NavMeshAgent nav;
        private Vector2 lastAgentPos;
        private float stuckTimer;
        private float repathTimer;

        protected override string info => "抓到玩具老鼠并隐藏其外观";

        protected override void OnExecute()
        {
            if (agent == null || targetObj == null || targetObj.value == null)
            {
                EndAction(false);
                return;
            }

            // 获取ToyMouseController与交互点
            toyMouse = ToyMouseController.CurrentInstance;
            if (toyMouse == null)
            {
                EndAction(false);
                return;
            }
            interactPos = toyMouse.InteractPos != null ? toyMouse.InteractPos : toyMouse.transform;

            // 获取导航
            nav = agent.GetComponent<NavMeshAgent>();
            if (nav == null)
            {
                EndAction(false);
                return;
            }
            if (!nav.isOnNavMesh)
            {
                EndAction(false);
                return;
            }
            
            // 设置NavMeshAgent的移动速度
            nav.speed = moveSpeed.value;
            
            // 初始化路径到当前交互点
            SetDestinationToInteractPos();
            
            // 初始化跟踪状态
            lastAgentPos = new Vector2(nav.transform.position.x, nav.transform.position.y);
            stuckTimer = 0f;
            repathTimer = 0f;
        }

        protected override void OnUpdate()
        {
            if (agent == null || toyMouse == null || interactPos == null)
            {
                EndAction(false);
                return;
            }
            
            // 追踪交互点移动，按间隔重设路径
            repathTimer += Time.deltaTime;
            if (repathTimer >= repathInterval.value)
            {
                SetDestinationToInteractPos();
                repathTimer = 0f;
            }
            
            // 卡住检测与路径重算
            Vector2 curPos = new Vector2(nav.transform.position.x, nav.transform.position.y);
            float moved = Vector2.Distance(curPos, lastAgentPos);
            lastAgentPos = curPos;
            if (moved < stuckMoveThreshold.value && !nav.pathPending)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= repathInterval.value)
                {
                    SetDestinationToInteractPos();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            
            // 抵达判定：使用NavMeshAgent remainingDistance 结合自定义半径
            float stopDist = nav.stoppingDistance;
            float dist = Vector2.Distance(agent.transform.position, interactPos.position);
            if (!nav.pathPending && nav.remainingDistance <= stopDist + arriveRadius.value)
            {
                // 最终距离再核验一遍
                if (dist <= stopDist + arriveRadius.value + 0.2f)
                {
                    // 设置宠物进入玩具老鼠互动状态
                    agent.IsPlayingMouse = true;
                    
                    // 隐藏老鼠供宠物播放融合了老鼠的互动动画
                    toyMouse.HideForInteraction();
                    EndAction(true);
                    return;
                }
                else
                {
                    // 路径误判，重算
                    SetDestinationToInteractPos();
                }
            }
        }

        private void SetDestinationToInteractPos()
        {
            if (nav != null && interactPos != null && nav.isOnNavMesh)
            {
                Vector3 dest = new Vector3(interactPos.position.x, interactPos.position.y, nav.transform.position.z);
                try { nav.SetDestination(dest); } catch { }
            }
        }
    }
}