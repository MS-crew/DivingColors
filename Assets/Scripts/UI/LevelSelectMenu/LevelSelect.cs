using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class LevelSelect : UIPanel
{
    [SerializeField] private Button backButton;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject levelCardPrefab;
    [SerializeField] private AudioClip levelUnlockedSound;
    [SerializeField] private AudioClip levelCantUnlockedSound;

    private LevelSelectPresenter presenter;

    protected override void Awake()
    {
        base.Awake();
        presenter = new LevelSelectPresenter(this);
    }

    private void OnEnable()
    {
        backButton.onClick.AddListener(OnReturnClicked);
        presenter.LoadLevels();
    }

    private void OnDisable() => backButton.onClick.RemoveAllListeners();

    private void OnReturnClicked() => UIManager.Instance.GoBack();

    public void DisplayLevels(IOrderedEnumerable<LevelDataSO> levels)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (LevelDataSO level in levels)
        {  
            GameObject levelCardGO = Instantiate(levelCardPrefab, contentParent);

            if (levelCardGO.TryGetComponent(out LevelCard card))
            {
                card.Setup(level, presenter.OnLevelSelected);
            }
        }
    }
}
