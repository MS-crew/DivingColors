using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using static Assets.PublicEnums;

public static class AutoLevelSOGenerator
{
    private const int TOTAL_LEVELS = 30;

    [MenuItem("DivingColors/AutoGenerate30Levels")]
    public static void Create()
    {
        string folder = "Assets/Resources/Levels";

        // Tüm küp ve slide prefabs, GUID üzerinden yükleniyor
        string[] cubeGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/ColorObjects" });
        string[] slideGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Slides" });

        List<GameObject> cubePrefabs = new List<GameObject>(cubeGuids.Length);
        foreach (string guid in cubeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) cubePrefabs.Add(go);
        }

        List<GameObject> slidePrefabs = new List<GameObject>(slideGuids.Length);
        foreach (string guid in slideGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) slidePrefabs.Add(go);
        }

        // Enum’daki bütün renkleri tek seferde alalım (GC-free değil ama EDITOR TOOL’de boxing problem değil)
        ColorType[] allColors = (ColorType[])System.Enum.GetValues(typeof(ColorType));

        for (int level = 1; level <= TOTAL_LEVELS; level++)
        {
            // 1) LevelDataSO instance’ı explicit tip
            LevelDataSO levelSO = ScriptableObject.CreateInstance<LevelDataSO>();
            levelSO.LevelId = (uint)level;

            // 2) ClickAttempts skalası (explicit int)
            int clickBase = 18;
            int clickBonus = level / 2;
            levelSO.ClickAttempts = clickBase + clickBonus;

            // 3) Grid boyutlandırma sanitize (explicit int)
            int gridRow = 3 + level / 4;     // 30. level’de 10 satır target → mantıklı puzzle alanı
            int gridCol = 4 + level / 3;     // color sayısı artarsa col’u da büyüt
            int finalRow = Mathf.Clamp(gridRow, 3, 10);
            int finalCol = Mathf.Clamp(gridCol, 4, 12);

            levelSO.RowCount = finalRow;
            levelSO.ColumnCount = finalCol;

            // 4) Aynı objective duplicate olmasın diye -> objective set’e toplanan renkleri izleyelim
            levelSO.Objectives = new List<ObjectiveData>(4);
            HashSet<ColorType> usedObjectiveColors = new HashSet<ColorType>();

            // 5) Objective sayısı bağımsız, renk uzayı genişlediği için enum’dan random seçelim
            int objectiveCount = level <= 10 ? 2 : (level <= 20 ? 3 : 4);

            for (int j = 0; j < objectiveCount; j++)
            {
                // duplicate olmayan bir renk bulana kadar random roll
                ColorType pickedColor = ColorType.Green;
                for (int tryPick = 0; tryPick < 20; tryPick++)
                {
                    int randomIndex = Random.Range(0, allColors.Length);
                    ColorType candidate = allColors[randomIndex];

                    if (!usedObjectiveColors.Contains(candidate))
                    {
                        pickedColor = candidate;
                        break;
                    }
                }

                if (pickedColor == ColorType.Red)
                {
                    // fallback: enum içinden kullanılmayan ilk renk
                    foreach (ColorType fallbackColor in allColors)
                    {
                        if (!usedObjectiveColors.Contains(fallbackColor))
                        {
                            pickedColor = fallbackColor;
                            break;
                        }
                    }
                }

                // seçtiğimiz kesin objective rengi
                usedObjectiveColors.Add(pickedColor);

                // explicit int hedef miktarı
                int baseTarget = 2 + level / 7;
                int objectiveBonus = j;
                int finalTarget = Mathf.Clamp(baseTarget + objectiveBonus, 2, 12);

                // explicit float spawn çarpanı
                float spawnMultBase = 1.0f + level * 0.03f - j * 0.05f;
                float finalSpawnMult = Mathf.Clamp(spawnMultBase, 0.5f, 2.5f);

                ObjectiveData objectiveData = new ObjectiveData();
                objectiveData.Color = pickedColor;
                objectiveData.TargetAmount = finalTarget;
                objectiveData.SpawnChanceMultiplier = finalSpawnMult;

                levelSO.Objectives.Add(objectiveData);
            }

            // 6) Slides listesi: Hepsini eklemek yerine col ile orantılı spawn edilecek kadar slide’ı alalım
            int maxAllowedSlides = Mathf.Clamp(finalCol - 2, 4, 8); // 30. level’de 8 slide’a kadar
            levelSO.SlidesPrefabs = new List<GameObject>(maxAllowedSlides);

            int added = 0;
            foreach (GameObject slide in slidePrefabs)
            {
                if (added >= maxAllowedSlides) break;

                Slide slideComp;
                if (!slide.TryGetComponent(out slideComp)) continue;

                // Eğer bu slide’ın rengi, objective renk seti içinde yoksa da ekleyebiliriz; puzzle’da sorun değil
                levelSO.SlidesPrefabs.Add(slide);
                added++;
            }

            // 7) Cube prefabs explicit referansla ekle (explicit type)
            levelSO.ColorObjectPrefabs = new List<GameObject>(cubePrefabs.Count);
            foreach (GameObject cubeGo in cubePrefabs)
            {
                levelSO.ColorObjectPrefabs.Add(cubeGo);
            }

            // 8) Asset kaydetme explicit string
            string soPath = folder + "/Level_" + level + ".asset";
            AssetDatabase.CreateAsset(levelSO, soPath);
            EditorUtility.SetDirty(levelSO);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Levels generated. Grid, objectives ve slides sanitize edildi.");
    }
}
