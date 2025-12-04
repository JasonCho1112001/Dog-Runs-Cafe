using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class gameManagerScript : MonoBehaviour
{
    // singleton instance to ensure callers always talk to the same manager
    public static gameManagerScript Instance { get; private set; }

    public GameObject[] miniGames;

    [Header("Lives")]
    [Tooltip("Starting number of lives for the player.")]
    public int maxLives = 5;
    [Tooltip("Current remaining lives (runtime).")]
    public int currentLives;
    [Tooltip("Optional TextMeshProUGUI to display remaining lives.")]
    public TextMeshProUGUI livesText;

    [Header("Playthrough")]
    [Tooltip("Current level index in the session (0..totalLevels-1). Not persisted.")]
    public int currentLevelIndex = 0;
    [Tooltip("Total levels in the session (miniGames.Length * difficulties).")]
    public int totalLevels = 9; // 3 minigames * 3 difficulties

    int MiniGameCount => Mathf.Max(1, miniGames != null ? miniGames.Length : 1);

    void Awake()
    {
        // ensure single instance
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"Duplicate gameManagerScript instance detected on '{gameObject.name}'. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // initialize lives before Start runs
        currentLives = Mathf.Max(0, maxLives);
        // clamp totalLevels to sensible value based on miniGames count
        totalLevels = Mathf.Max(totalLevels, MiniGameCount * 3);
        UpdateLivesUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Decrease lives by amount (default 1). Call this from other systems when player loses a life.
    public void LoseLife(int amount = 1)
    {
        currentLives = Mathf.Max(0, currentLives - amount);
        Debug.Log($"Life lost. Remaining lives: {currentLives}");
        UpdateLivesUI();
        if (currentLives <= 0)
            OnOutOfLives();
    }

    // Increase lives (capped at maxLives)
    public void GainLife(int amount = 1)
    {
        currentLives = Mathf.Min(maxLives, currentLives + amount);
        Debug.Log($"Life gained. Remaining lives: {currentLives}");
        UpdateLivesUI();
    }
    
    // Reset lives back to max
    public void ResetLives()
    {
        currentLives = Mathf.Max(0, maxLives);
        Debug.Log($"Lives reset to {currentLives}");
        UpdateLivesUI();
    }

    // Called when lives reach zero
    void OnOutOfLives()
    {
        Debug.Log("Out of lives! Implement game over handling here.");
        UpdateLivesUI();
        // Optional: restart current scene or show game over UI
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Update the assigned TextMeshProUGUI with the current lives (safe if null)
    void UpdateLivesUI()
    {
        if (livesText == null) return;
        livesText.text = $"Lives: {currentLives}/{maxLives}";
    }
    
    // Called by mini-game managers when time runs out
    public void OnPlayerRanOutOfTimeRestartLevel(string message = "You lost a life! Retrying current level...", float displaySeconds = 2f)
    {
        // prevent multiple concurrent sequences
        StopAllCoroutines();
        StartCoroutine(LoseLifeAndRestartRoutine(message, displaySeconds));
    }

    IEnumerator LoseLifeAndRestartRoutine(string message, float displaySeconds)
    {
        // decrement life immediately
        LoseLife(1);

        // create a simple fullscreen black canvas with message
        GameObject canvasGO = new GameObject("LifeLost_Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // background image (black)
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var img = bg.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // message text
        GameObject textGO = new GameObject("Message");
        textGO.transform.SetParent(canvasGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = message;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        // Arial.ttf is not a valid builtin in newer Unity versions; use LegacyRuntime.ttf and fall back to a dynamic OS font.
        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null)
            runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 32); // fallback
        txt.font = runtimeFont;
        txt.fontSize = 32;

        var tRT = txt.rectTransform;
        tRT.anchorMin = new Vector2(0.05f, 0.4f);
        tRT.anchorMax = new Vector2(0.95f, 0.6f);
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        // wait while showing message, then restart current level
        float t = 0f;
        while (t < displaySeconds)
        {
            t += Time.unscaledDeltaTime; // use unscaled so it works regardless of timeScale
            yield return null;
        }

        // cleanup UI
        if (canvasGO != null) Destroy(canvasGO);

        // Restart current level (do not reload scene; reinitialize current mini-game)
        RestartCurrentLevel();
    }

    // Called by a mini-game when the player completes the level successfully.
    // Shows a short "Task finished!" transition then advances to the next level.
    bool transitionInProgress = false;
    public void OnLevelPassed(string message = "Task Completed!", float displaySeconds = 2f)
    {
        if (transitionInProgress) return;
        StartCoroutine(LevelPassedRoutine(message, displaySeconds));
    }

    IEnumerator LevelPassedRoutine(string message, float displaySeconds)
    {
        transitionInProgress = true;

        // create a simple fullscreen black canvas with message (re-using pattern from LoseLife routine)
        GameObject canvasGO = new GameObject("LevelPassed_Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var img = bg.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("Message");
        textGO.transform.SetParent(canvasGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = message;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null) runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
        txt.font = runtimeFont;
        txt.fontSize = 36;
        var tRT = txt.rectTransform;
        tRT.anchorMin = new Vector2(0.05f, 0.4f);
        tRT.anchorMax = new Vector2(0.95f, 0.6f);
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        // wait while showing message (use unscaled time)
        float t = 0f;
        while (t < displaySeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canvasGO != null) Destroy(canvasGO);

        // Advance to next level and start it
        AdvanceToNextLevel();

        transitionInProgress = false;
    }

    // ----- Level progression helpers -----
    [System.Serializable]
    public struct LevelDef
    {
        public int miniGameIndex; // index into miniGames[]
        public int difficulty;    // 1..3
    }

    [Header("Optional custom level order")]
    [Tooltip("If non-empty this sequence is used instead of the implicit ordering.")]
    public LevelDef[] customLevelSequence;

    // replace GetMiniGameIndexForLevel / GetDifficultyForLevel with:
    int GetMiniGameIndexForLevel(int levelIndex)
    {
        if (customLevelSequence != null && levelIndex >= 0 && levelIndex < customLevelSequence.Length)
            return Mathf.Clamp(customLevelSequence[levelIndex].miniGameIndex, 0, miniGames.Length - 1);
        return levelIndex % MiniGameCount;
    }

    int GetDifficultyForLevel(int levelIndex)
    {
        if (customLevelSequence != null && levelIndex >= 0 && levelIndex < customLevelSequence.Length)
            return Mathf.Clamp(customLevelSequence[levelIndex].difficulty, 1, 3);
        return Mathf.Clamp((levelIndex / MiniGameCount) + 1, 1, 3);
    }
    
    // Activate the appropriate mini-game for currentLevelIndex and send difficulty
    public void StartCurrentLevel()
    {
        if (miniGames == null || miniGames.Length == 0) return;
        int idx = Mathf.Clamp(currentLevelIndex, 0, totalLevels - 1);
        int miniIdx = GetMiniGameIndexForLevel(idx);
        int difficulty = GetDifficultyForLevel(idx);

        // deactivate all games
        for (int i = 0; i < miniGames.Length; i++)
        {
            if (miniGames[i] != null) miniGames[i].SetActive(false);
        }

        // activate target mini-game
        var target = miniGames[miniIdx];
        if (target != null)
        {
            // 1) Apply difficulty to any component in the target (including inactive children) BEFORE activation.
            var childComps = target.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in childComps)
            {
                if (mb == null) continue;
                var setDiff = mb.GetType().GetMethod("SetDifficultyLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (setDiff != null)
                {
                    try { setDiff.Invoke(mb, new object[] { difficulty }); }
                    catch { /* ignore invocation errors */ }
                }
            }

            // 2) Now activate the mini-game
            target.SetActive(true);

            // 3) Call ResetLevel and OnLevelStart on any component that implements them.
            var activeComps = target.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in activeComps)
            {
                if (mb == null) continue;
                var reset = mb.GetType().GetMethod("ResetLevel", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (reset != null)
                {
                    try { reset.Invoke(mb, null); }
                    catch { }
                }
                var onStart = mb.GetType().GetMethod("OnLevelStart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (onStart != null)
                {
                    try { onStart.Invoke(mb, null); }
                    catch { }
                }
            }

            Debug.Log($"Starting level {currentLevelIndex} -> miniGame {miniIdx} difficulty {difficulty}");
        }
    }
    
    // Restart the current active mini-game (reinitialize its state)
    public void RestartCurrentLevel()
    {
        if (miniGames == null || miniGames.Length == 0) return;
        int idx = Mathf.Clamp(currentLevelIndex, 0, totalLevels - 1);
        int miniIdx = GetMiniGameIndexForLevel(idx);
        var target = miniGames[miniIdx];
        if (target == null) return;

        // quick restart: deactivate & reactivate and send ResetLevel
        StartCoroutine(RestartRoutine(target));
    }
    
    System.Collections.IEnumerator RestartRoutine(GameObject target)
    {
        target.SetActive(false);
        yield return null; // wait a frame
        target.SetActive(true);
        target.SendMessage("ResetLevel", SendMessageOptions.DontRequireReceiver);
        target.SendMessage("OnLevelStart", SendMessageOptions.DontRequireReceiver);

        // reload the current scene to ensure a full clean restart
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
    
    // Advance to the next level in the playthrough
    public void AdvanceToNextLevel()
    {
        Debug.Log($"Advancing to next level from {currentLevelIndex}.");
        currentLevelIndex++;
        if (currentLevelIndex >= totalLevels)
        {
            Debug.Log($"Playthrough complete. currentLevelIndex={currentLevelIndex}, totalLevels={totalLevels}");
            // Keep the index to indicate completion (one past the last valid level).
            // If you prefer to keep it at the last valid level, uncomment the next line instead:
            // currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, totalLevels - 1);
            // implement end-of-run behavior if desired
            return;
        }
        StartCurrentLevel();
    }

    // Advance to the next mini-game (same difficulty block).
    public void AdvanceToNextMiniGame()
    {
        int miniCount = MiniGameCount;
        if (miniCount <= 0) return;

        int difficulty = GetDifficultyForLevel(currentLevelIndex); // 1..3
        int miniIdx = GetMiniGameIndexForLevel(currentLevelIndex); // 0..miniCount-1

        int nextMiniIdx = (miniIdx + 1) % miniCount;
        int newLevelIndex = (difficulty - 1) * miniCount + nextMiniIdx;

        currentLevelIndex = Mathf.Clamp(newLevelIndex, 0, totalLevels - 1);
        Debug.Log($"AdvanceToNextMiniGame -> level {currentLevelIndex} (mini {nextMiniIdx}, diff {difficulty})");
        StartCurrentLevel();
    }

    // Public API: start the mini-game-passed transition (shows UI then advances to next mini-game)
    public void OnMiniGamePassed(string message = "Good job!", float displaySeconds = 2f)
    {
        if (transitionInProgress) return;
        StartCoroutine(MiniGamePassedRoutine(message, displaySeconds));
    }

    IEnumerator MiniGamePassedRoutine(string message, float displaySeconds)
    {
        transitionInProgress = true;

        // create a simple fullscreen black canvas with message (same as LevelPassedRoutine)
        GameObject canvasGO = new GameObject("LevelPassed_Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
 
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var img = bg.AddComponent<Image>();
        img.color = Color.black;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("Message");
        textGO.transform.SetParent(canvasGO.transform, false);
        var txt = textGO.AddComponent<Text>();
        txt.text = message;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (runtimeFont == null) runtimeFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
        txt.font = runtimeFont;
        txt.fontSize = 36;
        var tRT = txt.rectTransform;
        tRT.anchorMin = new Vector2(0.05f, 0.4f);
        tRT.anchorMax = new Vector2(0.95f, 0.6f);
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;

        float t = 0f;
        while (t < displaySeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (canvasGO != null) Destroy(canvasGO);

        // advance specifically to the next mini-game
        AdvanceToNextMiniGame();

        transitionInProgress = false;
    }

    // ----- end helpers -----

    void Start()
    {
        //Set Cursor to be invisible and locked at start
        if (SceneManager.GetActiveScene().name == "Main Game")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        //Disable all minigames except 2nd one at start
        // start from stored currentLevelIndex (default 0)
        StartCurrentLevel();
    }
    
    void Update()
    {
        //Press P to toggle cursor visibility and lock state
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            ToggleCursor();
        }

        // Press R to restart the current scene
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // DEBUG: press L to instantly win the current level and advance
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("Debug: forcing level win/advance");
            OnLevelPassed("Cheat: Level Skipped!", 0.5f);
        }

        // Press 1,2,3... to load mini-games (I know this code is spaghetti)
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            currentLevelIndex = 0;
            StartCurrentLevel();
        }
        else if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            currentLevelIndex = 1;
            StartCurrentLevel();
        }
        else if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            currentLevelIndex = 2;
            StartCurrentLevel();
        }
    }

    public void ToggleCursor()
    {
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Main Game");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
