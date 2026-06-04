using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class CupFilling : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // TODO: replace steps speed with audio length
    // TODO: replace emptying with the dialog audio
    // TODO: get the audio from the YarnSpinner and use its length for step timing
    
    [SerializeField] private string fillPropertyName = "_Fill";
    [SerializeField] private float fillSpeed = 0.1f;
    [SerializeField] private float emptySpeedMult = 2f;

    [SerializeField] private int fillSteps = 3;
    [SerializeField] private float stepDuration = 5f;
    [SerializeField] private AnimationCurve stepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Events for external listeners (camera controllers, etc.)
    public UnityEvent OnFillStarted = new UnityEvent();
    public UnityEvent<float> OnFillProgress = new UnityEvent<float>();
    public UnityEvent<float> OnFillReleased = new UnityEvent<float>();

    private bool isPressing;
    private bool isLocked; // prevents interaction until the next sequence
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
            OnFillProgress.Invoke(currentFill01);
        }

        CurrentStep = Mathf.Clamp(Mathf.FloorToInt(currentFill01 / StepSize), 0, fillSteps);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isPressing = true;
        OnFillStarted.Invoke();

        if (fillRoutine == null)
            fillRoutine = StartCoroutine(FillStepRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isPressing = false;
        isLocked = true; // lock immediately after the first release

        if (CurrentStep < 1)
        {
            OnFillReleased.Invoke(0f);
            return;
        }

        OnFillReleased.Invoke(currentFill01);
    }

    public void ResetForNextSequence(bool clearFill = true)
    {
        isLocked = false;
        isPressing = false;

        if (clearFill)
        {
            currentFill01 = 0f;
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);
        }
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
                OnFillProgress.Invoke(currentFill01);

                yield return null;
            }

            if (!isPressing) break;

            currentFill01 = stepEndFill;
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);

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

    public int CurrentStep { get; private set; }

    public bool IsLocked => isLocked;

    public void SetStepDuration(float duration)
    {
        stepDuration = Mathf.Max(0.01f, duration);
    }

    public void SetEmptyDuration(float duration, float startingFill01)
    {
        duration = Mathf.Max(0.01f, duration);
        startingFill01 = Mathf.Clamp01(startingFill01);

        if (fillSpeed <= 0.0001f)
        {
            emptySpeedMult = 1f;
            return;
        }

        emptySpeedMult = startingFill01 / (fillSpeed * duration);
    }
}