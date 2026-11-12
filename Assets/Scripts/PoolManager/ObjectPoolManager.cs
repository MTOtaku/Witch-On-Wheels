using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

/// <summary>
/// Lida com diversos tipos de pools de objetos.
/// </summary>

public class ObjectPoolManager : MonoBehaviour
{
    private GameObject _emptyHolder;

    private static GameObject _dialogSystemEmpty;
    private static GameObject _storeElementEmpty;

    private static Dictionary<GameObject, ObjectPool<GameObject>> _objectPool;
    private static Dictionary<GameObject, GameObject> _cloneToPrefabMap;

    void Awake()
    {
        _objectPool = new Dictionary<GameObject, ObjectPool<GameObject>>();
        _cloneToPrefabMap = new Dictionary<GameObject, GameObject>();
        SetupEmpties();
    }

    private void SetupEmpties()
    {
        _emptyHolder = new GameObject("Object Pools");

        _dialogSystemEmpty = new GameObject("DialogSystem");
        _dialogSystemEmpty.transform.SetParent(_emptyHolder.transform);
        DontDestroyOnLoad(_dialogSystemEmpty.transform.root);
    }

    private static void CreatePool(GameObject prefab, Vector3 position, Quaternion rot, PoolType poolType)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreateObject(prefab, position, rot, poolType),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
        );
        
        _objectPool.Add(prefab, pool);
    }

    private static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rot, PoolType poolType)
    {
        GameObject obj = Instantiate(prefab, position, rot);
        obj.SetActive(false);
        GameObject parentObject = GetParentObject(poolType);
        obj.transform.SetParent(parentObject.transform);
        return obj;
    }

    private static void OnGetObject(GameObject obj)
    {
    }

    private static void OnReleaseObject(GameObject obj)
    {
        obj.SetActive(false);
    }

    private static void OnDestroyObject(GameObject obj)
    {
        if (_cloneToPrefabMap.ContainsKey(obj))
        {
            _cloneToPrefabMap.Remove(obj);
        }
    }

    private static GameObject GetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.DialogSystem:
                return _dialogSystemEmpty;
            default:
                Debug.Log("Ainda não Implementado");
                return null;
        }

    }

    private static T SpawnObject<T>(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType) where T: Object
    {
        if (!_objectPool.ContainsKey(objectToSpawn))
        {
            CreatePool(objectToSpawn, spawnPosition, spawnRotation, poolType);
        }
        GameObject obj = _objectPool[objectToSpawn].Get();
        if(obj != null)
        {
            if (!_cloneToPrefabMap.ContainsKey(obj))
            {
                _cloneToPrefabMap.Add(obj, objectToSpawn);
            }
            obj.transform.position = spawnPosition;
            obj.transform.rotation = spawnRotation;
            obj.SetActive(true);

            if(typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }

            T component = obj.GetComponent<T>();

            if(component == null)
            {
                Debug.LogWarning($" Object{objectToSpawn.name} não existe componente do tipo {typeof(T)}");
                return null;
            }

            return component;
        }

        return null;
    }

    public static T SpawnObject<T>(T typePrefab, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType) where T: Component
    {
        return SpawnObject<T>(typePrefab.gameObject, spawnPosition, spawnRotation, poolType);
    }

    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType)
    {
        return SpawnObject<GameObject>(objectToSpawn, spawnPosition, spawnRotation, poolType);
    }

    public static void ReturnObjectToPool(GameObject obj, PoolType poolType)
    {
        if(_cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObject = GetParentObject(poolType);

            if(obj.transform.parent != parentObject.transform)
            {
                obj.transform.SetParent(parentObject.transform);

            }

            if(_objectPool.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }
        }
        else
        {
            Debug.LogWarning("Tentando retornar um objeto que não está armazenável" + obj.name);
        }
    }
}
