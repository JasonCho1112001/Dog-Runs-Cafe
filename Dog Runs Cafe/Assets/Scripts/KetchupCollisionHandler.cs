using UnityEngine;

public class KetchupCollisionHandler : MonoBehaviour
{
    public GameObject ketchupSplash;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Plate"))
        {
            Vector3 spawnPosition = GetContactPoint(other, transform.position);

            gameObject.SetActive(false);

            if (ketchupSplash != null)
            {
                Instantiate(ketchupSplash, spawnPosition, Quaternion.identity);
            }
        }
    }

    private Vector3 GetContactPoint(Collider other, Vector3 ketchupPosition)
    {
        Vector3 closestPoint = other.ClosestPoint(ketchupPosition);

        return closestPoint + (Vector3.up*4f) * 0.01f;
    }
}
