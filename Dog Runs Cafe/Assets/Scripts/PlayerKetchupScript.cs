using UnityEngine;
using UnityEngine.InputSystem;

public class playerKetchupScript : MonoBehaviour
{
    [Header("Tunable Variables")]
    public float acceleration = 5.0f;
    public float maxSpeed = 3.0f;
    public float rotationalAcceleration = 5.0f;
    public float rollAcceleration = 8.0f; // <-- roll (Z) control via Q/E

    [Header("Rotation Limits (degrees)")]
    public float maxPitch = 30f; // up/down
    public float maxYaw = 45f;   // left/right
    public float maxRoll = 20f;  // roll limit (degrees)

    [Header("Rotation Speed Limits (degrees/sec)")]
    [Tooltip("Max angular speed for pitch and yaw (degrees per second).")]
    public float maxPitchYawSpeed = 120f;
    [Tooltip("Max angular speed for roll (degrees per second).")]
    public float maxRollSpeed = 180f;

    [Header("Grab / Hold")]
    public float holdThreshold = 0.25f; // seconds to consider a hold

    [Header("Return to neutral")]
    [Tooltip("Degrees per second the head will rotate back to neutral when exiting Rotate mode.")]
    public float returnRotationSpeed = 360f;
    [Tooltip("Units per second the head will move back to neutral Y position when exiting Rotate mode.")]
    public float returnPositionSpeed = 5f;
    public float returnPositionTolerance = 0.02f; // threshold to consider position returned
    public float returnRotationTolerance = 0.25f; // threshold (degrees) to consider rotation returned

    [Header("States")]
    public string currentState = "Idle";

    [Header("References")]
    public Rigidbody rb;

    [Header("Bottle Tip Object")]
    public GameObject ketchupTip;

    // internal
    float leftDownTime = -1f;
    bool holdStarted = false;

    // reference rotation and position used to clamp / return (set when entering Grab)
    Quaternion grabReferenceRotation = Quaternion.identity;
    Vector3 grabReferencePosition = Vector3.zero;

    // return-to-neutral tracking
    public bool returningToNeutral = false;
    Quaternion returnTargetRotation = Quaternion.identity;
    Vector3 returnTargetPosition = Vector3.zero; // kept for compatibility but NOT used in return

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // ensure default rotation lock for Idle
        ToggleLockedRotation(true);

