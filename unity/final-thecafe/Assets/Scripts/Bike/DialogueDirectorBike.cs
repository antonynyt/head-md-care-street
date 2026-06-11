using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class DialogueDirectorBike : MonoBehaviour
{
    [System.Serializable]
    public class BikeSequence
    {
        public string[] daysYarnNodes = new string[3];
    }

    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private float delayBetweenLoops = 7f;
    [SerializeField] private BikeSequence sequence;

    [SerializeField] private BikeBrake bikeBrake;
    [SerializeField] private float brakeStopThreshold = 3f;
    private bool stopped = false;

    public static int CurrentDay { get; private set; } = 0;
    private string _currentNode;

    private void Awake()
    {
        CurrentDay = (CurrentDay) % (sequence.daysYarnNodes.Length) + 1;
        Debug.Log($"{sequence.daysYarnNodes.Length}");
        _currentNode = sequence.daysYarnNodes[CurrentDay - 1];

        Debug.Log($"CurrentDay: {CurrentDay}, Node: {_currentNode}");

        dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
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
        yield return new WaitForSeconds(delayBetweenLoops);
        dialogueRunner.StartDialogue(_currentNode);
    }

    private void OnDialogueComplete()
    {
        StartCoroutine(PlayLinesInOrder());
    }

    private void OnDestroy()
    {
        dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }
}