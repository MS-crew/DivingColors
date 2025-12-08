using System;
using System.Collections.Generic;

using Assets;

using UnityEngine;

using static Assets.PublicEnums;

public sealed class ColumnChainCombo : SlideCombo
{
    [SerializeField] private int minChainLength = 4;

    private readonly Dictionary<int, int> columnCount = new();
    public override byte Priority { get; } = 128;
    public override string ComboName { get; } = "CHAIN COMBO";
    public override ComboTier ComboTier { get; } = ComboTier.Super;
    public override float CameraShakeIntensity { get; } = 0.25f;
    public override float CameraShakeDuration { get; } = 0.2f;

    public override bool IsCanApply(LevelManager lm, List<ColorObject> selected)
    {
        columnCount.Clear();

        foreach (ColorObject obj in selected)
        {
            int col = obj.ColumnIndex;

            if (!columnCount.ContainsKey(col))
                columnCount[col] = 0;

            columnCount[col]++;

            if (columnCount[col] >= minChainLength)
                return true;
        }

        return false;
    }

    public override void Apply(LevelManager lm, List<ColorObject> selected)
    {
        Multiplier = 0;
        columnCount.Clear();

        foreach (ColorObject obj in selected)
        {
            int col = obj.ColumnIndex;

            if (!columnCount.ContainsKey(col))
                columnCount[col] = 0;

            columnCount[col]++;
        }

        HashSet<ColorObject> finalSet = new(selected);

        foreach (KeyValuePair<int, int> kvp in columnCount)
        {
            if (kvp.Value < minChainLength)
                continue;

            int col = kvp.Key;

            for (int row = 0; row < lm.LevelData.RowCount; row++)
            {
                ColorObject obj = lm.ColorObjects[row, col];
                if (obj != null)
                    finalSet.Add(obj);
            }

            Multiplier ++;
        }

        selected.Clear();
        selected.AddRange(finalSet);

        base.Apply(lm, selected);
    }
}
