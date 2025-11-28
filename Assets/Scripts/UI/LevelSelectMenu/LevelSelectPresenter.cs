using System.Linq;

using MEC;

using UnityEngine;

public class LevelSelectPresenter
{
    private readonly LevelSelect view;

    private IOrderedEnumerable<LevelDataSO> allLevels;

    public LevelSelectPresenter(LevelSelect view) => this.view = view;

    public void LoadLevels()
    {
        allLevels = Resources.LoadAll<LevelDataSO>(GameManager.levelsResourcePath).OrderBy(x => x.LevelId);

        view.DisplayLevels(allLevels);
    }

    public void OnLevelSelected(LevelDataSO selectedLevel) 
    { 
        Debug.Log($"Level selected: {selectedLevel.LevelId}");
        Timing.RunCoroutine(GameManager.Instance.StartLevel(selectedLevel, false)); 
    }
}
