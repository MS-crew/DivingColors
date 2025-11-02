using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int _score = 0;

    public bool IsHighScore { get; set; }

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

    private void ResetScore() => Score = 0;
}
