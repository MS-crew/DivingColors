using UnityEngine;

public class InputControllerManager : MonoBehaviour
{
    public static InputControllerManager Instance;

    const string slideTag = "Slide";

    public bool IsInputEnabled { get; set; } = true;

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ColorObjectsManager.Instance.Generate();
        }

        if (IsInputEnabled && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            if (!hit.transform.parent.CompareTag(slideTag))
                return;

            if (hit.transform.parent.TryGetComponent(out Slide slide))
                slide.OnClicked();
        }
    }
}
