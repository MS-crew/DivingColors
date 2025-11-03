using UnityEngine;

using static Assets.PublicEnums;

public class ColorObject : MonoBehaviour
{
    [field: SerializeField] public ColorType ColorType { get; private set; }
    [field: SerializeField] public AudioClip Collectsound {  get; private set; }

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
