using UnityEngine;
using UnityEngine.InputSystem;

public class mouthGrabberScript : MonoBehaviour
{
    [Header("Mouth / Detection")]
    public Transform mouthTransform;           // assign mouth bone / transform in inspector
    public float grabRadius = 0.4f;            // detection radius around the mouth
    public LayerMask grabbableMask;            // layer(s) for grabbable objects

    [Header("Grab / Joint")]
    public float jointBreakForce = 200f;       // break force to allow object to be torn off
    public float jointBreakTorque = 200f;
    public float grabCooldown = 0.25f;

    [SerializeField]
    Rigidbody grabbedRb;
    FixedJoint grabJoint;
    float lastGrabTime = -10f;

    // expose whether we're currently holding something
    public bool IsHolding
    {
        get { return grabbedRb != null; }
    }

    // New: toggle grab/release on left click
    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (grabbedRb != null)
            {
                Release();
            }
            else
            {
                TryBite();
            }
        }
    }

    // Attempt a bite/grab. Returns true if an object was grabbed.
    public bool TryBite()
    {
        if (Time.time - lastGrabTime < grabCooldown) return false;
        lastGrabTime = Time.time;

        if (mouthTransform == null) mouthTransform = transform;

        Collider[] hits = Physics.OverlapSphere(mouthTransform.position, grabRadius, grabbableMask);
        if (hits == null || hits.Length == 0) return false;

        Collider best = null;
        float bestDist = float.MaxValue;
        foreach (var c in hits)
        {
            if (c == null || c.attachedRigidbody == null) continue;
            float d = Vector3.Distance(mouthTransform.position, c.transform.position);
            if (d < bestDist)
            {
                best = c;
                bestDist = d;
            }
        }
        if (best == null) { return false; }

        Grab(best.attachedRigidbody);
        return true;
    }

    void Grab(Rigidbody rb)
    {
        if (rb == null) return;

        // If already holding something, release it first
        if (grabbedRb != null)
            Release();

        grabbedRb = rb;

        // create FixedJoint on the mouth and connect to the grabbed rigidbody
        grabJoint = mouthTransform.gameObject.AddComponent<FixedJoint>();
        grabJoint.connectedBody = grabbedRb;
        grabJoint.breakForce = jointBreakForce;
        grabJoint.breakTorque = jointBreakTorque;

        // optional: reduce interference
        // grabbedRb.drag = 2f;
        // grabbedRb.angularDrag = 2f;
    }

    // Release held object. Optionally apply an impulse.
    public void Release(Vector3 applyImpulse = default)
    {
        Debug.Log("Released grabbed object.");
        if (grabJoint != null)
        {
            Destroy(grabJoint);
            grabJoint = null;
        }

        if (grabbedRb != null)
        {
            if (applyImpulse != default)
                grabbedRb.AddForce(applyImpulse, ForceMode.Impulse);

            grabbedRb = null;
        }
    }

    // Called by Unity when a joint on this GameObject breaks
    void OnJointBreak(float breakForce)
    {
        // Clean up references if something was torn off
        grabJoint = null;
        grabbedRb = null;
    }

    

    void OnDrawGizmosSelected()
    {
        if (mouthTransform == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(mouthTransform.position, grabRadius);
    }
}
