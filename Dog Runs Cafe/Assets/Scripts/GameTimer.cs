using UnityEngine;

public class GameTimer : MonoBehaviour
{
    // Timer Constants
    public float surviveTime = 10f;

    private float timer = 0f;
    private bool gameOver = false;

    private float printInterval = 1f;
    private float nextPrintTime = 1f;

    // Mug Spawning
    public GameObject mugPrefab;      
    public BoxCollider spawnArea;     
    private bool hasSpawnedMug = false;

    void Update()
    {
        if (gameOver) return;

        timer += Time.deltaTime;

        // Print timer every second
        if (timer >= nextPrintTime)
        {
            Debug.Log("Timer: " + Mathf.FloorToInt(timer) + "s");
            nextPrintTime += printInterval;
        }

        // Spawn second mug at halfway point
        if (!hasSpawnedMug && timer >= surviveTime / 2f)
        {
            Debug.Log("Attempting random mug spawn...");
            SpawnRandomMug();
            hasSpawnedMug = true;
        }

        // Win condition
        if (timer >= surviveTime)
        {
            Win();
        }
    }

    public void Lose()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("GAME OVER — Mug or ball touched the ground.");
    }

    private void Win()
    {
        gameOver = true;
        Debug.Log("YOU SURVIVED 10 SECONDS!!!");
    }

    // Spawning Logic
    void SpawnRandomMug()
    {
        Vector3 pos = GetRandomPointInSpawnArea();

        float mugRadius = 0.5f;   // Adjust based on your mug size
        int maxAttempts = 20;
        int attempts = 0;

        // Ensure we don't overlap the first mug
        while (Physics.CheckSphere(pos, mugRadius))
        {
            pos = GetRandomPointInSpawnArea();
            attempts++;

            if (attempts > maxAttempts)
            {
                Debug.LogWarning("⚠ Could not find safe spawn point for mug!");
                return;
            }
        }

        Instantiate(mugPrefab, pos, Quaternion.identity);
        Debug.Log("Spawned second mug at: " + pos);
    }

    Vector3 GetRandomPointInSpawnArea()
    {
        Vector3 center = spawnArea.bounds.center;
        Vector3 size = spawnArea.bounds.size;

        float x = Random.Range(center.x - size.x / 2f, center.x + size.x / 2f);
        float z = Random.Range(center.z - size.z / 2f, center.z + size.z / 2f);

        float y = center.y;

        return new Vector3(x, y, z);
    }
}
