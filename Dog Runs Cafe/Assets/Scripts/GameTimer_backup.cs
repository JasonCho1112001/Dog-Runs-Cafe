// using UnityEngine;
// using UnityEngine.SceneManagement;

// public enum DifficultyLevel
// {
//     Easy,
//     Medium,
//     Hard
// }

// public class GameTimer : MonoBehaviour
// {
//     [Header("Timer Settings")]
//     public float surviveTime = 10f;

//     private float timer = 0f;
//     private bool gameOver = false;

//     private float printInterval = 1f;
//     private float nextPrintTime = 1f;

//     [Header("Difficulty Settings")]
//     public DifficultyLevel difficulty = DifficultyLevel.Easy;

//     private DifficultyLevel[] difficultyOrder =
//     {
//         DifficultyLevel.Easy,
//         DifficultyLevel.Medium,
//         DifficultyLevel.Hard
//     };

//     private int difficultyIndex = 0;

//     [Header("Mugs in Scene (Assign 4 mugs here)")]
//     public GameObject[] mugs;   // drag your 4 mugs into the inspector


//     void Start()
//     {
//         int mugCount = GetMugCount();
//         ActivateMugsForDifficulty(mugCount);
//     }


//     void Update()
//     {
//         if (gameOver) return;

//         timer += Time.deltaTime;

//         // Print timer every 1 second
//         if (timer >= nextPrintTime)
//         {
//             Debug.Log("Timer: " + Mathf.FloorToInt(timer) + "s");
//             nextPrintTime += printInterval;
//         }

//         // Win condition
//         if (timer >= surviveTime)
//         {
//             Win();
//         }
//     }


//     // Difficulty → Mug Count Logic
//     int GetMugCount()
//     {
//         switch (difficulty)
//         {
//             case DifficultyLevel.Easy:   return 1;
//             case DifficultyLevel.Medium: return 2;
//             case DifficultyLevel.Hard:   return 4;
//             default: return 1;
//         }
//     }

//     // Activate only the mugs needed
//     void ActivateMugsForDifficulty(int count)
//     {
//         for (int i = 0; i < mugs.Length; i++)
//         {
//             mugs[i].SetActive(i < count);
//         }
//     }


//     // Lose Condition (Spill)
//     public void Lose()
//     {
//         if (gameOver) return;

//         gameOver = true;
//         Debug.Log("You spilled! Retrying same difficulty...");

//         ReloadScene();   // retry same difficulty
//     }


//     // Win Condition
//     void Win()
//     {
//         gameOver = true;
//         Debug.Log("YOU WON THIS LEVEL!");

//         AdvanceDifficulty();
//         //ReloadScene();   // load next difficulty
//     }

//     // Cycle to next difficulty
//     void AdvanceDifficulty()
//     {
//         difficultyIndex++;

//         if (difficultyIndex >= difficultyOrder.Length)
//             difficultyIndex = 0;   // loop back to easy

//         difficulty = difficultyOrder[difficultyIndex];

//         Debug.Log("➡️ Next Difficulty: " + difficulty);
//     }


//     // Scene Reload
//     void ReloadScene()
//     {
//         SceneManager.LoadScene(SceneManager.GetActiveScene().name);
//     }
// }