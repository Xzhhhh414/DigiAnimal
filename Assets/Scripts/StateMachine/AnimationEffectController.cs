using UnityEngine;

public class AnimationEffectController : StateMachineBehaviour
{
    // 特效预制体
    [SerializeField] private GameObject effectPrefab;
    
    // 特效的位置偏移 (只使用X和Y值进行平面偏移)
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    
    // 特效实例的引用
    private GameObject effectInstance;
    private Transform attachPoint;
    
    // 进入状态时触发
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (effectPrefab == null)
            return;
            
        // 使用角色的根节点作为挂载点
        attachPoint = animator.transform;
        
        // 实例化特效并设置位置和旋转，不设置父物体避免SendMessage警告
        effectInstance = Instantiate(effectPrefab, 
                                     attachPoint.position + attachPoint.TransformDirection(new Vector3(positionOffset.x, positionOffset.y, 0)), 
                                     attachPoint.rotation);
    }
    
    // 每帧更新时触发
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 手动保持特效位置跟随挂载点，避免设置父物体可能引起的问题
        if (effectInstance != null && attachPoint != null)
        {
            effectInstance.transform.position = attachPoint.position + attachPoint.TransformDirection(new Vector3(positionOffset.x, positionOffset.y, 0));
            effectInstance.transform.rotation = attachPoint.rotation;
        }
    }
    
    // 退出状态时触发
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 销毁特效
        if (effectInstance != null)
        {
            Destroy(effectInstance);
            effectInstance = null;
        }
    }
    

} 