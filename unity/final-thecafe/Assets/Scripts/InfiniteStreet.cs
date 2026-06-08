using UnityEngine;

public class InfiniteStreet : MonoBehaviour
{
    // two games objects, streetA and streetB
    public GameObject streetA;
    public GameObject streetB;
    // Create a variable speed variable to control the speed of the street
    public float speed = 10.0f;
    float streetLength = 250.0f;

    // Update is called once per frame
    void Update()
    {
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
        // positive x is the correct direction for the street to move in
        // constantly move the street to the right
        street.transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void CheckStreet(GameObject street)
    {
        // if the street's x position is less than -170, use the JumpStreet function to move it to the right of the other street
        if (street.transform.position.x < -300)
        {
            JumpStreet(street);
        }
    }

    void JumpStreet(GameObject street)
    {
        // Jump 100 to the left
        street.transform.Translate(Vector3.left * streetLength * 2f);
    }
}
