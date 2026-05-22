using UnityEngine;
using UnityEngine.EventSystems;

public class CupFilling : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Transform fillTarget;
    [SerializeField] private float fillSpeed = 1f;
    [SerializeField] private float emptyMultiplier = 1f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 1f;

    [SerializeField] private Transform cameraToMove;
    [SerializeField] private float cameraEndY = 5f;
    [SerializeField] private float cameraEndZ = -3f;

    private bool isPressing;
    private bool hasPressedOnce;

    private float currentY = 0f;
    private float releasedFill01 = 0f;
    private float releaseStartY = 0f;

    private Vector3 cameraStartPos;
    private Vector3 cameraEndPos;

    private void Start()
    {
        if (cameraToMove != null)
        {
            cameraStartPos = cameraToMove.position;
            cameraEndPos = new Vector3(cameraStartPos.x, cameraEndY, cameraEndZ);
            cameraToMove.position = cameraStartPos;
        }
    }

    private void Update()
    {
        float delta = fillSpeed * Time.deltaTime;

        if (isPressing)
            currentY += delta;
        else
            currentY -= delta * emptyMultiplier;

        currentY = Mathf.Clamp(currentY, minY, maxY);

        if (fillTarget != null)
        {
            Vector3 scale = fillTarget.localScale;
            scale.y = currentY;
            fillTarget.localScale = scale;
        }

        if (cameraToMove != null && hasPressedOnce && !isPressing)
        {
            float empty01 = 1f - Mathf.InverseLerp(minY, releaseStartY, currentY);
            float cameraT = releasedFill01 * empty01;
            cameraToMove.position = Vector3.Lerp(cameraStartPos, cameraEndPos, cameraT);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        hasPressedOnce = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        releaseStartY = currentY;
        releasedFill01 = Mathf.InverseLerp(minY, maxY, currentY);
    }
}
