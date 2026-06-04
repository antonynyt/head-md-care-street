using UnityEngine;

public class CupFillCameraController : MonoBehaviour
{
    [SerializeField] private CupFilling cup;
    [SerializeField] private Transform cameraToMove;
    [SerializeField] private float cameraEndY = 5f;
    [SerializeField] private float cameraEndZ = -3f;
    [SerializeField] private AnimationCurve cameraCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 cameraStartPos;
    private Vector3 cameraEndPos;
    private Vector3 cameraTargetPos;

    private bool isPressing;
    private bool isZoomingOut;
    private float releasedFill01;

    private void Awake()
    {
        if (cameraToMove == null)
            cameraToMove = transform;
    }

    private void Start()
    {
        if (cup == null || cameraToMove == null)
        {
            Debug.LogWarning("CupFillCameraController: assign CupFilling and cameraToMove.");
            enabled = false;
            return;
        }

        cameraStartPos = cameraToMove.position;
        cameraEndPos = new Vector3(cameraStartPos.x, cameraEndY, cameraEndZ);
        cameraTargetPos = cameraStartPos;

        cup.OnFillStarted.AddListener(HandleStart);
        cup.OnFillProgress.AddListener(HandleProgress);
        cup.OnFillReleased.AddListener(HandleReleased);
    }

    private void OnDestroy()
    {
        if (cup == null) return;

        cup.OnFillStarted.RemoveListener(HandleStart);
        cup.OnFillProgress.RemoveListener(HandleProgress);
        cup.OnFillReleased.RemoveListener(HandleReleased);
    }

    private void HandleStart()
    {
        isPressing = true;
        isZoomingOut = false;
        releasedFill01 = 0f;

        cameraStartPos = cameraToMove.position;
        cameraEndPos = new Vector3(cameraStartPos.x, cameraEndY, cameraEndZ);
        cameraTargetPos = cameraStartPos;
    }

    private void HandleReleased(float releasedFill)
    {
        isPressing = false;

        if (releasedFill <= 0f)
        {
            isZoomingOut = false;
            cameraToMove.position = cameraStartPos;
            return;
        }

        releasedFill01 = releasedFill;
        cameraTargetPos = Vector3.Lerp(cameraStartPos, cameraEndPos, releasedFill01);
        isZoomingOut = true;
    }

    private void HandleProgress(float fill01)
    {
        if (isPressing || !isZoomingOut)
            return;

        float emptyProgress01 = Mathf.Clamp01((releasedFill01 - fill01) / Mathf.Max(0.0001f, releasedFill01));
        float t = cameraCurve.Evaluate(emptyProgress01);

        cameraToMove.position = Vector3.Lerp(cameraStartPos, cameraTargetPos, t);

        if (fill01 <= 0f)
            isZoomingOut = false;
    }
}