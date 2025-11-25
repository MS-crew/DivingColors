using System;
using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

[CreateAssetMenu(fileName = "New Level", menuName = "Level Data")]
public class LevelDataSO : ScriptableObject
{
    public uint LevelId = 1;
    public int ClickAttempts = 20;

    [Header("Color Objects Settings")]
    public int RowCount = 5;
    public int ColumnCount = 5;
    public List<GameObject> ColorObjectPrefabs;
    public List<GameObject> SlidesPrefabs;

    [Header("Combo Settings")]
    public bool CombosActive = false;
    public int minComboCount = 7;

    [Header("Objectives Settings")]
    public List<ObjectiveData> Objectives = new();
}

[Serializable]
public class ObjectiveData
{
    public ColorType Color;
    public int TargetAmount = 0;
    public float SpawnChanceMultiplier = 1f;
}


