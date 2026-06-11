using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class DialogueDirectorBike : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float delayBetweenLines = 7f;
    [SerializeField] private BikeBrake bikeBrake;
    [SerializeField] private float brakeStopThreshold = 3f;

    private string[] nodes = { "Bike_1", "Bike_2", "Bike_3" };
    private bool stopped = false;

    private void Start()
    {
        StartCoroutine(PlayLinesInOrder());
    }

    private void Update()
    {
        if (stopped || bikeBrake == null) return;

        // Stop dialogue when brake held 3s OR speed has fully zeroed out
        if (bikeBrake.HeldTime >= brakeStopThreshold || bikeBrake.HasReachedZeroSpeed)
        {
            stopped = true;
            StopAllCoroutines();
            if (dialogueRunner.IsDialogueRunning)
                dialogueRunner.Stop();
        }
    }

    private IEnumerator PlayLinesInOrder()
    {
        int index = 0;
        while (true)
        {
            yield return new WaitForSeconds(delayBetweenLines);
            dialogueRunner.StartDialogue(nodes[index]);
            yield return new WaitUntil(() => !dialogueRunner.IsDialogueRunning);
            index = (index + 1) % nodes.Length;
        }
    }
}