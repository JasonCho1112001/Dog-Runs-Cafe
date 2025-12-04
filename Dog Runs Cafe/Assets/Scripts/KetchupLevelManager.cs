using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;

public class KetchupLevelManager : MonoBehaviour
{
    public static KetchupLevelManager Instance;

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
        LoadLevel(currentLevel);
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
    }

    IEnumerator TransitionToLevel(int levelIndex)
    {
        isTransitioning = true;

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

    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}
