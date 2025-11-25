using System.Collections.Generic;

using MEC;

using UnityEngine;
using UnityEngine.UI;

using static Assets.PublicEnums;

public class InGame : UIPanel
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text attemptText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private GameObject objectiveItemPrefab;

    private void OnEnable()
    {
        Timing.RunCoroutine(SetupUI());

        EventManager.OnUpdateScore += UpdateScore;
        EventManager.OnClickAttemtUsed += UpdateAttempt;

        pauseButton.onClick.AddListener(OpenPauseMenu);
    }
    private void OnDisable()
    {
        EventManager.OnUpdateScore -= UpdateScore;
        EventManager.OnClickAttemtUsed -= UpdateAttempt;
        
        pauseButton.onClick.RemoveAllListeners();
    }

    public IEnumerator<float> SetupUI()
    {
        UpdateScore(ScoreManager.Instance.Score);
        UpdateAttempt(InputControllerManager.Instance.InputAttempt);

        foreach (Transform t in objectivesContainer)
            Destroy(t.gameObject);

        yield return Timing.WaitUntilTrue(() => LevelManager.Instance != null && LevelManager.Instance.CollectionObjectives != null);

        foreach (KeyValuePair<ColorType, int> objective in LevelManager.Instance.CollectionObjectives)
        {
            GameObject itemGO = Instantiate(objectiveItemPrefab, objectivesContainer);

            if (itemGO.TryGetComponent(out ObjectiveItem item))
            {
                item.Setup(objective.Key, 0, objective.Value);
            }
        }
    }
    private void OpenPauseMenu()
    {
        UIManager.Instance.ShowPanel<Pause>();
        GameManager.Instance.PauseGame();
    }

    private void UpdateScore(int newScore) => scoreText.text = newScore.ToString();

    private void UpdateAttempt(int newValue) => attemptText.text = newValue.ToString();
}
