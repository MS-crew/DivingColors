using System;
using System.Collections.Generic;
using System.Linq;

using MEC;

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool GameEnded { get; private set; } = false;

    public const string menuSceneName = "Menu";
    public const string levelSceneName = "Level";
    public const string clearSceneName = "ClearScene";
    public const string levelsResourcePath = "Levels";

    private const float gameOverDelay = 2f;
    private const float activeTimeScale = 1f;
    private const float stoppedTimeScale = 0f;

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
    }

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
            GameEnded = true;
            Timing.CallDelayed(gameOverDelay, EventManager.GameEnded);
            return;
        }

        if (LevelManager.Instance != null && LevelManager.Instance.AreAllObjectivesCompleted())
        {
            GameEnded = true;
            InputControllerManager.Instance.IsInputEnabled = false;
            Timing.CallDelayed(gameOverDelay, EventManager.GameFinished);
            return;
        }
    }

    private void GameFinished()
    {
        UIManager.Instance.ShowPanel<GameFinished>();
        InputControllerManager.Instance.IsInputEnabled = false;
    }

    private void GameOver()
    {
        UIManager.Instance.ShowPanel<GameOver>();
        InputControllerManager.Instance.IsInputEnabled = false;
    }

    public IEnumerator<float> StartLevel(LevelDataSO leveldata, bool useCleanScene)
    {
        GameEnded = false;
        Time.timeScale = activeTimeScale;
        ScoreManager.Instance.ResetScore();

        LevelManager oldLevel = LevelManager.Instance;
        if (oldLevel != null)
        {
            oldLevel.ReturnToPoolAll();
        }

        if (useCleanScene)
        {
            SceneManager.LoadScene(clearSceneName);
            yield return Timing.WaitForOneFrame;
        }

        SceneManager.LoadScene(levelSceneName);

        yield return Timing.WaitUntilTrue(() => SceneManager.GetActiveScene().name == levelSceneName && LevelManager.Instance != null);

        UIManager.Instance.ShowPanel<InGame>();

        LevelManager.Instance.Initialize(leveldata);

        InputControllerManager.Instance.Reset();
        InputControllerManager.Instance.IsInputEnabled = true;
    }

    public bool TryStartNextLevel()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.LevelData == null)
            return false;

        uint oldLevelId = LevelManager.Instance.LevelData.LevelId;
        LevelDataSO[] allLevels = Resources.LoadAll<LevelDataSO>(levelsResourcePath).OrderBy(lvl => lvl.LevelId).ToArray();

        int index = Array.FindIndex(allLevels, lvl => lvl.LevelId == oldLevelId);

        if (index == -1 || index == allLevels.Length - 1)
            return false;

        LevelDataSO nextLevel = allLevels[index + 1];
        if (nextLevel == null)
            return false;

        Timing.RunCoroutine(StartLevel(nextLevel, true));
        return true;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = activeTimeScale;
        Timing.RunCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator<float> ReturnToMainMenuRoutine()
    {
        LevelManager lvl = LevelManager.Instance;
        if (lvl != null)
        {
            lvl.ReturnToPoolAll();
        }

        SceneManager.LoadScene(clearSceneName);
        yield return Timing.WaitForOneFrame;

        SceneManager.LoadScene(menuSceneName);
        yield return Timing.WaitUntilTrue(() => SceneManager.GetActiveScene().name == menuSceneName);

        UIManager.Instance.ShowPanel<Menu>();
        InputControllerManager.Instance.Reset();
        InputControllerManager.Instance.IsInputEnabled = true;
    }

    public void RestartLevel()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.LevelData == null)
            return;

        Time.timeScale = activeTimeScale;
        LevelDataSO currentLevelData = LevelManager.Instance.LevelData;

        Timing.RunCoroutine(StartLevel(currentLevelData, true));
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

    public void Quit()
    {
        Application.Quit();
    }
}