using UnityEngine;
using UnityEngine.InputSystem;

public class playerScript : MonoBehaviour
{
    [Header("Tunable Variables")]
    public float acceleration = 5.0f;
    public float maxSpeed = 3.0f;
    public float rotationalAcceleration = 5.0f;

    [Header("Rotation Limits (degrees)")]
    public float maxPitch = 30f; // up/down
    public float maxYaw = 45f;   // left/right

    [Header("Grab / Hold")]
    public float holdThreshold = 0.25f; // seconds to consider a hold

    [Header("States")]
    public string currentState = "Idle";

    [Header("References")]
    public GameObject pivotPoint;
    public Rigidbody pivotRb;

    // internal
    float leftDownTime = -1f;
    bool holdStarted = false;

    // reference rotation used to clamp pitch/yaw (set when entering Grab)
    Quaternion grabReferenceRotation = Quaternion.identity;

    void Awake()
    {
        if (pivotRb == null && pivotPoint != null)
            pivotRb = pivotPoint.GetComponent<Rigidbody>();

        // ensure default rotation lock for Idle
        ToggleLockedRotation(true);

        // Set the rigidbody center of mass to the pivot point so AddTorque rotates about that pivot
        UpdateCenterOfMass();

        // initial reference rotation
        if (pivotRb != null) grabReferenceRotation = pivotRb.rotation;
    }

    void Update()
    {
        HandleLeftClickTapAndHold();
        DeveloperSwitchState();
    }

    void FixedUpdate()
    {
        if (pivotRb == null) return;

        // keep center of mass aligned in case pivotPoint moves at runtime
        UpdateCenterOfMass();

        var mouse = Mouse.current;
        Vector2 delta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

        if (currentState == "Idle" || currentState == "Grab")
        {
            // Horizontal movement via forces (same behavior in Idle and Grab)
            Vector3 movement = new Vector3(delta.x, 0f, delta.y);
            pivotRb.AddForce(movement * acceleration, ForceMode.Acceleration);

            // clamp horizontal velocity (use Rigidbody.velocity)
            Vector3 horizontalVel = new Vector3(pivotRb.linearVelocity.x, 0f, pivotRb.linearVelocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontalVel.normalized * maxSpeed;
                pivotRb.linearVelocity = new Vector3(clamped.x, pivotRb.linearVelocity.y, clamped.z);
            }
        }
        else if (currentState == "Rotate")
        {
            // Rotation via torque while holding
            // Map mouse delta to pitch (X) and yaw (Y) only; prevent any roll (Z)
            Vector3 torque = new Vector3(delta.y, delta.x, 0f); // X = pitch, Y = yaw, Z = 0 (no roll)
            pivotRb.AddTorque(torque * rotationalAcceleration, ForceMode.Acceleration);

            // keep the pivot's position fixed while allowing rotation:
            pivotRb.linearVelocity = Vector3.zero;

            // ensure no roll: zero Z component of angular velocity
            Vector3 av = pivotRb.angularVelocity;
            pivotRb.angularVelocity = new Vector3(av.x, av.y, 0f);

            // Clamp rotation relative to the grabReferenceRotation
            ClampPitchYawToLimits();
        }
    }

    void UpdateCenterOfMass()
    {
        if (pivotRb == null || pivotPoint == null) return;

        // centerOfMass is in local (transform) space; compute pivotPoint in rb's local space
        Vector3 localCoM = pivotRb.transform.InverseTransformPoint(pivotPoint.transform.position);
        pivotRb.centerOfMass = localCoM;
    }

