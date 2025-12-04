using UnityEngine;
using UnityEngine.InputSystem;

public class playerTrayGameScript : MonoBehaviour
{
    [Header("Rotation Limits (degrees)")]
    public float maxPitch = 30f;
    public float maxRoll = 20f;

    [Header("Rotation Speeds")]
    public float pitchSensitivity = 50f;
    public float rollSensitivity = 50f;

    [Header("Return To Neutral")]
    public float returnRotationSpeed = 180f;
    public float returnRotationTolerance = 0.25f;

    [Header("References")]
    public Rigidbody rb;
    public mouthGrabberScript mouthGrabber;
    public GameObject cupObject;

    [Header("States")]
    public string currentState = "Idle";

    // Internal state
    bool returningToNeutral = false;
    Quaternion neutralRotation = Quaternion.identity;

    bool firstMouseFrame = true;

    // Ignore mouse input at game start
    float startupIgnoreTime = 0.5f;
    float startupTimer = 0f;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Store starting rotation as neutral
        neutralRotation = transform.localRotation;

        // Freeze all physics-driven movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints =
            RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotation;

        if (cupObject != null)
            cupObject.SetActive(currentState == "Grab");

        // Lock mouse to center and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        startupTimer += Time.deltaTime;

        // Developer shortcut
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            BeginReturnToNeutral();
    }

    void FixedUpdate()
    {
        if (returningToNeutral)
        {
            HandleReturnToNeutral();
            return;
        }

        ApplyMouseRotation();
    }

    // ----------------------------------------------------
    // ROTATION CONTROL (manual, not physics-based)
    // ----------------------------------------------------
    void ApplyMouseRotation()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // Ignore first 0.5 seconds of mouse movement
        if (startupTimer < startupIgnoreTime)
            return;

        if (firstMouseFrame)
        {
            firstMouseFrame = false;
            // First locked frame may produce bogus delta, ignore it
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();

        // Convert to controlled movement
        float rollInput = delta.x * rollSensitivity * Time.fixedDeltaTime;
        float pitchInput = -delta.y * pitchSensitivity * Time.fixedDeltaTime;

        // Fix roll direction (mouse right = roll right)
        float rollDelta = -rollInput;
        float pitchDelta = pitchInput;

        // Get current rotation
        Quaternion current = transform.localRotation;
        Vector3 euler = current.eulerAngles;

        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
        float roll = euler.z > 180 ? euler.z - 360 : euler.z;

        // Apply delta
        pitch += pitchDelta;
        roll += rollDelta;

        // Clamp
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        roll = Mathf.Clamp(roll, -maxRoll, maxRoll);

        transform.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }

    // ----------------------------------------------------
    // RETURN-TO-NEUTRAL
    // ----------------------------------------------------
    void BeginReturnToNeutral()
    {
        returningToNeutral = true;
    }

    void HandleReturnToNeutral()
    {
        Quaternion current = transform.localRotation;
        float step = returnRotationSpeed * Time.fixedDeltaTime;

        Quaternion newRot = Quaternion.RotateTowards(current, neutralRotation, step);
        transform.localRotation = newRot;

        if (Quaternion.Angle(newRot, neutralRotation) <= returnRotationTolerance)
        {
            transform.localRotation = neutralRotation;
            returningToNeutral = false;
        }
    }
}
