using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShatterPiece : MonoBehaviour
{
    private const float minForce = 5f;
    private const float maxForce = 15f;

    private Rigidbody rigidBody;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        transform.GetLocalPositionAndRotation(out initialLocalPosition, out initialLocalRotation);
    }

    private void OnEnable()
    {
        Vector3 randomDirection = Random.onUnitSphere;
        float force = Random.Range(minForce, maxForce);
        rigidBody.AddForce(randomDirection * force, ForceMode.Impulse);
        rigidBody.AddTorque(Random.insideUnitSphere * 3, ForceMode.Impulse);
    }

    private void OnDisable()
    {
        rigidBody.velocity = rigidBody.angularVelocity = Vector3.zero;
        transform.SetLocalPositionAndRotation(initialLocalPosition, initialLocalRotation);
    }
}
