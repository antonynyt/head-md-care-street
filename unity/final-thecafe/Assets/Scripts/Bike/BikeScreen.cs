using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class BikeScreen : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Wheels")]
    public GameObject wheel_front;
    public GameObject wheel_back;

    [Header("Brakes (for vibration)")]
    public GameObject brake_left;
    public GameObject brake_right;

    [Header("Audio")]
    [SerializeField] private AudioClip pedalClip;
    [SerializeField] private AudioClip bellClip;

    private AudioSource audioSource;
    private InfiniteStreet infiniteStreet;

    private Vector3 brakeLeftInitialEuler;
    private Vector3 brakeRightInitialEuler;

    private Coroutine vibrationRoutine;

    private void Awake()
    {
        infiniteStreet = FindObjectOfType<InfiniteStreet>();
        audioSource = GetComponent<AudioSource>();

        if (brake_left != null)  brakeLeftInitialEuler  = brake_left.transform.localEulerAngles;
        if (brake_right != null) brakeRightInitialEuler = brake_right.transform.localEulerAngles;

        PlayPedalSound();
    }

    private void Update()
    {
        RotateWheels();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Play bell and start vibration
        if (bellClip != null) audioSource.PlayOneShot(bellClip);

        if (vibrationRoutine != null) StopCoroutine(vibrationRoutine);
        vibrationRoutine = StartCoroutine(VibrationRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
            vibrationRoutine = null;
        }

        ResetBrakes();
    }

    private void RotateWheels()
    {
        if (infiniteStreet == null) return;

        float speed = infiniteStreet.speed * 10f;
        if (wheel_front != null) wheel_front.transform.Rotate(Vector3.down * speed * Time.deltaTime);
        if (wheel_back  != null) wheel_back.transform.Rotate(Vector3.down * speed * Time.deltaTime);
    }

    private IEnumerator VibrationRoutine()
    {
        while (true)
        {
            float vx = Mathf.Sin(Time.time * 18f) * 5f;
            float vy = Mathf.Cos(Time.time * 21f) * 5f;

            if (brake_left != null)
                brake_left.transform.localEulerAngles  = brakeLeftInitialEuler  + new Vector3(vx,  vy, 0f);
            if (brake_right != null)
                brake_right.transform.localEulerAngles = brakeRightInitialEuler + new Vector3(-vx, -vy, 0f);

            yield return null;
        }
    }

    private void ResetBrakes()
    {
        if (brake_left != null)  brake_left.transform.localEulerAngles  = brakeLeftInitialEuler;
        if (brake_right != null) brake_right.transform.localEulerAngles = brakeRightInitialEuler;
    }

    private void PlayPedalSound()
    {
        if (audioSource == null || pedalClip == null) return;
        audioSource.clip = pedalClip;
        audioSource.loop = true;
        audioSource.Play();
    }
}