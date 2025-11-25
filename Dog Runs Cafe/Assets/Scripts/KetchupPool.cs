using System.Collections.Generic;
using UnityEngine;

public class KetchupPool : MonoBehaviour
{
    public static KetchupPool SharedInstance;
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public int amountToPool;

    float yLimit = -20.0f;

    void Awake()
    {
        SharedInstance = this;
    }

    void Start()
    {
        pooledObjects = new List<GameObject>();
        GameObject objHolder;
        for (int i = 0; i < amountToPool; i++)
        {
            objHolder = Instantiate(objectToPool);
            objHolder.SetActive(false);
            pooledObjects.Add(objHolder);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }
        return null;
    }

    public void updatePooledObject()
    {
        GameObject obj = null;
        for (int i = 0; i < amountToPool; i++)
        {
            obj = pooledObjects[i];
            if (obj.activeInHierarchy && obj.transform.position.y < yLimit)
            {
                obj.SetActive(false);
            }
        }
    }
}
