using UnityEngine;

public class ColorObject : MonoBehaviour
{
    [field: SerializeField] public GameManager.ColorType ColorType { get; private set; }

    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void OnDisable()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
