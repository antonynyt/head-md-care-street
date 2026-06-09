using UnityEngine;

public class TargetFramerate : MonoBehaviour
{
    [SerializeField] private int targetFramerate = 60;

    private void Awake()
    {
        Application.targetFrameRate = targetFramerate;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
