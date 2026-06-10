using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class DialogueDirectorBike : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float delayBetweenLines = 7f;

    private string[] nodes = { "Bike_1", "Bike_2", "Bike_3" };

    private void Start()
    {
        StartCoroutine(PlayLinesInOrder());
    }

    private IEnumerator PlayLinesInOrder()
    {
        int index = 0;

        while (true)
        {
            yield return new WaitForSeconds(delayBetweenLines);

            dialogueRunner.StartDialogue(nodes[index]);
            yield return new WaitUntil(() => !dialogueRunner.IsDialogueRunning);

            index = (index + 1) % nodes.Length; // loops back to 0 after 2
        }
    }
}