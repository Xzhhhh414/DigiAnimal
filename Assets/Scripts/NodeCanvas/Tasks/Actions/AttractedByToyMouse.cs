using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Tasks.Actions
{
    [Category("Pet AI")]
    [Description("宠物被玩具老鼠吸引的专用行为")] 
    public class AttractedByToyMouse : ActionTask<PetController2D>
    {
        [Name("被吸引持续时间")] 
        public BBParameter<float> attractedDuration = 2.5f;

        [BlackboardOnly]
        public BBParameter<GameObject> saveGameObjectTo; // 保存老鼠对象（供后续MoveToObj使用）

        private float startTime;
        private bool hasStarted = false;
        private bool hasCompletedNormally = false;
        private bool isInInteractionList = false;
        private ToyMouseController targetToyMouse;

        protected override string info
        {
            get { return string.Format("被玩具老鼠吸引 (持续:{0}s)", attractedDuration.value); }
        }

        protected override void OnExecute()
        {
            if (agent == null)
            {
                EndAction(false);
                return;
            }

            if (!ToyMouseController.HasActiveToyMouse)
            {
                EndAction(false);
                return;
            }

            if (!hasStarted)
            {
                StartAttraction();
            }
        }

        protected override void OnUpdate()
        {
            if (agent == null || targetToyMouse == null)
            {
                EndAction(false);
                return;
            }

            if (Time.time - startTime >= attractedDuration.value)
            {
                EndAttraction();
                return;
            }
        }

        private void StartAttraction()
        {
            hasStarted = true;
            startTime = Time.time;
            targetToyMouse = ToyMouseController.CurrentInstance;

            if (targetToyMouse == null)
            {
                EndAction(false);
                return;
            }

            // 立即尝试加入互动列表（单宠物限制）
            bool canInteract = targetToyMouse.OnPetStartInteraction(agent);
            if (!canInteract)
            {
                EndAction(false);
                return;
            }
            isInInteractionList = true;

            // 设置被吸引状态与气泡
            agent.IsAttracted = true;
            agent.ShowEmotionBubble(PetNeedType.Curious);

            // 保存交互位置到黑板（优先InteractPos，否则使用老鼠本体）
            Transform interactPos = targetToyMouse.InteractPos;
            if (interactPos != null)
            {
                saveGameObjectTo.value = interactPos.gameObject;
            }
            else
            {
                saveGameObjectTo.value = targetToyMouse.gameObject;
            }
        }

        private void EndAttraction()
        {
            if (agent != null)
            {
                agent.HideEmotionBubble(PetNeedType.Curious);
                agent.IsAttracted = false;
                
                // 不在这里设置 IsPlayingMouse，让 CatchToyMouse 在合适的时机设置
            }

            hasCompletedNormally = true;
            EndAction(true);
        }

        protected override void OnStop()
        {
            // 仅在被中断且已入互动队列时做清理
            if (agent != null && hasStarted && !hasCompletedNormally && isInInteractionList)
            {
                agent.HideEmotionBubble(PetNeedType.Curious);
                agent.IsAttracted = false;

                if (targetToyMouse != null)
                {
                    targetToyMouse.OnPetEndInteraction(agent);
                }
            }

            hasStarted = false;
            hasCompletedNormally = false;
            isInInteractionList = false;
        }
    }
}