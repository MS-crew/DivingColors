using System.Collections.Generic;

using DG.Tweening;

using TMPro;

using UnityEngine;

using static Assets.PublicEnums;

[RequireComponent(typeof(Rigidbody))]
public class ColorObject : MonoBehaviour
{
    protected const int MinLifeTime = 2;
    protected const float HideAnimTime = 0.3f;

    [SerializeField] protected TextMeshPro textMeshPro;

    [field: SerializeField] public int RowIndex { get; set; }
    [field: SerializeField] public int ColumnIndex { get; set; }
    [field: SerializeField] public ColorType ColorType { get; protected set; }
    [field: SerializeField] public AudioClip Collectsound { get; protected set; }

    public Rigidbody Rb { get; protected set; }

    public virtual bool CanBeClicable
    {
        get
        {
            LevelManager levelManager = LevelManager.Instance;
            if (levelManager == null) return false;

            if (!levelManager.SlideCache.TryGetValue(ColorType, out Slide slide))
                return false;

            if (slide.IsLocked) return false;

            for (int row = RowIndex; row >= 0; row--)
            {
                if (!levelManager.ColorObjects[row, ColumnIndex].ColorType.EqualsColorType(ColorType))
                    return false;
            }

            return true;
        }
    }

    protected int lifeTime;
    protected Vector3 scaleChache;
    protected bool isObjective;
    protected bool isSubscribedToSlide;

    public virtual void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        scaleChache = transform.localScale;
    }

    public virtual void OnEnable()
    {
        ResetRigidbody();
        transform.DOKill();
        transform.localScale = scaleChache;

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null)
            return;

        isObjective = lvl.CollectionObjectives.ContainsKey(ColorType);

        if (textMeshPro != null)
            textMeshPro.gameObject.SetActive(isObjective);

        if (!isObjective)
            return;

        lifeTime = Random.Range(MinLifeTime, lvl.LevelData.RowCount);
        UpdateText();

        SubscribeSlideEvents();
    }

    public virtual void OnDisable()
    {
        ResetRigidbody();
        UnsubscribeSlideEvents();

        transform.DOKill();
        transform.localScale = scaleChache;
    }

    public virtual void OnClicked()
    {
        if (LevelManager.Instance.SlideCache.ContainsKey(ColorType))
            StartCoroutine(LevelManager.Instance.SlideCache[ColorType].OnClicked());
    }

    public virtual void SubscribeSlideEvents()
    {
        if (isSubscribedToSlide)
            return;

        EventManager.OnSlideUsed += OnSlideUsed;
        isSubscribedToSlide = true;
    }

    public virtual void UnsubscribeSlideEvents()
    {
        if (!isSubscribedToSlide)
            return;

        EventManager.OnSlideUsed -= OnSlideUsed;
        isSubscribedToSlide = false;
    }

    public virtual void OnSlideUsed(Slide slide, List<ColorObject> collected)
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

    public void ResetRigidbody()
    {
        Rb.velocity = Rb.angularVelocity = Vector3.zero;
    }

    public void DetachFromGrid()
    {
        UnsubscribeSlideEvents();
        RowIndex = -1;
        ColumnIndex = -1;
    }

    public virtual void Expired()
    {
        EventManager.ObjectiveExpired(this);
    }

    public virtual void OnCollected() 
    { 
        SoundManager.Instance.PlayGlobalSound(Collectsound, false);
        DetachFromGrid();
    }

    protected void UpdateText()
    {
        if (textMeshPro != null) 
            textMeshPro.text = lifeTime.ToString();
    }
}