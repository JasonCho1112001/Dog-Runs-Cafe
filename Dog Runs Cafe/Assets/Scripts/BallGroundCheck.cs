using UnityEngine;

public class BallGroundCheck : MonoBehaviour
{
    private GameTimer timer;

    void Start()
    {
        timer = Object.FindFirstObjectByType<GameTimer>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ground"))
        {
            timer.Lose();
        }
    }
}