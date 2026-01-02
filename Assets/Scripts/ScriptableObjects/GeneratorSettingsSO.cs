using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

[CreateAssetMenu(fileName = "GenerationSettings", menuName = "Configs/Generation Settings")]
public class GenerationSettingsSO : ScriptableObject
{
    [Header("Grid Ayarlarý")]
    public int MinRows = 4;
    public int MaxRows = 9;
    public int MinCols = 4;
    public int MaxCols = 8;

    [Tooltip("X: Level, Y: Grid Size Ratio")]
    public AnimationCurve GridSizeCurve;

    [Header("Renk Sayýsý")]
    public int MinColorCount = 3;
    public int MaxColorCount = 9;

    [Tooltip("X: Level, Y: Renk sayýsý (0 = MinColorCount, 1 = MaxColorCount)")]
    public AnimationCurve ColorCountCurve;

    [Header("Hedef Ayarlarý")]
    [Tooltip("Toplam renklerin yüzde kaçý hedef (Örn: 0.5 seçilirse, 6 rengin 3 ü)")]
    public AnimationCurve ObjectiveRatioCurve;

    [Tooltip("Gridin ne kadarý hedefle dolsun? (Yoðunluk) \n0.15 = Seyrek (Aramalý) \n0.35 = Yoðun (Toplamalý)")]
    public AnimationCurve TargetDensityCurve;

    [Tooltip("Oyuncuya ne kadar hata payý verelim? \n2.5 = Çok Rahat \n1.3 = Zor (Stresli)")]
    public AnimationCurve MovesSafetyMarginCurve;

    [Header("Referanslar")]
    [SerializeReference, ComboSelector]
    public List<SlideCombo> AllCombos;
    public List<GameObject> SlidesPrefabs;
    public List<GameObject> ColorObjectPrefabs;

    [Header("Special Items")]
    public List<SpecialItemRule> SpecialItemRules;

    [Serializable]
    public class SpecialItemRule
    {
        public GameObject Prefab;

        [Min(1)]
        public int UnlockLevel;

        [Header("Zorluk ayarý")]
        [Tooltip("X: Level, Y: Çýkma Þansý (0.01 - 0.20)")]
        public AnimationCurve SpawnChanceCurve;

        [Tooltip("Gridin yüzde kaçý kadar izin verilsin? (0.05 = %5). Örn: 64 karede 3 tane.")]
        [Range(0.01f, 0.2f)]
        public float MaxCountDensity = 0.07f;

        [Tooltip("en az bu kadar olsun")]
        [Min(1)] public int MinAbsoluteCount = 1;
    }

    public GameObject GetPrefabByColor(ColorType type)
    {
        foreach (GameObject prefab in ColorObjectPrefabs)
        {
            if (prefab.TryGetComponent(out ColorObject obj) && obj.ColorType == type)
                return prefab;
        }

        return null;
    }

    public GameObject GetSlideByColor(ColorType type)
    {
        foreach (GameObject prefab in SlidesPrefabs)
        {
            if (prefab.TryGetComponent(out Slide slide) && slide.Color == type)
                return prefab;
        }

        return null;
    }
}