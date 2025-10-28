using MEC;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Slide : MonoBehaviour
{
    [SerializeField] private int minObjectCount; 
    [SerializeField] private Light avaliableLight;
    [SerializeField] private Transform slidePoint;
    [SerializeField] private GameManager.ColorType color;

    private bool isLocked = false;
    private Vector3 forceDirection;
    private const float slideDuration = 1;
    private readonly List<ColorObject> selectedObjects = new(4);

    private void Awake() => forceDirection = (transform.position - slidePoint.position).normalized;

    private void OnEnable()
    {
        isLocked = false;
        EventManager.OnSlideUsed += OnSlideUsed;
    }

    private void OnDisable()
    {
        EventManager.OnSlideUsed -= OnSlideUsed;
    }

    private void OnSlideUsed(Slide slide)
    {
        if (slide == this)
        {
            isLocked = true;
            avaliableLight.color = Color.red;
        }
        else
        {
            avaliableLight.color = Color.green;
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

        int cols = colorObjectsManager.ColorObjects.GetLength(1);
        for (int c = 0; c < cols; c++)
        {
            GameObject Object = colorObjectsManager.ColorObjects[0, c];
            if (Object == null)
                continue;

            if (!Object.TryGetComponent(out ColorObject colorObject))
                continue;

            if (colorObject.ColorType != color)
                continue;

            selectedObjects.Add(colorObject);
        }

        if (selectedObjects.Count < minObjectCount)
        {
            foreach (ColorObject obj in selectedObjects)
            {
                Transform t = obj.transform;

                DOTween.Sequence().Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad))
                    .Append(t.DORotate(new Vector3(0, 0, -30), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad))
                    .Append(t.DORotate(new Vector3(0, 0, 15), 0.25f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
            }

            InputControllerManager.Instance.IsInputEnabled = true;
            Debug.Log("Not enough objects to slide.");
            return;
        }

        foreach (ColorObject obj in selectedObjects)
        {
            await obj.transform.DOMove(slidePoint.position, slideDuration).AsyncWaitForCompletion();
            obj.GetComponent<Rigidbody>().AddForce(forceDirection * 40, ForceMode.Impulse);

            Timing.CallDelayed(3f, () => obj.gameObject.ReturnToPool());

            colorObjectsManager.ColorObjects[obj.RowIndex, obj.ColumnIndex] = null;

            for (int row = obj.RowIndex + 1; row < colorObjectsManager.Rows; row++)
            {
                GameObject objectWillMove = colorObjectsManager.ColorObjects[row, obj.ColumnIndex];

                if (objectWillMove == null)
                    continue;

                colorObjectsManager.ColorObjects[row, obj.ColumnIndex] = null;

                colorObjectsManager.ColorObjects[row - 1, obj.ColumnIndex] = objectWillMove;
                objectWillMove.GetComponent<ColorObject>().RowIndex = row - 1;

                Vector3 newPos = colorObjectsManager.FindPosition(row - 1, obj.ColumnIndex);
                objectWillMove.transform.DOMove(newPos, 0.5f);
            }
        }

        EventManager.SlideUsed(this);
        InputControllerManager.Instance.IsInputEnabled = true;
    }
}
