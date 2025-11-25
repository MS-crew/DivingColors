using UnityEngine;
using UnityEngine.UI;

public class GameFinished : UIPanel
{
    [SerializeField] private Text scoreText;
    [SerializeField] private AudioClip sound;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button nextLvlButton;
    [SerializeField] private GameObject highScoreText;
    private void OnEnable()
    {
        SetUI();
        menuButton.onClick.AddListener(OnClickedMenu);
        quitButton.onClick.AddListener(OnClickedQuit);
        nextLvlButton.onClick.AddListener(OnClickedNextLvl);
    }

    private void OnDisable()
    {
        menuButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        nextLvlButton.onClick.RemoveAllListeners();
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
    private void OnClickedNextLvl() 
    {
        if (!GameManager.Instance.TryStartNextLevel())
            OnClickedMenu();
    }
}
