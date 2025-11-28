using System;
using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

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
    public static T SetSceneSingleton<T>(this T instance, ref T staticInstance) where T : MonoBehaviour
    {
        if (staticInstance != null && staticInstance != instance)
        {
            Object.Destroy(instance.gameObject);
            return staticInstance;
        }

        staticInstance = instance;
        return instance;
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

    public static void ReturnToPool<T>(this T obj, byte id = 0) where T : MonoBehaviour => PoolManager.Instance.ReturnToPool(obj.gameObject, id);

    public static void ReturnToPool(this GameObject obj, byte id = 0) => PoolManager.Instance.ReturnToPool(obj, id);

    public static Color GetUnityColor(this ColorType type)
    {
        return type switch
        {
            ColorType.Red => Color.red,
            ColorType.Blue => Color.blue,
            ColorType.Black => Color.black,
            ColorType.Green => Color.green,
            ColorType.Pink => Color.magenta,
            ColorType.Yellow => Color.yellow,
            ColorType.Orange => new(1f, 0.45f, 0.05f),
            ColorType.Purple => new Color(124, 1, 217),
            _ => Color.white
        };
    }
}

