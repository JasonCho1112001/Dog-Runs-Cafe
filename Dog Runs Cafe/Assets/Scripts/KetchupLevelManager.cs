using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UI;
using TMPro;

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

    // -----------------------
    // TIMER FIELDS (NEW)
    // -----------------------
    private float timer = 5f;          // countdown duration
    private bool timerActive = false;  
    private TextMeshProUGUI timerText;
    private GameObject timerCanvas;
    // -----------------------

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
        LoadLevel(currentLevel);
        SetupTimerUI();
        ResetTimer();
    }

    // -----------------------
    // SET UP TIMER UI (NEW)
    // -----------------------
    void SetupTimerUI()
    {
        timerCanvas = new GameObject("KetchupTimerCanvas");
        timerCanvas.layer = LayerMask.NameToLayer("UI");

        Canvas c = timerCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        timerCanvas.AddComponent<CanvasScaler>();
        timerCanvas.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("TimerText");
        textGO.transform.SetParent(timerCanvas.transform);
        timerText = textGO.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 36;
        timerText.color = Color.red;
        timerText.alignment = TextAlignmentOptions.Center;

        RectTransform rt = timerText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.9f);
        rt.anchorMax = new Vector2(0.5f, 0.9f);
        rt.anchoredPosition = Vector2.zero;
    }
    // -----------------------

    // Called by gameManager on difficulty changes
    public void SetDifficultyLevel(int difficulty)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 3);
        LoadLevel(difficulty);
        ResetTimer();
    }

    public void ResetLevel()
    {
        LoadLevel(currentLevel);
        ResetTimer();
    }

    public void OnLevelStart()
    {
        ResetLevel();
    }

    void Update()
    {
        // -----------------------
        // TIMER LOGIC (NEW)
        // -----------------------
        if (timerActive)
        {
            timer -= Time.deltaTime;

            if (timerText != null)
                timerText.text = $"Time Left: {timer:F1}";

            if (timer <= 0)
            {
                timerActive = false;

                var gm = gameManagerScript.Instance;
                if (gm != null)
                {
                    gm.OnPlayerRanOutOfTimeRestartLevel("You lost a life!", 2.0f);
                }

                return; // skip rest of update
            }
        }
        // -----------------------

        if (finishAction.WasPerformedThisFrame() && !isTransitioning)
        {
            float scoreAccuracy = ketchupScoreManager.Instance.GetScoreAccuracy();
            int nextLevel = currentLevel + 1;

            if (!AreAllOmelettesHit() && scoreAccuracy < 50f)
                nextLevel = currentLevel;

            if (nextLevel <= 3)
            {
                StartCoroutine(TransitionToLevel(nextLevel));
            }
        }
    }

    // Reset timer when level loads
    void ResetTimer()
    {
        timer = 5f;
        timerActive = true;
        if (timerText != null)
            timerText.text = $"Time Left: {timer:F1}";
    }

    IEnumerator TransitionToLevel(int levelIndex)
    {
        isTransitioning = true;

        yield return new WaitForSeconds(3f);

        CleanupSpawnedObjects();
        uiResetHandler.ResetScoreText();

        var gm = gameManagerScript.Instance;
        if (gm != null)
        {
            gm.OnLevelPassed("Task Completed!", 2.0f);
        }

        isTransitioning = false;
    }

    void LoadLevel(int difficulty)
    {
        difficulty = Mathf.Clamp(difficulty, 1, 3);

        SetLevelActive(level1Plates, false);
        SetLevelActive(level2Plates, false);
        SetLevelActive(level3Plates, false);

        switch (difficulty)
        {
            case 1: SetLevelActive(level1Plates, true); break;
            case 2: SetLevelActive(level2Plates, true); break;
            case 3: SetLevelActive(level3Plates, true); break;
        }

        currentLevel = difficulty;
        scoreManager.allHits.Clear();

        activeOmelettes.Clear();
        OmeletteController[] allOmelettes = 
            FindObjectsByType<OmeletteController>(FindObjectsSortMode.None);

        foreach (OmeletteController omelette in allOmelettes)
        {
            if (omelette != null && omelette.gameObject.activeInHierarchy)
                activeOmelettes.Add(omelette);
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
            if (ketchupSplash != null) Destroy(ketchupSplash);
        }
        spawnedObjects.Clear();
    }

    public void RegisterSpawnedObject(GameObject ketchupSplat)
    {
        spawnedObjects.Add(ketchupSplat);
    }

    public bool AreAllOmelettesHit()
    {
        if (activeOmelettes.Count == 0) return false;

        foreach (OmeletteController omelette in activeOmelettes)
        {
            if (omelette != null && !omelette.hasKetchup)
                return false;
        }
        return true;
    }
}
