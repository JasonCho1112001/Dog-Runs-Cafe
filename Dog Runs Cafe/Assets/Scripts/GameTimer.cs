using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    public float surviveTime = 10f;
    private float timer = 0f;
    private bool gameOver = false;
    private float printInterval = 1f;
    private float nextPrintTime = 1f;

    void Update()
    {
        if (gameOver) return;

        timer += Time.deltaTime;

        // Print timer every full second
        if (timer >= nextPrintTime)
        {
            Debug.Log("Timer: " + Mathf.FloorToInt(timer) + "s");
            nextPrintTime += printInterval;
        }

        if (timer >= surviveTime)
        {
            Win();
        }
    }

    public void Lose()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("GAME OVER — Touching Ground");
    }

    private void Win()
    {
        gameOver = true;
        Debug.Log("You survived!");
    }
}
