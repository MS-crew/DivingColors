using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using static Assets.PublicEnums;

public static class AutoLevelGenerator
{
    public static LevelDataSO GenerateLevel(uint levelIndex, GenerationSettingsSO settings)
    {
        // 1. DETERMINISTIC RANDOM
        // Ayný level ID her zaman ayný haritayý oluþturur.
        Random.InitState((int)levelIndex);

        LevelDataSO levelData = ScriptableObject.CreateInstance<LevelDataSO>();
        levelData.LevelId = levelIndex;
        levelData.name = $"Level_{levelIndex}";

        // -------------------------------------------------------------------------
        // 2. SONSUZ OYUN MATEMATÝÐÝ (Infinite Progression)
        // -------------------------------------------------------------------------
        // Cycle (Döngü): Her 500 levelde bir zorluk eðrileri baþa sarar.
        // Böylece 501. levelde Grid tekrar küçülür ama "Base" zorluk arttýðý için
        // hedefler biraz daha zorlaþabilir.

        float cycleLength = 500f;

        // 0.0 ile 1.0 arasýnda sürekli dönen deðer (Curve'leri okumak için)
        float loopDifficulty = (levelIndex % cycleLength) / cycleLength;

        // Oyunun baþýndan beri geçen genel zorluk (Çok yavaþ artar, asla azalmaz)
        float globalDifficulty = Mathf.Clamp01(levelIndex / 2000f);

        // Curve'lere göndereceðimiz nihai zorluk:
        // Aðýrlýklý olarak Loop (%80) + Biraz Global (%20) etkisi
        float currentDifficulty = Mathf.Clamp01((loopDifficulty * 0.85f) + (globalDifficulty * 0.15f));

        // -------------------------------------------------------------------------
        // 3. GRID VE RENK AYARLARI
        // -------------------------------------------------------------------------

        // Grid Boyutu Hesapla
        float gridSizeRatio = settings.GridSizeCurve.Evaluate(currentDifficulty);
        levelData.RowCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinRows, settings.MaxRows, gridSizeRatio));
        levelData.ColumnCount = Mathf.RoundToInt(Mathf.Lerp(settings.MinCols, settings.MaxCols, gridSizeRatio));
        int totalGridCells = levelData.RowCount * levelData.ColumnCount;

        // Renk Sayýsý Hesapla (Senin yeni J-Curve burada devreye giriyor)
        float colorRatio = settings.ColorCountCurve.Evaluate(currentDifficulty);
        int totalColors = Mathf.RoundToInt(Mathf.Lerp(settings.MinColorCount, settings.MaxColorCount, colorRatio));

        // Mevcut prefab sayýsýndan fazla renk isteyemeyiz, kontrol edelim:
        int maxAvailablePrefabs = settings.ColorObjectPrefabs.Count;
        totalColors = Mathf.Clamp(totalColors, settings.MinColorCount, maxAvailablePrefabs);

        // Rastgele renkleri seç
        List<ColorType> activeColors = GetRandomColors(totalColors, settings);

        // -------------------------------------------------------------------------
        // 4. HEDEF (OBJECTIVE) OLUÞTURMA
        // -------------------------------------------------------------------------

        float objRatio = settings.ObjectiveRatioCurve.Evaluate(currentDifficulty);
        int objectiveCount = Mathf.RoundToInt(totalColors * objRatio);
        // En az 1 hedef, en fazla (Renk Sayýsý - 1) hedef olsun ki hepsi hedef olmasýn.
        objectiveCount = Mathf.Clamp(objectiveCount, 1, Mathf.Max(1, totalColors - 1));

        List<ColorType> targetColors = activeColors.Take(objectiveCount).ToList();

        // Listeleri hazýrla
        levelData.ColorObjectPrefabs = new List<GameObject>();
        levelData.SlidesPrefabs = new List<GameObject>();
        levelData.Objectives = new List<ObjectiveData>();

        // Seçilen renklerin prefablarýný data'ya ekle
        foreach (ColorType color in activeColors)
        {
            GameObject objPrefab = settings.GetPrefabByColor(color);
            GameObject slidePrefab = settings.GetSlideByColor(color);

            if (objPrefab != null) levelData.ColorObjectPrefabs.Add(objPrefab);
            if (slidePrefab != null) levelData.SlidesPrefabs.Add(slidePrefab);
        }

        // Hedef sayýlarýný belirle
        foreach (ColorType color in targetColors)
        {
            ObjectiveData objData = new() { Color = color };

            float totalDensity = settings.TargetDensityCurve.Evaluate(currentDifficulty);
            float variance = Random.Range(0.9f, 1.1f); // %10 þaþma payý

            // Hedef sayýsý Global Zorluk arttýkça çok az artabilir (+ globalDifficulty * 5)
            float baseAmount = (totalGridCells * totalDensity) / objectiveCount;
            int finalTargetAmount = Mathf.RoundToInt(baseAmount * variance) + Mathf.RoundToInt(globalDifficulty * 5);

            objData.TargetAmount = Mathf.Max(3, finalTargetAmount); // En az 3 tane iste

            // Spawn Chance
            float chanceBase = Mathf.Lerp(0.4f, 0.15f, currentDifficulty); // Zorlaþtýkça düþme þansý azalýr (daha stratejik)
            objData.SpawnChanceMultiplier = Mathf.Clamp(chanceBase, 0.1f, 1.0f);

            levelData.Objectives.Add(objData);
        }

        // -------------------------------------------------------------------------
        // 5. HAMLE (MOVE) HESABI
        // -------------------------------------------------------------------------

        int totalTargetsToCollect = levelData.Objectives.Sum(x => x.TargetAmount);

        // Bir hamlede ortalama kaç taþ patlar? 
        // Grid büyüdükçe artar (4.0f), ama RENK SAYISI arttýkça düþer!
        // Burasý kritik: Çok renk varsa kombo yapmak zordur.
        float baseCollectRate = Mathf.Lerp(2.2f, 4.5f, gridSizeRatio);

        // Renk cezasý: Eðer 9 renk varsa, ortalama toplama hýzý düþer.
        float colorPenalty = Mathf.Lerp(1.0f, 0.6f, colorRatio);

        float adjustedCollectRate = baseCollectRate * colorPenalty;

        float estimatedMovesNeeded = totalTargetsToCollect / adjustedCollectRate;
        float safetyMargin = settings.MovesSafetyMarginCurve.Evaluate(currentDifficulty);

        // Minimum 8 hamle verelim
        levelData.ClickAttempts = Mathf.Max(8, Mathf.RoundToInt(estimatedMovesNeeded * safetyMargin) + 5);

        // -------------------------------------------------------------------------
        // 6. SPECIAL ITEMS
        // -------------------------------------------------------------------------

        levelData.SpecialObjects = new List<SpecialObject>();
        if (settings.SpecialItemRules != null)
        {
            foreach (var rule in settings.SpecialItemRules)
            {
                if (rule.Prefab == null) continue;

                if (levelIndex >= rule.UnlockLevel)
                {
                    // Þans eðrisini Loop deðil, Global progression üzerinden okuyabiliriz
                    // veya Loop üzerinden okuyup her "bölüm"de item çýkmasýný saðlayabiliriz.
                    // Loop kullanmak daha iyi hissettirir.
                    float chance = rule.SpawnChanceCurve.Evaluate(loopDifficulty);

                    if (Random.value < chance) // Eðer þans tutarsa
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

        // Kombo listesini kopyala
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

        // Listeyi karýþtýr ve istenen sayý kadar al
        return availableColors.OrderBy(x => Random.value).Take(count).ToList();
    }
}