// using System.Collections;
// using System.Collections.Generic;
// using Unity.VisualScripting;
// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.ProBuilder.MeshOperations;

// public class KetchupLevelManager : MonoBehaviour
// {
//     public static KetchupLevelManager Instance;

//     [Header("Level 1 Objects")]
//     public GameObject level1Plates;

//     [Header("Level 2 Objects")]
//     public GameObject level2Plates;

//     [Header("Level 3 Objects")]
//     public GameObject level3Plates;

//     [SerializeField] private playerKetchupScript uiResetHandler;

//     private int currentLevel = 1;
//     private bool isTransitioning = false;
//     private List<GameObject> spawnedObjects = new List<GameObject>();
//     private List<OmeletteController> activeOmelettes = new List<OmeletteController>();
//     private ketchupScoreManager scoreManager;
//     InputAction finishAction;

//     private void Awake()
//     {
//         if (Instance == null)
//             Instance = this;
//         else
//             Destroy(gameObject);

//         finishAction = InputSystem.actions.FindAction("Complete");
//         finishAction.Enable();

//         scoreManager = GetComponent<ketchupScoreManager>();
//     }

//     void Start()
//     {
//         LoadLevel(1);
//     }

//     void Update()
//     {
//         if (finishAction.WasPerformedThisFrame() && !isTransitioning)
//         {
//             float scoreAccuracy = ketchupScoreManager.Instance.GetScoreAccuracy();
//             int nextLevel = currentLevel + 1;

//             if (!AreAllOmelettesHit() && scoreAccuracy < 50f) nextLevel = currentLevel;
//             if (nextLevel <= 3)
//             {
//                 StartCoroutine(TransitionToLevel(nextLevel));
//             }
//         }
//     }

//     IEnumerator TransitionToLevel(int levelIndex)
//     {
//         isTransitioning = true;

//         yield return new WaitForSeconds(5f);

//         CleanupSpawnedObjects();
//         uiResetHandler.ResetScoreText();
//         LoadLevel(levelIndex);
//         isTransitioning = false;
//     }

//     void LoadLevel(int levelIndex)
//     {
//         SetLevelActive(level1Plates, false);
//         SetLevelActive(level2Plates, false);
//         SetLevelActive(level3Plates, false);

//         // Activate target
//         switch (levelIndex)
//         {
//             case 1: SetLevelActive(level1Plates, true); break;
//             case 2: SetLevelActive(level2Plates, true); break;
//             case 3: SetLevelActive(level3Plates, true); break;
//         }

//         currentLevel = levelIndex;
//         scoreManager.allHits.Clear();

//         activeOmelettes.Clear();
//         OmeletteController[] allOmelettes = FindObjectsByType<OmeletteController>(FindObjectsSortMode.None);
//         foreach (OmeletteController omelette in allOmelettes)
//         {
//             if (omelette != null && omelette.gameObject.activeInHierarchy)
//             {
//                 activeOmelettes.Add(omelette);
//             }
//         }
//     }

//     void SetLevelActive(GameObject level, bool activeStatus)
//     {
//         if (level != null) level.SetActive(activeStatus);
//     }
//     void CleanupSpawnedObjects()
//     {
//         foreach (GameObject ketchupSplash in spawnedObjects)
//         {
//             if (ketchupSplash != null)
//             {
//                 Destroy(ketchupSplash);
//             }
//         }
//         spawnedObjects.Clear();
//     }

//     public void RegisterSpawnedObject(GameObject ketchupSplat)
//     {
//         spawnedObjects.Add(ketchupSplat);
//     }

//     // Check if every omelette has been hit
//     public bool AreAllOmelettesHit()
//     {
//         if (activeOmelettes.Count == 0) return false;

//         foreach (OmeletteController omelette in activeOmelettes)
//         {
//             if (omelette != null && !omelette.hasKetchup) return false;
//         }
//         return true;
//     }
// }
