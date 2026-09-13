using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class CustomYarnCommands : MonoBehaviour
{
    public static float lastZoomOutTime = 0f;
    public static bool useCupFillForZoomOut = true;

    /// <summary> When true, the next <<ZoomOut>> command call is skipped (its effects
    /// were already applied early by CupInteractionDirector on an early finger release,
    /// and re-applying it with the plain node duration would stomp the extended timing). </summary>
    public static bool suppressNextZoomOut = false;

    public static UnityEvent<float> OnZoomOutCommand = new UnityEvent<float>();

    [YarnCommand("ZoomOut")]
    public static void ZoomOut(float zoomOutTime, bool useCupFill = true)
    {
        if (suppressNextZoomOut)
        {
            suppressNextZoomOut = false;
            Debug.Log("ZoomOut command suppressed (already applied early on release).");
            return;
        }

        ApplyZoomOut(zoomOutTime, useCupFill);
    }

    /// <summary> Applies the zoom-out/cup-drain timing. Called either by the Yarn
    /// <<ZoomOut>> command itself, or early by CupInteractionDirector when the finger
    /// is released before the current line finishes, using an extended duration. </summary>
    public static void ApplyZoomOut(float zoomOutTime, bool useCupFill = true)
    {
        Debug.Log($"Zooming out over {zoomOutTime} seconds, useCupFill={useCupFill}");
        lastZoomOutTime = zoomOutTime;
        useCupFillForZoomOut = useCupFill;

        if (CupFilling.Instance != null)
        {
            CupFilling.Instance.totalEmptyTime = zoomOutTime;

            if (!CupFilling.Instance.IsFilling)
                CupFilling.Instance.SetEmptySpeedFromCurrentFill();
        }

        OnZoomOutCommand.Invoke(zoomOutTime);
    }
}