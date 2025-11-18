using UnityEngine;
using UnityEngine.InputSystem;

public class playerScript : MonoBehaviour
{
    [Header("Tunable Variables")]
    public float acceleration = 5.0f;
    public float maxSpeed = 3.0f;

    [Header("References")]
    public Rigidbody rb;


    void Awake()
    {
        //Automatically assign references if not set in inspector
        if (rb == null) { rb = GetComponent<Rigidbody>(); }

    }

    //Use FixedUpdate for physics interactions
    void FixedUpdate()
    {
        //Movement code
        float moveHorizontal = Mouse.current.delta.ReadValue().x;
        float moveVertical = Mouse.current.delta.ReadValue().y;

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        rb.AddForce(movement * acceleration);

        // Maximum horizontal movement speed
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVel.magnitude > maxSpeed)
        {
            Vector3 clamped = horizontalVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
        }
    }

    void Update()
    {
        
    }
}
