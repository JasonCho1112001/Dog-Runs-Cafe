using System;
using UnityEngine;

public class customerScript : MonoBehaviour
{
    [Header("Customer")]
    public PaymentMethod paymentMethod = PaymentMethod.Cash;
    public float walkSpeed = 2f;
    public float stopDistance = 0.05f;

    // assigned by manager on spawn
    public GameObject[] waitingPoints;
    public Transform exitPoint;

    [Header("Card")]
    public GameObject cardPrefab;      // prefab to spawn when customer reaches waiting point
    public Transform cardSpawnPoint;   // local transform where the card appears (optional)
    [Tooltip("Aiming point the customer tosses the card toward")]
    public Transform aimingPoint;

    public Action<customerScript> OnReachedQueue;
    public Action<customerScript> OnServed;

    Transform targetSlot;
    bool moving;
    bool leaving = false; // true when customer is walking to exit (satisfied)
    bool cardThrown = false;

    void Update()
    {
        if (!moving || targetSlot == null) return;

        Vector3 dir = targetSlot.position - transform.position;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= stopDistance)
        {
            moving = false;

            if (leaving)
            {
                // reached exit: notify manager (served) and destroy
                OnServed?.Invoke(this);
                Destroy(gameObject);
            }
            else
            {
                // reached queue slot
                OnReachedQueue?.Invoke(this);

                // toss card to aiming point once when reaching queue
                if (!cardThrown)
                {
                    TossCardToAimingPoint();
                    cardThrown = true;
                }
            }
            return;
        }

        Vector3 move = dir.normalized * walkSpeed * Time.deltaTime;
        transform.position += move;
        if (move.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(move.normalized, Vector3.up);
    }

    // move this customer toward a slot transform
    public void MoveTo(Transform slot)
    {
        if (slot == null) return;
        targetSlot = slot;
        moving = true;
        leaving = false;
    }

    // immediately mark satisfied and walk to exit point
    public void Satisfy()
    {
        if (exitPoint == null)
        {
            // fallback: if no exit assigned, immediately invoke served and destroy
            OnServed?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        targetSlot = exitPoint;
        moving = true;
        leaving = true;
    }

    // instantly serve (skip walking)
    public void Serve()
    {
        OnServed?.Invoke(this);
        Destroy(gameObject);
    }

    void TossCardToAimingPoint()
    {
        if (cardPrefab == null || aimingPoint == null) return;

        Vector3 spawnPos = (cardSpawnPoint != null) ? cardSpawnPoint.position : transform.position + transform.forward * 0.25f + Vector3.up * 0.4f;
        GameObject go = Instantiate(cardPrefab, spawnPos, Quaternion.identity);
        var card = go.GetComponent<CardScript>();
        if (card == null)
        {
            Debug.LogWarning("Card prefab missing CardScript component.", go);
            return;
        }

        // assign ownership and aiming target
        card.owner = this;
        card.aimPoint = aimingPoint;
        // ensure card has proper tag for reader detection
        go.tag = "Card";
    }

    // called by CardScript when a fulfilled card is brought back close to this customer
    public void ReceiveReturnedCard(CardScript card)
    {
        if (card == null) return;
        if (card.owner != this) return;
        if (!card.isFulfilled) return;

        // destroy the card and mark customer satisfied
        Destroy(card.gameObject);
        Satisfy();
    }
}
