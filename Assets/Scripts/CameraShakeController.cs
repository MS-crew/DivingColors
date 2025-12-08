using System.Collections.Generic;

using MEC;

using UnityEngine;

public sealed class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance { get; private set; }

    private Transform camTransform;
    private Quaternion initialRot;

    private CoroutineHandle shakeHandle;
    private bool isShaking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        camTransform = transform;
        initialRot = camTransform.localRotation;
    }

    public void Shake(float intensity, float duration, bool forceOverride)
    {
        if (isShaking)
        {
            if (!forceOverride)
                return;

            Timing.KillCoroutines(shakeHandle);
            camTransform.localRotation = initialRot;
        }

        shakeHandle = Timing.RunCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator<float> ShakeRoutine(float intensity, float duration)
    {
        isShaking = true;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;

            camTransform.localRotation =
                initialRot * Quaternion.Euler(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return Timing.WaitForOneFrame;
        }

        camTransform.localRotation = initialRot;
        isShaking = false;
    }
}
