using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class touchscreen : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject wheel_front;
    public GameObject wheel_back;
    public GameObject brake_left;
    public GameObject brake_right;
    public GameObject bell;

    [SerializeField] private AudioClip pedal_1;
    [SerializeField] private AudioClip brake_0;
    [SerializeField] private AudioClip bellClip;

    public float wheel_speed = 100f;
    [SerializeField] private float pressDurationToZero = 4f;
    [SerializeField] private float speedTransitionRate = 6f;
    [SerializeField] private float shortPressThreshold = 1.5f;
    [SerializeField] private float sceneChangeThreshold = 6f;
    [SerializeField] private float bellEffectDuration = 1f;
    [SerializeField] private float bellRotationAmount = 10f;
    [SerializeField] private float brakeRotationAmount = 40f;
    [SerializeField] private float brakeRotationDuration = 1.5f;
    [SerializeField] private float brakeReturnDuration = 1f;
    [SerializeField] private float brakeLeftRotationAmount = -40f;
    [SerializeField] private float brakeRightRotationAmount = 40f;

    private const string TransitionScene = "Transition";
 
    private InfiniteStreet infiniteStreet;
    private AudioSource audioSource;
    private float initialStreetSpeed;
    private float pressStartTime;
    private Coroutine speedRoutine;
    private Coroutine effectRoutine;
    private Coroutine brakeReturnRoutine;
    private bool isPressing;
    private bool brakeActiveDuringPress;
    private Quaternion bellInitialRotation;
    private Quaternion brakeLeftInitialRotation;
    private Quaternion brakeRightInitialRotation;
    // store Euler angles to apply simple +Y offsets matching inspector behaviour
    private Vector3 bellInitialEuler;
    private Vector3 brakeLeftInitialEuler;
    private Vector3 brakeRightInitialEuler;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        infiniteStreet = GetComponent<InfiniteStreet>();

        if (infiniteStreet != null)
        {
            initialStreetSpeed = infiniteStreet.speed;
        }

        StoreInitialRotations();
        PlayIdlePedalSound();
    }

    private void Update()
    {
        if (infiniteStreet != null)
        {
            wheel_speed = infiniteStreet.speed * 10.0f;
        }

        WheelRotation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (infiniteStreet == null)
        {
            return;
        }

        initialStreetSpeed = infiniteStreet.speed;
        pressStartTime = Time.time;
        isPressing = true;

        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
        }

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        PlayIdlePedalSound();
        speedRoutine = StartCoroutine(PressSpeedRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (infiniteStreet == null)
        {
            return;
        }

        float heldTime = Time.time - pressStartTime;
        isPressing = false;

        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
            speedRoutine = null;
        }

        if (heldTime >= sceneChangeThreshold)
        {
            SceneManager.LoadScene(TransitionScene);
            return;
        }

        speedRoutine = StartCoroutine(RestoreSpeedRoutine());

        if (heldTime < shortPressThreshold)
        {
            effectRoutine = StartCoroutine(PlayBellEffectRoutine());
        }
        else
        {
            // If the brake was already activated during the press, just stop its audio and reset.
            if (brakeActiveDuringPress)
            {
                StopBrakeDuringPress();
                if (brakeReturnRoutine != null)
                {
                    StopCoroutine(brakeReturnRoutine);
                }

                brakeReturnRoutine = StartCoroutine(ReturnBrakesToInitialRoutine());
                brakeActiveDuringPress = false;
            }
            else
            {
                effectRoutine = StartCoroutine(PlayBrakeEffectRoutine());
            }
        }
    }

    private void WheelRotation()
    {
        if (wheel_front == null || wheel_back == null)
        {
            return;
        }

        wheel_front.transform.Rotate(Vector3.down * wheel_speed * Time.deltaTime);
        wheel_back.transform.Rotate(Vector3.down * wheel_speed * Time.deltaTime);
    }

    private IEnumerator PressSpeedRoutine()
    {
        float heldTime = 0f;

        while (isPressing)
        {
            heldTime += Time.deltaTime;

            float holdFactor = Mathf.Clamp01(1f - (heldTime / pressDurationToZero));
            float targetSpeed = initialStreetSpeed * holdFactor;
            infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, targetSpeed, Time.deltaTime * speedTransitionRate);

            if (heldTime >= pressDurationToZero && Mathf.Approximately(infiniteStreet.speed, 0f))
            {
                infiniteStreet.speed = 0f;
            }

            // Start brake audio and ramp brakes after shortPressThreshold is reached
            if (heldTime >= shortPressThreshold)
            {
                if (!brakeActiveDuringPress)
                {
                    StartBrakeDuringPress();
                }

                // Ramp brake rotation from 0 at shortPressThreshold to full over brakeRotationDuration
                float rampT = Mathf.Clamp01((heldTime - shortPressThreshold) / brakeRotationDuration);
                float leftAngle = rampT * brakeLeftRotationAmount;
                float rightAngle = rampT * brakeRightRotationAmount;

                if (brake_left != null)
                {
                    // apply simple +Y offset so inspector rotation matches
                    brake_left.transform.localEulerAngles = brakeLeftInitialEuler + new Vector3(0f, leftAngle, 0f);
                }

                if (brake_right != null)
                {
                    // apply simple +Y offset so inspector rotation matches
                    brake_right.transform.localEulerAngles = brakeRightInitialEuler + new Vector3(0f, rightAngle, 0f);
                }

                // If held past the sceneChangeThreshold, transition automatically
                if (heldTime >= sceneChangeThreshold)
                {
                    SceneManager.LoadScene(TransitionScene);
                    yield break;
                }
            }

            yield return null;
        }

        speedRoutine = null;
    }

    private IEnumerator RestoreSpeedRoutine()
    {
        while (!Mathf.Approximately(infiniteStreet.speed, initialStreetSpeed))
        {
            infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, initialStreetSpeed, Time.deltaTime * speedTransitionRate);

            if (Mathf.Abs(infiniteStreet.speed - initialStreetSpeed) < 0.01f)
            {
                infiniteStreet.speed = initialStreetSpeed;
                break;
            }

            yield return null;
        }

        speedRoutine = null;
    }

    private IEnumerator PlayBellEffectRoutine()
    {
        if (bell != null)
        {
            bell.transform.localEulerAngles = bellInitialEuler;
        }

        yield return PlayTemporaryClipRoutine(bellClip, bellEffectDuration);
        ResetEffectObjects();
        effectRoutine = null;
    }

    private IEnumerator PlayBrakeEffectRoutine()
    {
        if (brake_left != null)
        {
            brake_left.transform.localEulerAngles = brakeLeftInitialEuler + new Vector3(0f, brakeLeftRotationAmount, 0f);
        }

        if (brake_right != null)
        {
            brake_right.transform.localEulerAngles = brakeRightInitialEuler + new Vector3(0f, brakeRightRotationAmount, 0f);
        }

        yield return PlayTemporaryClipRoutine(brake_0, brake_0 != null ? brake_0.length : 0f);
        ResetEffectObjects();
        effectRoutine = null;
    }

    private IEnumerator PlayTemporaryClipRoutine(AudioClip clip, float duration)
    {
        if (audioSource == null || clip == null)
        {
            yield break;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.volume = 1f;
        audioSource.Play();

        float waitTime = duration > 0f ? duration : clip.length;
        if (clip == bellClip)
        {
            waitTime = Mathf.Min(waitTime, bellEffectDuration);
        }

        float elapsed = 0f;
        while (elapsed < waitTime)
        {
            elapsed += Time.deltaTime;

                if (clip == bellClip && bell != null)
                {
                    float x = Mathf.Sin(elapsed * 24f) * bellRotationAmount;
                    float y = Mathf.Sin(elapsed * 31f) * bellRotationAmount;
                    float z = Mathf.Sin(elapsed * 27f) * bellRotationAmount;
                    // apply vibration as Euler offsets so inspector values remain reference
                    bell.transform.localEulerAngles = bellInitialEuler + new Vector3(x, y, z);
                }

            yield return null;
        }

        audioSource.Stop();
        PlayIdlePedalSound();
    }

    private void PlayIdlePedalSound()
    {
        if (audioSource == null || pedal_1 == null)
        {
            return;
        }

        if (audioSource.clip == pedal_1 && audioSource.isPlaying)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = pedal_1;
        audioSource.loop = true;
        audioSource.volume = 1f;
        audioSource.Play();
    }

    private void StoreInitialRotations()
    {
        if (bell != null)
        {
            bellInitialRotation = bell.transform.localRotation;
            bellInitialEuler = bell.transform.localEulerAngles;
        }

        if (brake_left != null)
        {
            brakeLeftInitialRotation = brake_left.transform.localRotation;
            brakeLeftInitialEuler = brake_left.transform.localEulerAngles;
        }

        if (brake_right != null)
        {
            brakeRightInitialRotation = brake_right.transform.localRotation;
            brakeRightInitialEuler = brake_right.transform.localEulerAngles;
        }
    }

    private void ResetEffectObjects()
    {
        if (bell != null)
        {
            bell.transform.localEulerAngles = bellInitialEuler;
        }

        if (brake_left != null)
        {
            brake_left.transform.localEulerAngles = brakeLeftInitialEuler;
        }

        if (brake_right != null)
        {
            brake_right.transform.localEulerAngles = brakeRightInitialEuler;
        }
    }

    private IEnumerator ReturnBrakesToInitialRoutine()
    {
        Vector3 brakeLeftStart = brake_left != null ? brake_left.transform.localEulerAngles : brakeLeftInitialEuler;
        Vector3 brakeRightStart = brake_right != null ? brake_right.transform.localEulerAngles : brakeRightInitialEuler;

        float elapsed = 0f;
        while (elapsed < brakeReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / brakeReturnDuration);

            if (brake_left != null)
            {
                float leftX = Mathf.LerpAngle(brakeLeftStart.x, brakeLeftInitialEuler.x, t);
                float leftY = Mathf.LerpAngle(brakeLeftStart.y, brakeLeftInitialEuler.y, t);
                float leftZ = Mathf.LerpAngle(brakeLeftStart.z, brakeLeftInitialEuler.z, t);
                brake_left.transform.localEulerAngles = new Vector3(leftX, leftY, leftZ);
            }

            if (brake_right != null)
            {
                float rightX = Mathf.LerpAngle(brakeRightStart.x, brakeRightInitialEuler.x, t);
                float rightY = Mathf.LerpAngle(brakeRightStart.y, brakeRightInitialEuler.y, t);
                float rightZ = Mathf.LerpAngle(brakeRightStart.z, brakeRightInitialEuler.z, t);
                brake_right.transform.localEulerAngles = new Vector3(rightX, rightY, rightZ);
            }

            yield return null;
        }

        ResetEffectObjects();
        brakeReturnRoutine = null;
    }

    private void StartBrakeDuringPress()
    {
        if (audioSource == null || brake_0 == null)
        {
            return;
        }

        // stop any current sound and play the brake sound continuously while pressing
        audioSource.Stop();
        audioSource.clip = brake_0;
        audioSource.loop = true;
        audioSource.volume = 1f;
        audioSource.Play();

        brakeActiveDuringPress = true;
    }

    private void StopBrakeDuringPress()
    {
        if (audioSource == null)
        {
            brakeActiveDuringPress = false;
            return;
        }

        audioSource.Stop();
        PlayIdlePedalSound();
        brakeActiveDuringPress = false;
    }
}

