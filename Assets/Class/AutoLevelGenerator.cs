using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using static Assets.PublicEnums;

public static class AutoLevelGenerator
{
    const int MaxLevelIndex = 500;

    public static LevelDataSO GenerateLevel(uint levelIndex, GenerationSettingsSO settings)
    {
        Random.InitState((int)levelIndex);

        LevelDataSO levelData = ScriptableObject.CreateInstance<LevelDataSO>();
        levelData.LevelId = levelIndex;
        levelData.name = $"Level_{levelIndex}";

        #region Zorluk Hesabý
        float progression = Mathf.Clamp01(levelIndex / MaxLevelIndex);

        // Zorlluk dalgalanmasý -0.15 ile +0.15 arasý, MaxLevelIndex e kadar ondann sonra daha az
        float waveIntensity = (levelIndex > MaxLevelIndex) ? 0.05f : 0.15f;
        float wave = Mathf.Sin(levelIndex * 0.3f) * waveIntensity; //0.3 (6.28 sinüsden 2 pi / 0.3) = 21 seviyede bir döngü

        float baseDifficulty = progression + wave;

        // MaxLevelIndex ün üstü ortalam bir zorlukta kalsýn
        if (levelIndex > MaxLevelIndex)
            baseDifficulty = Mathf.Max(0.8f, baseDifficulty);

        float difficulty = Mathf.Clamp01(baseDifficulty);
        #endregion

        #region Grid Boyutu

        float gridSizeRatio = settings.GridSizeCurve.Evaluate(difficulty);
        levelData.RowCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinRows, settings.MaxRows, gridSizeRatio));
        levelData.ColumnCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinCols, settings.MaxCols, gridSizeRatio));
        int totalGridCells = levelData.RowCount * levelData.ColumnCount;

        #endregion

        #region Kaç Renk Olacak
        float colorRatio = settings.ColorCountCurve.Evaluate(difficulty);
        int totalColors = Mathf.RoundToInt(Mathf.Lerp(settings.MinColorCount, settings.MaxColorCount, colorRatio));

        int maxAvailablePrefabs = settings.ColorObjectPrefabs.Count;
        totalColors = Mathf.Clamp(totalColors, settings.MinColorCount, maxAvailablePrefabs);

        List<ColorType> activeColors = GetRandomColors(totalColors, settings);

        #endregion

        #region Hedefleri Oluþturma

        float objRatio = settings.ObjectiveRatioCurve.Evaluate(difficulty);
        int objectiveCount = Mathf.RoundToInt(totalColors * objRatio);

        //en fazla renkler - 2, yani 2 dummy renk olsun en az
        objectiveCount = Mathf.Clamp(objectiveCount, 1, Mathf.Max(1, totalColors - 2));

        List<ColorType> targetColors = activeColors.Take(objectiveCount).ToList();
        levelData.ColorObjectPrefabs = new List<GameObject>();
        levelData.SlidesPrefabs = new List<GameObject>();
        levelData.Objectives = new List<ObjectiveData>();

        foreach (ColorType color in activeColors)
        {
            GameObject objPrefab = settings.GetPrefabByColor(color);
            GameObject slidePrefab = settings.GetSlideByColor(color);

            if (objPrefab != null) levelData.ColorObjectPrefabs.Add(objPrefab);
            if (slidePrefab != null) levelData.SlidesPrefabs.Add(slidePrefab);
        }

        foreach (ColorType color in targetColors)
        {
            ObjectiveData objData = new() { Color = color };

            float totalDensity = settings.TargetDensityCurve.Evaluate(difficulty);
            float variance = Random.Range(0.9f, 1.1f);

            float baseAmount = (totalGridCells * totalDensity) / objectiveCount;
            int finalTargetAmount = Mathf.RoundToInt(baseAmount * variance) + Mathf.RoundToInt(difficulty * 5);

            objData.TargetAmount = Mathf.Max(3, finalTargetAmount); // En az 3 tane

            // Spawn Chance
            float chanceBase = Mathf.Lerp(0.4f, 0.15f, difficulty); // Zorlaþtýkça þansý azalýyor
            objData.SpawnChanceMultiplier = Mathf.Clamp(chanceBase, 0.1f, 1.0f);

            levelData.Objectives.Add(objData);
        }
        #endregion

        #region Hamle Hesabý
        int totalTargetsToCollect = levelData.Objectives.Sum(x => x.TargetAmount);

        // Bir hamlede ortalama kaç toplanir temel 
        float baseCollectRate = Mathf.Lerp(2, 3.5f, gridSizeRatio);

        // Renk cezasý: Çok renk == tek hamlede ortalama toplanan düþer
        float colorPenalty = Mathf.Lerp(1.0f, 0.5f, colorRatio);

        // gereklli hamle sayisi = toplam topanacak sasyi / ortalama tek hamlede toplanan
        float adjustedCollectRate = baseCollectRate * colorPenalty;
        float estimatedMovesNeeded = totalTargetsToCollect / adjustedCollectRate;

        levelData.ClickAttempts = Mathf.Max(12, Mathf.RoundToInt(estimatedMovesNeeded * 4) + 8);
        #endregion

        #region Özel Nesneler
        levelData.SpecialObjects = new List<SpecialObject>();
        if (settings.SpecialItemRules != null)
        {
            foreach (var rule in settings.SpecialItemRules)
            {
                if (rule.Prefab == null) continue;

                if (levelIndex >= rule.UnlockLevel)
                {
                    float chance = rule.SpawnChanceCurve.Evaluate(progression);

                    if (Random.value < chance)
                    {
                        int calculatedMax = Mathf.FloorToInt(totalGridCells * rule.MaxCountDensity);
                        int maxCount = Mathf.Max(rule.MinAbsoluteCount, calculatedMax);

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
        #endregion

        levelData.Combos = new List<SlideCombo>(settings.AllCombos ?? new List<SlideCombo>());

        return levelData;
    }

    private static List<ColorType> GetRandomColors(int count, GenerationSettingsSO settings)
    {
        if (settings.ColorObjectPrefabs == null) return new List<ColorType>();

        HashSet<ColorType> availableColors = new();

        foreach (GameObject go in settings.ColorObjectPrefabs)
        {
            if (go != null && go.TryGetComponent(out ColorObject co))
                availableColors.Add(co.ColorType);
        }

        return availableColors.OrderBy(x => Random.value).Take(count).ToList();
    }
}