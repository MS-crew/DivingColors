using MEC;

using Unity.VisualScripting;

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public const string menuSceneName = "Menu";
    public const string levelSceneName = "Level";
    public const string clearSceneName = "ClearScene";

    public static GameManager Instance { get; private set; }

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnEnable()
    {
        EventManager.OnLevelFinished += GameEnded; 
    }

    private void OnDisable()
    {
        EventManager.OnLevelFinished -= GameEnded;
    }

    private void GameEnded()
    {
        Time.timeScale = 0;
        InputControllerManager.Instance.IsInputEnabled = false;
        Debug.Log("Game Ended");
        
        //UIManager.Instance.ShowPanel<GameOverMenu>();
    }

    public void StartLevel()
    {
        SceneManager.LoadScene(levelSceneName);
        UIManager.Instance.ShowPanel<InGame>();
        //Timing.CallDelayed(5f, ColorObjectsManager.Instance.Generate);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(clearSceneName);
        UIManager.Instance.ShowPanel<Menu>();
    }

    public void RestartLevel() 
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(clearSceneName);
        StartLevel();
    }

    public void PauseGame()
    {
        Time.timeScale = 0;
        InputControllerManager.Instance.IsInputEnabled = false;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        InputControllerManager.Instance.IsInputEnabled = true;
    }

    public void Quit() => Application.Quit();
}
