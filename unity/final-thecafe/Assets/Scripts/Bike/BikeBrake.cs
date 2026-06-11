using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class BikeBrake : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Rotation")]
    [SerializeField] private float brakeRotationAmount = 40f;
    [SerializeField] private float brakeRotationDuration = 0.6f;
    [SerializeField] private float brakeReturnDuration = 0.5f;

    [Header("Speed / Scene")]
    [SerializeField] private float pressDurationToZero = 3f;
    [SerializeField] private string transitionScene = "RobertoDay1";

    [Header("Animation")]
    [SerializeField] private Animator brakeAnimator;

    [Header("Audio")]
    [SerializeField] private AudioClip brakeClip;
    [SerializeField] private AudioClip pedalClip;
    [SerializeField] private float brakeVolume = 1f;
    [SerializeField] private float pedalFadeDuration = 0.8f;

    private const float SpeedTransitionRate = 4f;

    private AudioSource brakeAudioSource;
    private AudioSource pedalAudioSource;
    private InfiniteStreet infiniteStreet;
    private Vector3 initialLocalEuler;
    private float initialStreetSpeed;
    private float heldTime;
    private bool isPressed;
    private bool hasReachedZeroSpeed;
    private bool sceneChangeTriggered;

    private Coroutine pressRoutine;
    private Coroutine returnRoutine;
    private Coroutine pedalFadeRoutine;
    private Coroutine restoreSpeedRoutine;
    private Coroutine sceneChangeRoutine;

    private void Awake()
    {
        infiniteStreet = FindFirstObjectByType<InfiniteStreet>();
        initialLocalEuler = transform.localEulerAngles;

        // Brake sound — the AudioSource already on this GameObject
        brakeAudioSource = GetComponent<AudioSource>();
        if (brakeAudioSource == null) brakeAudioSource = gameObject.AddComponent<AudioSource>();
        brakeAudioSource.spatialBlend = 0f;
        brakeAudioSource.volume = 1f;

        // Pedal sound — a second AudioSource added at runtime
        pedalAudioSource = gameObject.AddComponent<AudioSource>();
        pedalAudioSource.spatialBlend = 0f;
        pedalAudioSource.loop = true;
        pedalAudioSource.volume = 1f;
        if (pedalClip != null)
        {
            pedalAudioSource.clip = pedalClip;
            pedalAudioSource.Play();
        }

        if (infiniteStreet != null)
            initialStreetSpeed = infiniteStreet.speed;
    }

    private void Start()
    {
        Debug.Log("[BikeBrake] pedalAudioSource in Start: " + (pedalAudioSource != null ? pedalAudioSource.gameObject.name : "NULL"));
    }

    public void OnPointerDown(PointerEventData eventData) => StartPress();
    public void OnPointerUp(PointerEventData eventData)   => EndPress();
    private void OnDisable()                              => EndPress();

    private void StartPress()
    {
        if (isPressed || sceneChangeTriggered) return;
        isPressed = true;
        heldTime = 0f;

        if (brakeAnimator != null)
            brakeAnimator.SetBool("Brake", true);

        if (!hasReachedZeroSpeed && infiniteStreet != null)
            initialStreetSpeed = infiniteStreet.speed;

        if (restoreSpeedRoutine != null)
        {
            StopCoroutine(restoreSpeedRoutine);
            restoreSpeedRoutine = null;
        }

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        // Play brake sound immediately — no fade in
        if (brakeClip != null)
        {
            brakeAudioSource.Stop();
            brakeAudioSource.PlayOneShot(brakeClip, brakeVolume);
        }

        // Fade pedal out immediately
        Debug.Log("[BikeBrake] StartPress — pedalAudioSource: " + (pedalAudioSource != null ? pedalAudioSource.gameObject.name : "NULL"));
        if (pedalFadeRoutine != null) StopCoroutine(pedalFadeRoutine);
        pedalFadeRoutine = StartCoroutine(FadePedalOut());

        pressRoutine = StartCoroutine(PressRoutine());
    }

    private void EndPress()
    {
        if (!isPressed) return;
        isPressed = false;

        if (brakeAnimator != null && !hasReachedZeroSpeed)
            brakeAnimator.SetBool("Brake", false);

        if (pressRoutine != null)
        {
            StopCoroutine(pressRoutine);
            pressRoutine = null;
        }

        brakeAudioSource.Stop();

        if (!gameObject.activeInHierarchy) return;

        if (hasReachedZeroSpeed)
        {
            // Speed stays 0, pedal stays silent, scene change already scheduled
            if (infiniteStreet != null) infiniteStreet.speed = 0f;
            returnRoutine = StartCoroutine(ReturnBrakeRoutine());
            return;
        }

        // Early release — restore pedal and speed
        if (pedalFadeRoutine != null)
        {
            StopCoroutine(pedalFadeRoutine);
            pedalFadeRoutine = null;
        }

        Debug.Log("[BikeBrake] Early release — restoring pedal");
        if (pedalAudioSource != null)
        {
            pedalAudioSource.volume = 1f;
            if (!pedalAudioSource.isPlaying) pedalAudioSource.Play();
        }

        if (infiniteStreet != null)
            restoreSpeedRoutine = StartCoroutine(RestoreSpeedRoutine());

        returnRoutine = StartCoroutine(ReturnBrakeRoutine());
    }

    private IEnumerator PressRoutine()
    {
        while (isPressed)
        {
            heldTime += Time.deltaTime;

            if (infiniteStreet != null)
            {
                if (hasReachedZeroSpeed)
                {
                    infiniteStreet.speed = 0f;
                }
                else
                {
                    float holdFactor = Mathf.Clamp01(1f - (heldTime / pressDurationToZero));
                    infiniteStreet.speed = initialStreetSpeed * holdFactor;
                if (holdFactor <= 0f)
                {
                    infiniteStreet.speed = 0f;
                    hasReachedZeroSpeed = true;

                    // Let SceneChangeRoutine handle the fade
                    if (sceneChangeRoutine == null)
                        sceneChangeRoutine = StartCoroutine(SceneChangeRoutine());
                }
                }
            }

            // Ramp brake lever rotation
            float rampT = Mathf.Clamp01(heldTime / brakeRotationDuration);
            transform.localEulerAngles = initialLocalEuler + new Vector3(0f, rampT * brakeRotationAmount, 0f);

            yield return null;
        }
    }

