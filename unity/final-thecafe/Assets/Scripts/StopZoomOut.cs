using UnityEngine;
using Yarn.Unity;

public class StopZoomOut : StateMachineBehaviour
{

    DialogueRunner dialogRunner;

    protected float cupFill = 0f;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log("Entered StopZoomOut state. State info: " + stateInfo.ToString());

        cupFill = animator.GetComponent<CameraController>().cupFilling.currentFill01;
        Debug.Log($"Current cup fill level: {cupFill}");

        float zoomOutTime = CustomYarnCommands.lastZoomOutTime;
        Debug.Log($"ZoomOut time from CustomYarnCommands: {zoomOutTime} seconds");

        // Calculate the state speed multiplier so that after zoomOutTime seconds, the animation time will be cupFill * animation duration
        float animationDuration = stateInfo.length;
        Debug.Log($"Animation duration for StopZoomOut state: {animationDuration} seconds");
        float targetTime = cupFill * animationDuration;
        float speedMultiplier = animationDuration * cupFill / zoomOutTime;
        animator.speed = speedMultiplier;
        Debug.Log($"Calculated speed multiplier: {speedMultiplier} to reach cupFill {cupFill} at zoomOutTime {zoomOutTime} seconds");

        // CustomYarnCommands.OnZoomOutCommand.AddListener(OnZoomOutCommandReceived);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // stop zooming out when moving time is more than fill progress
        if (stateInfo.normalizedTime > cupFill && animator.speed > 0f)
        {
            Debug.Log($"Stopping zoom out at normalized time {stateInfo.normalizedTime} which is greater than cupFill {cupFill}");
            animator.speed = 0f;
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
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

    // private void OnZoomOutCommandReceived(float zoomOutTime)
    // {
    //     Debug.Log($"Received ZoomOut command with time: {zoomOutTime} on StopZoomOut state");
    //     // You can implement additional logic here if needed when the command is received
    // }


}
