using UnityEngine;

public class InfiniteStreet : MonoBehaviour
{
    // two games objects, streetA and streetB
    public GameObject streetA;
    public GameObject streetB;
    // Create a variable speed variable to control the speed of the street
    public float speed = 5.0f;
    float streetLength = 100.0f;

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
        // negative x is the correct direction for the street to move in
        // constantly move the street downwards
        street.transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    void CheckStreet(GameObject street)
    {
        // if the street's x position is less than -50, use the JumpStreet function to move it to the right of the other street
        if (street.transform.position.x < -50)
        {
            JumpStreet(street);
        }
    }

    void JumpStreet(GameObject street)
    {
        // Jump 100 to the right
        street.transform.Translate(Vector3.right * streetLength * 2f);
    }
}
