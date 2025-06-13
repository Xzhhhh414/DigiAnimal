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
    private bool needSetParent = false;
    
    // 进入状态时触发
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (effectPrefab == null)
            return;
            
        // 使用角色的根节点作为挂载点
        attachPoint = animator.transform;
        
        // 实例化特效并设置位置和旋转
        effectInstance = Instantiate(effectPrefab, 
                                     attachPoint.position + attachPoint.TransformDirection(new Vector3(positionOffset.x, positionOffset.y, 0)), 
                                     attachPoint.rotation);
                                     
        // 标记需要在下一帧设置父物体，避免在Awake阶段的SendMessage警告
        needSetParent = true;
    }
    
    // 每帧更新时触发
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 在第一次Update时设置父物体，此时Awake已经完成
        if (needSetParent && effectInstance != null && attachPoint != null)
        {
        effectInstance.transform.SetParent(attachPoint);
            needSetParent = false;
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