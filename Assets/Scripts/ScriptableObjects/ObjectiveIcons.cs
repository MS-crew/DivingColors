using System;
using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

[CreateAssetMenu(fileName = "ObjectiveIcon", menuName = "Configs/Objective Icons")]
public class ObjectiveIcons : ScriptableObject
{
    [Serializable]
    public struct ColorIconPair
    {
        public ColorType Color;
        public Sprite Icon;
    }

    [SerializeField] private List<ColorIconPair> icons;
    private Dictionary<ColorType, Sprite> iconMap;

    public Sprite GetIcon(ColorType color)
    {
        if (iconMap == null || iconMap.Count == 0)
        {
            iconMap = new Dictionary<ColorType, Sprite>();
            foreach (ColorIconPair pair in icons)
            {
                if (!iconMap.ContainsKey(pair.Color))
                    iconMap.Add(pair.Color, pair.Icon);
            }
        }

        if (iconMap.TryGetValue(color, out Sprite icon))
            return icon;

        return null;
    }
}
