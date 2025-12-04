using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;
using System.Collections;

public enum DifficultyLevel
 {
     Easy,
     Medium,
     Hard
 }

 public class GameTimer : MonoBehaviour
 {
    [Header("UI")]
    [Tooltip("Optional TextMeshProUGUI to display remaining time.")]
    public TextMeshProUGUI timerText;

    [Header("Tray")]
    [Tooltip("Optional reference to the tray transform to snap flat on reset.")]
    public Transform tray;
    [Tooltip("If true the tray will be forced flat (zero pitch/roll) when ResetLevel is called.")]
    public bool setTrayFlatOnReset = true;

    [Header("Player")]
    [Tooltip("Optional reference to the player transform to snap upright on reset.")]
    public Transform player;
    [Tooltip("If true the player will be forced upright (zero pitch/roll) when ResetLevel is called.")]
    public bool setPlayerUprightOnReset = true;

    [Header("Behavior")]
    [Tooltip("When true this component will accept difficulty settings from gameManager. If false the component keeps its own progression.")]
    public bool allowExternalDifficulty = true;
    [Tooltip("Enable debug logs for difficulty changes.")]
    public bool debugLogDifficulty = false;

     [Header("Timer Settings")]
     public float surviveTime = 10f;

     private float timer = 0f;
     private bool gameOver = false;

     private float printInterval = 1f;
     private float nextPrintTime = 1f;

    [Header("UI Popups")]

    public TextMeshProUGUI resultText;
    public float popupDuration = 3f;

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

    // remember initial mug transforms so ResetMugsIfNeeded can restore them
    private Vector3[] mugStartLocalPositions;
    private Quaternion[] mugStartLocalRotations;

    void Awake()
    {
        CacheMugStartTransforms();

        if (resultText != null) resultText.gameObject.SetActive(false);

    }

    void CacheMugStartTransforms()
    {
        if (mugs == null) return;
        mugStartLocalPositions = new Vector3[mugs.Length];
        mugStartLocalRotations = new Quaternion[mugs.Length];
        for (int i = 0; i < mugs.Length; i++)
        {
            var m = mugs[i];
            if (m == null) continue;
            var t = m.transform;
            mugStartLocalPositions[i] = t.localPosition;
            mugStartLocalRotations[i] = t.localRotation;
        }
    }

     void Start()
     {
        // ensure timer/reset state is clean when started standalone
        ApplyDifficultyToScene();
        ResetLevelState();
        UpdateTimerUI();

     }

     void OnEnable()
     {
        // When the mini-game is activated by the global manager, ensure it resets
        ApplyDifficultyToScene();
        // ResetLevel is intended to be called by gameManager when it wants a fresh start;
        // OnEnable should avoid overriding an internal progression, so call ResetLevelState only.
         ResetLevelState();
         EnsureTrayFlat();
        UpdateTimerUI();
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

        // update UI each frame while running
        UpdateTimerUI();

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
        for (int i = 0; i < mugs.Length; i++)
        {
            var m = mugs[i];
            if (m == null) continue;

            // restore recorded local transform if available
            if (mugStartLocalPositions != null && i < mugStartLocalPositions.Length && mugStartLocalRotations != null)
            {
                var t = m.transform;
                t.localPosition = mugStartLocalPositions[i];
                t.localRotation = mugStartLocalRotations[i];
            }

            // clear physics so mug doesn't keep falling/spinning after reset
            var rb = m.GetComponent<Rigidbody>() ?? m.GetComponentInChildren<Rigidbody>();
            if (rb != null)
            {
                // stop motion and snap rigidbody to transform
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = m.transform.position;
                rb.rotation = m.transform.rotation;
            }

            // allow mug objects to reset themselves if they expose ResetMug()
            m.SendMessage("ResetMug", SendMessageOptions.DontRequireReceiver);
        }

        // Ensure only the correct number of mugs are active according to difficulty
        ActivateMugsForDifficulty(GetMugCount());
    }

    // Win Condition
    public void Win()
    {
        if (gameOver) return;
        gameOver = true;

        ShowResultPopup("Fur-nomenal!", Color.yellow); 
        Debug.Log("YOU WON THIS LEVEL! Advancing local difficulty and transitioning to next mini-game.");

        // Advance local difficulty and restart locally (do not immediately accept external difficulty overrides)
        AdvanceDifficultyAndPrepareForTransition();

        // Notify game manager to handle transition to next mini-game
        var gm = gameManagerScript.Instance ?? FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnLevelPassed("", 0f);   // no message
        }
    }

    // Lose Condition (Spill)
    public void Lose()
    {
        if (gameOver) return;

        ShowResultPopup("You Spilled!", Color.red);

        gameOver = true;
        Debug.Log("You spilled! Retrying same difficulty...");

        // notify global manager to handle life loss & restart current level if available
        var gm = gameManagerScript.Instance ?? FindObjectOfType<gameManagerScript>();
        if (gm != null)
        {
            gm.OnPlayerRanOutOfTimeRestartLevel("", 0f);   // no message
        }
        else
        {
            // fallback: restart locally without scene reload
            ResetLevel();
        }
    }

    // Advance the local difficulty index and prepare the mini-game for when it is shown again.
    public void AdvanceDifficultyAndPrepareForTransition()
    {
        // Advance index but clamp to max (do not loop)
        difficultyIndex++;
        if (difficultyIndex >= difficultyOrder.Length) difficultyIndex = difficultyOrder.Length - 1;

        // apply new difficulty value and tuned surviveTime
        difficulty = difficultyOrder[difficultyIndex];
        switch (difficultyIndex + 1) // 1..3 mapping
        {
            case 1: surviveTime = 15f; break;
            case 2: surviveTime = 25f; break;
            case 3: surviveTime = 35f; break;
            default: surviveTime = 15f; break;
        }

        if (debugLogDifficulty) Debug.Log($"GameTimer: locally advanced difficulty -> {difficulty} (idx {difficultyIndex})");

        // keep local progression: ignore gameManager SetDifficultyLevel until ResetLevel is called by manager
        allowExternalDifficulty = false;

        // apply scene changes and reset runtime state (no scene reload)
        EnsureTrayFlat();
        ResetMugsIfNeeded();
        ApplyDifficultyToScene();
        ResetLevelState(); // keep allowExternalDifficulty as set above
        ResetMugsIfNeeded();
        UpdateTimerUI();
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
        // if this mini-game advanced itself and chose to keep local progression, ignore external changes
        if (!allowExternalDifficulty)
        {
            if (debugLogDifficulty) Debug.Log($"GameTimer: ignoring external SetDifficultyLevel({difficultyLevel}) because allowExternalDifficulty==false");
            return;
        }

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

        if (debugLogDifficulty) Debug.Log($"GameTimer: SetDifficultyLevel({clamped}) applied -> difficulty {difficulty}");

        // apply changes to scene and reset local state
        ApplyDifficultyToScene();
        ResetLevelState();
        UpdateTimerUI();
     }

     // Called by gameManager when restarting the current mini-game.
     public void ResetLevel()
     {
        Debug.Log("[GameTimer] ResetLevel invoked");
         // Resetting via gameManager means we should accept future SetDifficultyLevel calls again
         allowExternalDifficulty = true;
         if (debugLogDifficulty) Debug.Log("GameTimer: ResetLevel called -> allowExternalDifficulty = true");
         
        ResetLevelState();
        // ensure mugs match the configured difficulty and are reactivated
        ApplyDifficultyToScene();
        ActivateMugsForDifficulty(GetMugCount());
        ResetMugsIfNeeded();

        // make sure physical tray and player are leveled on reset
        EnsureTrayFlat();


        // update UI and internal state
        UpdateTimerUI();
     }
     
     // Optional hook when the level is actively started by the game manager
     public void OnLevelStart()
     {
        // Starting via gameManager should accept external difficulty
        allowExternalDifficulty = true;
        if (debugLogDifficulty) Debug.Log("GameTimer: OnLevelStart called -> allowExternalDifficulty = true");

         ResetLevel();
     }
     
     // Ensure surviveTime / mugs are set according to current difficulty
     void ApplyDifficultyToScene()
     {
         ActivateMugsForDifficulty(GetMugCount());
     }

     // Helper to reset timer and internal flags (does not change difficulty)
     void ResetLevelState()
     {
         timer = 0f;
         gameOver = false;
         nextPrintTime = 1f;
        UpdateTimerUI();
     }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        float remaining = Mathf.Max(0f, surviveTime - timer);
        int totalSeconds = Mathf.CeilToInt(remaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"Survive: {minutes}:{seconds:00}";
    }

    // Force the tray to a flat orientation and clear physics velocities if present.
    void EnsureTrayFlat()
    {
        if (tray == null) return;
        Debug.Log("GameTimer: Ensuring tray is flat on reset.");
        // zero pitch and roll, preserve yaw
        Vector3 e = tray.localEulerAngles;
        tray.localEulerAngles = new Vector3(0f, e.y, 0f);

        // clear physics so it doesn't keep tumbling
        var rb = tray.GetComponent<Rigidbody>() ?? tray.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.rotation = Quaternion.Euler(tray.localEulerAngles);
        }

        // also ensure player is upright if requested
        if (player != null && setPlayerUprightOnReset)
        {
            Vector3 pe = player.localEulerAngles;
            player.localEulerAngles = new Vector3(0f, pe.y, 0f);
            var prb = player.GetComponent<Rigidbody>() ?? player.GetComponentInChildren<Rigidbody>();
            if (prb != null)
            {
                prb.linearVelocity = Vector3.zero;
                prb.angularVelocity = Vector3.zero;
                prb.rotation = Quaternion.Euler(player.localEulerAngles);
            }
        }
     }

    // Popup for UI Text
     public void ShowResultPopup(string message, Color color)
    {

        
        if (resultText == null) return;

        resultText.text = message;
        resultText.color = color;
        resultText.gameObject.SetActive(true);

        StopAllCoroutines();
        Debug.Log("Displaying Message!");
        StartCoroutine(PopupAnimation(resultText.transform));
    }

    // Popup animation for UI Text
   IEnumerator PopupAnimation(Transform targetTransform)
    {
        targetTransform.localScale = Vector3.zero;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            targetTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one/2.5f, t);
            yield return null;
        }
        targetTransform.localScale = Vector3.one/2.5f;
    }

 }