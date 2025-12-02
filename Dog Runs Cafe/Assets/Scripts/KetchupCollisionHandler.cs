using NUnit.Framework;
using System.Runtime.CompilerServices;
using UnityEngine;

public class KetchupCollisionHandler : MonoBehaviour
{
    public GameObject ketchupSplash;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Plate") || other.CompareTag("Omelette"))
        {
            bool isCorrect = other.CompareTag("Omelette");
            if (ketchupScoreManager.Instance != null)
            {
                ketchupScoreManager.Instance.AddHit(isCorrect);
            }

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

        return closestPoint + (Vector3.up*10f) * 0.01f;
    }
}
