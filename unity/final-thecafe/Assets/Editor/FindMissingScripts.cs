#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void Run()
    {
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in all)
        {
            foreach (Component c in go.GetComponents<Component>())
            {
                if (c == null)
                    Debug.LogError("Missing script on: " + go.name + " (scene: " + go.scene.name + ")", go);
            }
        }
    }
}
#endif