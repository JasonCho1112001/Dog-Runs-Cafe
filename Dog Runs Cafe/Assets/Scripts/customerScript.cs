using System;
using UnityEngine;

public class customerScript : MonoBehaviour
{
    [Header("Customer")]
    public PaymentMethod paymentMethod = PaymentMethod.Cash;
    public float walkSpeed = 2f;
    public float stopDistance = 0.05f;

    public GameObject[] waitingPoints;

    public Action<customerScript> OnReachedQueue;
    public Action<customerScript> OnServed;

    Transform targetSlot;
    bool moving;

    
    void Start()
    {
        
    }


    void Update()
    {
        if (!moving || targetSlot == null) return;

        Vector3 dir = targetSlot.position - transform.position;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist <= stopDistance)
        {
            moving = false;
            OnReachedQueue?.Invoke(this);
            return;
        }

        Vector3 move = dir.normalized * walkSpeed * Time.deltaTime;
        transform.position += move;
        if (move.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(move.normalized, Vector3.up);
    }

    public void MoveTo(Transform slot)
    {
        targetSlot = slot;
        moving = true;
    }

    public void Serve()
    {
        OnServed?.Invoke(this);
        Destroy(gameObject);
    }
}
