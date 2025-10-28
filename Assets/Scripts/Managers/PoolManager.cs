using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Havuzlanacaklar")]
    [SerializeField] private List<GameObject> prefabPoolsToCreate;

    private GameObject[] prefabArray = new GameObject[256];
    private Queue<GameObject>[] poolArray = new Queue<GameObject>[256];

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void Start()
    {
        foreach (GameObject prefab in prefabPoolsToCreate)
            CreatePool(prefab);
    }

    public void CreatePool(GameObject prefab)
    {
        if (!prefab.TryGetComponent(out PooledObject pooledObject))
            return;

        byte id = pooledObject.poolID;
        int size = pooledObject.poolSize;

        if (poolArray[id] != null)
            return;

        Queue<GameObject> queue = new(size);
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }

        poolArray[id] = queue;
        prefabArray[id] = prefab;
    }

    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!prefab.TryGetComponent(out PooledObject pooledObject))
            throw new System.Exception("Prefab does not have a PooledObject component.");

        return SpawnFromPool(pooledObject.poolID, position, rotation);
    }

    public GameObject SpawnFromPool(byte poolID, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> objectQueue = poolArray[poolID];
        if (objectQueue == null)
            return null;

        GameObject objectToSpawn;
        if (objectQueue.Count > 0)
        {
            objectToSpawn = objectQueue.Dequeue();
            objectToSpawn.transform.SetPositionAndRotation(position, rotation);
            objectToSpawn.SetActive(true);
        }
        else
        {
            GameObject prefab = prefabArray[poolID];
            objectToSpawn = Instantiate(prefab, position, rotation, transform);
        }

        return objectToSpawn;
    }

    public void ReturnToPool(GameObject objectToReturn) => ReturnToPool(objectToReturn, 0);

    public void ReturnToPool(GameObject objectToReturn, byte id = 0)
    {
        if (id == 0)
            id = objectToReturn.GetComponent<PooledObject>().poolID;

        Queue<GameObject> queue = poolArray[id];

        if (queue == null)
        {
            Destroy(objectToReturn);
            return;
        }

        objectToReturn.SetActive(false);
        queue.Enqueue(objectToReturn);
    }
}
