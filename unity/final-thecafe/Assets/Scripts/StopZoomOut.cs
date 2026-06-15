using UnityEngine;
using Yarn.Unity;

public class StopZoomOut : StateMachineBehaviour
{

    DialogueRunner dialogRunner;

    private float cupFill = 0f;

    private float stateTime = 0f;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (CustomYarnCommands.useCupFillForZoomOut)
        {
            cupFill = animator.GetComponent<CameraController>().cupFilling.currentFill01;
        }
        else
        {
            cupFill = 1f; // ignore actual cup fill, play the whole zoom-out animation
        }

        stateTime = 0f;
        animator.SetFloat("MotionTime", 0);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

        stateTime += Time.deltaTime;

        // Relative time is going from 0 to 1 during the the zoom out duration.
        float relativeTime = stateTime / CustomYarnCommands.lastZoomOutTime;
        if (relativeTime < 1)
        {
            float easedTime = relativeTime;

            // Cubic ease-out
            //easedTime = 1 - Mathf.Pow(1 - relativeTime, 3);

            // Sine ease-out
            //easedTime = Mathf.Sin(relativeTime * Mathf.PI * 0.5f);

            // Sine ease-in-out
            easedTime = 0.5f * (1 - Mathf.Cos(relativeTime * Mathf.PI));

            // only play the part of the animation that corresponds to the current fill level of the cup.
            float motionTime = easedTime * cupFill;

            animator.SetFloat("MotionTime", motionTime);

            // Debug.Log($"Updating MotionTime to {motionTime} (normalized: {stateInfo.normalizedTime}) with cupFill {cupFill}");
        }
        else
        {
            CupInteractionDirector.Instance.OnCupZoomOutDone();
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Exiting StopZoomOut state. State info: " + stateInfo.ToString());
        // CustomYarnCommands.OnZoomOutCommand.RemoveListener(OnZoomOutCommandReceived);
    }

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
