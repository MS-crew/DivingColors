using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPanel : MonoBehaviour
{
    public CanvasGroup CanvasGroup { get; private set; }

    protected virtual void Awake()
    {
        CanvasGroup = GetComponent<CanvasGroup>();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterPanel(this);
        }

        Hide();
    }

    protected virtual void OnDestroy()
    {
        CanvasGroup = null;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnRegisterPanel(this);
        }
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        SetInteractable(true);
    }

    public virtual void Hide()
    {
        SetInteractable(false);
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool isInteractable)
    {
        CanvasGroup.interactable = isInteractable;
    }
}