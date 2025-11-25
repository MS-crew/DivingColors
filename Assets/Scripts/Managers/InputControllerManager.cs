using MEC;

using UnityEngine;

public class InputControllerManager : MonoBehaviour
{
    public static InputControllerManager Instance;

    private const string slideTag = "Slide";
    

    private int _attempt;
    public int InputAttempt
    {
        get {  return _attempt; }
        set 
        { 
            _attempt = value;
            EventManager.ClickAttemtUsed(value);
        }
    }

    [field: SerializeField]
    public bool IsInputEnabled { get; set; } = true;

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void Update()
    {
        if (!IsInputEnabled || !Input.GetMouseButtonDown(0))
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (!hit.transform.parent.CompareTag(slideTag))
            return;

        if (!hit.transform.parent.TryGetComponent(out Slide slide) || slide.IsLocked)
            return;

        InputAttempt--;
        StartCoroutine(slide.OnClicked());
    }

    public void Reset()
    {
        IsInputEnabled = true;
    }
}
