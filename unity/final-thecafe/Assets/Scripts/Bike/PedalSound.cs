using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PedalSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip pedalClip;
    [SerializeField] private float fadeDuration = 1f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private BikeBrake[] brakes;
    private bool anyBrakeWasPressed;

    private void Awake()
    {
        brakes = FindObjectsByType<BikeBrake>(FindObjectsSortMode.None);

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.loop = true;
        audioSource.volume = 1f;

        if (pedalClip != null)
        {
            audioSource.clip = pedalClip;
            audioSource.Play();
        }
    }

    private void Update()
    {
        bool anyPressed    = AnyBrake(b => b.IsPressed);
        bool anyZeroed     = AnyBrake(b => b.HasReachedZeroSpeed);
        bool sceneChanging = AnyBrake(b => b.SceneChangeTriggered);

        if (sceneChanging || anyZeroed)
        {
            if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
            audioSource.volume = 0f;
            if (audioSource.isPlaying) audioSource.Stop();
            return;
        }

        if (anyPressed && !anyBrakeWasPressed)
        {
            anyBrakeWasPressed = true;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(0f, fadeDuration));
        }
        else if (!anyPressed && anyBrakeWasPressed)
        {
            anyBrakeWasPressed = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(1f, fadeDuration));
        }
    }

    private bool AnyBrake(System.Func<BikeBrake, bool> predicate)
    {
        foreach (var b in brakes)
            if (b != null && predicate(b)) return true;
        return false;
    }

    private IEnumerator FadeTo(float targetVolume, float duration)
    {
        float startVolume = audioSource.volume;

        if (targetVolume > 0f && !audioSource.isPlaying)
            audioSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        if (targetVolume <= 0f) audioSource.Stop();
        fadeRoutine = null;
    }
}