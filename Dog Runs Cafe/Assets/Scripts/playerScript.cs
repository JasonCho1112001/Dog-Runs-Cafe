using UnityEngine;
using UnityEngine.InputSystem;

public class playerScript : MonoBehaviour
{
    [Header("Tunable Variables")]
    public float acceleration = 5.0f;
    public float maxSpeed = 3.0f;
    public float rotationalAcceleration = 5.0f;
    public float rollAcceleration = 8.0f; // <-- roll (Z) control via Q/E

    // new: vertical movement limit (driven by mouse wheel)
    [Tooltip("Max vertical speed (Y axis) when moving with the mouse wheel.")]
    public float maxVerticalSpeed = 2.0f;
    public float verticalSpeed = 5.0f;

    [Header("Mouse Wheel Smoothing")]
    [Tooltip("How quickly the current wheel input approaches the target (units/sec). Higher = snappier)")]
    public float wheelSmoothing = 8f;
    [Tooltip("How quickly the wheel target decays back to zero (units/sec).")]
    public float wheelDecay = 3f;
    // runtime wheel state
    float wheelInputTarget = 0f;
    float wheelInputCurrent = 0f;

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
    // reference to mouth grabber so Q/E timer can be disabled while holding
    public mouthGrabberScript mouthGrabber;
    [Tooltip("Assign the cup GameObject (instance placed in the scene). It will be enabled when entering Grab and disabled when exiting Grab.")]
    public GameObject cupObject;

    // --- new: Q/E return-timer settings & internal tracking ---
    [Header("Q/E/W/S return timer")]
    [Tooltip("Seconds of continuous Q, E, W or S hold before BeginReturnToNeutral() is called.")]
    public float qEReturnDelay = 0.6f;

    // internal tracking for Q/E/W/S timer
    public float qEDownTime = -1f;
    bool qEReturnTriggered = false;
    // --- end new fields ---

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

        // initial reference rotation/position
        if (rb != null)
        {
            grabReferenceRotation = rb.rotation;
            grabReferencePosition = rb.position;
        }

