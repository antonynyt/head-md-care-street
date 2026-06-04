using System.Collections;
using UnityEngine;
using Yarn.Unity;

public class CupInteractionDirector : MonoBehaviour
{
    [System.Serializable]
    public class CupSequence
    {
        public string sequenceName;
        public string fillNode;
        public string replyNode;

        public AudioClip[] fillStepAudios;   // one audio per fill step
        public AudioClip releaseAudio;       // one audio for emptying
    }

    [Header("References")]
    [SerializeField] private CupFilling cup;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private AudioSource audioSource;

    [Header("Sequence Data")]
    [SerializeField] private CupSequence[] sequences;
    [SerializeField] private int currentSequenceIndex = 0;

    private int lastFillStep = -1;
    private Coroutine releaseRoutine;
    private bool waitingForRelease;

    private void Start()
    {
        if (cup == null || dialogueRunner == null || audioSource == null)
        {
            Debug.LogWarning("CupInteractionDirector: assign cup, dialogueRunner, and audioSource.");
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
            return;

        waitingForRelease = false;
        lastFillStep = -1;

        if (!string.IsNullOrWhiteSpace(sequence.fillNode))
            dialogueRunner.StartDialogue(sequence.fillNode);

        PlayFillStepAudio(0);
    }

    private void HandleFillProgress(float fill01)
    {
        var sequence = CurrentSequence;
        if (sequence == null || sequence.fillStepAudios == null || sequence.fillStepAudios.Length == 0)
            return;

        int step = Mathf.Clamp(cup.CurrentStep, 0, sequence.fillStepAudios.Length - 1);

        if (step != lastFillStep)
            PlayFillStepAudio(step);
    }

    private void HandleFillReleased(float fill01)
    {
        var sequence = CurrentSequence;
        if (sequence == null || waitingForRelease)
            return;

        waitingForRelease = true;

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(RunReleaseSequence(sequence));
    }

    private void PlayFillStepAudio(int stepIndex)
    {
        var sequence = CurrentSequence;
        if (sequence == null || sequence.fillStepAudios == null)
            return;

        if (stepIndex < 0 || stepIndex >= sequence.fillStepAudios.Length)
            return;

        var clip = sequence.fillStepAudios[stepIndex];
        if (clip == null)
            return;

        lastFillStep = stepIndex;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        cup.SetStepDuration(clip.length);
    }

    private IEnumerator RunReleaseSequence(CupSequence sequence)
    {
        if (sequence.releaseAudio != null)
        {
            audioSource.Stop();
            audioSource.clip = sequence.releaseAudio;
            audioSource.Play();

            cup.SetEmptyDuration(sequence.releaseAudio.length, 1f);

            yield return new WaitForSeconds(sequence.releaseAudio.length);
        }

        if (!string.IsNullOrWhiteSpace(sequence.replyNode))
            dialogueRunner.StartDialogue(sequence.replyNode);

        releaseRoutine = null;
    }

    public void SetSequenceIndex(int index)
    {
        currentSequenceIndex = Mathf.Clamp(index, 0, Mathf.Max(0, sequences.Length - 1));
        waitingForRelease = false;
        cup.ResetForNextSequence(clearFill: true);
    }
}