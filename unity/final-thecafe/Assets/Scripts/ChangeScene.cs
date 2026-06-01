using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class ChangeScene : MonoBehaviour
{
    [YarnCommand("change_scene")]
    public static void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}