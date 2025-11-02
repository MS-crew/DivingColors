using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using static Assets.PublicEnums;

public class ColorObjectsManager : MonoBehaviour
{
    public static ColorObjectsManager Instance { get; private set; }

    public List<Slide> Slides { get; set; } = new();
    [field: SerializeField] public Transform StartPoint { get; private set; }
    [field: SerializeField] public int Cols { get; private set; } = 9;
    [field: SerializeField] public int Rows { get; private set; } = 5;

    [SerializeField] private float xPadding = 1, zPadding = 1;
    [SerializeField] private List<GameObject> cubePrefabs;

    public GameObject[,] ColorObjects { get; set; }

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnEnable() => EventManager.OnSlideUsed += FillEmptys;

    private void OnDisable() => EventManager.OnSlideUsed -= FillEmptys;

    private void FillEmptys(Slide _)
    {
        int lastRowIndex = Rows - 1;
        GenerateRow(lastRowIndex);
    }

    public void Generate()
    {
        if (Slides.Count <= 0 || cubePrefabs.Count <= 0)
            return;

        foreach (GameObject cube in cubePrefabs)
        {
            if (Slides.Count(x => x.Color == cube.GetComponent<ColorObject>().ColorType) <= 0)
                cubePrefabs.Remove(cube);
        }

        if (StartPoint == null)
            StartPoint = GameObject.FindWithTag("SpawnPoint").transform;

        Slide chosenSlide = Slides.GetRandomValue();
        ColorType guaranteedColor = chosenSlide.Color;
        int neededCount = chosenSlide.MinObjectCount;

        List<int> guaranteedCols = new();
        while (guaranteedCols.Count < neededCount)
        {
            int randomCol = Random.Range(0, Cols);
            if (!guaranteedCols.Contains(randomCol))
                guaranteedCols.Add(randomCol);
        }

        ColorObjects = new GameObject[Rows, Cols];

        #region Garanteed First Row
        int row = 0;
        for (int col = 0; col < Cols; col++)
        {
            GameObject cubePrefab;
            if (row == 0 && guaranteedCols.Contains(col))
                cubePrefab = cubePrefabs.Find(p => p.GetComponent<ColorObject>().ColorType == guaranteedColor);
            else
                cubePrefab = cubePrefabs.GetRandomValue();

            GameObject cube = PoolManager.Instance.SpawnFromPool(cubePrefab, FindPosition(row, col), Quaternion.identity);

            ColorObjects[row, col] = cube;

            if (cube.TryGetComponent(out ColorObject colorObject))
            {
                colorObject.RowIndex = row;
                colorObject.ColumnIndex = col;
            }
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD ||  UNITY_STANDALONE
        Debug.Log($"Guaranteed color: {guaranteedColor} | Count: {neededCount}");
        #endif

        #endregion

        for (row = 1; row < Rows; row++)
        {
            GenerateRow(row);
        }
    }

    private void GenerateRow(int rowIndex = 0)
    {
        if (rowIndex <= 0)
            return;

        Dictionary<ColorType, int> colorCounts = new();
        for (int col = 0; col < Cols; col++)
        {
            GameObject prevCube = ColorObjects[rowIndex - 1, col];
            if (prevCube == null)
                continue;

            if (prevCube.TryGetComponent(out ColorObject prevColor))
            {
                if (!colorCounts.ContainsKey(prevColor.ColorType))
                    colorCounts[prevColor.ColorType] = 0;

                colorCounts[prevColor.ColorType]++;
            }
        }

        Dictionary<ColorType, float> spawnChancesWeights = new();
        foreach (Slide slide in Slides)
        {
            ColorType color = slide.Color;
            int count = colorCounts.ContainsKey(color) ? colorCounts[color] : 0;

            float weight = 1f;
            if (count >= slide.MinObjectCount)
                weight = 0.5f;
            else if (count == slide.MinObjectCount - 1)
                weight = 1.25f;
            else if (count == 0)
                weight = 1f;

            spawnChancesWeights[color] = weight;
        }

        float totalWeight = 0;
        foreach (KeyValuePair<ColorType, float> kvp in spawnChancesWeights)
            totalWeight += kvp.Value;


        for (int col = 0; col < Cols; col++)
        {
            ColorType chosenColor;

            float roll = Random.Range(0f, totalWeight);
            float runningWeight = 0;

            chosenColor = Slides[0].Color;

            foreach (KeyValuePair<ColorType, float> kvp in spawnChancesWeights)
            {
                runningWeight += kvp.Value;
                if (roll <= runningWeight)
                {
                    chosenColor = kvp.Key;
                    break;
                }
            }

            GameObject prefab = cubePrefabs.Find(p => p.GetComponent<ColorObject>().ColorType == chosenColor);

            GameObject cube = PoolManager.Instance.SpawnFromPool(prefab, FindPosition(rowIndex, col), Quaternion.identity);

            ColorObjects[rowIndex, col] = cube;

            if (cube.TryGetComponent(out ColorObject colorObject))
            {
                colorObject.RowIndex = rowIndex;
                colorObject.ColumnIndex = col;
            }
        }

        Debug.Log($"Generated row {rowIndex} with adaptive weights.");
    }


    public Vector3 FindPosition(int row, int col) => StartPoint.position + new Vector3(col * xPadding, 0, row * zPadding);
}
