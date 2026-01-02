using UnityEngine;
using UnityEngine.UI;

using static Assets.PublicEnums;

public class ObjectiveItem : MonoBehaviour
{
    private ColorType colorType;
    [SerializeField] private Text countText;
    [SerializeField] private Image colorImage;
    [SerializeField] private ObjectiveIcons colorIcons;

    private void OnEnable() => EventManager.OnObjectiveUpdated += UpdateCount;
    private void OnDisable() => EventManager.OnObjectiveUpdated -= UpdateCount;

    private void UpdateCount(ColorType type)
    {
        if (type != colorType)
            return;

        if (!ScoreManager.Instance.CollectedObjectives.TryGetValue(type, out int current) || !LevelManager.Instance.CollectionObjectives.TryGetValue(type, out int target))
            return;

        UpdateCount(current, target);   
    }

    public void Setup(ColorType color, int target)
    {
        colorType = color;
        colorImage.sprite = colorIcons.GetIcon(color);
        countText.text = $"0/{target}";
    }

    public void UpdateCount(int current, int target)
    {
        countText.text = $"{current}/{target}";
    }
}