using Unity.VisualScripting;

using UnityEngine;

public class InputControllerManager : MonoBehaviour
{
    public static InputControllerManager Instance;

    private const string slideTag = "Slide";
    private const string colorObjectTag = "ColorObject";

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

    public bool IsInputEnabled { get; set; } = true;

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnEnable() => EventManager.OnAttemptGained += AttemptGained;

    private void OnDisable() => EventManager.OnAttemptGained -= AttemptGained;

    private void Update()
    {
        if (!IsInputEnabled || !Input.GetMouseButtonDown(0))
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.transform.CompareTag(colorObjectTag))
        {
            if (!hit.transform.TryGetComponent(out ColorObject colorObject) || !colorObject.CanBeClicable)
                return;

            InputAttempt -= 1;
            IsInputEnabled = false;
            colorObject.OnClicked();
            return;
        }

        if (hit.transform.parent != null && hit.transform.parent.CompareTag(slideTag))
        {
            if (!hit.transform.parent.TryGetComponent(out Slide slide) || slide.IsLocked)
                return;

            InputAttempt -= 1;
            IsInputEnabled = false;
            StartCoroutine(slide.OnClicked());
        }
    }

    public void Reset()
    {
        IsInputEnabled = true;
    }

    private void AttemptGained(int newAttempt) => InputAttempt += newAttempt;
}
