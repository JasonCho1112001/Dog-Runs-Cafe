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
        Debug.Log("A liquid ball hit the ground!");
        timer.Lose();
    }

    if (collision.collider.CompareTag("Tray"))
    {
        Debug.Log("A liquid ball touched the tray!");
        timer.Lose();
    }
}
}