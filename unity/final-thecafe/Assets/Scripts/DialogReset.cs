using UnityEngine;
using Yarn.Unity;

public class DialogReset : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ResetDialog()
    {
        DialogueRunner dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        if (dialogueRunner == null)
        {
            Debug.LogWarning("DialogReset: no DialogueRunner found.");
            return;
        }

        // Stop the dialogue if it's currently running
        if (dialogueRunner.IsDialogueRunning)
        {
            _ = dialogueRunner.Stop();
        }

        // Reset the current day in the DialogueDirectorBike
        DialogueDirectorBike.ResetCurrentDay();

        // Clear the variable storage and save the state to persistent storage
        dialogueRunner.VariableStorage.Clear();
        dialogueRunner.SaveStateToPersistentStorage("roberto_save");
        Debug.Log("Dialog has been reset.");
    }
}
