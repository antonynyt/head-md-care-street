using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Simple transition controller: plays a supplied AudioSource GameObject for `duration`
// and then loads `targetSceneName`.
// Drop any GameObject that contains an AudioSource into `footstepsObject`.
public class transition_steps : MonoBehaviour
{
    [Tooltip("GameObject that contains an AudioSource to use as the footsteps soundtrack.")]
    public GameObject footstepsObject;

    [Tooltip("Direct AudioClip to play for the footsteps soundtrack. You can drop an mp3/wav here.")]
    public AudioClip footstepsClip;

    [Tooltip("Scene name to load after the transition completes.")]
    public string targetSceneName = "GBoxCupScene";

    [Tooltip("How long the transition lasts in seconds.")]
    public float duration = 5f;

    AudioSource footstepsSource;

    IEnumerator Start()
    {
        // Priority: footstepsObject (GameObject with AudioSource) -> footstepsClip (AudioClip asset)
        if (footstepsObject != null)
        {
            footstepsSource = footstepsObject.GetComponent<AudioSource>();
            if (footstepsSource != null)
            {
                footstepsSource.Play();
            }
            else
            {
                Debug.LogWarning("transition_steps: footstepsObject has no AudioSource component.");
            }
        }
        else if (footstepsClip != null)
        {
            // Create a temporary AudioSource on this GameObject so the clip can be played
            footstepsSource = gameObject.AddComponent<AudioSource>();
            footstepsSource.clip = footstepsClip;
            footstepsSource.playOnAwake = false;
            footstepsSource.Play();
        }
        else
        {
            Debug.LogWarning("transition_steps: no footstepsObject or footstepsClip assigned.");
        }

        yield return new WaitForSeconds(duration);

        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("transition_steps: targetSceneName is empty, not loading any scene.");
        }
    }
}
