using UnityEngine;
using UnityEngine.EventSystems;

public class CupFilling : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Transform fillTarget;
    [SerializeField] private float fillSpeed = 1f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 1f;

    private bool isPressing;
    private float currentY = 0f;

    private void Update()
    {
        float delta = fillSpeed * Time.deltaTime;

        if (isPressing)
            currentY += delta;
        else
            currentY -= delta;

        currentY = Mathf.Clamp(currentY, minY, maxY);

        if (fillTarget != null)
        {
            Vector3 scale = fillTarget.localScale;
            scale.y = currentY;
            fillTarget.localScale = scale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        Debug.Log("Started filling the cup.");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        Debug.Log("Stopped filling the cup.");
    }
}
