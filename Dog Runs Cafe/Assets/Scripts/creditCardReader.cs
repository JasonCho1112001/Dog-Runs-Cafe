using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class creditCardReader : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag used by the card GameObject")]
    public string cardTag = "Card";

    [Header("State (runtime)")]
    [Tooltip("True when any card collider is touching the reader trigger")]
    public bool isCardTouching = false;

    [InspectorName("Can Tap")]
    [Tooltip("True when a touching card is oriented flat (X ≈ 90° or 270°)")]
    public bool canTap = false;

    [Tooltip("Tolerance (degrees) for X rotation to count as flat")]
    public float tapAngleTolerance = 10f;

    // internal set to handle multiple colliders contacting the reader
    HashSet<Collider> touchingCards = new HashSet<Collider>();
    
    void Reset()
    {
        // ensure the collider is a trigger so we receive trigger events
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnValidate()
    {
        // keep exposed bool consistent in editor (touch set is empty in edit mode)
        isCardTouching = touchingCards.Count > 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(cardTag)) return;

        touchingCards.Add(other);
        isCardTouching = touchingCards.Count > 0;
        UpdateCanTap();
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(cardTag)) return;

        touchingCards.Remove(other);
        isCardTouching = touchingCards.Count > 0;
        UpdateCanTap();
    }

    void UpdateCanTap()
    {
        canTap = false;
        foreach (var col in touchingCards)
        {
            if (col == null) continue;
            Transform cardT = col.transform;
            // check X rotation (eulerAngles.x) near 90 or 270 using DeltaAngle for wrap-around safety
            float angleX = cardT.eulerAngles.x;
            if (Mathf.Abs(Mathf.DeltaAngle(angleX, 90f)) <= tapAngleTolerance ||
                Mathf.Abs(Mathf.DeltaAngle(angleX, 270f)) <= tapAngleTolerance)
            {
                canTap = true;
                return;
            }
        }
    }

    void OnDisable()
    {
        touchingCards.Clear();
        isCardTouching = false;
        canTap = false;
    }
}
