using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    private const float SampleInterval = 0.5f;
    private static FpsCounter instance;

    private Font labelFont;
    private GUIStyle labelStyle;
    private float elapsedTime;
    private int frameCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("FPS Counter");
        DontDestroyOnLoad(root);
        root.AddComponent<FpsCounter>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CreateStyle();
    }

    private void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;

        if (elapsedTime < SampleInterval)
            return;

        float fps = frameCount / elapsedTime;
        fpsDisplay = $"{fps:0} FPS";
        frameCount = 0;
        elapsedTime = 0f;
    }

    private string fpsDisplay = "FPS";

    private void OnGUI()
    {
        if (labelStyle == null)
            return;

        GUI.Label(new Rect(16f, 16f, 120f, 30f), fpsDisplay, labelStyle);
    }

    private void CreateStyle()
    {
        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        labelStyle = new GUIStyle()
        {
            font = labelFont,
            fontSize = 22,
            alignment = TextAnchor.UpperLeft,
            normal =
            {
                textColor = new Color(1f, 1f, 1f, 0.95f)
            }
        };
    }
}