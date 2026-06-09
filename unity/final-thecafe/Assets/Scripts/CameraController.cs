using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CameraController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public CupFilling cupFilling; // drag reference in Inspector
    bool released = false;

    public float pressThreshold = 0.5f;

    private bool isPressing;
    private bool longPressTriggered;
    private Coroutine pressCoroutine;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        longPressTriggered = false;

        pressCoroutine = StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;

        if (pressCoroutine != null)
            StopCoroutine(pressCoroutine);

        if (!longPressTriggered)
        {
            Debug.Log("Tap");

            // jiggle the cup
            if (cupFilling != null)
            {
                cupFilling.JiggleCup();
            }

        }
        else
        {
            Debug.Log("Press End");

            // end filling
            if (cupFilling != null) 
            {
                GetComponent<Animator>().SetTrigger("ZoomOut");
                cupFilling.EndFill();
                released = true;
            }
        }
    }

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSeconds(pressThreshold);

        if (isPressing)
        {
            longPressTriggered = true;
            
            //start filling
            if (cupFilling != null && released == false)
            {
                GetComponent<Animator>().SetTrigger("ZoomIn");
                cupFilling.BeginFill();
            }
        }
    }
    


    // public CupFilling cupFilling; // drag reference in Inspector


    // public void OnPointerDown(PointerEventData eventData)
    // {

    //     // if it's just a tap don't fill (tap is 0.5s or less)
        
    //     if (cupFilling != null && released == false && eventData.clickTime > 0.5f)
    //     {
    //         GetComponent<Animator>().SetTrigger("ZoomIn");
    //         cupFilling.BeginFill();
    //     }
    // }

    // public void OnPointerUp(PointerEventData eventData)
    // {
    //     if (cupFilling != null && eventData.clickTime > 0.5f) 
    //     {
    //         GetComponent<Animator>().SetTrigger("ZoomOut");
    //         cupFilling.EndFill();
    //         released = true;
    //     }
    // }
}
