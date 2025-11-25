using UnityEngine;
using UnityEngine.UI;

public class Menu : UIPanel
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button levelSelectButton;

    private void OnEnable()
    {
        quitButton.onClick.AddListener(OnClickedQuit);
        settingsButton.onClick.AddListener(OnClickedSettings);
        levelSelectButton.onClick.AddListener(OnClickedLevelSelect);
    }

    private void OnDisable()
    {
        quitButton.onClick.RemoveAllListeners();
        settingsButton.onClick.RemoveAllListeners();
        levelSelectButton.onClick.RemoveAllListeners();
    }

    private void OnClickedQuit() => GameManager.Instance.Quit();

    private void OnClickedSettings() 
    {
        //UIManager.Instance.NavigateTo<Settings>(UIManager.NavigationMode.Replace);
    }

    private void OnClickedLevelSelect() 
    {
        UIManager.Instance.NavigateTo<LevelSelect>(UIManager.NavigationMode.Replace);
    }
}
