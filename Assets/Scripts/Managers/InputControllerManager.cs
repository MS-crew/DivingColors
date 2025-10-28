using UnityEngine;

public class InputControllerManager : MonoBehaviour
{
    public static InputControllerManager Instance;

    const string slideTag = "Slide";
    public bool IsInputEnabled { get; set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
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
