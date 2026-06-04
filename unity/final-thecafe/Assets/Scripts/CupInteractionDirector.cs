using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class CupInteractionDirector : MonoBehaviour
{
    [System.Serializable]
    public class CupSequence
    {
        public string sequenceName;

        [Header("Yarn nodes")]
        public string[] fillStepNodes = new string[3];
        public string[] replyStepNodes = new string[3];
    }

    [Header("References")]
    [SerializeField] private CupFilling cup;
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Sequence Data")]
    [SerializeField] private CupSequence[] sequences;
    [SerializeField] private int currentSequenceIndex = 0;

    private int lastTriggeredStep = 0;
    private bool isPressing;
    private bool waitingForRelease;
    private Coroutine releaseRoutine;

    private void Start()
    {
        if (cup == null || dialogueRunner == null)
        {
            Debug.LogWarning("CupInteractionDirector: assign cup and dialogueRunner.");
            enabled = false;
            return;
        }

        cup.OnFillStarted.AddListener(HandleFillStarted);
        cup.OnFillProgress.AddListener(HandleFillProgress);
        cup.OnFillReleased.AddListener(HandleFillReleased);
    }

    private void OnDestroy()
    {
        if (cup == null) return;

        cup.OnFillStarted.RemoveListener(HandleFillStarted);
        cup.OnFillProgress.RemoveListener(HandleFillProgress);
        cup.OnFillReleased.RemoveListener(HandleFillReleased);
    }

    private CupSequence CurrentSequence
    {
        get
        {
            if (sequences == null || sequences.Length == 0)
                return null;

            return sequences[Mathf.Clamp(currentSequenceIndex, 0, sequences.Length - 1)];
        }
    }

    private void HandleFillStarted()
    {
        var sequence = CurrentSequence;
        if (sequence == null)
        {
            Debug.LogWarning("CupInteractionDirector: no current sequence.");
            return;
        }

        isPressing = true;
        waitingForRelease = false;
        lastTriggeredStep = 0;

        PlayFillStep(sequence, 0);
    }

    private void HandleFillProgress(float fill01)
    {
        var sequence = CurrentSequence;
        if (sequence == null || !isPressing)
            return;

        int currentStep = cup.CurrentStep;

        if (currentStep > lastTriggeredStep)
        {
            lastTriggeredStep = currentStep;
            PlayFillStep(sequence, currentStep);
        }
    }

    private void HandleFillReleased(float fill01)
    {
        var sequence = CurrentSequence;
        if (sequence == null || waitingForRelease)
            return;

        isPressing = false;
        waitingForRelease = true;

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(RunReleaseSequence(sequence));
    }

    private void PlayFillStep(CupSequence sequence, int stepNumber)
    {
        if (sequence.fillStepNodes == null)
            return;

        if (stepNumber < 0 || stepNumber >= sequence.fillStepNodes.Length)
            return;

        string node = sequence.fillStepNodes[stepNumber];
        if (!string.IsNullOrWhiteSpace(node))
            dialogueRunner.StartDialogue(node);
    }

    private IEnumerator RunReleaseSequence(CupSequence sequence)
    {
        int replyIndex = Mathf.Clamp(lastTriggeredStep, 0, 2);

        if (sequence.replyStepNodes != null &&
            replyIndex < sequence.replyStepNodes.Length)
        {
            string replyNode = sequence.replyStepNodes[replyIndex];
            if (!string.IsNullOrWhiteSpace(replyNode))
                dialogueRunner.StartDialogue(replyNode);
        }

        releaseRoutine = null;
        yield break;
    }

    public void SetSequenceIndex(int index)
    {
        currentSequenceIndex = Mathf.Clamp(index, 0, Mathf.Max(0, sequences.Length - 1));
        lastTriggeredStep = 0;
        isPressing = false;
        waitingForRelease = false;

        cup.ResetForNextSequence(clearFill: true);
    }
}