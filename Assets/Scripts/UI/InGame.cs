using UnityEngine;
using UnityEngine.UI;

public class InGame : UIPanel
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Button pauseButton;

    private void OnEnable()
    {
        UpdateScore(ScoreManager.Instance.Score);

        EventManager.OnUpdateScore += UpdateScore;
        pauseButton.onClick.AddListener(OpenPauseMenu);
    }

    private void OnDisable()
    {
        EventManager.OnUpdateScore -= UpdateScore;
        pauseButton.onClick.RemoveAllListeners();
    }

    private void OpenPauseMenu()
    {
        UIManager.Instance.ShowPanel<Pause>();
        GameManager.Instance.PauseGame();
    }

    private void UpdateScore(int newScore) => scoreText.text = newScore.ToString();
}
