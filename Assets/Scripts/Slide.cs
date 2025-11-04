using System.Collections.Generic;
using System.Threading.Tasks;

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

    private bool isLocked = false;
    private Vector3 forceDirection;
    private const float slideDuration = 0.5f;
    private readonly List<ColorObject> selectedObjects = new(5);

    private void Awake() => forceDirection = (transform.position - slidePoint.position).normalized;

    private void OnEnable()
    {
        Timing.CallDelayed(0.1f, ()=> ColorObjectsManager.Instance.Slides.Add(this));
        // geçiçi delay ColorObjectsManager.Instance.Slides.Add(this);
        EventManager.OnSlideUsed += OnSlideUsed;
    }

    private void OnDisable()
    {
        ColorObjectsManager.Instance.Slides.Remove(this);
        EventManager.OnSlideUsed -= OnSlideUsed;
    }

    private void OnSlideUsed(Slide slide)
    {
        if (slide == this)
        {
            isLocked = true;
            avaliableLight.color = UnityEngine.Color.red;
        }
        else
        {  
            if (!isLocked)
                return;

            avaliableLight.color = UnityEngine.Color.green;
            isLocked = false;
        }
    }

    public async void OnClicked()
    {
        if (isLocked)
            return;

        InputControllerManager.Instance.IsInputEnabled = false;

        ColorObjectsManager colorObjectsManager = ColorObjectsManager.Instance;

        selectedObjects.Clear();

        if (colorObjectsManager.ColorObjects == null)
            return;

        int cols = colorObjectsManager.ColorObjects.GetLength(1);
        for (int c = 0; c < cols; c++)
        {
            GameObject Object = colorObjectsManager.ColorObjects[0, c];
            if (Object == null)
                continue;

            if (!Object.TryGetComponent(out ColorObject colorObject))
                continue;

            if (colorObject.ColorType != Color)
                continue;

            selectedObjects.Add(colorObject);
        }

        if (selectedObjects.Count < MinObjectCount)
        {
            await RejectSelecteds(selectedObjects);

            InputControllerManager.Instance.IsInputEnabled = true;
            Debug.Log("Not enough objects to slide.");
            return;
        }

        await CollectSelected(selectedObjects, colorObjectsManager);

        EventManager.SlideUsed(this);

        if (!HasAnyAvailableSlide(colorObjectsManager))
        {
            Timing.CallDelayed(1.5f, ()=> EventManager.GameEnded());
            return;
        }

        InputControllerManager.Instance.IsInputEnabled = true;
    }

    private async Task CollectSelected(List<ColorObject> selectedObjects, ColorObjectsManager colorObjectsManager)
    {
        foreach (ColorObject obj in selectedObjects)
        {
            await obj.transform.DOMove(slidePoint.position, slideDuration).AsyncWaitForCompletion();
            obj.GetComponent<Rigidbody>().AddForce(forceDirection * 40, ForceMode.Impulse);

            ScoreManager.Instance.Score += 1;
            SoundManager.Instance.PlayGlobalSound(obj.Collectsound);
            Timing.CallDelayed(3f, () => obj.gameObject.ReturnToPool());

            colorObjectsManager.ColorObjects[obj.RowIndex, obj.ColumnIndex] = null;

            Sequence mainSequance = DOTween.Sequence();
            for (int row = obj.RowIndex + 1; row < colorObjectsManager.Rows; row++)
            {
                GameObject objectWillMove = colorObjectsManager.ColorObjects[row, obj.ColumnIndex];

                if (objectWillMove == null)
                    continue;

                colorObjectsManager.ColorObjects[row, obj.ColumnIndex] = null;

                colorObjectsManager.ColorObjects[row - 1, obj.ColumnIndex] = objectWillMove;
                objectWillMove.GetComponent<ColorObject>().RowIndex = row - 1;

                Vector3 newPos = colorObjectsManager.FindPosition(row - 1, obj.ColumnIndex);

                mainSequance.Join(objectWillMove.transform.DOMove(newPos, 0.5f));
            }

            await mainSequance.AsyncWaitForCompletion();
        }
    }

    private async Task RejectSelecteds(List<ColorObject> selectedObjects)
    {
        Sequence mainSequence = DOTween.Sequence();
        foreach (ColorObject obj in selectedObjects)
        {
            Transform t = obj.transform;
            mainSequence.Join(DOTween.Sequence()
                .Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad))
                .Append(t.DORotate(new Vector3(0, 0, -30), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad))
                .Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad)));
        }

        await mainSequence.AsyncWaitForCompletion();
    }

    private bool HasAnyAvailableSlide(ColorObjectsManager colorObjectsManager)
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
    }
}
