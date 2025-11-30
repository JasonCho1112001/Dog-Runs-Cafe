using System.Collections.Generic;
using UnityEngine;

public class ketchupScoreManager : MonoBehaviour
{
    public static ketchupScoreManager Instance;

    public List<bool> allHits = new List<bool>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void AddHit(bool isCorrect)
    {
        allHits.Add(isCorrect);
    }

    public float GetScoreAccuracy()
    {
        if (allHits.Count == 0) return 0;
        int correct = allHits.FindAll(x => x).Count;
        return (float)correct / allHits.Count;
    }
}
