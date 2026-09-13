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
    [SerializeField] private float SceneChangeWaitTime = 5f;

    [Header("Sequence Data")]
    [SerializeField] private CupSequence sequence;

    private int lastTriggeredStep = 0;
    private bool isPressing;
    private bool waitingForRelease;
    private Coroutine releaseRoutine;
    private bool zoomAlreadyTriggered;

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

        // PlayFillStep(1);
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

        // Released before step 1 — drain naturally, no dialog, no lock
        if (lastTriggeredStep == 0)
        {
            isPressing = false;
            return;
        }

        isPressing = false;
        waitingForRelease = true;
        zoomAlreadyTriggered = false;

        string replyNode = GetReplyNode();

        // If we released mid-sentence, try to start the zoom-out + cup drain right now
        // instead of freezing until M finishes, by reading the reply's own <<ZoomOut N>>
        // duration ahead of time and stretching it by however long M has left to talk.
        // If we can't determine N (e.g. no such command on the node), fall back to the
        // safe freeze-and-wait behaviour so nothing desyncs.
        float? zoomOutSeconds = !string.IsNullOrWhiteSpace(replyNode)
            ? TryGetZoomOutSeconds(replyNode)
            : null;

        if (zoomOutSeconds.HasValue)
        {
            float remaining = dialogueRunner.IsDialogueRunning ? VoiceOverPresenterFixed.RemainingLineTime : 0f;
            float extendedDuration = zoomOutSeconds.Value + Mathf.Max(0f, remaining);

            // Skip the reply node's own <<ZoomOut N>> command later — we're applying its
            // effects right now, with the extended duration, so it doesn't stomp on it.
            CustomYarnCommands.suppressNextZoomOut = true;
            CustomYarnCommands.ApplyZoomOut(extendedDuration);

            if (CameraController.Instance != null)
            {
                CameraController.Instance.TriggerZoomOut();
                zoomAlreadyTriggered = true;
            }
        }
        else
        {
            // Hold the fill level exactly where it is at release, so the reply node's
            // ZoomOut command later computes drain speed from the same fill it saw here.
            cup.SetDrainFrozen(true);
        }

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        releaseRoutine = StartCoroutine(RunReleaseSequence(replyNode));
    }

    private string GetReplyNode()
    {
        if (sequence == null || sequence.replyStepNodes == null || sequence.replyStepNodes.Length == 0)
            return null;

        int replyIndex = Mathf.Clamp(lastTriggeredStep - 1, 0, sequence.replyStepNodes.Length - 1);
        return sequence.replyStepNodes[replyIndex];
    }

    /// <summary>
    /// Reads the numeric argument of a "ZoomOut" command compiled into the given node,
    /// straight from the Yarn project's compiled program — so the .yarn script stays the
    /// single source of truth and nothing needs to be duplicated in the Inspector.
    /// Uses reflection over Yarn Spinner's internal instruction format, since that shape
    /// isn't public API: if anything about it doesn't match at runtime, this simply
    /// returns null and the caller falls back to the freeze-and-wait behaviour.
    /// </summary>
    private float? TryGetZoomOutSeconds(string nodeName)
    {
        try
        {
            var program = dialogueRunner.YarnProject?.Program;
            if (program == null || !program.Nodes.TryGetValue(nodeName, out var node))
                return null;

            foreach (var instruction in node.Instructions)
            {
                var instructionType = instruction.GetType();
                string caseName = instructionType.GetProperty("InstructionTypeCase")?.GetValue(instruction)?.ToString();
                if (caseName != "RunCommand") continue;

                object runCommand = instructionType.GetProperty("RunCommand")?.GetValue(instruction);
                if (runCommand == null) continue;

                string commandText = runCommand.GetType().GetProperty("CommandText")?.GetValue(runCommand) as string;
                if (string.IsNullOrEmpty(commandText)) continue;

                string[] parts = commandText.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[0] == "ZoomOut" &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    return value;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"CupInteractionDirector: couldn't read ZoomOut duration from node '{nodeName}': {e.Message}");
        }

        return null;
    }

    private void PlayFillStep(int stepNumber)
    {
        if (sequence.fillStepNodes == null) return;
        
        int index = stepNumber - 1; // step 1 = index 0, step 2 = index 1, etc.
        
        if (index < 0 || index >= sequence.fillStepNodes.Length) return;

        string node = sequence.fillStepNodes[index];
        if (!string.IsNullOrWhiteSpace(node))
            _ = dialogueRunner.StartDialogue(node);
    }

    private IEnumerator RunReleaseSequence(string replyNode)
    {
        if (string.IsNullOrWhiteSpace(replyNode)) { cup.SetDrainFrozen(false); releaseRoutine = null; yield break; }

        // Wait for M's current dialogue node (lines + animations) to fully finish
        // before starting Roberto's reply, so releasing early doesn't cut it off.
        yield return new WaitUntil(() => !dialogueRunner.IsDialogueRunning);

        // No-op if we never froze it (the early-start path already got this moving).
        cup.SetDrainFrozen(false);

        _ = dialogueRunner.StartDialogue(replyNode);

        // Only fire the camera zoom-out here if the early-release path above didn't
        // already fire it — otherwise this would restart the animation from scratch.
        if (!zoomAlreadyTriggered && CameraController.Instance != null)
            CameraController.Instance.TriggerZoomOut();

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

    public void OnCupZoomOutDone()
    {
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(SceneChangeWaitTime);
        dialogueRunner.SaveStateToPersistentStorage("roberto_save");
        SceneManager.LoadScene("BikeScene");
    }

}