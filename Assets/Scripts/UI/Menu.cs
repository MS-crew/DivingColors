using UnityEngine;
using UnityEngine.UI;

public class Menu : UIPanel
{
    [SerializeField] private Button playButton; 
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void OnEnable()
    {
        playButton.onClick.AddListener(OnClickedPlay);
        quitButton.onClick.AddListener(OnClickedQuit);
        settingsButton.onClick.AddListener(OnClickedSettings);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
    }

    private void OnClickedQuit() => GameManager.Instance.Quit();

    private void OnClickedPlay() => GameManager.Instance.StartLevel();

    private void OnClickedSettings() 
    {
        //UIManager.Instance.NavigateTo<Settings>(UIManager.NavigationMode.Replace);
    } 
}
