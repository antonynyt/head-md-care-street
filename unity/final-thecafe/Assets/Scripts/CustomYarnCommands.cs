using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

public class CustomYarnCommands : MonoBehaviour
{
    public static float lastZoomOutTime = 0f;

    public static UnityEvent<float> OnZoomOutCommand = new UnityEvent<float>();   

    [YarnCommand("ZoomOut")]
    public static void ZoomOut(float zoomOutTime)
    {
        // Implement zoom out logic here
        Debug.Log($"Zooming out over {zoomOutTime} seconds");

        lastZoomOutTime = zoomOutTime;
        OnZoomOutCommand.Invoke(zoomOutTime);
    }
}