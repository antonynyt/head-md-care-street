using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public CupFilling cupFilling; // drag reference in Inspector

    bool released = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        
        if (cupFilling != null && released == false)
        {
            GetComponent<Animator>().SetTrigger("ZoomIn");
            cupFilling.BeginFill();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (cupFilling != null) 
        {
            GetComponent<Animator>().SetTrigger("ZoomOut");
            cupFilling.EndFill();
            released = true;
        }
    }
}