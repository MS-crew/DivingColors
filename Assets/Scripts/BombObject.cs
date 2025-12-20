using System.Collections.Generic;

using MEC;

using UnityEngine;

public class BombObject : ColorObject
{
    [Header("Bomb Settings")]
    [SerializeField] private byte shatterPoolId = 11;
    [field: SerializeField] public int ExplodeRadius { get; private set; } = 2;

    public override bool CanBeClicable => false;

    private bool hasExploded = false;

    public override void OnEnable()
    {
        ResetRigidbody();
        transform.localScale = scaleChache;
        hasExploded = false;

        isObjective = true;

        if (textMeshPro != null)
            textMeshPro.gameObject.SetActive(true);

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null) 
            return;

        int maxRows = lvl.LevelData.RowCount;
        lifeTime = Random.Range(MinLifeTime, maxRows);
        UpdateText();

        SubscribeSlideEvents();
    }

    public override void OnClicked() { }

    public override void OnSlideUsed(Slide slide, List<ColorObject> collected)
    {
        if (collected != null && collected.Contains(this))
            return;

        if (lifeTime <= 0 || hasExploded) 
            return;

        lifeTime--;
        UpdateText();

        if (lifeTime <= 0)
            Explode();
    }

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