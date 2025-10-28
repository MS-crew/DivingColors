using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public UIPanel CurrentPanel { get; private set; }

    private readonly List<UIPanel> allPanels = new(13);
    private readonly Stack<UIPanel> panelHistory = new(4);

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return null;
        allPanels.ForEach(p => p.Hide());

        /*if (FireBaseManager.Instance.User != null)
        {
            ShowPanel<MainMenu>();
            yield return FireBaseManager.Instance.LoadPlayerData();
        }
        else
        {
            ShowPanel<LoginSelectMenu>();
        }*/
    }

    public void RegisterPanel(UIPanel panel) => allPanels.Add(panel);

    public void UnRegisterPanel(UIPanel panel) => allPanels.Remove(panel);

    public void ShowPanel<T>() where T : UIPanel
    {
        panelHistory.Clear();
        UIPanel panelToOpen = allPanels.FirstOrDefault(p => p is T);

        foreach (UIPanel p in allPanels)
        {
            if (p == panelToOpen)
                p.Show();
            else
                p.Hide();
        }

        CurrentPanel = panelToOpen;
    }

    public void NavigateTo<T>(NavigationMode mode) where T : UIPanel
    {
        if (CurrentPanel != null)
        {
            panelHistory.Push(CurrentPanel);

            if (mode == NavigationMode.Replace)
            {
                CurrentPanel.Hide();
            }
            else
            {
                CurrentPanel.SetInteractable(false);
            }
        }

        UIPanel panelToOpen = allPanels.FirstOrDefault(p => p is T);
        if (panelToOpen != null)
        {
            panelToOpen.Show();
            CurrentPanel = panelToOpen;
        }
    }

    public void GoBack()
    {
        if (panelHistory.Count <= 0)
            return;

        if (CurrentPanel != null)
        {
            CurrentPanel.Hide();
        }

        UIPanel previousPanel = panelHistory.Pop();
        previousPanel.Show();
        CurrentPanel = previousPanel;
    }

    public enum NavigationMode { Replace, Popup }
}