using System.Collections.Generic;

using DG.Tweening;

using MEC;

using TMPro;

using UnityEngine;

using static Assets.PublicEnums;

[RequireComponent(typeof(Rigidbody))]
public class ColorObject : MonoBehaviour
{
    private const int MinLifeTime = 2;
    private const float HideAnimTime = 0.3f;

    [SerializeField] private TextMeshPro textMeshPro;

    [field: SerializeField] public int RowIndex { get; set; }
    [field: SerializeField] public int ColumnIndex { get; set; }
    [field: SerializeField] public ColorType ColorType { get; private set; }
    [field: SerializeField] public AudioClip Collectsound { get; private set; }

    public Rigidbody Rb { get; private set; }

    private int lifeTime;
    private Vector3 scaleChache;
    private bool isObjective, isSubscribedToSlide;

    private void Awake() 
    { 
        Rb = GetComponent<Rigidbody>();
        scaleChache = transform.localScale;
    }

    private void OnEnable()
    {
        ResetRigidbody();

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null)
            return;

        isObjective = lvl.CollectionObjectives.ContainsKey(ColorType);

        if (textMeshPro != null)
            textMeshPro.gameObject.SetActive(isObjective);

        if (!isObjective)
            return;

        lifeTime = Random.Range(MinLifeTime, lvl.LevelData.RowCount - 1);
        UpdateText();

        SubscribeSlideEvents();
    }

    private void OnDisable()
    {
        ResetRigidbody();
        UnsubscribeSlideEvents();
        transform.localScale = scaleChache;
    }

    private void ResetRigidbody()
    {
        Rb.velocity = Rb.angularVelocity = Vector3.zero;
    }

    private void SubscribeSlideEvents()
    {
        if (!isObjective)
            return;

        if (isSubscribedToSlide)
            return;

        EventManager.OnSlideUsed += OnSlideUsed;
        isSubscribedToSlide = true;
    }

    private void UnsubscribeSlideEvents()
    {
        if (!isSubscribedToSlide)
            return;

        EventManager.OnSlideUsed -= OnSlideUsed;
        isSubscribedToSlide = false;
    }

    public void DetachFromGrid()
    {
        UnsubscribeSlideEvents();
        RowIndex = -1;
        ColumnIndex = -1;
    }

    private void OnSlideUsed(Slide slide, List<ColorObject> collected)
    {
        if (collected != null && collected.Contains(this))
            return;

        if (lifeTime <= 0)
            return;

        lifeTime--;
        UpdateText();

        if (lifeTime <= 0)
            transform.DOScale(0f, HideAnimTime).SetEase(Ease.InBack).OnComplete(Expired);
    }

    private void Expired()
    {
        EventManager.ObjectiveExpired(this);
    }

    private void UpdateText() => textMeshPro.text = lifeTime.ToString();
}