        // ensure cup is disabled initially unless starting in Grab
        if (cupObject != null)
            cupObject.SetActive(currentState == "Grab");
    }

    void Update()
    {
        // handle Q/E hold timer which triggers BeginReturnToNeutral after qEReturnDelay
        HandleQETimer();

        // Developer shortcut: press F to immediately start returning the head to neutral
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            // start the return and reset Q/E timer state to avoid accidental re-triggers
            BeginReturnToNeutral();
            qEDownTime = -1f;
            qEReturnTriggered = false;
        }
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

                // restore appropriate constraints so mouse movement / translation works again
                //ToggleLockedRotation(true);
            }

            return;
        }

        var mouse = Mouse.current;
        Vector2 delta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

        // In both Idle and Grab, allow horizontal movement and rotation (roll via Q/E).
        if (currentState == "Idle" || currentState == "Grab")
        {
            // Vertical input from W/S keys (replaces mouse wheel)
            float upDown = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) upDown += 1f;
                if (Keyboard.current.sKey.isPressed) upDown -= 1f;
            }
            else
            {
                // fallback for legacy Input system
                if (Input.GetKey(KeyCode.W)) upDown += 1f;
                if (Input.GetKey(KeyCode.S)) upDown -= 1f;
            }

            // Apply continuous vertical force while W or S is held.
            // Use ForceMode.Acceleration so mass does not change feel across different rigidbodies.
            if (Mathf.Abs(upDown) > Mathf.Epsilon)
            {
                rb.AddForce(Vector3.up * upDown * verticalSpeed, ForceMode.Acceleration);
            }

            // Horizontal movement via mouse delta, vertical handled above
            Vector3 movement = new Vector3(delta.x, 0f, delta.y);
            rb.AddForce(movement * acceleration, ForceMode.Acceleration);

            // clamp horizontal velocity
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.magnitude > maxSpeed)
            {
                Vector3 clamped = horizontalVel.normalized * maxSpeed;
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }

            // clamp vertical velocity (Y)
            float clampedY = Mathf.Clamp(rb.linearVelocity.y, -maxVerticalSpeed, maxVerticalSpeed);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, clampedY, rb.linearVelocity.z);

            // Rotation (roll) controls: Q/E as before.
            const float axisEpsilon = 0.1f;

            float rollInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.isPressed) rollInput += 1f;
                if (Keyboard.current.eKey.isPressed) rollInput -= 1f;
            }

            // determine current roll relative to the grab reference (signed -180..180)
            Quaternion relative = Quaternion.Inverse(grabReferenceRotation) * rb.rotation;
            Vector3 relEuler = relative.eulerAngles;
            float roll = relEuler.z > 180f ? relEuler.z - 360f : relEuler.z;

            // block roll if it would push past configured angle limits
            bool blockRollPositive = rollInput > 0f && roll >= (maxRoll - axisEpsilon);
            bool blockRollNegative = rollInput < 0f && roll <= (-maxRoll + axisEpsilon);

            if (Mathf.Abs(rollInput) > Mathf.Epsilon && !(blockRollPositive || blockRollNegative))
            {
                rb.AddRelativeTorque(Vector3.forward * rollInput * rollAcceleration, ForceMode.Acceleration);
            }

            // Prevent any unwanted pitch/yaw by zeroing local X/Y angular velocity and clamp roll speed.
            Vector3 avWorld = rb.angularVelocity;
            Vector3 avLocal = rb.transform.InverseTransformDirection(avWorld);

            // zero pitch/yaw local components to remove any rotation except roll
            avLocal.x = 0f;
            avLocal.y = 0f;

            float maxRollRad = maxRollSpeed * Mathf.Deg2Rad;
            // if roll was blocked, zero local Z to prevent drifting into the limit
            if (blockRollPositive || blockRollNegative) avLocal.z = 0f;

            avLocal.z = Mathf.Clamp(avLocal.z, -maxRollRad, maxRollRad);

            rb.angularVelocity = rb.transform.TransformDirection(avLocal);
        }
        else
        {
            // For other states (e.g. Disabled), ensure no input-driven motion
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // helper to enable/disable cup when entering/exiting Grab
    void SetCupActive(bool active)
    {
        if (cupObject != null)
            cupObject.SetActive(active);
    }


    void BeginReturnToNeutral()
    {
        if (rb == null) return;

        // set rotation target to the stored grab reference value (neutral)
        returnTargetRotation = grabReferenceRotation;
        returningToNeutral = true;

        // clear velocities to prevent physics from fighting the return
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // new: start timer only after Q/E hold ends (W/S removed)
    void HandleQETimer()
    {
        if (Keyboard.current == null)
        {
            qEDownTime = -1f;
            qEReturnTriggered = false;
            return;
        }

        // don't run the Q/E timer while the mouth grabber is holding something
        if (mouthGrabber != null && mouthGrabber.IsHolding)
        {
            qEDownTime = -1f;
            qEReturnTriggered = false;
            return;
        }

        bool qPressed = Keyboard.current.qKey.isPressed;
        bool ePressed = Keyboard.current.eKey.isPressed;
        bool eitherPressed = qPressed || ePressed;

        // qEDownTime state machine:
        // -1f = idle (no hold, no pending)
        // -2f = currently holding (keys down)
        // >=0 = pending timer start time (after release)

        if (eitherPressed)
        {
            // If a pending return was running, cancel it and mark as holding again.
            if (qEDownTime >= 0f)
            {
                qEDownTime = -2f;
                qEReturnTriggered = false;
            }
            else if (qEDownTime == -1f)
            {
                // newly started hold
                qEDownTime = -2f;
                qEReturnTriggered = false;
            }
            // else already holding (-2f): nothing to do
            return;
        }
        else
        {
            // No keys currently pressed.
            if (qEDownTime == -2f)
            {
                // We were holding and now released -> start the post-release timer.
                qEDownTime = Time.time;
                qEReturnTriggered = false;
                return;
            }

            if (qEDownTime >= 0f && !qEReturnTriggered)
            {
                // Timer running: check completion
                if (Time.time - qEDownTime >= qEReturnDelay)
                {
                    if ((currentState == "Idle" || currentState == "Grab") && !returningToNeutral)
                    {
                        BeginReturnToNeutral();
                        qEReturnTriggered = true;
                    }
                }
            }

            // If triggered or idle, ensure we go back to idle state (so subsequent presses behave correctly)
            if (qEReturnTriggered)
                qEDownTime = -1f;
        }
    }

}
