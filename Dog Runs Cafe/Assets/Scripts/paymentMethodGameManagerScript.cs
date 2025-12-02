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

    // how many customers must be served to win this level (set from difficulty)
    int remainingToServe = 0;
    // flag to prevent multiple win triggers
    bool levelCompleted = false;

    // helper: whether any active customer slots remain
    bool AnyActiveCustomers()
    {
        if (customers == null) return false;
        foreach (var c in customers) if (c != null) return true;
        return false;
    }
    // call after a customer is served/removed
    void CheckForLevelWin()
    {
        if (levelCompleted) return;
        if (remainingToServe > 0) return;
        // ensure no active customers remain in the list
        if (AnyActiveCustomers()) return;

        levelCompleted = true;
        levelActive = false;
        if (spawnRoutine != null) { StopCoroutine(spawnRoutine); spawnRoutine = null; }

        Debug.Log("All customers served -> level complete.");
        var gm = FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnLevelPassed("Task Completed!", 2.0f);
        }
    }

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

    // Support the mini-game hopping system: Start level when this component / GameObject becomes enabled.
    void OnEnable()
    {
        StartLevelIfReady();
    }

    void Start()
    {
        // Start level logic (also called from OnEnable so manager works when GameObject is toggled by gameManager)
        StartLevelIfReady();
    }

    // minimal helper to start the level / spawn loop when the manager becomes active
    void StartLevelIfReady()
    {
        Debug.Log($"GM StartLevelIfReady: remainingTime={remainingTime}, spawnPoint={(spawnPoint!=null)}, customer={(customer!=null)}, spawnInterval={spawnInterval}");
        if (remainingTime > 0f)
        {
            levelActive = true;
            // initialize remainingToServe from configured customerListLength (set by ApplyDifficulty)
            remainingToServe = Mathf.Max(0, customerListLength);
            levelCompleted = false;
            if (spawnPoint != null && customer != null && spawnInterval > 0f && spawnRoutine == null)
            {
                Debug.Log("Starting SpawnLoop coroutine");
                // spawn one immediately so the level has at least one customer without waiting
                TrySpawnCustomer();
                spawnRoutine = StartCoroutine(SpawnLoop());
            }
            else
            {
                Debug.LogWarning("SpawnLoop NOT started: check spawnPoint, customer prefab, spawnInterval > 0 or spawnRoutine already running");
            }
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

        Debug.Log($"Level time expired for difficulty {difficulty}. Remaining customers must still be served.");

        // Notify game manager to handle life loss / transition / restart sequence.
        var gm = FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            // Use a short message and delay; gameManager handles restarts.
            gm.OnPlayerRanOutOfTimeRestartLevel("You lost a life! Retrying current level...", 2.0f);
        }
        else
        {
            // fallback: restart current scene immediately if no game manager present
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
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
        Debug.Log("SpawnLoop entered");
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            TrySpawnCustomer();
        }
    }

    void TrySpawnCustomer()
    {
        Debug.Log($"TrySpawnCustomer: spawnPoint={(spawnPoint!=null)}, customer={(customer!=null)}, IsFull={IsFull()}, customerCount={customers?.Count}");
        if (customer == null || spawnPoint == null) 
        {
            Debug.LogWarning("TrySpawnCustomer aborted: missing customer prefab or spawnPoint");
            return;
        }
        if (IsFull()) 
        {
            Debug.Log("TrySpawnCustomer aborted: customer list full");
            return;
        }

        int freeWaitingIndex = FindFirstFreeWaitingPointIndex();
        if (freeWaitingIndex == -1) return; // no free waiting point available

        // instantiate as child of the game manager so hierarchy stays organized
        GameObject go = Instantiate(customer, spawnPoint.position, spawnPoint.rotation, this.transform);
        // ensure it's active even if the prefab was mistakenly saved inactive
        if (!go.activeSelf) go.SetActive(true);
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

        // decrement remaining target count when a customer is served
        if (remainingToServe > 0)
            remainingToServe = Mathf.Max(0, remainingToServe - 1);

        RemoveCustomer(c);

        // check whether level has been completed
        CheckForLevelWin();
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

    // Check if the customer list is full (no null slots)
    public bool IsFull()
    {
        if (customers == null) return true;
        for (int i = 0; i < customers.Count; i++)
        {
            if (customers[i] == null) return false;
        }
        return true;
    }
}
