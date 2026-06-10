using UnityEngine;

public class InfiniteStreet : MonoBehaviour
{
    public GameObject streetA;
    public GameObject streetB;
    public float speed = 8.0f;
    public float streetLength = 250.0f;  // made public so you can tweak in Inspector

    void Awake()
    {
        if (streetA == null || streetB == null)
        {
            if (transform.childCount >= 2)
            {
                if (streetA == null) streetA = transform.GetChild(0).gameObject;
                if (streetB == null) streetB = transform.GetChild(1).gameObject;
            }
            else
            {
                Debug.LogWarning("InfiniteStreet needs two street objects assigned or two direct children.", this);
            }
        }
    }

    void Update()
    {
        if (streetA == null || streetB == null) return;

        MoveStreet(streetA);
        MoveStreet(streetB);
        CheckStreet(streetA, streetB);
        CheckStreet(streetB, streetA);
    }

    void MoveStreet(GameObject street)
    {
        street.transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void CheckStreet(GameObject street, GameObject other)
    {
        if (street.transform.position.x <= -250.0f) // when street goes off-screen to the left
        {
            // Reposition absolutely relative to the other segment — no drift
            float newX = other.transform.position.x + streetLength;
            street.transform.position = new Vector3(
                newX,
                street.transform.position.y,
                street.transform.position.z
            );
        }
    }
}