using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static Assets.PublicEnums;

namespace Assets.Class.SlideCombos
{
    public class FullColumnCombo : SlideCombo
    {
        public override byte Priority { get; } = 129;

        public override string ComboName { get; } = "FULL CAHIN COMBO";

        public override ComboTier ComboTier { get; } = ComboTier.Mega;

        public override float CameraShakeDuration { get; } = 0.37f;

        public override float CameraShakeIntensity { get; } = 0.2f;

        private Dictionary<int, int> columnCounts = new();

        public override bool IsCanApply(LevelManager lm, List<ColorObject> selecteds)
        {
            if (selecteds == null || selecteds.Count < lm.LevelData.RowCount) 
                return false;

            columnCounts.Clear();
            int maxRows = lm.LevelData.RowCount;

            foreach (ColorObject obj in selecteds)
            {
                int col = obj.ColumnIndex;

                if (!columnCounts.ContainsKey(col))
                    columnCounts[col] = 0;

                columnCounts[col]++;

                if (columnCounts[col] == maxRows)
                    return true;
            }

            return false;
        }

        public override void Apply(LevelManager lm, List<ColorObject> selected)
        {
            HashSet<int> affectedColumns = new();
            HashSet<ColorObject> bonusObjects = new();

            foreach (KeyValuePair<int, int> kvp in columnCounts)
            {
                if (kvp.Value == lm.LevelData.RowCount)
                    affectedColumns.Add(kvp.Key);
            }

            foreach (int col in affectedColumns)
            {
                int rightCol = col + 1;

                if (rightCol < lm.LevelData.ColumnCount)
                {
                    ColorObject rightNeighbor = lm.ColorObjects[0, rightCol];

                    if (rightNeighbor != null && !selected.Contains(rightNeighbor))
                    {
                        bonusObjects.Add(rightNeighbor);
                    }
                }
            }

            selected.AddRange(bonusObjects);

            base.Apply(lm, selected);
        }
    }
}
