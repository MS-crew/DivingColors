using DG.Tweening;

using MEC;

using UnityEngine;

public class BombObject : ColorObject
{
    [SerializeField] private byte shatterPoolId = 11;
    [field: SerializeField] public int ExplodeRadius { get; private set; } = 2;
    [field: SerializeField] public float ShakeDuration { get; private set; } = 0.4f;
    [field: SerializeField] public float ShakeInsentity { get; private set; } = 0.7f;

    public override bool CanBeClicable => false;

    private bool hasExploded = false;

    public override void OnEnable()
    {
        spawnFrameCount = Time.frameCount;

        ResetRigidbody();
        transform.DOKill();
        transform.localScale = Vector3.one;

        hasExploded = false;
        isObjective = true;

        if (textMeshPro != null)
            textMeshPro.gameObject.SetActive(true);

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null) 
            return;

        lifeTime = Random.Range(MinLifeTime, lvl.LevelData.RowCount);

        UpdateText();
        SubscribeSlideEvents();
    }

    public override void OnClicked() { }

    public override void Expired()
    {
        if (!hasExploded)
            Explode();
    }

    private void Explode()
    {
        if (hasExploded) 
            return;

        hasExploded = true;

        GameObject shattered = PoolManager.Instance.SpawnFromPool(shatterPoolId, transform.position, transform.rotation);
        Timing.CallDelayed(3f, () => { shattered.ReturnToPool(shatterPoolId); });

        EventManager.BombExploded(this);
    }
}