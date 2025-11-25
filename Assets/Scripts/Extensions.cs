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

    public static T SetSingleton<T>(this T instance, T thisInstance) where T : MonoBehaviour
    {
        if (instance != null && instance != thisInstance)
        {
            Object.Destroy(thisInstance.gameObject);
            return instance;
        }

        return thisInstance;
    }

    public static GameObject ReturnToPool(this GameObject obj, byte id = 0)
    {
        if (obj.TryGetComponent(out ColorObject color))
        {
            color.ColumnIndex = color.RowIndex = -0;
        }

        PoolManager.Instance.ReturnToPool(obj, id);

        return obj;
    }

    public static Color GetUnityColor(this ColorType type)
    {
        return type switch
        {
            ColorType.Red => Color.red,
            ColorType.Yellow => Color.yellow,
            ColorType.Blue => Color.blue,
            _ => Color.white
        };
    }
}

