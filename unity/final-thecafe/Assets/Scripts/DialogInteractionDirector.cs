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

    [Header("Release Audio")]
    [SerializeField] private AudioSource releaseSfxSource;
    [SerializeField] private AudioClip releaseSfxClip;
    [Range(0f, 1f)] [SerializeField] private float releaseSfxVolume = 1f;

    [SerializeField] private AudioSource songSource;
    [SerializeField] private AudioClip songClip;
    [Range(0f, 1f)] [SerializeField] private float songVolume = 1f;

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

        PlayReleaseAudio();

        string replyNode = GetReplyNode();

        float zoomOutSeconds = 0f;
        bool useCupFillForReply = true;
        bool foundArgs = !string.IsNullOrWhiteSpace(replyNode)
            && TryGetZoomOutArgs(replyNode, out zoomOutSeconds, out useCupFillForReply);

        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        if (foundArgs && !useCupFillForReply)
        {
            // This reply's camera zoom is explicitly independent of the cup fill (e.g.
            // Day 4's silent "..." replies via <<ZoomOut N, false>>), so the camera can
            // just wait for M to finish as normal — no early trigger, no suppression.
            // But the cup's own liquid-drain visual still needs a correct speed *right
            // now*: EndFill() already locked in a drain speed based on whatever
            // totalEmptyTime was last set to (often a stale/default value), and without
            // correcting it here the cup would visibly drain at that wrong pace for the
            // whole time we're waiting on M. So extend it by M's remaining time now —
            // the reply's own <<ZoomOut>> command will re-apply the same (by-then-lower)
            // fill over its plain duration once M finishes, which continues at the exact
            // same rate with no jump, since it's all one linear drain either way.
            float remainingM = dialogueRunner.IsDialogueRunning ? VoiceOverPresenterFixed.RemainingLineTime : 0f;
            cup.totalEmptyTime = zoomOutSeconds + Mathf.Max(0f, remainingM);
            cup.SetEmptySpeedFromCurrentFill();

            releaseRoutine = StartCoroutine(RunReleaseSequence(replyNode));
            return;
        }

        // If we released mid-sentence, try to start the zoom-out + cup drain right now
        // instead of freezing until M finishes, using the reply's own <<ZoomOut N>>
        // duration read ahead of time and stretched by however long M has left to talk.
        // If we can't determine N (e.g. no such command on the node), fall back to the
        // safe freeze-and-wait behaviour so nothing desyncs.
        if (foundArgs)
        {
            float remaining = dialogueRunner.IsDialogueRunning ? VoiceOverPresenterFixed.RemainingLineTime : 0f;
            float extendedDuration = zoomOutSeconds + Mathf.Max(0f, remaining);

            // Skip the reply node's own <<ZoomOut N>> command later — we're applying its
            // effects right now, with the extended duration, so it doesn't stomp on it.
            CustomYarnCommands.suppressNextZoomOut = true;
            CustomYarnCommands.ApplyZoomOut(extendedDuration, useCupFillForReply);

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

        releaseRoutine = StartCoroutine(RunReleaseSequence(replyNode));
    }

    /// <summary> Plays the release SFX immediately, then starts the song right as the
    /// SFX ends (sample-accurate via PlayDelayed, no coroutine needed). Uses two separate
    /// AudioSources so the one-shot SFX and the looping/long-form song don't fight over
    /// the same source. Volumes are independently tunable in the Inspector. </summary>
    private void PlayReleaseAudio()
    {
        float delay = 0f;

        if (releaseSfxSource != null && releaseSfxClip != null)
        {
            releaseSfxSource.PlayOneShot(releaseSfxClip, releaseSfxVolume);
            delay = releaseSfxClip.length;
        }

        if (songSource != null && songClip != null)
        {
            //only play if cup is > 1
            if (cup.CurrentStep > 1)
            {
                songSource.clip = songClip;
                songSource.volume = songVolume;
                songSource.PlayDelayed(delay);
            }
        }
    }

    private string GetReplyNode()
    {
        if (sequence == null || sequence.replyStepNodes == null || sequence.replyStepNodes.Length == 0)
            return null;

        int replyIndex = Mathf.Clamp(lastTriggeredStep - 1, 0, sequence.replyStepNodes.Length - 1);
        return sequence.replyStepNodes[replyIndex];
    }

    /// <summary>
    /// Reads the arguments of a "ZoomOut" command compiled into the given node, straight
    /// from the Yarn project's compiled program — so the .yarn script stays the single
    /// source of truth and nothing needs to be duplicated in the Inspector. Handles both
    /// "ZoomOut 3" and "ZoomOut 30, false" forms. Uses reflection over Yarn Spinner's
    /// internal instruction format, since that shape isn't public API: if anything about
    /// it doesn't match at runtime, this simply returns false and the caller falls back
    /// to the freeze-and-wait behaviour.
    /// </summary>
    private bool TryGetZoomOutArgs(string nodeName, out float seconds, out bool useCupFill)
    {
        seconds = 0f;
        useCupFill = true;

        try
        {
            var program = dialogueRunner.YarnProject?.Program;
            if (program == null || !program.Nodes.TryGetValue(nodeName, out var node))
                return false;

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
                if (parts.Length < 2 || parts[0] != "ZoomOut") continue;

                string secondsToken = parts[1].TrimEnd(',');
                if (!float.TryParse(secondsToken, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds))
                    continue;

                if (parts.Length >= 3 && bool.TryParse(parts[2].TrimEnd(','), out bool parsedFlag))
                    useCupFill = parsedFlag;

                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"CupInteractionDirector: couldn't read ZoomOut args from node '{nodeName}': {e.Message}");
        }

        return false;
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