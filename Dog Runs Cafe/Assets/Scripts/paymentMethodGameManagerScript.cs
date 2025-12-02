using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class paymentMethodGameManagerScript : MonoBehaviour
{
    public enum DifficultyLevel { Level1 = 1, Level2 = 2, Level3 = 3 }

    [Header("Difficulty")]
    [Tooltip("Select game difficulty")]
    public DifficultyLevel difficulty = DifficultyLevel.Level1;

    [Tooltip("Seconds remaining in the current level (runtime)")]
    public float remainingTime = 0f;

    [Tooltip("Whether the level is currently active (timer running / spawning)")]
    public bool levelActive = false;

    // Difficulty presets (editable if you want different values in inspector)
    [Header("Difficulty Presets (editable)")]
    public int level1CustomerCount = 1;
    public float level1Time = 15f;
    public int level2CustomerCount = 3;
    public float level2Time = 25f;
    public int level3CustomerCount = 6;
    public float level3Time = 35f;

    [Header("Simple Customer List")]
    [Tooltip("Configurable length of the customer list (capacity).")]
    public int customerListLength = 5;

    [Header("Prefabs")]
    public GameObject customer;
    public GameObject[] waitingPoints;
    [Header("Exit")]
    [Tooltip("Transform customers walk to when satisfied")]
    public Transform exitPoint;

    [Header("Aiming")]
    [Tooltip("Where customers toss their card to (Aiming Point)")]
    public Transform aimingPoint;

    [Header("Spawn")]
    [Tooltip("Where customers will spawn.")]
    public Transform spawnPoint;
    [Tooltip("Seconds between spawn attempts.")]
    public float spawnInterval = 3f;

    [Header("Runtime")]
    public List<customerScript> customers;

    Coroutine spawnRoutine;

    // track which waitingPoints are currently occupied
    bool[] waitingPointOccupied;
    // map spawned customer -> assigned waiting point index
    Dictionary<customerScript, int> waitingAssignments = new Dictionary<customerScript, int>();

    void Awake()
    {
        ApplyDifficulty();
        EnsureListCapacity();
        EnsureWaitingPoints();
    }

    void OnValidate()
    {
        // keep editor preview consistent when changing difficulty in inspector
        ApplyDifficulty();
        EnsureListCapacity();
        EnsureWaitingPoints();
    }

    void Start()
    {
        // start level timer and spawn loop based on difficulty
        if (remainingTime > 0f)
        {
            levelActive = true;
            if (spawnPoint != null && customer != null && spawnInterval > 0f)
                spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    void Update()
    {
        // developer shortcut: satisfy all customers when F5 is pressed (new Input System)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f5Key.wasPressedThisFrame)
        {
            SatisfyAllCustomers();
        }

        // countdown level timer
        if (levelActive)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                levelActive = false;
                OnLevelTimeExpired();
            }
        }
    }

    void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    void ApplyDifficulty()
    {
        switch (difficulty)
        {
            case DifficultyLevel.Level1:
                customerListLength = Mathf.Max(1, level1CustomerCount);
                remainingTime = Mathf.Max(0f, level1Time);
                break;
            case DifficultyLevel.Level2:
                customerListLength = Mathf.Max(1, level2CustomerCount);
                remainingTime = Mathf.Max(0f, level2Time);
                break;
            case DifficultyLevel.Level3:
                customerListLength = Mathf.Max(1, level3CustomerCount);
                remainingTime = Mathf.Max(0f, level3Time);
                break;
        }
    }

    void OnLevelTimeExpired()
    {
        // stop spawning new customers
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        // optional: you can decide what happens when time expires.
        // Default: stop new spawns but let existing customers be served.
        Debug.Log($"Level time expired for difficulty {difficulty}. Remaining customers must still be served.");
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

    void EnsureWaitingPoints()
    {
        int len = (waitingPoints != null) ? waitingPoints.Length : 0;
        if (waitingPointOccupied == null || waitingPointOccupied.Length != len)
            waitingPointOccupied = new bool[len];

        // clear map entries that reference out-of-range indices
        var toRemove = new List<customerScript>();
        foreach (var kv in waitingAssignments)
        {
            if (kv.Value < 0 || kv.Value >= len) toRemove.Add(kv.Key);
        }
        foreach (var k in toRemove)
            waitingAssignments.Remove(k);
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

        int freeWaitingIndex = FindFirstFreeWaitingPointIndex();
        if (freeWaitingIndex == -1) return; // no free waiting point available

        GameObject go = Instantiate(customer, spawnPoint.position, spawnPoint.rotation);
        var cust = go.GetComponent<customerScript>();
        if (cust == null)
        {
            Debug.LogWarning("Spawned prefab does not contain customerScript; destroying instance.", go);
            Destroy(go);
            return;
        }

        // assign manager waitingPoints array to the customer if available
        if (waitingPoints != null && waitingPoints.Length > 0)
            cust.waitingPoints = waitingPoints;

        // give the exit point reference to the customer
        cust.exitPoint = exitPoint;

        // give aiming point reference to the customer so they toss their card when they reach the slot
        cust.aimingPoint = aimingPoint;

        // assign specific waiting point to the customer and mark it occupied
        var assignedPoint = waitingPoints[freeWaitingIndex];
        if (assignedPoint != null)
        {
            cust.MoveTo(assignedPoint.transform);
            waitingPointOccupied[freeWaitingIndex] = true;
            waitingAssignments[cust] = freeWaitingIndex;
        }

        // attempt to place into first free slot in customers list
        if (!AddCustomer(cust))
        {
            // no free slot -> cleanup
            waitingAssignments.Remove(cust);
            if (assignedPoint != null) waitingPointOccupied[freeWaitingIndex] = false;
            Destroy(go);
            return;
        }

        // subscribe so manager cleans up the slot and waiting point when customer is served
        cust.OnServed += HandleCustomerServed;
    }

    int FindFirstFreeWaitingPointIndex()
    {
        if (waitingPoints == null) return -1;
        for (int i = 0; i < waitingPoints.Length; i++)
        {
            if (waitingPoints[i] == null) continue; // skip null entries
            if (waitingPointOccupied == null || i >= waitingPointOccupied.Length) continue;
            if (!waitingPointOccupied[i]) return i;
        }
        return -1;
    }

    void HandleCustomerServed(customerScript c)
    {
        if (c != null) c.OnServed -= HandleCustomerServed;

        // free assigned waiting point if any
        if (c != null && waitingAssignments.TryGetValue(c, out int idx))
        {
            if (waitingPointOccupied != null && idx >= 0 && idx < waitingPointOccupied.Length)
                waitingPointOccupied[idx] = false;
            waitingAssignments.Remove(c);
        }

        RemoveCustomer(c);
    }

    // trigger all current customers to walk to exit (developer shortcut)
    public void SatisfyAllCustomers()
    {
        for (int i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            if (c != null)
            {
                c.Satisfy();
            }
        }
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

        // also free waiting point if mapping exists
        if (waitingAssignments.TryGetValue(c, out int idx))
        {
            if (waitingPointOccupied != null && idx >= 0 && idx < waitingPointOccupied.Length)
                waitingPointOccupied[idx] = false;
            waitingAssignments.Remove(c);
        }

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
