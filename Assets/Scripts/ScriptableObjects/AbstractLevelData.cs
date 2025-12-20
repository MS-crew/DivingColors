using System.Collections.Generic;

using UnityEngine;

[CreateAssetMenu(fileName = "Level Abstract", menuName = "Abstract Level Data")]
public class AbstractLevelDataSO : ScriptableObject
{
    public List<GameObject> SlidesPrefabs;
    public List<GameObject> ColorObjectPrefabs;

    [SerializeReference, ComboSelector]
    public List<SlideCombo> Combos = new();
}