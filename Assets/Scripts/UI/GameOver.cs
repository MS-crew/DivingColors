using UnityEngine;
using UnityEngine.UI;

public class GameOver : UIPanel
{
    [SerializeField] private Text scoreText; 
    [SerializeField] private AudioClip sound; 
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private GameObject highScoreText;

    private void OnEnable()
    {
        SetUI();
        menuButton.onClick.AddListener(OnClickedMenu);
        quitButton.onClick.AddListener(OnClickedQuit);
        restartButton.onClick.AddListener(OnClickedRestart);
    }

    private void OnDisable()
    {
        menuButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
    }

    private void SetUI()
    {
        SoundManager.Instance.PlayGlobalSound(sound);

        bool isHighScore = ScoreManager.Instance.IsHighScore;

        highScoreText.SetActive(isHighScore);
        scoreText.text = ScoreManager.Instance.Score.ToString();
    }

    private void OnClickedQuit() => GameManager.Instance.Quit();
    private void OnClickedMenu() => GameManager.Instance.ReturnToMainMenu();
    private void OnClickedRestart() => GameManager.Instance.RestartLevel();
}
