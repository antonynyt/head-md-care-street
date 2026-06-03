using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CupFillingShader : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private string fillPropertyName = "_Fill";
    [SerializeField] private float fillSpeed = 0.1f;
    [SerializeField] private float emptySpeedMult = 2f;

    [SerializeField] private int fillSteps = 3;
    [SerializeField] private float stepDuration = 5f;
    [SerializeField] private AnimationCurve stepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isPressing;
    private float currentFill01 = 0f;

    private Coroutine fillRoutine;
    private Renderer liquidRenderer;
    private MaterialPropertyBlock propertyBlock;

    private float StepSize => 1f / Mathf.Max(1, fillSteps);

    private void Start()
    {

        propertyBlock = new MaterialPropertyBlock();
        ResolveLiquidRenderer();
        ApplyFill();
    }

    private void Update()
    {
        if (!isPressing)
        {
            currentFill01 -= fillSpeed * emptySpeedMult * Time.deltaTime;
            currentFill01 = Mathf.Clamp01(currentFill01);
            ApplyFill();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressing = true;

        if (fillRoutine == null)
            fillRoutine = StartCoroutine(FillStepRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
    }

    private IEnumerator FillStepRoutine()
    {
        while (isPressing && currentFill01 < 1f)
        {
            int nextStepIndex = Mathf.Clamp(
                Mathf.FloorToInt(currentFill01 / StepSize) + 1,
                1, fillSteps
            );

            float stepStartFill = currentFill01;
            float stepEndFill = Mathf.Min(StepSize * nextStepIndex, 1f);

            float elapsed = 0f;
            while (isPressing && elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = stepCurve.Evaluate(Mathf.Clamp01(elapsed / stepDuration));
                currentFill01 = Mathf.Lerp(stepStartFill, stepEndFill, t);
                ApplyFill();
                yield return null;
            }

            if (!isPressing) break;

            currentFill01 = stepEndFill;
            ApplyFill();

            if (Mathf.Approximately(currentFill01, 1f)) break;
        }

        fillRoutine = null;
    }

    private void ResolveLiquidRenderer()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLowerInvariant().Contains("liquid"))
            {
                liquidRenderer = child.GetComponent<Renderer>();
                if (liquidRenderer != null) return;
            }
        }

        Debug.LogWarning("CupFillingShader: No child named 'Liquid' found.");
    }

    private void ApplyFill()
    {
        if (liquidRenderer == null) return;

        liquidRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(fillPropertyName, currentFill01);
        liquidRenderer.SetPropertyBlock(propertyBlock);
    }
}