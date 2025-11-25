using System.Collections.Generic;

using UnityEngine;

using static Assets.PublicEnums;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int _score = 0;

    public bool IsHighScore { get; set; }

    public Dictionary<ColorType, int> CollectedObjectives { get; set; } = new Dictionary<ColorType, int>();

    public int Score
    {
        get => _score;
        set
        {
            _score = value;
            EventManager.UpdateScore(_score);
        }
    }

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
        EventManager.OnResetScore += ResetScore;
    }

    private void OnDestroy()
    {
        EventManager.OnResetScore -= ResetScore;
        Instance = null;
    }

    private void ResetScore() 
    {
        Score = 0;
        CollectedObjectives.Clear();
    }

    public void AddObjective(ColorType type)
    {
        if (!CollectedObjectives.ContainsKey(type))
            CollectedObjectives[type] = 0;

        CollectedObjectives[type] += 1;
        EventManager.ObjectiveUpdated(type);
    }
}