    void HandleLeftClickTapAndHold()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            leftDownTime = Time.time;
            holdStarted = false;
        }

        if (mouse.leftButton.isPressed && leftDownTime > 0f && !holdStarted)
        {
            if (Time.time - leftDownTime >= holdThreshold)
            {
                // start a hold
                holdStarted = true;
                // Only start rotate on hold when currently in Grab (spec)
                if (currentState == "Grab")
                {
                    currentState = "Rotate";
                    ToggleLockedRotation(false); // will freeze position, allow rotation (except roll)
                    Debug.Log("Entered Rotate (hold)");
                }
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            float heldFor = leftDownTime > 0f ? (Time.time - leftDownTime) : 0f;

            if (heldFor < holdThreshold)
            {
                // tap: toggle between Idle <-> Grab
                if (currentState == "Idle")
                {
                    currentState = "Grab";
                    ToggleLockedRotation(true);

                    // set reference rotation when entering Grab so Rotate clamps from here
                    if (pivotRb != null) grabReferenceRotation = pivotRb.rotation;

                    Debug.Log("Tapped -> Entered Grab");
                }
                else if (currentState == "Grab")
                {
                    currentState = "Idle";
                    ToggleLockedRotation(true);
                    Debug.Log("Tapped -> Returned to Idle");
                }
                else if (currentState == "Rotate")
                {
                    // rare: quick release from rotate treat as returning to Grab
                    currentState = "Grab";
                    ToggleLockedRotation(true);

                    // update reference to current rotation
                    if (pivotRb != null) grabReferenceRotation = pivotRb.rotation;

                    Debug.Log("Tapped while rotating -> Grab");
                }
            }
            else
            {
                // was a hold-release: if we were rotating, return to Grab on release
                if (currentState == "Rotate")
                {
                    currentState = "Grab";
                    ToggleLockedRotation(true);

                    // update reference to current rotation
                    if (pivotRb != null) grabReferenceRotation = pivotRb.rotation;

                    Debug.Log("Released -> Returned to Grab");
                }
            }

            // reset hold tracking
            leftDownTime = -1f;
            holdStarted = false;
        }
    }

    void ToggleLockedRotation(bool isLocked)
    {
        if (pivotRb == null) return;

        if (isLocked)
        {
            // Prevent the pivot from rotating while still allowing translation
            pivotRb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            // During Rotate: prevent translation but allow rotation in X and Y only (freeze roll/Z)
            pivotRb.constraints =
                RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void DeveloperSwitchState()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentState = "Idle";
            ToggleLockedRotation(true);
            Debug.Log("Developer switched to Idle state");
        }
        else if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentState = "Grab";
            ToggleLockedRotation(true);

            if (pivotRb != null) grabReferenceRotation = pivotRb.rotation;

            Debug.Log("Developer switched to Grab state");
        }
        else if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentState = "Rotate";
            ToggleLockedRotation(false);
            Debug.Log("Developer switched to Rotate state");
        }
        else if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            currentState = "Disabled";
            ToggleLockedRotation(false);
            Debug.Log("Developer switched to Disabled state");
        }
    }

    // clamp pivotRb.rotation so pitch (X) and yaw (Y) stay within +/- configured limits relative to grabReferenceRotation
    void ClampPitchYawToLimits()
    {
        if (pivotRb == null) return;

        // compute rotation relative to reference
        Quaternion relative = Quaternion.Inverse(grabReferenceRotation) * pivotRb.rotation;
        Vector3 relEuler = relative.eulerAngles;

        // convert to -180..180
        float pitch = relEuler.x > 180f ? relEuler.x - 360f : relEuler.x;
        float yaw = relEuler.y > 180f ? relEuler.y - 360f : relEuler.y;

        bool clamped = false;

        float clampedPitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        if (!Mathf.Approximately(clampedPitch, pitch)) clamped = true;

        float clampedYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        if (!Mathf.Approximately(clampedYaw, yaw)) clamped = true;

        if (clamped)
        {
            Quaternion targetRelative = Quaternion.Euler(clampedPitch, clampedYaw, 0f);
            Quaternion targetWorld = grabReferenceRotation * targetRelative;

            pivotRb.rotation = targetWorld;

            // clear angular velocity to avoid immediate overshoot
            pivotRb.angularVelocity = Vector3.zero;
        }
    }
}
