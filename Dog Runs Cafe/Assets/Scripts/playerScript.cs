using UnityEngine;
using UnityEngine.InputSystem;

public class playerScript : MonoBehaviour
{
    [Header("Tunable Variables")]

    //Horizontal Movement Mode
    public float acceleration = 5.0f;
    public float maxSpeed = 3.0f;

    //Rotational Movement Mode
    public float rotationalAcceleration = 5.0f;

    [Header("States")]
    public string currentState = "Idle";

    [Header("References")]
    public GameObject pivotPoint;
    public Rigidbody pivotRb;


    void Awake()
    {
        //Automatically assign references if not set in inspector
        if (pivotRb == null && pivotPoint != null) { pivotRb = pivotPoint.GetComponent<Rigidbody>(); }
    }

    //Use FixedUpdate for physics interactions
    void FixedUpdate()
    {
        if (currentState == "Disabled")
        {
            return;
        }
        else if(currentState == "Idle")
        {
            //Trash code
            ToggleLockedRotation(true);    

            //Horizontal Movement code
            float moveHorizontal = Mouse.current.delta.ReadValue().x;
            float moveVertical = Mouse.current.delta.ReadValue().y;

            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            //pivotRb.AddForce(movement * acceleration);
            pivotPoint.GetComponent<Transform>().Translate(movement * acceleration * Time.fixedDeltaTime);

            // Maximum horizontal movement speed
            Vector3 horizontalVel = new Vector3(pivotRb.linearVelocity.x, 0f, pivotRb.linearVelocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontalVel.normalized * maxSpeed;
                pivotRb.linearVelocity = new Vector3(clamped.x, pivotRb.linearVelocity.y, clamped.z);
            }

            //Switch state to Grab if left mouse button is pressed
        
            //Switch state to Grab if left mouse button is held down
        }
        else if (currentState == "Grab")
        {
            
        }
        else if (currentState == "Rotate")
        {
            //Trash code
            ToggleLockedRotation(false);    

            //Rotational Movement code
            float moveHorizontal = Mouse.current.delta.ReadValue().x;
            float moveVertical = Mouse.current.delta.ReadValue().y;

            Vector3 movement = new Vector3(moveVertical, moveHorizontal, 0.0f);
            //pivotRb.AddTorque(movement * rotationalAcceleration);
            pivotPoint.GetComponent<Transform>().Rotate(movement * rotationalAcceleration);
        }
        
    }

    void Update()
    {
        DeveloperSwitchState();

    }

    void ToggleLockedRotation(bool isLocked)
    {
        // //Horizontal Movement
        // if(isLocked)
        // {
        //     pivotRb.constraints = RigidbodyConstraints.FreezeRotation;
        // }
        // //Rotational Movement
        // else
        // {
        //     pivotRb.constraints = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePosition;
        // }
            
    }

    void DeveloperSwitchState()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentState = "Idle";
            ToggleLockedRotation(true);
            Debug.Log("Developer switched to Idle state");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentState = "Grab";
            ToggleLockedRotation(true);
            Debug.Log("Developer switched to Grab state");
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentState = "Rotate";
            ToggleLockedRotation(false);
            Debug.Log("Developer switched to Rotate state");
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            currentState = "Disabled";
            ToggleLockedRotation(false);
            Debug.Log("Developer switched to Disabled state");
        }
    }
}
