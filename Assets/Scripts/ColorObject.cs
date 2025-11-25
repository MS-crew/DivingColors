using System.Collections.Generic;

using DG.Tweening;

using MEC;

using TMPro;

using UnityEngine;

using static Assets.PublicEnums;

[RequireComponent(typeof(Rigidbody))]
public class ColorObject : MonoBehaviour
{
    private int lifeTime = 0;

    private const int MinLifeTime = 2;
    private const float HideAnimTime = 0.3f;

    [SerializeField] private TextMeshPro textMeshPro;

    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public Rigidbody Rb { get; private set; }

    [field: SerializeField] public ColorType ColorType { get; private set; }
    [field: SerializeField] public AudioClip Collectsound { get; private set; }

    private void Awake() => Rb = GetComponent<Rigidbody>();

    private void OnEnable()
    {
        LevelManager lvl = LevelManager.Instance;
        if (lvl == null)
            return;

        bool isObjective = lvl.CollectionObjectives.ContainsKey(ColorType);

        textMeshPro.gameObject.SetActive(isObjective);

        if (!isObjective)
            return;

        lifeTime = Random.Range(MinLifeTime, lvl.LevelData.RowCount - 1);
        UpdateText();

        EventManager.OnSlideUsed += OnSlideUsed;
    }

    private void OnDisable()
    {
        Rb.velocity = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;

        LevelManager lvl = LevelManager.Instance;
        if (lvl == null)
            return;

        if (lvl.CollectionObjectives.ContainsKey(ColorType))
            EventManager.OnSlideUsed -= OnSlideUsed;
    }

    private void OnSlideUsed(Slide slide, List<ColorObject> collected)
    {
        if (collected.Contains(this))
            return;

        lifeTime--;
        UpdateText();

        if (lifeTime <= 0)
            transform.DOScale(0, HideAnimTime).SetEase(Ease.InBack).OnComplete(() => Expired());
    }

    private void Expired()
    {
        //InputControllerManager.Instance.IsInputEnabled = false;
        EventManager.ObjectiveExpired(this); 
        //InputControllerManager.Instance.IsInputEnabled = true;
    }

    private void UpdateText() => textMeshPro.text = lifeTime.ToString();
}
