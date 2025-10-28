using System.Collections.Generic;

using MEC;

using UnityEngine;

public class ColorObjectsManager : MonoBehaviour
{
    public static ColorObjectsManager Instance { get; private set; }

    [field: SerializeField] public Transform StartPoint { get; private set; }
    [field: SerializeField] public int Cols { get; private set; } = 9;
    [field: SerializeField] public int Rows { get; private set; } = 5;
    [SerializeField] private float xPadding = 1, zPadding = 1;
    [SerializeField] private List<GameObject> cubePrefabs;

    public GameObject[,] ColorObjects { get; set; }

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
    }

    private void Start()
    {
        Timing.CallDelayed(3f, Generate);
    }

    private void Generate()
    {
        ColorObjects = new GameObject[Rows, Cols];

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                GameObject cube = PoolManager.Instance.SpawnFromPool(cubePrefabs.GetRandomValue(), FindPosition(row, col), Quaternion.identity);
                ColorObjects[row, col] = cube;
                if (cube.TryGetComponent(out ColorObject colorObject))
                {
                    colorObject.RowIndex = row;
                    colorObject.ColumnIndex = col;
                }
            }
        }
    }

    public Vector3 FindPosition (int row, int col)
    {
        return StartPoint.position + new Vector3(col * xPadding, 0, row * zPadding);
    }
}
