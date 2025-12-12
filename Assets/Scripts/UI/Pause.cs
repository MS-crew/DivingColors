using UnityEngine;
using UnityEngine.UI;

public class Pause : UIPanel
{   
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton; 
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button settingsButton;

    private void OnEnable()
    {
        resumeButton.onClick.AddListener(OnClickedResume);
        restartButton.onClick.AddListener(OnClickedRestart); 
        settingsButton.onClick.AddListener(OnClickedSettings);
        mainMenuButton.onClick.AddListener(OnClickedMainMenu);
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
    }

    private void OnClickedResume()
    {
        GameManager.Instance.ResumeGame();
        UIManager.Instance.ShowPanel<InGame>();
    }

    private void OnClickedRestart() => GameManager.Instance.RestartLevel();

    private void OnClickedMainMenu() => GameManager.Instance.ReturnToMainMenu();

    private void OnClickedSettings() => UIManager.Instance.NavigateTo<Settings>(UIManager.NavigationMode.Popup); 
}
