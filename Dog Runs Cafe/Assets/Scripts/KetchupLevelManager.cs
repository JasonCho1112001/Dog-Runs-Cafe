using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using TMPro;

public class KetchupLevelManager : MonoBehaviour
{
    public static KetchupLevelManager Instance;
    
    [Header("UI")]
    [Tooltip("Optional TextMeshProUGUI to display remaining time.")]
    public TextMeshProUGUI timerText;

    [Header("Timer per difficulty (seconds)")]
    public float level1Time = 30f;
    public float level2Time = 45f;
    public float level3Time = 60f;

    // runtime timer
    [HideInInspector] public float remainingTime = 0f;
    bool timerRunning = false;

    [Header("Level 1 Objects")]
    public GameObject level1Plates;

    [Header("Level 2 Objects")]
    public GameObject level2Plates;

    [Header("Level 3 Objects")]
    public GameObject level3Plates;

    [SerializeField] private playerKetchupScript uiResetHandler;

    private int currentLevel = 1;
    private bool isTransitioning = false;
    private List<GameObject> spawnedObjects = new List<GameObject>();
    private List<OmeletteController> activeOmelettes = new List<OmeletteController>();
    private ketchupScoreManager scoreManager;
    InputAction finishAction;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        finishAction = InputSystem.actions.FindAction("Complete");
        finishAction.Enable();

