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
            // yield return RejectSelecteds(selectedObjects).WaitForCompletion();
        }
        else
            yield return CollectSelecteds(selectedObjects, lvlManager).WaitForCompletion();

        EventManager.SlideUsed(this, selectedObjects);
    }

    private Sequence CollectSelecteds(List<ColorObject> selected, LevelManager lvlManager)
    {
        Sequence finalSeq = DOTween.Sequence();

        foreach (ColorObject obj in selectedObjects)
        {
            Rigidbody rb = obj.Rb;
            finalSeq.Join(obj.transform.DOMove(slidePoint.position, slideDuration).OnComplete(() =>
            {
                rb.velocity = rb.angularVelocity = Vector3.zero;
                rb.AddForce(forceDirection * 35, ForceMode.Impulse);
            }));

            if (lvlManager.CollectionObjectives.ContainsKey(Color))
                ScoreManager.Instance.AddObjective(Color);

            ScoreManager.Instance.Score++;
            SoundManager.Instance.PlayGlobalSound(obj.Collectsound);
        }

        return finalSeq;
    }

    private Sequence RejectSelecteds(List<ColorObject> selectedObjects)
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

        if (lvlManager.LevelData.CombosActive && selectedObjects.Count >= lvlManager.LevelData.minComboCount)
        {
            selectedObjects.Clear();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    ColorObject obj = grid[r, c];
                    if (obj == null)
                        continue;

                    if (obj.ColorType != Color)
                        continue;

                    selectedObjects.Add(obj);
                }
            }
        }
    }

    /* Eski
    private bool HasAnyAvailableSlide(LevelManager colorObjectsManager)
    {
        if (colorObjectsManager.ColorObjects == null)
            return false;

        int cols = colorObjectsManager.ColorObjects.GetLength(1);

        foreach (Slide slide in colorObjectsManager.Slides)
        {
            int matchCount = 0;

            for (int c = 0; c < cols; c++)
            {
                GameObject obj = colorObjectsManager.ColorObjects[0, c];
                if (obj == null)
                    continue;

                if (!obj.TryGetComponent(out ColorObject colorObj))
                    continue;

                if (colorObj.ColorType != slide.Color)
                    continue;

                matchCount++;
            }

            if (matchCount >= slide.MinObjectCount)
                return true;
        }

        return false;
    }*/
}
