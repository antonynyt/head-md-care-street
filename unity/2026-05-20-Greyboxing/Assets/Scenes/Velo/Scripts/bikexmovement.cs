using UnityEngine;
using UnityEngine.EventSystems;
public class bikexmovement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField]
    float moveSpeed = 20f;

    bool isMoving;

    void Update()
    {
        if (isMoving)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isMoving = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isMoving = false;
    }
}
