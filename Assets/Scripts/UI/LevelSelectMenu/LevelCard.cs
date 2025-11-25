using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelCard : MonoBehaviour
{
    [SerializeField] private Text levelNameText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject scoreText;

    public void Setup(LevelDataSO levelData, Action<LevelDataSO> onLevelSelected)
    {
        levelNameText.text = levelData.LevelId.ToString();

        selectButton.onClick.RemoveAllListeners();

        selectButton.onClick.AddListener(() => onLevelSelected?.Invoke(levelData)); 
    }

    private void OnDisable()
    {
        selectButton.onClick.RemoveAllListeners();
    }
}
