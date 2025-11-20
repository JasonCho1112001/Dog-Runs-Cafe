using UnityEngine;

public class cameraScript : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // player head / pivot

    [Header("Edge (viewport space 0..1)")]
    [Tooltip("When the target's viewport X/Y goes within this margin of an edge, the camera will start following.")]
    public Vector2 viewportEdge = new Vector2(0.1f, 0.1f);

    [Header("Forward Distance")]
    [Tooltip("Maximum allowed distance (camera space) before the camera starts moving to keep the target closer.")]
    public float maxForwardDistance = 10f;

    [Header("Smoothing")]
    public float smoothTime = 0.15f;

    [Header("Vertical Follow")]
    [Tooltip("When true the camera will follow the target's Y (up/down) movement as well.")]
    public bool followY = true;

    [Header("Offset")]
    [Tooltip("Configurable offset applied to the camera's desired position. If 'Offset In Camera Space' is checked the offset is interpreted in the camera's local space.")]
    public Vector3 followOffset = Vector3.zero;
    public bool offsetInCameraSpace = true;

    Camera cam;
    Vector3 velocity = Vector3.zero;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;

        // target position in viewport (x,y in 0..1, z = distance from camera)
        Vector3 vp = cam.WorldToViewportPoint(target.position);

        // compute clamped viewport inside the allowed rectangle
        float minX = viewportEdge.x;
        float maxX = 1f - viewportEdge.x;
        float minY = viewportEdge.y;
        float maxY = 1f - viewportEdge.y;

        float clampedX = Mathf.Clamp(vp.x, minX, maxX);
        float clampedY = Mathf.Clamp(vp.y, minY, maxY);

        // decide whether to follow: outside edge box OR beyond forward distance
        bool outsideEdge = (!Mathf.Approximately(clampedX, vp.x) || !Mathf.Approximately(clampedY, vp.y));
        bool beyondForward = vp.z > maxForwardDistance;

        if (outsideEdge || beyondForward)
        {
            // build a viewport position we want the target to be at:
            // - use clamped X/Y (so it will move toward the inner box if needed)
            // - if target is too far away, set viewport z to maxForwardDistance so camera moves closer
            float targetZ = beyondForward ? maxForwardDistance : vp.z;
            Vector3 clampedVP = new Vector3(clampedX, clampedY, targetZ);

            // world point that corresponds to the clamped viewport coordinate (at chosen depth)
            Vector3 worldAtClamped = cam.ViewportToWorldPoint(clampedVP);

            // delta needed so the target ends up at clamped viewport pos
            Vector3 delta = target.position - worldAtClamped;

            Vector3 desiredPos;
            // compute offset in world space (from configured offset)
            Vector3 offsetWorld = offsetInCameraSpace ? cam.transform.TransformVector(followOffset) : followOffset;

            if (followY)
            {
                // follow X/Y/Z movement delta and apply offset
                desiredPos = cam.transform.position + delta + offsetWorld;
            }
            else
            {
                // only follow X/Z, preserve camera Y (height)
                Vector3 moveDelta = new Vector3(delta.x, 0f, delta.z);
                // do not apply any Y component of the offset when followY is false
                Vector3 offsetNoY = new Vector3(offsetWorld.x, 0f, offsetWorld.z);
                desiredPos = cam.transform.position + moveDelta + offsetNoY;
                desiredPos.y = cam.transform.position.y;
            }

            // smooth follow (affects X/Z and Y when followY == true)
            cam.transform.position = Vector3.SmoothDamp(cam.transform.position, desiredPos, ref velocity, smoothTime);
        }
        else
        {
            // optionally damp residual velocity so camera doesn't snap when target re-enters center
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 5f);
        }
    }
}
