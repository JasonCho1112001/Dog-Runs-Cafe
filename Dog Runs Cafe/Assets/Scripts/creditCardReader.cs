using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class creditCardReader : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Tag used by the card GameObject")]
    public string cardTag = "Card";

    [Header("Tap Trigger")]
    [Tooltip("Collider used only for tap-to-pay detection. Attach a child collider and assign it here.")]
    public Collider tapTrigger;

    [Header("State (runtime)")]
    [Tooltip("True when any card collider is touching the tap trigger")]
    public bool isCardTouching = false;

    [InspectorName("Can Tap")]
    [Tooltip("True when a touching card is oriented flat (X ≈ 90° or 270°) and positioned above the reader within bounds")]
    public bool canTap = false;

    [Tooltip("Tolerance (degrees) for X rotation to count as flat")]
    public float tapAngleTolerance = 10f;

    // vertical / lateral checks for tap validity
    [Tooltip("Allow small negative offset so a slightly penetrating card still counts as 'above'")]
    public float tapMaxVerticalOffset = 0.02f;
    [Tooltip("Maximum allowed local X offset from reader center for a valid tap")]
    public float tapMaxOffsetX = 0.08f;
    [Tooltip("Maximum allowed local Z offset from reader center for a valid tap")]
    public float tapMaxOffsetZ = 0.08f;

    // internal set to handle multiple colliders contacting the tapTrigger
    HashSet<Collider> touchingCards = new HashSet<Collider>();

    [Header("Insert Detection")]
    [Tooltip("True while a card's chip is inside the insert trigger")]
    public bool isInserted = false;         // exposed bool requested

    [Header("Light / Materials")]
    [Tooltip("Optional: GameObject that represents the light bulb whose material should change.")]
    public GameObject lightBulb;
    [Tooltip("Optional: direct renderer reference (kept for compatibility). If both are set, lightBulb takes precedence.")]
    public Renderer lightRenderer;
    [Tooltip("Materials: 0 = default (deactivated), 1 = activated, 2 = error (red)")]
    public Material[] lightMaterials;
    [Tooltip("How long the activated/error material stays before reverting to default (seconds)")]
    public float flashDuration = 1.0f;

    // runtime
    int currentLightIndex = 0;
    Coroutine flashCoroutine;

    void Reset()
    {
        // ensure the reader's main collider exists
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false; // main reader collider can be non-trigger

        // try to auto-find a child trigger collider to use for taps
        if (tapTrigger == null)
        {
            var childCols = GetComponentsInChildren<Collider>();
            foreach (var c in childCols)
            {
                if (c == col) continue;
                if (c != null && c.isTrigger)
                {
                    tapTrigger = c;
                    break;
                }
            }
        }
    }

    void Awake()
    {
        // prefer lightBulb's renderer if provided
        if (lightRenderer == null && lightBulb != null)
            lightRenderer = lightBulb.GetComponent<Renderer>();

        // ensure tapTrigger is configured to be a trigger
        if (tapTrigger != null && !tapTrigger.isTrigger)
            tapTrigger.isTrigger = true;

        // initialize light to default material (index 0)
        SetLightMaterial(0);
    }

    void OnValidate()
    {
        // keep exposed bool consistent in editor (touch set is empty in edit mode)
        isCardTouching = touchingCards.Count > 0;

        // if a lightBulb is assigned but no renderer, attempt to get one
        if (lightRenderer == null && lightBulb != null)
            lightRenderer = lightBulb.GetComponent<Renderer>();

        // also ensure default material is set in editor when possible
        if (lightRenderer != null && Application.isEditor && !Application.isPlaying)
            SetLightMaterial(0);

        // ensure tapTrigger is a trigger in editor when assigned
        #if UNITY_EDITOR
        if (tapTrigger != null) tapTrigger.isTrigger = true;
        #endif
    }

    // These methods are called by a small forwarder component placed on the tapTrigger GameObject
    // (see TapTriggerForwarder.cs). They replace using this object's OnTriggerEnter/Exit so that
    // only the assigned tapTrigger is used for tap detection.
    public void RegisterTapEnter(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(cardTag)) return;

        touchingCards.Add(other);
        isCardTouching = touchingCards.Count > 0;
        UpdateCanTap();
    }

    public void RegisterTapExit(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(cardTag)) return;

        touchingCards.Remove(other);
        isCardTouching = touchingCards.Count > 0;
        UpdateCanTap();
    }

    void Update()
    {
        // If a card is touching and the reader determines it can tap, fulfill that card and flash green.
        if (isCardTouching && canTap)
        {
            // iterate over a snapshot to avoid modification issues
            var snapshot = new Collider[touchingCards.Count];
            touchingCards.CopyTo(snapshot);
            foreach (var col in snapshot)
            {
                if (col == null) continue;

                // try to find a CardScript (common name used in this project)
                var card = col.GetComponent<CardScript>();
                if (card == null)
                {
                    // try lowercase alternative if present
                    var alt = col.GetComponent("cardScript");
                    if (alt != null) card = alt as CardScript;
                }

                if (card == null) continue;

                // only fulfill once
                if (!card.isFulfilled)
                {
                    // Notify reader (plays flash) and fulfill the card
                    NotifyCardTapped(card.gameObject);
                    card.Fulfill();
                }
            }
        }
    }

    void UpdateCanTap()
    {
        canTap = false;

        if (touchingCards.Count == 0) return;

        foreach (var col in touchingCards)
        {
            if (col == null) continue;
            Transform cardT = col.transform;

            // check X rotation (eulerAngles.x) near 90 or 270 using DeltaAngle for wrap-around safety
            float angleX = cardT.eulerAngles.x;
            bool orientationOk =
                 Mathf.Abs(Mathf.DeltaAngle(angleX, 90f)) <= tapAngleTolerance ||
                 Mathf.Abs(Mathf.DeltaAngle(angleX, 270f)) <= tapAngleTolerance;

             if (!orientationOk) continue;

             // passed orientation and positional checks -> can tap
             canTap = true;
             return;
        }
    }

    void OnDisable()
    {
        touchingCards.Clear();
        isCardTouching = false;
        canTap = false;
        isInserted = false;
        // revert light to default when disabled
        StopFlash();
        SetLightMaterial(0);
    }

    // Public API: call this when a card has been tapped on the reader.
    public void NotifyCardTapped(GameObject card)
    {
        Debug.Log("Card TAPPED on reader: " + (card != null ? card.name : "null"));
        FlashActivated();
        // further processing (e.g. notify card script) can go here
    }

    void OnCardInserted(GameObject card)
    {
        Debug.Log("Card INSERTED into reader: " + card.name);
        // If you later want to show error on wrong method, call FlashError()
        // FlashError();
    }

    // Public setter so external trigger scripts can mark inserted state
    public void SetInserted(bool value)
    {
        if (isInserted == value) return;
        isInserted = value;
        // optional: respond immediately when insertion occurs (play sound/flash)
        if (isInserted)
            OnCardInserted(null);
    }

    // Light / Material helpers

    // Immediately set the renderer's material to the indexed material (safe checks).
    void SetLightMaterial(int index)
    {
        currentLightIndex = index;
        if (lightMaterials == null || lightMaterials.Length == 0) return;
        if (index < 0 || index >= lightMaterials.Length) return;

        // prefer renderer from lightBulb if available, otherwise use lightRenderer
        Renderer r = null;
        if (lightBulb != null) r = lightBulb.GetComponent<Renderer>();
        if (r == null) r = lightRenderer;
        if (r == null) return;

        r.material = lightMaterials[index];
    }

    // Stop any running flash coroutine
    void StopFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }

    // Flash the activated material (index 1) then revert to default (index 0)
    public void FlashActivated()
    {
        if (lightMaterials == null || lightMaterials.Length < 2) return;
        StartFlash(1);
    }

    // Flash the error material (index 2) then revert to default (index 0)
    public void FlashError()
    {
        if (lightMaterials == null || lightMaterials.Length < 3) return;
        StartFlash(2);
    }

    void StartFlash(int flashIndex)
    {
        StopFlash();
        SetLightMaterial(flashIndex);
        flashCoroutine = StartCoroutine(FlashRoutine(flashIndex));
    }

    System.Collections.IEnumerator FlashRoutine(int activeIndex)
    {
        float t = 0f;
        float duration = Mathf.Max(0f, flashDuration);
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }
        flashCoroutine = null;
        // revert to default (0) if available
        SetLightMaterial(0);
    }
}
