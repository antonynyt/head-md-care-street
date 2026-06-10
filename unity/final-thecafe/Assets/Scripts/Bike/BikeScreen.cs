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

    // NEW: input tracking
    private bool fingerHeld;
    private bool heldOutsideBrake;

    private void Awake()
    {
        infiniteStreet = FindObjectOfType<InfiniteStreet>();
        audioSource = GetComponent<AudioSource>();

        if (brake_left != null)  brakeLeftInitialEuler  = brake_left.transform.localEulerAngles;
        if (brake_right != null) brakeRightInitialEuler = brake_right.transform.localEulerAngles;

        PlayIdlePedalSound();
    }

    private void Update()
    {
        RotateWheels();

        // NEW: keep vibration going while finger is held outside brakes
        if (fingerHeld && heldOutsideBrake)
        {
            if (vibrationRoutine == null)
                vibrationRoutine = StartCoroutine(VibrateBrakesRoutine(true));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        fingerHeld = true;

        GameObject hit = eventData.pointerCurrentRaycast.gameObject
                      ?? eventData.pointerPressRaycast.gameObject;

        bool hitBrake = IsChildOf(hit, brake_left) || IsChildOf(hit, brake_right);

        heldOutsideBrake = !hitBrake;

        if (heldOutsideBrake)
        {
            PlayBell();

            if (vibrationRoutine != null)
                StopCoroutine(vibrationRoutine);

            vibrationRoutine = StartCoroutine(VibrateBrakesRoutine(false));
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        fingerHeld = false;
        heldOutsideBrake = false;

        if (vibrationRoutine != null)
        {
            StopCoroutine(vibrationRoutine);
            vibrationRoutine = null;
        }

        ResetBrakes();
    }

    // ── Wheels ─────────────────────────────────────────────

    private void RotateWheels()
    {
        if (infiniteStreet == null) return;

        float speed = infiniteStreet.speed * 10f;

        if (wheel_front != null) wheel_front.transform.Rotate(Vector3.down * speed * Time.deltaTime);
        if (wheel_back  != null) wheel_back.transform.Rotate(Vector3.down * speed * Time.deltaTime);
    }

    // ── Vibration ──────────────────────────────────────────

    private IEnumerator VibrateBrakesRoutine(bool extended)
    {
        float baseDuration = bellClip != null ? bellClip.length : 0.5f;

        // NEW: extend duration while holding outside
        float duration = extended ? baseDuration + 1.5f : baseDuration;

        float elapsed = 0f;

        while (fingerHeld && (extended || elapsed < duration))
        {
            elapsed += Time.deltaTime;

            float strength = 1f;

            // optional: fade in/out smoother feel
            if (!extended)
                strength = 1f - (elapsed / duration);

            float vx = Mathf.Sin(Time.time * 18f) * 5f * strength;
            float vy = Mathf.Cos(Time.time * 21f) * 5f * strength;

            if (brake_left != null)
                brake_left.transform.localEulerAngles = brakeLeftInitialEuler + new Vector3(vx, vy, 0f);

            if (brake_right != null)
                brake_right.transform.localEulerAngles = brakeRightInitialEuler + new Vector3(-vx, -vy, 0f);

            yield return null;
        }

        ResetBrakes();
        vibrationRoutine = null;
    }

    private void ResetBrakes()
    {
        if (brake_left != null)
            brake_left.transform.localEulerAngles = brakeLeftInitialEuler;

        if (brake_right != null)
            brake_right.transform.localEulerAngles = brakeRightInitialEuler;
    }

    // ── Audio ─────────────────────────────────────────────

    private void PlayBell()
    {
        if (audioSource != null && bellClip != null)
            audioSource.PlayOneShot(bellClip);
    }

    private void PlayIdlePedalSound()
    {
        if (audioSource == null || pedalClip == null) return;

        audioSource.Stop();
        audioSource.clip = pedalClip;
        audioSource.loop = true;
        audioSource.volume = 10f;
        audioSource.Play();
    }

    // ── Helpers ───────────────────────────────────────────

    private static bool IsChildOf(GameObject obj, GameObject target)
    {
        if (obj == null || target == null) return false;

        Transform current = obj.transform;
        Transform t = target.transform;

        while (current != null)
        {
            if (current == t) return true;
            current = current.parent;
        }

        return false;
    }
}