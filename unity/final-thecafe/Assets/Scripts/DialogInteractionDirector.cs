using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class CupInteractionDirector : MonoBehaviour
{
    public static CupInteractionDirector Instance { get; private set; }

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
    [SerializeField] private CupSequence sequence;

    private int lastTriggeredStep = 0;
    private bool isPressing;
    private bool waitingForRelease;
    private Coroutine releaseRoutine;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.rKey.wasPressedThisFrame)
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Start()
    {
        Instance = this;
        if (cup == null || dialogueRunner == null)
        {
            Debug.LogWarning("CupInteractionDirector: assign cup and dialogueRunner.");
            enabled = false;
            return;
        }

        // Charger les variables sauvegardées (day1_fill, day2_fill, etc.)
        string savePath = System.IO.Path.Combine(Application.persistentDataPath, "roberto_save");
        if (System.IO.File.Exists(savePath))
            dialogueRunner.LoadStateFromPersistentStorage("roberto_save");

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

    private void HandleFillStarted()
    {
        if (sequence == null)
        {
            Debug.LogWarning("CupInteractionDirector: no sequence assigned.");
            return;
        }

        isPressing = true;
        waitingForRelease = false;
        lastTriggeredStep = 0;

        PlayFillStep(0);
    }

    private void HandleFillProgress(float fill01)
    {
        if (sequence == null || !isPressing) return;

        int currentStep = cup.CurrentStep;
        if (currentStep > lastTriggeredStep)
        {
            lastTriggeredStep = currentStep;
            PlayFillStep(currentStep);
        }
    }

    private void HandleFillReleased(float fill01)
    {
        if (sequence == null || waitingForRelease) return;

        isPressing = false;
        waitingForRelease = true;

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(RunReleaseSequence());
    }

    private void PlayFillStep(int stepNumber)
    {
        if (sequence.fillStepNodes == null) return;
        if (stepNumber < 0 || stepNumber >= sequence.fillStepNodes.Length) return;

        string node = sequence.fillStepNodes[stepNumber];
        if (!string.IsNullOrWhiteSpace(node))
            _ = dialogueRunner.StartDialogue(node);
    }

    private IEnumerator RunReleaseSequence()
    {
        int replyIndex = Mathf.Clamp(lastTriggeredStep, 0, 2);

        if (sequence.replyStepNodes != null && replyIndex < sequence.replyStepNodes.Length)
        {
            string replyNode = sequence.replyStepNodes[replyIndex];
            if (!string.IsNullOrWhiteSpace(replyNode))
                _ = dialogueRunner.StartDialogue(replyNode);
        }

        releaseRoutine = null;
        yield break;
    }

    /// <summary>
    /// Appelé par CustomYarnCommands.change_scene avant de changer de scène,
    /// pour s'assurer que les variables Yarn sont sauvegardées sur disque.
    /// </summary>
    public void SaveState()
    {
        dialogueRunner.SaveStateToPersistentStorage("roberto_save");
        Debug.Log("CupInteractionDirector: state saved.");
    }

    /// <summary>
    /// Reset complet — appelé si on veut recommencer depuis le jour 1.
    /// </summary>
    public void ResetStory()
    {
        dialogueRunner.VariableStorage.Clear();
        dialogueRunner.SaveStateToPersistentStorage("roberto_save");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}