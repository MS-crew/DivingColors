using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class Extensions
{
    public static T GetRandomValue<T>(this IList<T> list)
    {
        if (list is null)
            return default;

        int randomIndex = Random.Range(0, list.Count);

        return list[randomIndex];
    }

    public static T GetRandomValue<T>(this IList<T> list, Func<T, bool> condition)
    {
        if (list is null)
            return default;

        List<T> list2 = new();
        foreach (T item in list)
        {
            if (condition(item))
            {
                list2.Add(item);
            }
        }

        int randomIndex = Random.Range(0, list2.Count);

        return list2[randomIndex];
    }

    public static T SetSingleton<T>(this T instance, T thisInstance) where T : MonoBehaviour
    {
        if (instance != null && instance != thisInstance)
        {
            Object.Destroy(thisInstance.gameObject);
            return instance;
        }

        return thisInstance;
    }

    public static T ReturnToPool<T>(this T obj, byte id = 0)
    {
        PoolManager.Instance.ReturnToPool(obj as GameObject, id);

        return obj;
    }
}

