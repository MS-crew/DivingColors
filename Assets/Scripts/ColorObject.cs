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

    public bool CanBeClicable 
    {
        get
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null)
                return false;

            if (!levelManager.SlideCache.TryGetValue(ColorType, out Slide slide))
                return false;

            if (slide.IsLocked)
                return false;

            for (int row = RowIndex; row >= 0; row--)
            {
                if (levelManager.ColorObjects[row, ColumnIndex].ColorType != ColorType)
                    return false;
            }

            return true; 
        }
    }

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

    public void OnClicked() 
    {
        StartCoroutine(LevelManager.Instance.SlideCache[ColorType].OnClicked());
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

    private void ResetRigidbody()
    {
        Rb.velocity = Rb.angularVelocity = Vector3.zero;
    }

    public void DetachFromGrid()
    {
        UnsubscribeSlideEvents();
        RowIndex = -1;
        ColumnIndex = -1;
    }

    private void Expired()
    {
        EventManager.ObjectiveExpired(this);
    }

    private void UpdateText() => textMeshPro.text = lifeTime.ToString();
}
