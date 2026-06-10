using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public CupFilling cupFilling;

    public float pressThreshold = 0.5f;

    private bool released = false;
    private bool isPressing = false;
    private bool longPressTriggered = false;
    private bool zoomInFired = false;
    private Coroutine pressCoroutine;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isPressing || released || zoomInFired) return;

        // Step 1 crossed — deactivate Pressed, fire ZoomIn once
        if (cupFilling != null && cupFilling.CurrentStep >= 1)
        {
            animator.SetBool("Pressed", false);
            animator.SetTrigger("ZoomIn");
            zoomInFired = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (released) return;

        isPressing = true;
        longPressTriggered = false;
        zoomInFired = false;

        pressCoroutine = StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        animator.SetBool("Pressed", false);

        if (pressCoroutine != null)
            StopCoroutine(pressCoroutine);

        if (!longPressTriggered)
        {
            // Tap — jiggle hint
            if (cupFilling != null)
                cupFilling.JiggleCup();
        }
        else
        {
            if (cupFilling != null)
            {
                cupFilling.EndFill();

                if (cupFilling.CurrentStep >= 1)
                {
                    animator.SetTrigger("ZoomOut");
                    released = true;
                }
                // else: drain naturally, can try again
            }
        }
    }

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSeconds(pressThreshold);

        if (isPressing && !released)
        {
            longPressTriggered = true;
            animator.SetBool("Pressed", true);

            if (cupFilling != null)
                cupFilling.BeginFill();
        }
    }
}