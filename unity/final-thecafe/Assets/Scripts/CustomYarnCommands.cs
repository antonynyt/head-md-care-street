using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;
using UnityEngine.SceneManagement;

public class CustomYarnCommands : MonoBehaviour
{
    public static float lastZoomOutTime = 0f;

    public static UnityEvent<float> OnZoomOutCommand = new UnityEvent<float>();   

    [YarnCommand("ZoomOut")]
    public static void ZoomOut(float zoomOutTime)
    {
        Debug.Log($"Zooming out over {zoomOutTime} seconds");
        lastZoomOutTime = zoomOutTime;

        if (CupFilling.Instance != null)
        {
            CupFilling.Instance.totalEmptyTime = zoomOutTime;

            // If the cup is already draining, recalculate the speed immediately
            if (!CupFilling.Instance.IsFilling)
                CupFilling.Instance.SetEmptySpeedFromCurrentFill();
        }

        OnZoomOutCommand.Invoke(zoomOutTime);
    }

    [YarnCommand("change_scene")]
    public static void LoadScene(string sceneName)
    {
        // Sauvegarder avant de quitter la scène
        if (CupInteractionDirector.Instance != null)
            CupInteractionDirector.Instance.SaveState();
        
        SceneManager.LoadScene(sceneName);
    }
}