private IEnumerator SceneChangeRoutine()
{
    Debug.Log("[BikeBrake] SceneChangeRoutine started — loading scene: " + transitionScene);
    sceneChangeTriggered = true;

    // Ensure pedal fades completely before scene transition
    if (pedalAudioSource != null && pedalAudioSource.isPlaying)
    {
        if (pedalFadeRoutine != null)
            StopCoroutine(pedalFadeRoutine);

        yield return StartCoroutine(FadePedalOut());
    }

    // Optional pause after fade
    yield return new WaitForSeconds(2f);

    if (FadeManager.Instance != null)
        FadeManager.Instance.LoadScene(transitionScene);
    else
        SceneManager.LoadScene(transitionScene);
}

    private IEnumerator RestoreSpeedRoutine()
    {
        while (infiniteStreet != null)
        {
            infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, initialStreetSpeed, Time.deltaTime * SpeedTransitionRate);
            if (Mathf.Abs(infiniteStreet.speed - initialStreetSpeed) < 0.01f)
            {
                infiniteStreet.speed = initialStreetSpeed;
                break;
            }
            yield return null;
        }
        restoreSpeedRoutine = null;
    }

    private IEnumerator ReturnBrakeRoutine()
    {
        Vector3 startEuler = transform.localEulerAngles;
        float elapsed = 0f;

        while (elapsed < brakeReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / brakeReturnDuration);
            transform.localEulerAngles = new Vector3(
                Mathf.LerpAngle(startEuler.x, initialLocalEuler.x, t),
                Mathf.LerpAngle(startEuler.y, initialLocalEuler.y, t),
                Mathf.LerpAngle(startEuler.z, initialLocalEuler.z, t)
            );
            yield return null;
        }

        transform.localEulerAngles = initialLocalEuler;
        returnRoutine = null;
    }

    private IEnumerator FadePedalOut()
    {
        if (pedalAudioSource == null)
            yield break;

        float startVolume = pedalAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < pedalFadeDuration)
        {
            if (pedalAudioSource == null)
                yield break;

            elapsed += Time.deltaTime;

            pedalAudioSource.volume =
                Mathf.Lerp(startVolume, 0f, elapsed / pedalFadeDuration);

            Debug.Log("Pedal volume = " + pedalAudioSource.volume);

            yield return null;
        }

        pedalAudioSource.volume = 0f;
        pedalAudioSource.Stop();

        pedalFadeRoutine = null;
    }
    public float GetCurrentWheelSpeed()
    {
        return infiniteStreet != null ? infiniteStreet.speed * 10f : 0f;
    }
}