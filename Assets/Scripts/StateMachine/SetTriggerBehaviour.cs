using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTriggerBehaviour : StateMachineBehaviour
{
    public string triggerName;
    public bool setOnStateEnter, setOnStateExit, setOnStateMachineEnter, setOnStateMachineExit;
    public bool resetOnStateEnter, resetOnStateExit, resetOnStateMachineEnter, resetOnStateMachineExit;

        

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (setOnStateEnter)
        {
            animator.SetTrigger(triggerName);
        }
        if (resetOnStateEnter)
        {
            animator.ResetTrigger(triggerName);
        }
    }


    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (setOnStateExit)
        {
            animator.SetTrigger(triggerName);
        }
        if (resetOnStateExit)
        {
            animator.ResetTrigger(triggerName);
        }
    }
    override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (setOnStateMachineEnter)
        {
            animator.SetTrigger(triggerName);
        }
        if (resetOnStateMachineEnter)
        {
            animator.ResetTrigger(triggerName);
        }
    }

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (setOnStateMachineExit)
        {
            animator.SetTrigger(triggerName);
        }
        if (resetOnStateMachineExit)
        {
            animator.ResetTrigger(triggerName);
        }

    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //  
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
