using UnityEngine;

public class StopZoomOut : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Entered StopZoomOut state");
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        //get audio length from DialogInteractionDirector
        float audioLength = CupInteractionDirector.Instance != null ? CupInteractionDirector.Instance.GetCurrentAudioLength() : 0f;
        Debug.Log($"Audio length: {audioLength}");

        var filling = animator.GetComponent<CameraController>().cupFilling;
        float fill = filling != null ? filling.currentFill01 : 0f;
        // stop zooming out when moving time is more than fill progress
        if (stateInfo.normalizedTime > fill)
        {
            animator.speed = 0f;
        }
        else
        {
            animator.speed = 1f;
        }



    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
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
