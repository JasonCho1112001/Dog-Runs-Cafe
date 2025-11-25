using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CardScript : MonoBehaviour
{
    [Header("Runtime")]
    public customerScript owner;          // assigned when card spawned
    public Transform aimPoint;            // where the customer tosses the card
    public float tossSpeed = 6f;          // speed while flying to aim point
    public float arriveThreshold = 0.15f; // distance to consider arrived at aim
    public bool isFulfilled = false;      // set true when tapped at reader

    [Header("Return")]
    public float pickupRadius = 0.6f;     // how close player must bring card to owner to return
    public bool isAtAim { get; private set; }

    Rigidbody rb;
    Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        if (rb != null) rb.isKinematic = true; // card movement is controlled by script initially
    }

    void Start()
    {
        isAtAim = false;
    }

    void Update()
    {
        // fly to aiming point immediately after spawn
        if (!isAtAim && aimPoint != null)
        {
            Vector3 dir = aimPoint.position - transform.position;
            float dist = dir.magnitude;
            if (dist <= arriveThreshold)
            {
                isAtAim = true;
                // enable physics after arriving so player can pick it up / grab
                if (rb != null) { rb.isKinematic = false; }
                return;
            }

            transform.position += dir.normalized * tossSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            return;
        }

        // if fulfilled and owner exists, allow owner to take it when player brings it close
        if (isFulfilled && owner != null)
        {
            float d = Vector3.Distance(transform.position, owner.transform.position);
            if (d <= pickupRadius)
            {
                owner.ReceiveReturnedCard(this);
                Debug.Log($"Card returned to owner {owner.name}");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // detect card reader contact and request tap fulfilment
        var reader = other.GetComponent<creditCardReader>();
        if (reader != null)
        {
            // if reader currently allows tap (flat orientation) and this card is touching it -> fulfill
            if (reader.canTap)
            {
                // notify reader (runs flash / visual feedback) then mark the card fulfilled
                reader.NotifyCardTapped(gameObject);
                Fulfill();
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        // also allow fulfilling while staying in reader if canTap becomes true later
        var reader = other.GetComponent<creditCardReader>();
        if (reader != null && reader.canTap && !isFulfilled)
        {
            // notify reader and then fulfill
            reader.NotifyCardTapped(gameObject);
            Fulfill();
        }
    }

    // mark card as satisfied (tapped) and optionally play feedback
    public void Fulfill()   
    {
        if (isFulfilled) return;
        isFulfilled = true;
        // visual/audio feedback can be added here
        Debug.Log($"Card fulfilled for owner { (owner != null ? owner.name : "unknown") }");
    }
}
