using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class CustomYarnCommands : MonoBehaviour
{
    public static float lastZoomOutTime = 0f;
    public static bool useCupFillForZoomOut = true;

    public static UnityEvent<float> OnZoomOutCommand = new UnityEvent<float>();   

    [YarnCommand("ZoomOut")]
    public static void ZoomOut(float zoomOutTime, bool useCupFill = true)
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