        scoreManager = GetComponent<ketchupScoreManager>();
    }

    void Start()
    {
        // Ensure standalone start loads currentLevel (defaults to 1)
        LoadLevel(currentLevel);
        UpdateTimerUI();
    }

    // Called by gameManager via SendMessage("SetDifficultyLevel", difficulty)
    // Accepts difficulty 1,2,3 and loads the corresponding local level.
    public void SetDifficultyLevel(int difficulty)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 3);
        LoadLevel(difficulty);
    }

    // Optional ResetLevel hook used by gameManager; reload same difficulty
    public void ResetLevel()
    {
        Debug.Log("[Ketchup] ResetLevel called");

        // stop any transition and ensure timer coroutines are stopped
        isTransitioning = false;
        timerRunning = false;
        StopAllCoroutines();

        // remove any spawned ketchup splats so they don't persist across resets
        CleanupSpawnedObjects();

        // reset score UI / manager state if available
        uiResetHandler?.ResetScoreText();
        if (scoreManager != null) scoreManager.allHits.Clear();

        // Ensure omelettes are put back into initial state if they expose a reset hook (best-effort)
        foreach (var omelette in FindObjectsOfType<OmeletteController>())
        {
            if (omelette != null)
                omelette.gameObject.SendMessage("ResetOmelette", SendMessageOptions.DontRequireReceiver);
        }

        // Rebuild the level (LoadLevel will set remainingTime and start the timer)
        // make sure currentLevel is valid
        currentLevel = Mathf.Clamp(currentLevel, 1, 3);

        // Ensure timeScale is normal
        if (Time.timeScale <= 0f) Time.timeScale = 1f;

        // Call LoadLevel to set remainingTime for the difficulty and start timerRunning
        LoadLevel(currentLevel);
        
        // Enforce timerRunning true and update UI to ensure immediate visible reset
        timerRunning = true;
        UpdateTimerUI();

        Debug.Log($"[Ketchup] ResetLevel finished: level={currentLevel}, remainingTime={remainingTime}, timerRunning={timerRunning}");
    }

    // Optional hook invoked by gameManager when the level is actively started.
    // Keeps compatibility with gameManagerScript which calls OnLevelStart().
    public void OnLevelStart()
    {
        // Ensure the level is initialized / reset when the game manager starts it.
        ResetLevel();
    }

    void Update()
    {
        if (finishAction.WasPerformedThisFrame() && !isTransitioning)
        {
            float scoreAccuracy = ketchupScoreManager.Instance.GetScoreAccuracy();
            int nextLevel = currentLevel + 1;

            if (!AreAllOmelettesHit() && scoreAccuracy < 50f) nextLevel = currentLevel;
            if (nextLevel <= 3)
            {
                StartCoroutine(TransitionToLevel(nextLevel));
            }
        }
        
        // timer countdown while level is active and not transitioning
        if (timerRunning && !isTransitioning)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                timerRunning = false;
                UpdateTimerUI();
                // notify global manager that time ran out (lose a life / restart)
                var gm = FindObjectOfType<gameManagerScript>();
                if (gm != null)
                {
                    gm.OnPlayerRanOutOfTimeRestartLevel("Time's up! You lost a life.", 2f);
                }
                else
                {
                    // local fallback: restart same level
                    StartCoroutine(TransitionToLevel(currentLevel));
                }
            }
            else
            {
                // update UI every frame while running
                UpdateTimerUI();
            }
        }
    }

    IEnumerator TransitionToLevel(int levelIndex)
    {
        isTransitioning = true;
        // stop timer while transitioning
        timerRunning = false;

        yield return new WaitForSeconds(3f);

        CleanupSpawnedObjects();
        uiResetHandler.ResetScoreText();
        //LoadLevel(levelIndex);
        var gm = FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnLevelPassed("Task Completed!", 2.0f);
        }
        isTransitioning = false;
    }

    IEnumerator DelayedOnLevelPassed(gameManagerScript gm)
    {
        // wait 2 seconds before telling the global manager (keeps timing consistent)
        yield return new WaitForSeconds(2f);

        // prefer the singleton instance (more reliable than FindObjectOfType)
        if (gameManagerScript.Instance != null)
        {
            gameManagerScript.Instance.OnLevelPassed("Task Completed!", 2f);
        }
        else if (gm != null)
        {
            gm.OnLevelPassed("Task Completed!", 2f);
        }
        else
        {
            // fallback - try FindObjectOfType once more and call it
            var found = FindObjectOfType<gameManagerScript>();
            if (found != null) found.OnLevelPassed("Task Completed!", 2f);
        }
    }

    // LoadLevel now takes the difficulty (1..3) coming from gameManager's difficulty selection.
    // Internally it's the same mapping used previously: difficulty 1 -> local level1, etc.
    void LoadLevel(int difficulty)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 3);

        SetLevelActive(level1Plates, false);
        SetLevelActive(level2Plates, false);
        SetLevelActive(level3Plates, false);

        // Activate the matching difficulty level
        switch (difficulty)
        {
            case 1: SetLevelActive(level1Plates, true); break;
            case 2: SetLevelActive(level2Plates, true); break;
            case 3: SetLevelActive(level3Plates, true); break;
        }

        currentLevel = difficulty;
        scoreManager.allHits.Clear();

        activeOmelettes.Clear();
        OmeletteController[] allOmelettes = FindObjectsByType<OmeletteController>(FindObjectsSortMode.None);
        foreach (OmeletteController omelette in allOmelettes)
        {
            if (omelette != null && omelette.gameObject.activeInHierarchy)
            {
                activeOmelettes.Add(omelette);
            }
        }
        
        // set timer based on selected difficulty and start countdown
        switch (difficulty)
        {
            case 1: remainingTime = level1Time; break;
            case 2: remainingTime = level2Time; break;
            case 3: remainingTime = level3Time; break;
            default: remainingTime = level1Time; break;
        }
        timerRunning = true;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"Time: {minutes}:{seconds:00}";
    }

    void SetLevelActive(GameObject level, bool activeStatus)
    {
        if (level != null) level.SetActive(activeStatus);
    }
    void CleanupSpawnedObjects()
    {
        foreach (GameObject ketchupSplash in spawnedObjects)
        {
            if (ketchupSplash != null)
            {
                Destroy(ketchupSplash);
            }
        }
        spawnedObjects.Clear();
    }

    public void RegisterSpawnedObject(GameObject ketchupSplat)
    {
        spawnedObjects.Add(ketchupSplat);
    }

    // Check if every omelette has been hit
    public bool AreAllOmelettesHit()
    {
        if (activeOmelettes.Count == 0) return false;

        foreach (OmeletteController omelette in activeOmelettes)
        {
            if (omelette != null && !omelette.hasKetchup) return false;
        }
        return true;
    }
}
