using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CupFilling : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    // TODO: replace steps speed with audio length
    // TODO: replace emptying with the dialog audio

    // TODO: get the audio from the YarnSpinner and use its length for step timing

    [SerializeField] private Transform fillTarget;
    [SerializeField] private float fillSpeed = 1f;
    [SerializeField] private float emptySpeedMult = 1f;
    [SerializeField] private float minYFill = 0f;
    [SerializeField] private float maxYFill = 1f;

    // steps for filling
    [SerializeField] private int fillSteps = 3;
    [SerializeField] private float stepDuration = 0.2f;
    [SerializeField] private AnimationCurve stepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // camera movement
    [SerializeField] private Transform cameraToMove;
    [SerializeField] private float cameraEndY = 5f;
    [SerializeField] private float cameraEndZ = -3f;

    private bool isPressing;
    private bool hasPressedOnce;
    private bool cameraMovementEnabled;

    private float currentY = 0f;
    private float releasedFill01 = 0f;
    private float releaseStartY = 0f;

    private Vector3 cameraStartPos;
    private Vector3 cameraEndPos;

    private Coroutine fillRoutine;

    private float StepSize => (maxYFill - minYFill) / Mathf.Max(1, fillSteps);

    private void Start()
    {
        cameraMovementEnabled = fillSteps >= 1;

        if (cameraToMove != null && cameraMovementEnabled)
        {
            cameraStartPos = cameraToMove.position;
            cameraEndPos = new Vector3(cameraStartPos.x, cameraEndY, cameraEndZ);
            cameraToMove.position = cameraStartPos;
        }
    }

    private void Update()
    {
        if (!isPressing)
        {
            float delta = fillSpeed * Time.deltaTime;
            currentY -= delta * emptySpeedMult;
            currentY = Mathf.Clamp(currentY, minYFill, maxYFill);
        }

        ApplyFillVisuals();

        if (cameraToMove != null && cameraMovementEnabled && hasPressedOnce && !isPressing)
        {
            float empty01 = 1f - Mathf.InverseLerp(minYFill, releaseStartY, currentY);
            float cameraT = releasedFill01 * empty01;
            cameraToMove.position = Vector3.Lerp(cameraStartPos, cameraEndPos, cameraT);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;
        hasPressedOnce = true;

        if (fillRoutine == null)
            fillRoutine = StartCoroutine(FillStepRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        releaseStartY = currentY;

        float firstStepThreshold = minYFill + StepSize;
        releasedFill01 = currentY >= firstStepThreshold
            ? Mathf.InverseLerp(minYFill, maxYFill, currentY)
            : 0f;
    }

    private IEnumerator FillStepRoutine()
    {
        while (isPressing && currentY < maxYFill)
        {
            int nextStepIndex = Mathf.Clamp(
                Mathf.FloorToInt((currentY - minYFill) / StepSize) + 1,
                1,
                fillSteps
            );

            float stepStartY = currentY;
            float stepEndY = Mathf.Min(minYFill + StepSize * nextStepIndex, maxYFill);

            float elapsed = 0f;
            while (isPressing && elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                t = EaseOutStrong(t);

                currentY = Mathf.Lerp(stepStartY, stepEndY, t);
                ApplyFillVisuals();
                yield return null;
            }

            if (!isPressing)
                break;

            currentY = stepEndY;
            ApplyFillVisuals();

            Debug.Log($"Fill step {nextStepIndex} reached");

            if (Mathf.Approximately(currentY, maxYFill))
                break;
        }

        fillRoutine = null;
    }

    private void ApplyFillVisuals()
    {
        currentY = Mathf.Clamp(currentY, minYFill, maxYFill);

        if (fillTarget != null)
        {
            Vector3 scale = fillTarget.localScale;
            scale.y = currentY;
            fillTarget.localScale = scale;
        }
    }

    private float EaseOutStrong(float t)
    {
        return Mathf.Clamp01(stepCurve.Evaluate(Mathf.Clamp01(t)));
    }
}
