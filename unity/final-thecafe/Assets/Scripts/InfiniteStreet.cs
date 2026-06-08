using UnityEngine;

public class InfiniteStreet : MonoBehaviour
{
    // two games objects, streetA and streetB
    public GameObject streetA;
    public GameObject streetB;
    // Create a variable speed variable to control the speed of the street
    public float speed = 8.0f;
    float streetLength = 250.0f;

    void Awake()
    {
        if (streetA == null || streetB == null)
        {
            if (transform.childCount >= 2)
            {
                if (streetA == null)
                {
                    streetA = transform.GetChild(0).gameObject;
                }

                if (streetB == null)
                {
                    streetB = transform.GetChild(1).gameObject;
                }
            }
            else
            {
                Debug.LogWarning("InfiniteStreet needs two street root objects assigned, or at least two direct children on the same GameObject.", this);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (streetA == null || streetB == null)
        {
            return;
        }

        // move both streetA and streetB using the MoveStreet function
        MoveStreet(streetA);
        MoveStreet(streetB);
        // check both streetA and streetB using the CheckStreet function
        CheckStreet(streetA);
        CheckStreet(streetB);
    }

    // Move Street Method
    void MoveStreet(GameObject street)
    {
        if (street == null)
        {
            return;
        }

        // positive x is the correct direction for the street to move in
        // constantly move the street to the right
        street.transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void CheckStreet(GameObject street)
    {
        if (street == null)
        {
            return;
        }

        // if the street's x position is less than -170, use the JumpStreet function to move it to the right of the other street
        if (street.transform.position.x < -250.0f)
        {
            JumpStreet(street);
        }
    }

    void JumpStreet(GameObject street)
    {
        if (street == null)
        {
            return;
        }

        // Jump 100 to the left
        street.transform.Translate(Vector3.left * streetLength * 2f);
    }
}