        // initial reference rotation/position
        if (rb != null)
        {
            grabReferenceRotation = rb.rotation;
            grabReferencePosition = rb.position;
        }
    }

    void Update()
    {
        //HandleLeftClickTapAndHold();
        HandleKetchupSqueeze();
        DeveloperSwitchState();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // If returning to neutral, drive rotation toward the target and skip input rotation/movement.
        // TRANSLATION part removed: we no longer move position back here, only rotation.
        if (returningToNeutral)
        {
            // prevent translation being driven by physics while returning
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Rotate toward target
            float step = returnRotationSpeed * Time.fixedDeltaTime;
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, returnTargetRotation, step);
            rb.MoveRotation(newRot);

            // check completion (rotation only)
            bool rotDone = Quaternion.Angle(rb.rotation, returnTargetRotation) <= returnRotationTolerance;

            if (rotDone)
            {
                // snap final rotation, clear velocities and re-freeze rotation (back to Idle/Grab behavior)
                rb.MoveRotation(returnTargetRotation);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                returningToNeutral = false;
                ToggleLockedRotation(true);
            }

            return;
        }

        var mouse = Mouse.current;
        Vector2 delta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

        if (currentState == "Idle" || currentState == "Grab")
        {
            // Horizontal movement via forces (same behavior in Idle and Grab)
            Vector3 movement = new Vector3(delta.x, 0f, delta.y);
            rb.AddForce(movement * acceleration, ForceMode.Acceleration);

            // clamp horizontal velocity
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontalVel.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }
        else if (currentState == "Rotate")
        {
            // Rotation via torque while holding (pitch & yaw from mouse)
            // apply pitch/yaw as relative (local) torque so roll (local Z) doesn't change their axes
            Vector3 localTorque = new Vector3(delta.y, delta.x, 0f); // X = pitch, Y = yaw

            // determine current pitch/yaw/roll (degrees) relative to the grab reference
            Quaternion relative = Quaternion.Inverse(grabReferenceRotation) * rb.rotation;
            Vector3 relEuler = relative.eulerAngles;
            float pitch = relEuler.x > 180f ? relEuler.x - 360f : relEuler.x;
            float yaw = relEuler.y > 180f ? relEuler.y - 360f : relEuler.y;
            float roll = relEuler.z > 180f ? relEuler.z - 360f : relEuler.z;

            const float axisEpsilon = 0.1f; // small tolerance to avoid jitter at limits

            // Decide whether applying torque would increase magnitude past the allowed max for each axis.
            bool blockPitchPositive = localTorque.x > 0f && pitch >= (maxPitch - axisEpsilon);
            bool blockPitchNegative = localTorque.x < 0f && pitch <= (-maxPitch + axisEpsilon);
            bool blockYawPositive = localTorque.y > 0f && yaw >= (maxYaw - axisEpsilon);
            bool blockYawNegative = localTorque.y < 0f && yaw <= (-maxYaw + axisEpsilon);

            Vector3 applyLocalTorque = Vector3.zero;

            // Apply pitch torque only if it won't push past limits
            if (!(blockPitchPositive || blockPitchNegative))
                applyLocalTorque.x = localTorque.x;

            // Apply yaw torque only if it won't push past limits
            if (!(blockYawPositive || blockYawNegative))
                applyLocalTorque.y = localTorque.y;

            // If there's any pitch/yaw to apply, add relative torque
            if (applyLocalTorque.sqrMagnitude > Mathf.Epsilon)
                rb.AddRelativeTorque(applyLocalTorque * rotationalAcceleration, ForceMode.Acceleration);

            // roll from Q/E keys (relative Z axis)
            float rollInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed) rollInput += 1f;
                if (Keyboard.current.eKey.isPressed) rollInput -= 1f;
            }

            if (Mathf.Abs(rollInput) > Mathf.Epsilon)
            {
                // block roll torque if it would increase magnitude past maxRoll
                bool wouldIncreasePositive = rollInput > 0f && roll >= (maxRoll - axisEpsilon);
                bool wouldIncreaseNegative = rollInput < 0f && roll <= (-maxRoll + axisEpsilon);

                if (!(wouldIncreasePositive || wouldIncreaseNegative))
                    rb.AddRelativeTorque(Vector3.forward * rollInput * rollAcceleration, ForceMode.Acceleration);
            }

            // After applying torque, clamp angular speed per-axis (local space) to configured max speeds
            // Note: Rigidbody.angularVelocity is in world-space radians/sec. Convert to local to clamp per-axis.
            Vector3 avWorld = rb.angularVelocity;
            Vector3 avLocal = rb.transform.InverseTransformDirection(avWorld);

            float maxPYRad = maxPitchYawSpeed * Mathf.Deg2Rad;
            float maxRollRad = maxRollSpeed * Mathf.Deg2Rad;

            // If an axis was blocked above, zero its local angular velocity to prevent drift.
            if (blockPitchPositive || blockPitchNegative) avLocal.x = 0f;
            if (blockYawPositive || blockYawNegative) avLocal.y = 0f;
            // For roll, if we're at limit and attempted roll was blocked, zero Z later — detect now:
            bool rollBlocked = false;
            if (Mathf.Abs(rollInput) > Mathf.Epsilon)
            {
                bool wouldIncreasePositive = rollInput > 0f && roll >= (maxRoll - axisEpsilon);
                bool wouldIncreaseNegative = rollInput < 0f && roll <= (-maxRoll + axisEpsilon);
                rollBlocked = (wouldIncreasePositive || wouldIncreaseNegative);
            }
            if (rollBlocked) avLocal.z = 0f;

            // Clamp magnitudes
            avLocal.x = Mathf.Clamp(avLocal.x, -maxPYRad, maxPYRad);
            avLocal.y = Mathf.Clamp(avLocal.y, -maxPYRad, maxPYRad);
            avLocal.z = Mathf.Clamp(avLocal.z, -maxRollRad, maxRollRad);

            // write back angular velocity in world space
            rb.angularVelocity = rb.transform.TransformDirection(avLocal);

            // keep the pivot's position fixed while allowing rotation:
            rb.linearVelocity = Vector3.zero;

            // clamp pitch/yaw/roll to configured limits (angles)
            ClampPitchYawToLimits();
        }
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
                if (currentState == "Idle" || currentState == "Grab")
                {
                    currentState = "Rotate";
                    ToggleLockedRotation(false); // freeze position, allow rotation
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
                    // quick release from rotate -> return to Grab and start return-to-neutral (rotation only)
                    currentState = "Grab";

                    BeginReturnToNeutral();
                    Debug.Log("Tapped while rotating -> Grab (returning to neutral rotation)");
                }
            }
            else
            {
                // was a hold-release: if we were rotating, return to Grab on release and start return (rotation only)
                if (currentState == "Rotate")
                {
                    currentState = "Grab";

                    BeginReturnToNeutral();
                    Debug.Log("Released -> Returned to Grab (returning to neutral rotation)");
                }
            }

            // reset hold tracking
            leftDownTime = -1f;
            holdStarted = false;
        }
    }

    void BeginReturnToNeutral()
    {
        if (rb == null) return;

        // set rotation target to the stored grab reference value (neutral)
        returnTargetRotation = grabReferenceRotation;
        returningToNeutral = true;

        // freeze position to prevent the head being displaced while it rotates back
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;

        // clear velocities to prevent physics from fighting the return
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void ToggleLockedRotation(bool isLocked)
    {
        if (rb == null) return;

        if (isLocked)
        {
            // Prevent the pivot from rotating while still allowing translation
            rb.constraints =
            RigidbodyConstraints.FreezeRotation |
            RigidbodyConstraints.FreezePositionY;
        }
        else
        {
            // During Rotate: prevent translation but allow rotation on all axes (Q/E will roll).
            rb.constraints =
                RigidbodyConstraints.FreezePositionX |
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezePositionZ;
        }
    }

    void DeveloperSwitchState()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            bool wasRotate = currentState == "Rotate";
            currentState = "Idle";
            ToggleLockedRotation(true);
            if (wasRotate) BeginReturnToNeutral();
            Debug.Log("Developer switched to Idle state");
        }
        else if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            bool wasRotate = currentState == "Rotate";
            currentState = "Grab";
            ToggleLockedRotation(true);

            if (wasRotate) BeginReturnToNeutral();

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

    // clamp pivot.rotation so pitch (X), yaw (Y) and roll (Z) stay within +/- configured limits relative to grabReferenceRotation
    void ClampPitchYawToLimits()
    {
        if (rb == null) return;

        Quaternion relative = Quaternion.Inverse(grabReferenceRotation) * rb.rotation;
        Vector3 relEuler = relative.eulerAngles;

        float pitch = relEuler.x > 180f ? relEuler.x - 360f : relEuler.x;
        float yaw = relEuler.y > 180f ? relEuler.y - 360f : relEuler.y;
        float roll = relEuler.z > 180f ? relEuler.z - 360f : relEuler.z;

        bool clampedPitch = false;
        bool clampedYaw = false;
        bool clampedRoll = false;

        float clampedPitchVal = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        if (!Mathf.Approximately(clampedPitchVal, pitch)) clampedPitch = true;

        float clampedYawVal = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        if (!Mathf.Approximately(clampedYawVal, yaw)) clampedYaw = true;

        float clampedRollVal = Mathf.Clamp(roll, -maxRoll, maxRoll);
        if (!Mathf.Approximately(clampedRollVal, roll)) clampedRoll = true;

        if (clampedPitch || clampedYaw || clampedRoll)
        {
            Quaternion targetRelative = Quaternion.Euler(clampedPitchVal, clampedYawVal, clampedRollVal);
            Quaternion targetWorld = grabReferenceRotation * targetRelative;

            rb.rotation = targetWorld;

            Vector3 av = rb.angularVelocity;
            if (clampedPitch) av.x = 0f;
            if (clampedYaw) av.y = 0f;
            if (clampedRoll) av.z = 0f;
            rb.angularVelocity = av;
        }
    }

    void HandleKetchupSqueeze() 
    {
        var mouse = Mouse.current;
        Vector3 bottleTip = ketchupTip.transform.position;

        if (mouse == null) return;

        if (mouse.rightButton.isPressed) 
        {
            GameObject ketchupSphere = KetchupPool.SharedInstance.GetPooledObject();
            if (ketchupSphere != null)
            {
                ketchupSphere.transform.position = new Vector3(bottleTip.x, bottleTip.y - .1f, bottleTip.z);
                ketchupSphere.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                ketchupSphere.SetActive(true);
            }
            KetchupPool.SharedInstance.updatePooledObject();
        }
    }
}
