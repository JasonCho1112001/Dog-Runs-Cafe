using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public int ballCount = 10;

    void Start()
    {
        for (int i = 0; i < ballCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.1f;
            offset.y = Mathf.Abs(offset.y); 

            var ball = Instantiate(ballPrefab, transform.position + offset, Quaternion.identity, transform.parent);

            ball.transform.localScale = Vector3.one * 0.03f;
        }
    }
}