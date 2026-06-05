using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class touchscreen : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject wheel_front;
    public GameObject wheel_back;
    public float wheel_speed = 100f;
    [SerializeField] private float pressDurationToZero = 4f;
    [SerializeField] private float speedTransitionRate = 6f;

    private InfiniteStreet infiniteStreet;
    private float initialStreetSpeed;
    private Coroutine speedRoutine;

    void Awake()
    {
        infiniteStreet = GetComponent<InfiniteStreet>();
        if (infiniteStreet != null)
        {
            initialStreetSpeed = infiniteStreet.speed;
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

        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
        }

        speedRoutine = StartCoroutine(PressSpeedRoutine());
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

        speedRoutine = StartCoroutine(RestoreSpeedRoutine());
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

        while (true)
        {
            heldTime += Time.deltaTime;

            float holdFactor = Mathf.Clamp01(1f - (heldTime / pressDurationToZero));
            float targetSpeed = initialStreetSpeed * holdFactor;
            infiniteStreet.speed = Mathf.Lerp(infiniteStreet.speed, targetSpeed, Time.deltaTime * speedTransitionRate);

            if (heldTime >= pressDurationToZero && Mathf.Approximately(infiniteStreet.speed, 0f))
            {
                infiniteStreet.speed = 0f;
                break;
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

    //play sound pedal_1.mp3 in continous loop when nothing happens
    // AudioSource audioSource;
    // public AudioClip pedal_1;
    void ConstantSound()
     {
        // play the audio source    
       GetComponent<AudioSource>().Play();

     }

     
    

}

