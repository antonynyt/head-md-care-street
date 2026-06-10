using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class BikeBrake : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Rotation")]
    [SerializeField] private float brakeRotationAmount = 40f;
    [SerializeField] private float brakeRotationDuration = 0.6f;
    [SerializeField] private float brakeReturnDuration = 0.5f;

    [Header("Speed / Scene")]
    [SerializeField] private float pressDurationToZero = 4f;
    [SerializeField] private float speedTransitionRate = 4f;
    [SerializeField] private float sceneChangeThreshold = 4f;
    [SerializeField] private string transitionScene = "Roberto";

    [Header("Audio")]
    [SerializeField] private AudioClip brakeClip;
   
    [SerializeField] private float pedalFadeDuration = 0.4f;

    [Header("UI")]
    [SerializeField] private Image BrakeBar;

    private AudioSource audioSource;
    private InfiniteStreet infiniteStreet;
    private Vector3 initialLocalEuler;
    private AudioSource pedalAudioSource;
    private float initialStreetSpeed;
    private float heldTime;
    private bool isPressed;
    private bool brakeAudioStarted;
    private Coroutine pressRoutine;
    private Coroutine returnRoutine;
    private Coroutine pedalFadeRoutine;

    private void Awake()
    {
        BikeScreen bikeScreen = FindObjectOfType<BikeScreen>();
        if (bikeScreen != null) pedalAudioSource = bikeScreen.GetComponent<AudioSource>();

        infiniteStreet = FindObjectOfType<InfiniteStreet>();
        initialLocalEuler = transform.localEulerAngles;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (infiniteStreet != null)
            initialStreetSpeed = infiniteStreet.speed;
    }

    public void OnPointerDown(PointerEventData eventData) => StartPress();
    public void OnPointerUp(PointerEventData eventData)   => EndPress();
    private void OnDisable()                              => EndPress();

    private void StartPress()
    {
        if (isPressed) return;
        isPressed = true;
        brakeAudioStarted = false;
        heldTime = 0f;

        if (infiniteStreet != null)
            initialStreetSpeed = infiniteStreet.speed;

        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        pressRoutine = StartCoroutine(PressRoutine());
    }

    private void EndPress()
    {
        if (!isPressed) return;
        isPressed = false;

        if (pressRoutine != null)
        {
            StopCoroutine(pressRoutine);
            pressRoutine = null;
        }

        if (audioSource.isPlaying) audioSource.Stop();

        // Restore pedal sound
        if (pedalAudioSource != null)
        {
            pedalAudioSource.volume = 1f;
            if (!pedalAudioSource.isPlaying) pedalAudioSource.Play();
        }

        if (infiniteStreet != null)
            StartCoroutine(RestoreSpeedRoutine());

        returnRoutine = StartCoroutine(ReturnBrakeRoutine());
        StartCoroutine(ReturnBarRoutine());

    }

    private IEnumerator PressRoutine()
    {
        while (isPressed)
        {
            heldTime += Time.deltaTime;

            if (BrakeBar != null)
                // change image rotation from x=90 degrees to x=0degrees based on heldTime / pressDurationToZero
                BrakeBar.transform.localEulerAngles = new Vector3(
                    Mathf.Lerp(90f, 0f, Mathf.Clamp01(heldTime / pressDurationToZero)),
                    BrakeBar.transform.localEulerAngles.y,
                    BrakeBar.transform.localEulerAngles.z
                );

            // Slow street proportionally
            if (infiniteStreet != null)
            {
                float holdFactor = Mathf.Clamp01(1f - (heldTime / pressDurationToZero));
                float targetSpeed = initialStreetSpeed * holdFactor;
                infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, targetSpeed, Time.deltaTime * speedTransitionRate);
            }

            // Ramp brake rotation
            float rampT = Mathf.Clamp01(heldTime / brakeRotationDuration);
            transform.localEulerAngles = initialLocalEuler + new Vector3(0f, rampT * brakeRotationAmount, 0f);

            // Fade pedal out and play brake sound once
            if (!brakeAudioStarted && brakeClip != null)
            {
                if (pedalFadeRoutine != null) StopCoroutine(pedalFadeRoutine);
                pedalFadeRoutine = StartCoroutine(FadePedalOut());
                audioSource.PlayOneShot(brakeClip);
                brakeAudioStarted = true;
            }

            if (heldTime >= sceneChangeThreshold)
            {
                FadeManager.Instance.LoadScene(transitionScene);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator RestoreSpeedRoutine()
    {
        while (infiniteStreet != null)
        {
            infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, initialStreetSpeed, Time.deltaTime * speedTransitionRate);
            if (Mathf.Abs(infiniteStreet.speed - initialStreetSpeed) < 0.01f)
            {
                infiniteStreet.speed = initialStreetSpeed;
                break;
            }
            yield return null;
        }
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

    // make a private IEnumerator ReturnBarRoutine() that rotates the brake bar back to 90 degrees over brakeReturnDuration seconds
    private IEnumerator ReturnBarRoutine()
    {
        if (BrakeBar == null) yield break;

        float duration = brakeReturnDuration;
        float elapsed = 0f;

        float startRotation = BrakeBar.transform.localEulerAngles.x;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float x = Mathf.LerpAngle(startRotation, 90f, t);

            BrakeBar.transform.localEulerAngles = new Vector3(
                x,
                BrakeBar.transform.localEulerAngles.y,
                BrakeBar.transform.localEulerAngles.z
            );

            yield return null;
        }

        BrakeBar.transform.localEulerAngles = new Vector3(
            90f,
            BrakeBar.transform.localEulerAngles.y,
            BrakeBar.transform.localEulerAngles.z
        );
    }

    private IEnumerator FadePedalOut()
    {
        if (pedalAudioSource == null) yield break;

        float startVolume = pedalAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < pedalFadeDuration)
        {
            elapsed += Time.deltaTime;
            pedalAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / pedalFadeDuration);
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