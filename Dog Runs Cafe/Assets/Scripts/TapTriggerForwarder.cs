using UnityEngine;

// Attach this to the GameObject that holds the tap-area collider (non-trigger).
// The collider must NOT have "Is Trigger" enabled. Assign the parent creditCardReader in the inspector.
[RequireComponent(typeof(Collider))]
public class TapTriggerForwarder : MonoBehaviour
{
    public creditCardReader reader;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = false;
    }

    // Use collision callbacks (non-trigger collider)
    void OnCollisionEnter(Collision collision)
    {
        if (reader == null) return;
        var otherCol = collision.collider;
        if (otherCol == null) return;
        reader.RegisterTapEnter(otherCol);
        Debug.Log("TapTriggerForwarder: OnCollisionEnter forwarded");
    }

    void OnCollisionExit(Collision collision)
    {
        if (reader == null) return;
        var otherCol = collision.collider;
        if (otherCol == null) return;
        reader.RegisterTapExit(otherCol);
        Debug.Log("TapTriggerForwarder: OnCollisionExit forwarded");
    }
}