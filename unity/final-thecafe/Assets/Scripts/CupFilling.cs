using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CupFilling : MonoBehaviour
{
    public static CupFilling Instance { get; private set; }

    [SerializeField] private string fillPropertyName = "_Fill";
    [SerializeField] private int fillSteps = 3;
    [SerializeField] private float totalFillTime = 10f;   // seconds to fill from 0 to 1
    [SerializeField] public float totalEmptyTime = 10f;  // seconds to empty from CURRENT fill to 0

    public UnityEvent OnFillStarted = new UnityEvent();
    public UnityEvent<float> OnFillProgress = new UnityEvent<float>();
    public UnityEvent<float> OnFillReleased = new UnityEvent<float>();

    private bool isFilling;
    public float currentFill01 = 0f;
    private float fillSpeed;          // cached 1 / totalFillTime
    private float currentEmptySpeed;  // computed at start of each drain cycle

    private Renderer liquidRenderer;
    private MaterialPropertyBlock propertyBlock;

    private float StepSize => 1f / Mathf.Max(1, fillSteps);
    public bool IsFilling => isFilling;
    public int CurrentStep { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResolveLiquidRenderer();
        fillSpeed = 1f / Mathf.Max(0.01f, totalFillTime);
        SetEmptySpeedFromCurrentFill();   // in case the cup starts with some fill
        ApplyFill();
    }

    private void Update()
    {
        if (!isFilling) {
            //stop fill sound if not filling
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying) {
                audioSource.Stop();
            }
        }
        if (isFilling)
        {
            currentFill01 += fillSpeed * Time.deltaTime;
            if (currentFill01 >= 1f)
            {
                // small bounce effect when reaching full fill
                currentFill01 = 1.01f + Mathf.Sin(Time.time * 5f) * 0.01f;
            }
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);
            // play
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && !audioSource.isPlaying)            {
                audioSource.Play();
            }
        }
        else if ( CurrentStep < 1 )
        {
            currentFill01 -= fillSpeed * Time.deltaTime;
            currentFill01 = Mathf.Clamp01(currentFill01);
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);
        } else
        {
            // Drain at a constant speed that was set when emptying began
            currentFill01 -= currentEmptySpeed * Time.deltaTime;
            currentFill01 = Mathf.Clamp01(currentFill01);
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);
        }

        CurrentStep = Mathf.Clamp(Mathf.FloorToInt(currentFill01 / StepSize), 0, fillSteps);
    }

    public void BeginFill()
    {
        if (isFilling) return;
        if (Mathf.Approximately(currentFill01, 1f)) return;

        isFilling = true;
        OnFillStarted.Invoke();
    }

    public void EndFill()
    {
        if (!isFilling) return;
        isFilling = false;
        SetEmptySpeedFromCurrentFill();   // lock the drain speed based on current fill
        OnFillReleased.Invoke(currentFill01);
    }

    public void ResetFill(bool clearFill = true)
    {
        EndFill();
        if (clearFill)
        {
            currentFill01 = 0f;
            ApplyFill();
            OnFillProgress.Invoke(currentFill01);
        }
    }

    /// <summary> Computes the drain speed so it takes totalEmptyTime to reach 0 from currentFill01. </summary>
    public void SetEmptySpeedFromCurrentFill()
    {
        float safeTime = Mathf.Max(0.0001f, totalEmptyTime);
        currentEmptySpeed = currentFill01 / safeTime;
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
        Debug.LogWarning("CupFilling: No child named 'Liquid' found.");
    }

    private void ApplyFill()
    {
        if (liquidRenderer == null) return;
        liquidRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(fillPropertyName, currentFill01);
        liquidRenderer.SetPropertyBlock(propertyBlock);
    }

    public void JiggleCup()
    {
        // jiggle the cup by briefly modifying the fill level in a way that doesn't affect the actual fill state
        StartCoroutine(JiggleRoutine());
    }

    private IEnumerator JiggleRoutine()
    {
        float jiggleAmount = 0.05f;
        float jiggleDuration = 0.5f;

        float elapsed = 0f;
        while (elapsed < jiggleDuration)
        {
            elapsed += Time.deltaTime;
            float offset = Mathf.Sin(elapsed / jiggleDuration * Mathf.PI * 2) * jiggleAmount;
            propertyBlock.SetFloat(fillPropertyName, currentFill01 + offset);
            liquidRenderer.SetPropertyBlock(propertyBlock);
            yield return null;
        }

        // Ensure we end on the correct fill level
        ApplyFill();
    }
}