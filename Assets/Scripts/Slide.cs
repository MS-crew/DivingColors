using System.Collections.Generic;

using DG.Tweening;

using MEC;

using UnityEngine;

using static Assets.PublicEnums;

public class Slide : MonoBehaviour
{
    [SerializeField] private Light avaliableLight;
    [SerializeField] private Transform slidePoint;

    [field: SerializeField] public int MinObjectCount { get; private set; }
    [field: SerializeField] public ColorType Color { get; private set; }

    public bool IsLocked { get; private set; } = false;

    private Vector3 forceDirection;
    private const float slideDuration = 0.5f;
    private readonly List<ColorObject> selectedObjects = new(15);

    private void Awake() => forceDirection = (transform.position + slidePoint.position).normalized;

    private void OnEnable()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.Slides.Add(this);

        EventManager.OnSlideUsed += OnSlideUsed;
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null)
            LevelManager.Instance.Slides.Remove(this);

        EventManager.OnSlideUsed -= OnSlideUsed;
    }

    private void OnSlideUsed(Slide slide, List<ColorObject> _)
    {
        if (slide == this)
        {
            IsLocked = true;
            avaliableLight.color = UnityEngine.Color.red;
        }
        else
        {
            if (!IsLocked)
                return;

            avaliableLight.color = UnityEngine.Color.green;
            IsLocked = false;
        }
    }

    public IEnumerator<YieldInstruction> OnClicked()
    {
        InputControllerManager.Instance.IsInputEnabled = false;

        LevelManager lvlManager = LevelManager.Instance;

        GetAvaliableObjects(lvlManager);

        if (selectedObjects.Count < MinObjectCount)
        {
            selectedObjects.Clear();
        }
        else
        {
            ProcessCombos(lvlManager, selectedObjects);
            Sequence seq = CollectSelecteds(selectedObjects, lvlManager);
            yield return seq.WaitForCompletion();
        }

        List<ColorObject> collectedSnapshot = new(selectedObjects);

        selectedObjects.Clear();

        EventManager.SlideUsed(this, collectedSnapshot);
    }

    private Sequence CollectSelecteds(List<ColorObject> selected, LevelManager lvlManager)
    {
        Sequence finalSeq = DOTween.Sequence();

        AudioClip clip = null;
        foreach (ColorObject obj in selected)
        {
            if (clip == null)
                clip = obj.Collectsound;

            Rigidbody rb = obj.Rb;

            finalSeq.Join(obj.transform.DOMove(slidePoint.position, slideDuration).OnComplete(() =>
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(forceDirection * 10f, ForceMode.Impulse);
            }));
        }

#if !UNITY_ANDROID || UNITY_IOS
        if (UserDataManager.Instance.Data.IsVibrationEnabled) 
            Handheld.Vibrate();
#endif
        SoundManager.Instance.PlayGlobalSound(clip);
        return finalSeq;
    }

    private Sequence RejectSelecteds(List<ColorObject> selectedObjects)// kullaným dýþý
    {
        selectedObjects.Clear();
        Sequence mainSequence = DOTween.Sequence();
        foreach (ColorObject obj in selectedObjects)
        {
            Transform t = obj.transform;
            mainSequence.Join(DOTween.Sequence()
                .Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad))
                .Append(t.DORotate(new Vector3(0, 0, -30), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad))
                .Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad)));
        }

        return mainSequence;
    }

    private void GetAvaliableObjects(LevelManager lvlManager)
    {
        selectedObjects.Clear();

        ColorObject[,] grid = lvlManager.ColorObjects;

        int rows = lvlManager.LevelData.RowCount;
        int cols = lvlManager.LevelData.ColumnCount;

        for (int col = 0; col < cols; col++)
        {
            ColorObject firstObj = grid[0, col];
            if (firstObj == null)
                continue;

            if (firstObj.ColorType != Color)
                continue;

            selectedObjects.Add(firstObj);

            for (int row = 1; row < rows; row++)
            {
                ColorObject nextObj = grid[row, col];
                if (nextObj == null)
                    break;

                if (nextObj.ColorType != Color)
                    break;

                selectedObjects.Add(nextObj);
            }
        }
    }

    private void ProcessCombos(LevelManager lm, List<ColorObject> selected)
    {
        List<SlideCombo> combos = lm.LevelData.Combos;
        if (combos == null || combos.Count == 0)
            return;

        byte bestPriority = 0;
        SlideCombo bestCombo = null;
        
        foreach (SlideCombo combo in combos)
        {
            if (combo == null)
                continue;

            if (!combo.IsCanApply(lm, selected))
                continue;

            if (bestCombo == null || combo.Priority > bestPriority)
            {
                bestCombo = combo;
                bestPriority = combo.Priority;
            }
        }

        bestCombo?.Apply(lm, selected);
    }
}
