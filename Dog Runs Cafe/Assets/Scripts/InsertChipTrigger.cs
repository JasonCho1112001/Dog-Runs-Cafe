using UnityEngine;

// Attach this to the insert trigger collider GameObject (insertChipCollider).
// The collider must be marked as "Is Trigger" and the Card objects must use the same tag as the reader.cardTag.
[RequireComponent(typeof(Collider))]
public class InsertChipTrigger : MonoBehaviour
{
    [Tooltip("Reference to the parent creditCardReader that should be notified.")]
    public creditCardReader reader;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (reader == null) return;
        if (!other.CompareTag(reader.cardTag)) return;
        reader.SetInserted(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (reader == null) return;
        if (!other.CompareTag(reader.cardTag)) return;
        reader.SetInserted(false);
    }
}