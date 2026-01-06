using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

using MEC;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using static Assets.PublicEnums;

public class InGame : UIPanel
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text attemptText;
    [SerializeField] private TMP_Text levelIDText;

    [SerializeField] private Button pauseButton;
    [SerializeField] private Transform objectivesContainer;
    [SerializeField] private GameObject objectiveItemPrefab;

    [Header("Combo Hint")]
    [SerializeField] private TMP_Text comboX;
    [SerializeField] private TMP_Text comboTier;
    [SerializeField] private TMP_Text comboHeader;
    [SerializeField] private GameObject comboHint;
    [SerializeField] private CanvasGroup comboCanvas;

    [Header("Gain Attempt")]
    [SerializeField] private TMP_Text gainAttemptText;
    [SerializeField] private GameObject gainAttemptHint;
    [SerializeField] private CanvasGroup gainAttemptCanvas;

    private void OnEnable()
    {
        Timing.RunCoroutine(SetupUI());

        EventManager.OnComboUsed += ComboUsed;
        EventManager.OnUpdateScore += UpdateScore; 
        EventManager.OnAttemptGained += AttemptGained;
        EventManager.OnClickAttemtUsed += UpdateAttempt;

        pauseButton.onClick.AddListener(OpenPauseMenu);
    }

    private void OnDisable()
    {
        EventManager.OnComboUsed -= ComboUsed;
        EventManager.OnUpdateScore -= UpdateScore;
        EventManager.OnAttemptGained -= AttemptGained;
        EventManager.OnClickAttemtUsed -= UpdateAttempt;
        
        pauseButton.onClick.RemoveAllListeners();
    }

    public IEnumerator<float> SetupUI()
    {
        comboHint.SetActive(false);
        gainAttemptHint.SetActive(false);

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
                item.Setup(objective.Key, objective.Value);
            }
        }

        levelIDText.text = $"LEVEL\n {LevelManager.Instance.LevelData.LevelId}";
    }

    private void OpenPauseMenu()
    {
        UIManager.Instance.ShowPanel<Pause>();
        GameManager.Instance.PauseGame();
    }

    private void ComboUsed(string comboName, ComboTier tier, int x)
    {
        if (comboHint == null || comboCanvas == null)
            return;

        comboHint.SetActive(true);

        comboHeader.text = comboName;
        comboTier.text = tier.GetComboTierToString();

        comboX.text = string.Concat(x,"X");

        RectTransform rect = (RectTransform)comboHint.transform;

        comboCanvas.alpha = 0f;
        rect.localScale = Vector3.one * 0.5f;

        DOTween.Kill(comboCanvas);
        DOTween.Kill(rect);

        Sequence seq = DOTween.Sequence();
        seq.Append(comboCanvas.DOFade(1f, 0.15f));
        seq.Join(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.7f);
        seq.Append(comboCanvas.DOFade(0f, 0.25f));
        seq.OnComplete(() => comboHint.SetActive(false));
    }

    private void AttemptGained(int amount)
    {
        if (gainAttemptHint == null || gainAttemptCanvas == null)
            return;

        gainAttemptHint.SetActive(true);

        gainAttemptText.text = "+" + amount.ToString();

        RectTransform rect = (RectTransform)gainAttemptHint.transform;

        gainAttemptCanvas.alpha = 0f;
        rect.localScale = Vector3.one * 0.5f;

        DOTween.Kill(gainAttemptCanvas);
        DOTween.Kill(rect);

        Sequence seq = DOTween.Sequence();
        seq.Append(gainAttemptCanvas.DOFade(1f, 0.15f));
        seq.Join(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.7f);
        seq.Append(gainAttemptCanvas.DOFade(0f, 0.25f));
        seq.OnComplete(() => gainAttemptHint.SetActive(false));
    }

    private void UpdateScore(int newScore) => scoreText.text = newScore.ToString();

    private void UpdateAttempt(int newValue) => attemptText.text = newValue.ToString();
}