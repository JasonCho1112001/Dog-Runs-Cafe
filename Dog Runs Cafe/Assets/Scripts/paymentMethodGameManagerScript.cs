using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class paymentMethodGameManagerScript : MonoBehaviour
{
    [Header("Simple Customer List")]
    [Tooltip("Configurable length of the customer list (capacity).")]
    public int customerListLength = 5;

    [Header("Prefabs")]
    public GameObject customer;
    public GameObject[] waitingPoints;

    [Header("Spawn")]
    [Tooltip("Where customers will spawn.")]
    public Transform spawnPoint;
    [Tooltip("Seconds between spawn attempts.")]
    public float spawnInterval = 3f;

    [Header("Runtime")]
    public List<customerScript> customers;

    Coroutine spawnRoutine;

    void Awake()
    {
        EnsureListCapacity();
    }

    void OnValidate()
    {
        EnsureListCapacity();
    }

    void Start()
    {
        // start spawn loop if configured
        if (spawnPoint != null && customer != null && spawnInterval > 0f)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    void EnsureListCapacity()
    {
        if (customerListLength < 0) customerListLength = 0;
        if (customers == null)
            customers = new List<customerScript>(customerListLength);

        while (customers.Count < customerListLength)
            customers.Add(null);
        while (customers.Count > customerListLength)
            customers.RemoveAt(customers.Count - 1);
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnCustomer();
        }
    }

    void TrySpawnCustomer()
    {
        if (customer == null || spawnPoint == null) return;
        if (IsFull()) return;

        GameObject go = Instantiate(customer, spawnPoint.position, spawnPoint.rotation);
        var cust = go.GetComponent<customerScript>();
        if (cust == null)
        {
            Debug.LogWarning("Spawned prefab does not contain customerScript; destroying instance.", go);
            Destroy(go);
            return;
        }

        // pass manager's waitingPoints array to the spawned customer (if the customer script exposes this field)
        if (waitingPoints != null && waitingPoints.Length > 0)
        {
            cust.waitingPoints = waitingPoints;
        }

        // attempt to place into first free slot
        if (!AddCustomer(cust))
        {
            // no free slot (race) -> destroy spawned object
            Destroy(go);
            return;
        }

        // subscribe so manager cleans up the slot when customer is served
        cust.OnServed += HandleCustomerServed;
    }

    void HandleCustomerServed(customerScript c)
    {
        // unsubscribe and clear slot
        if (c != null) c.OnServed -= HandleCustomerServed;
        RemoveCustomer(c);
    }

    // Try to add a customer into the first free slot. Returns true if added.
    public bool AddCustomer(customerScript c)
    {
        if (c == null) return false;
        for (int i = 0; i < customers.Count; i++)
        {
            if (customers[i] == null)
            {
                customers[i] = c;
                return true;
            }
        }
        return false; // no free slot
    }

    // Remove a specific customer (sets its slot to null). Returns true if removed.
    public bool RemoveCustomer(customerScript c)
    {
        if (c == null) return false;
        for (int i = 0; i < customers.Count; i++)
        {
            if (customers[i] == c)
            {
                customers[i] = null;
                return true;
            }
        }
        return false;
    }

    // Get customer at index (may be null). Returns null on invalid index.
    public customerScript GetCustomerAt(int index)
    {
        if (index < 0 || index >= customers.Count) return null;
        return customers[index];
    }

    // Number of occupied slots
    public int OccupiedCount()
    {
        int n = 0;
        for (int i = 0; i < customers.Count; i++)
            if (customers[i] != null) n++;
        return n;
    }

    // Whether the list is full (no null slots)
    public bool IsFull()
    {
        return OccupiedCount() >= customers.Count;
    }

    // Whether the list is empty (all null)
    public bool IsEmpty()
    {
        return OccupiedCount() == 0;
    }
}
