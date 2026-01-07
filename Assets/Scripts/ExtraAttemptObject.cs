using DG.Tweening;

using UnityEngine;

public class ExtraAttemptObject : ColorObject
{
    [SerializeField] private int GainAmmount = 1;

    public override bool CanBeClicable { get; } = false;

    public override void OnEnable()
    {
        spawnFrameCount = Time.frameCount;

        ResetRigidbody();
        transform.DOKill();
        transform.localScale = Vector3.one;

        isObjective = true;

        if (textMeshPro != null)
            textMeshPro.gameObject.SetActive(isObjective);

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null)
            return;

        lifeTime = Random.Range(MinLifeTime, lvl.LevelData.RowCount + 1); 

        UpdateText();
        SubscribeSlideEvents();
    }

    public override void OnCollected()
    {
        EventManager.AttemptGained(GainAmmount);
        DetachFromGrid();
    }

    public override void OnClicked() { }
}
