using UnityEngine;
using System.Collections.Generic;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public int ballCount = 10;

    private List<GameObject> spawnedBalls = new List<GameObject>();

    public void SpawnBalls()
    {
        ClearBalls();

        for (int i = 0; i < ballCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.02f;
            offset.y = Mathf.Abs(offset.y);

            var ball = Instantiate(ballPrefab, transform.position + offset, Quaternion.identity, transform.parent);
            ball.transform.localScale = Vector3.one * 0.02f;

            spawnedBalls.Add(ball);
        }
    }

    public void ClearBalls()
    {
        foreach (var b in spawnedBalls)
        {
            if (b != null) Destroy(b);
        }
        spawnedBalls.Clear();
    }

    void Start()
    {
        SpawnBalls();
    }
}