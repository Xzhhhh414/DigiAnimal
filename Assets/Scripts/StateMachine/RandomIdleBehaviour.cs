using UnityEngine;

public class RandomIdleBehaviour : StateMachineBehaviour
{
    // 可以配置的闲置动画数量
    [SerializeField] private int idleAnimationCount = 3;
    
    // 在状态进入时触发
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 生成一个随机索引 (1到idleAnimationCount)
        int randomIndex = Random.Range(1, idleAnimationCount + 1);
        
        // 设置IdleIndex参数
        animator.SetInteger("idleIndex", randomIndex);
        
        // 调试信息
        // Debug.Log($"Setting random idle: {randomIndex}");
    }
} 