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
    [SerializeField] private float brakeVolume = 1f;

    private const float SpeedTransitionRate = 4f;

    private AudioSource brakeAudioSource;
    private InfiniteStreet infiniteStreet;
    private Vector3 initialLocalEuler;
    private float initialStreetSpeed;
    private float heldTime;

    // Public state read by PedalSound
    public bool IsPressed       { get; private set; }
    public bool HasReachedZeroSpeed { get; private set; }
    public bool SceneChangeTriggered { get; private set; }

    private Coroutine pressRoutine;
    private Coroutine returnRoutine;
    private Coroutine restoreSpeedRoutine;
    private Coroutine sceneChangeRoutine;
    public float HeldTime => heldTime;

    private void Awake()
    {
        infiniteStreet = FindFirstObjectByType<InfiniteStreet>();
        initialLocalEuler = transform.localEulerAngles;

        brakeAudioSource = GetComponent<AudioSource>();
        if (brakeAudioSource == null) brakeAudioSource = gameObject.AddComponent<AudioSource>();
        brakeAudioSource.spatialBlend = 0f;
        brakeAudioSource.volume = 1f;

        if (infiniteStreet != null)
            initialStreetSpeed = infiniteStreet.speed;
    }

    public void OnPointerDown(PointerEventData eventData) => StartPress();
    public void OnPointerUp(PointerEventData eventData)   => EndPress();
    private void OnDisable()                              => EndPress();

    private void StartPress()
    {
        if (IsPressed || SceneChangeTriggered) return;
        IsPressed = true;
        heldTime = 0f;

        if (brakeAnimator != null)
            brakeAnimator.SetBool("Brake", true);

        if (!HasReachedZeroSpeed && infiniteStreet != null)
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

        if (brakeClip != null)
        {
            brakeAudioSource.Stop();
            brakeAudioSource.PlayOneShot(brakeClip, brakeVolume);
        }

        pressRoutine = StartCoroutine(PressRoutine());
    }

    private void EndPress()
    {
        if (!IsPressed) return;
        IsPressed = false;

        if (brakeAnimator != null && !HasReachedZeroSpeed)
            brakeAnimator.SetBool("Brake", false);

        if (pressRoutine != null)
        {
            StopCoroutine(pressRoutine);
            pressRoutine = null;
        }

        brakeAudioSource.Stop();

        if (!gameObject.activeInHierarchy) return;

        if (HasReachedZeroSpeed)
        {
            if (infiniteStreet != null) infiniteStreet.speed = 0f;
            returnRoutine = StartCoroutine(ReturnBrakeRoutine());
            return;
        }

        // Early release — restore speed
        if (infiniteStreet != null)
            restoreSpeedRoutine = StartCoroutine(RestoreSpeedRoutine());

        returnRoutine = StartCoroutine(ReturnBrakeRoutine());
    }

    private IEnumerator PressRoutine()
    {
        while (IsPressed)
        {
            heldTime += Time.deltaTime;

            if (infiniteStreet != null)
            {
                if (HasReachedZeroSpeed)
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
                        HasReachedZeroSpeed = true;

                        if (sceneChangeRoutine == null)
                            sceneChangeRoutine = StartCoroutine(SceneChangeRoutine());
                    }
                }
            }

            float rampT = Mathf.Clamp01(heldTime / brakeRotationDuration);
            transform.localEulerAngles = initialLocalEuler + new Vector3(0f, rampT * brakeRotationAmount, 0f);

            yield return null;
        }
    }

    private IEnumerator SceneChangeRoutine()
    {
        SceneChangeTriggered = true;
        yield return new WaitForSeconds(2f);

        StopAllCoroutines();

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

    public float GetCurrentWheelSpeed()
    {
        return infiniteStreet != null ? infiniteStreet.speed * 10f : 0f;
    }
}