using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class touchscreen : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject wheel_front;
    public GameObject wheel_back;
    [SerializeField] private AudioClip pedal_1;
    [SerializeField] private AudioClip brake_0;
    [SerializeField] private AudioClip footsteps;
    public float wheel_speed = 100f;
    [SerializeField] private float pressDurationToZero = 4f;
    [SerializeField] private float footstepsDelay = 5f;
    [SerializeField] private float speedTransitionRate = 6f;
    [SerializeField] private float audioFadeDuration = 0.2f;

    private InfiniteStreet infiniteStreet;
    private AudioSource audioSource;
    private float initialStreetSpeed;
    private Coroutine speedRoutine;
    private Coroutine audioRoutine;
    private bool isPressing;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        infiniteStreet = GetComponent<InfiniteStreet>();
        if (infiniteStreet != null)
        {
            initialStreetSpeed = infiniteStreet.speed;
        }

        if (audioSource != null && pedal_1 != null)
        {
            audioSource.clip = pedal_1;
            audioSource.loop = true;
            audioSource.volume = 1f;
            audioSource.Play();
        }
    }

    void Update()
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
        isPressing = true;

        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
        }

        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
        }

        speedRoutine = StartCoroutine(PressSpeedRoutine());
        audioRoutine = StartCoroutine(PressAudioRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (infiniteStreet == null)
        {
            return;
        }

        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
        }

        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
        }

        isPressing = false;

        speedRoutine = StartCoroutine(RestoreSpeedRoutine());
        StartAudioTransition(pedal_1, true);
    }

    void WheelRotation()
    {
        if (wheel_front == null || wheel_back == null)
        {
            return;
        }

        // rotate the wheels x direction using the speed variable
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

            yield return null;
        }

        speedRoutine = null;
    }

    private IEnumerator PressAudioRoutine()
    {
        if (audioSource == null || brake_0 == null)
        {
            yield break;
        }

        yield return StartCoroutine(FadeOutAudioRoutine());

        audioSource.Stop();
        audioSource.clip = brake_0;
        audioSource.loop = false;
        audioSource.volume = 1f;
        audioSource.Play();

        yield return new WaitForSeconds(brake_0.length);

        if (!isPressing)
        {
            yield break;
        }

        if (footsteps == null)
        {
            yield break;
        }

        audioSource.Stop();
        audioSource.clip = footsteps;
        audioSource.loop = true;
        audioSource.volume = 1f;
        audioSource.Play();

        audioRoutine = null;
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

    private void StartAudioTransition(AudioClip targetClip, bool shouldLoop)
    {
        if (audioSource == null || targetClip == null)
        {
            return;
        }

        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
        }

        audioRoutine = StartCoroutine(AudioTransitionRoutine(targetClip, shouldLoop));
    }

    private IEnumerator FadeOutAudioRoutine()
    {
        if (audioSource == null)
        {
            yield break;
        }

        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / audioFadeDuration));
            yield return null;
        }

        audioSource.volume = 0f;
    }

    private IEnumerator AudioTransitionRoutine(AudioClip targetClip, bool shouldLoop)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / audioFadeDuration));
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = targetClip;
        audioSource.loop = shouldLoop;
        audioSource.Play();

        elapsed = 0f;
        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / audioFadeDuration));
            yield return null;
        }

        audioSource.volume = 1f;
        audioRoutine = null;
    }

    //play sound pedal_1.mp3 in continous loop when nothing happens
    // AudioSource audioSource;
    // public AudioClip pedal_1;
    void ConstantSound()
     {
        // play the audio source    
             if (audioSource != null)
             {
                     audioSource.Play();
             }

     }

     
    

}

