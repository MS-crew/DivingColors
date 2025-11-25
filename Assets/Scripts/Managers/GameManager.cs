using System;
using System.Collections.Generic;
using System.Linq;

using MEC;

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const string menuSceneName = "Menu";
    public const string levelSceneName = "Level";
    public const string clearSceneName = "ClearScene";
    public const string levelsResourcePath = "Levels";

    private const float gameOverDelay = 2f;
    private const float activeTimeScale = 1f;
    private const float stoppedTimeScale = 0f;
    
    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnEnable()
    {
        EventManager.OnGameEnded += GameOver;
        EventManager.OnSlideUsed += CheckGame;
        EventManager.OnGameFinished += GameFinished;
    }

    private void OnDisable()
    {
        EventManager.OnGameEnded -= GameOver;
        EventManager.OnSlideUsed -= CheckGame;
        EventManager.OnGameFinished -= GameFinished;
    }

    private void CheckGame(Slide _, List<ColorObject> _2)
    {
        if (InputControllerManager.Instance.InputAttempt <= 0)
        {
            Timing.CallDelayed(gameOverDelay, EventManager.GameEnded);
            return;
        }

        if (LevelManager.Instance.AreAllObjectivesCompleted())
        {
            Timing.CallDelayed(gameOverDelay, EventManager.GameFinished);
            return;
        }
    }

    private void GameFinished()
    {
        Time.timeScale = stoppedTimeScale;
        UIManager.Instance.ShowPanel<GameFinished>();
        InputControllerManager.Instance.IsInputEnabled = false;
    }


    private void GameOver()
    {
        Time.timeScale = stoppedTimeScale; 
        UIManager.Instance.ShowPanel<GameOver>();
        InputControllerManager.Instance.IsInputEnabled = false;
    }

    public IEnumerator<float> StartLevel(LevelDataSO leveldata)
    {
        ScoreManager.Instance.Score = 0;
        SceneManager.LoadScene(levelSceneName);
        UIManager.Instance.ShowPanel<InGame>();

        yield return Timing.WaitUntilTrue(() => SceneManager.GetActiveScene().name == levelSceneName &&  LevelManager.Instance != null);

        LevelManager.Instance.Initialize(leveldata);
        InputControllerManager.Instance.Reset();
    }

    public bool TryStartNextLevel()
    {
        uint oldLevelId = LevelManager.Instance.LevelData.LevelId;
        LevelDataSO[] allLevels = Resources.LoadAll<LevelDataSO>(GameManager.levelsResourcePath).OrderBy(lvl => lvl.LevelId).ToArray();
        int index = Array.FindIndex(allLevels, lvl => lvl.LevelId == oldLevelId);

        if (index == -1 || index == allLevels.Length - 1)
            return false;

        LevelDataSO nextLevel = allLevels[index + 1];
        if (nextLevel == null) 
            return false;

        Timing.RunCoroutine(StartLevel(nextLevel));
        return true;
    }

    public void ReturnToMainMenu()
    {
        LevelManager.Instance.ReturnToPoolAll();

        Time.timeScale = activeTimeScale;
        SceneManager.LoadScene(clearSceneName);
        SceneManager.LoadScene(menuSceneName);
        UIManager.Instance.ShowPanel<Menu>();
    }

    public void RestartLevel() 
    {
        LevelManager.Instance.ReturnToPoolAll();

        LevelDataSO levelDataSO = LevelManager.Instance.LevelData;

        Time.timeScale = activeTimeScale;
        SceneManager.LoadScene(clearSceneName);
        Timing.RunCoroutine(StartLevel(levelDataSO));
    }

    public void PauseGame()
    {
        Time.timeScale = stoppedTimeScale;
        InputControllerManager.Instance.IsInputEnabled = false;
    }

    public void ResumeGame()
    {
        Time.timeScale = activeTimeScale;
        InputControllerManager.Instance.IsInputEnabled = true;
    }

    public void Quit() => Application.Quit();
}
