using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using static Assets.PublicEnums;

public static class AutoLevelGenerator
{
    public static LevelDataSO GenerateLevel(uint levelIndex, GenerationSettingsSO settings)
    {
        Random.InitState((int)levelIndex);

        LevelDataSO levelData = ScriptableObject.CreateInstance<LevelDataSO>();
        levelData.LevelId = levelIndex;
        levelData.name = $"Generated_Level_{levelIndex}";

        float progression = Mathf.Clamp01(levelIndex / 500f);
        float wave = Mathf.Sin(levelIndex * 0.3f) * 0.15f;
        float difficulty = Mathf.Clamp01(progression + wave);

        float gridSizeRatio = settings.GridSizeCurve.Evaluate(difficulty);
        levelData.RowCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinRows, settings.MaxRows, gridSizeRatio));
        levelData.ColumnCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinCols, settings.MaxCols, gridSizeRatio));

        int totalGridCells = levelData.RowCount * levelData.ColumnCount;

        float colorRatio = settings.ColorCountCurve.Evaluate(difficulty);
        int totalColors = Mathf.RoundToInt(Mathf.Lerp(settings.MinColorCount, settings.MaxColorCount, colorRatio));

        List<ColorType> activeColors = GetRandomColors(totalColors, settings);

        float objRatio = settings.ObjectiveRatioCurve.Evaluate(difficulty);
        int objectiveCount = Mathf.RoundToInt(totalColors * objRatio);
        int maxPossibleObjectives = Mathf.Max(1, totalColors - 1);
        objectiveCount = Mathf.Clamp(objectiveCount, 1, maxPossibleObjectives);

        List<ColorType> targetColors = activeColors.Take(objectiveCount).ToList();

        levelData.ColorObjectPrefabs = new List<GameObject>();
        levelData.SlidesPrefabs = new List<GameObject>();
        levelData.Objectives = new List<ObjectiveData>();

        foreach (ColorType color in activeColors)
        {
            GameObject objPrefab = settings.GetPrefabByColor(color);
            GameObject slidePrefab = settings.GetSlideByColor(color);

            if (objPrefab != null)
                levelData.ColorObjectPrefabs.Add(objPrefab);

            if (slidePrefab != null) 
                levelData.SlidesPrefabs.Add(slidePrefab);
        }

        foreach (ColorType color in targetColors)
        {
            ObjectiveData objData = new()
            {
                Color = color
            };

            float totalDensity = settings.TargetDensityCurve.Evaluate(difficulty);
            float variance = Random.Range(0.8f, 1.2f);
            float perObjectiveDensity = (totalDensity / objectiveCount) * variance;

            objData.TargetAmount = Mathf.Max(3, Mathf.RoundToInt(totalGridCells * perObjectiveDensity));

            float chanceBase = Mathf.Lerp(0.5f, 0.2f, difficulty);
            objData.SpawnChanceMultiplier = Mathf.Clamp(chanceBase + Random.Range(-0.1f, 0.1f), 0.1f, 1.0f);

            levelData.Objectives.Add(objData);
        }


        int totalTargetsToCollect = levelData.Objectives.Sum(x => x.TargetAmount);
        float averageCollectPerSlide = Mathf.Lerp(2.5f, 4f, gridSizeRatio);
        float estimatedMovesNeeded = totalTargetsToCollect / averageCollectPerSlide;
        float safetyMargin = settings.MovesSafetyMarginCurve.Evaluate(difficulty);

        levelData.ClickAttempts = Mathf.Max(10, Mathf.RoundToInt((estimatedMovesNeeded * safetyMargin) + 8));


        levelData.SpecialObjects = new List<SpecialObject>();
        if (settings.SpecialItemRules != null)
        {
            foreach (var rule in settings.SpecialItemRules)
            {
                if (levelIndex >= rule.UnlockLevel)
                {
                    float chance = rule.SpawnChanceCurve.Evaluate(progression);
                    int calculatedMax = Mathf.FloorToInt(totalGridCells * rule.MaxCountDensity);
                    int maxCount = Mathf.Max(rule.MinAbsoluteCount, calculatedMax);

                    if (chance > 0 && maxCount > 0)
                    {
                        SpecialObject sp = new()
                        {
                            Prefab = rule.Prefab,
                            SpawnChance = chance,
                            MaxOnSameTime = maxCount
                        };

                        levelData.SpecialObjects.Add(sp);
                    }
                }
            }
        }

        levelData.Combos = new List<SlideCombo>(settings.AllCombos);

        return levelData;
    }

    private static List<ColorType> GetRandomColors(int count, GenerationSettingsSO settings)
    {
        HashSet<ColorType> availableColors = new();

        foreach (GameObject go in settings.ColorObjectPrefabs)
        {
            if (go.TryGetComponent(out ColorObject co))
                availableColors.Add(co.ColorType);
        }

        return availableColors.OrderBy(x => Random.value).Take(count).ToList();
    }
}