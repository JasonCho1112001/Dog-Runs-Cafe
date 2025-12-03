using UnityEngine;
using UnityEngine.SceneManagement;

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float surviveTime = 10f;

    private float timer = 0f;
    private bool gameOver = false;

    private float printInterval = 1f;
    private float nextPrintTime = 1f;

    [Header("Difficulty Settings")]
    public DifficultyLevel difficulty = DifficultyLevel.Easy;

    private DifficultyLevel[] difficultyOrder =
    {
        DifficultyLevel.Easy,
        DifficultyLevel.Medium,
        DifficultyLevel.Hard
    };

    private int difficultyIndex = 0;

    [Header("Mugs in Scene (Assign 4 mugs here)")]
    public GameObject[] mugs;   // drag your 4 mugs into the inspector

    void Start()
    {
        // ensure timer/reset state is clean when started standalone
        ApplyDifficultyToScene();
        ResetLevelState();
    }

    void OnEnable()
    {
        // When the mini-game is activated by the global manager, ensure it resets
        ApplyDifficultyToScene();
        ResetLevel();
    }
    
    void OnDisable()
    {
        // stop timer activities while disabled
        StopAllCoroutines();
    }

    void Update()
    {
        if (gameOver) return;

        timer += Time.deltaTime;

        // Print timer every 1 second
        if (timer >= nextPrintTime)
        {
            Debug.Log("Timer: " + Mathf.FloorToInt(timer) + "s");
            nextPrintTime += printInterval;
        }

        // Win condition
        if (timer >= surviveTime)
        {
            Win();
        }
    }

    // Difficulty → Mug Count Logic
    int GetMugCount()
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:   return 1;
            case DifficultyLevel.Medium: return 2;
            case DifficultyLevel.Hard:   return 4;
            default: return 1;
        }
    }

    // Activate only the mugs needed
    void ActivateMugsForDifficulty(int count)
    {
        if (mugs == null) return;
        for (int i = 0; i < mugs.Length; i++)
        {
            if (mugs[i] != null)
                mugs[i].SetActive(i < count);
        }
    }

    // Reset individual mug if it supports a reset hook
    void ResetMugsIfNeeded()
    {
        if (mugs == null) return;
        foreach (var m in mugs)
        {
            if (m == null) continue;
            // allow mug objects to reset themselves if they expose ResetMug()
            m.SendMessage("ResetMug", SendMessageOptions.DontRequireReceiver);
        }
    }

    // Lose Condition (Spill)
    public void Lose()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("You spilled! Retrying same difficulty...");

        // notify global manager to handle life loss & restart current level if available
        var gm = gameManagerScript.Instance ?? FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnPlayerRanOutOfTimeRestartLevel("You lost a life! Retrying current level...", 2f);
        }
        else
        {
            // fallback: retry locally
            ReloadScene();   // retry same difficulty
        }
    }

    // Win Condition
    void Win()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("YOU WON THIS LEVEL!");

        // notify global manager to handle transition and advancing levels if available
        var gm = gameManagerScript.Instance ?? FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnLevelPassed("Good job!", 2f);
        }
        else
        {
            // fallback local behaviour: advance difficulty and reload scene
            AdvanceDifficulty();
            ReloadScene();
        }
    }

    // Cycle to next difficulty
    void AdvanceDifficulty()
    {
        difficultyIndex++;

        if (difficultyIndex >= difficultyOrder.Length)
            difficultyIndex = 0;   // loop back to easy

        difficulty = difficultyOrder[difficultyIndex];

        Debug.Log("➡️ Next Difficulty: " + difficulty);
    }

    // Scene Reload
    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ----- Integration API for gameManager -----
    // gameManager will call SetDifficultyLevel(int 1..3) to configure this mini-game.
    public void SetDifficultyLevel(int difficultyLevel)
    {
        // map 1->Easy, 2->Medium, 3->Hard
        int clamped = Mathf.Clamp(difficultyLevel, 1, 3);
        difficultyIndex = clamped - 1;
        difficulty = difficultyOrder[difficultyIndex];

        // set surviveTime per difficulty (tuned values)
        switch (clamped)
        {
            case 1: surviveTime = 15f; break;
            case 2: surviveTime = 25f; break;
            case 3: surviveTime = 35f; break;
            default: surviveTime = 15f; break;
        }

        // apply changes to scene and reset local state
        ApplyDifficultyToScene();
        ResetLevelState();
    }

    // Called by gameManager when restarting the current mini-game.
    public void ResetLevel()
    {
        ResetLevelState();
        ApplyDifficultyToScene();
        ResetMugsIfNeeded();
    }
    
    // Optional hook when the level is actively started by the game manager
    public void OnLevelStart()
    {
        ResetLevel();
    }
    
    // Ensure surviveTime / mugs are set according to current difficulty
    void ApplyDifficultyToScene()
    {
        ActivateMugsForDifficulty(GetMugCount());
    }

    // ResetLevel is called by gameManager when restarting the current mini-game.
    // public void ResetLevel()
    // {
    //     ResetLevelState();
    //     ActivateMugsForDifficulty(GetMugCount());
    // }

    // Helper to reset timer and internal flags (does not change difficulty)
    void ResetLevelState()
    {
        timer = 0f;
        gameOver = false;
        nextPrintTime = 1f;
    }
}