using System.Collections.Generic;

using static Assets.PublicEnums;

public sealed class PerfectFrontCombo : SlideCombo
{
    public override byte Priority { get; } = 255;
    public override string ComboName { get; } = "PERFECT FRONT";
    public override ComboTier ComboTier { get; } = ComboTier.Ultra;
    public override float CameraShakeIntensity { get; } = 0.5f;
    public override float CameraShakeDuration { get; } = 0.2f;

    public override bool IsCanApply(LevelManager lm, List<ColorObject> selected)
    {
        ColorObject[,] grid = lm.ColorObjects;
        int cols = lm.LevelData.ColumnCount;

        ColorObject first = grid[0, 0];
        if (first == null)
            return false;

        ColorType color = first.ColorType;

        for (int col = 1; col < cols; col++)
        {
            ColorObject obj = grid[0, col];
            if (obj == null || obj.ColorType != color)
                return false;
        }

        return true;
    }

    public override void Apply(LevelManager lm, List<ColorObject> selected)
    {
        ColorObject[,] grid = lm.ColorObjects;
        int rows = lm.LevelData.RowCount;
        int cols = lm.LevelData.ColumnCount;

        HashSet<ColorObject> finalSet = new();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                ColorObject obj = grid[row, col];
                if (obj != null)
                    finalSet.Add(obj);
            }
        }

        Multiplier = cols;

        selected.Clear();
        selected.AddRange(finalSet);

        base.Apply(lm, selected);
    }
}
