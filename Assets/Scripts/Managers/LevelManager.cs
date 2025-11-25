using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

using UnityEditor.PackageManager;
using UnityEditor.SearchService;

using UnityEngine;
using UnityEngine.SceneManagement;

using static Assets.PublicEnums;
using static UnityEngine.Rendering.DebugUI.Table;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public LevelDataSO LevelData { get; private set; }

    public List<Slide> Slides { get; set; } = new();
    public ColorObject[,] ColorObjects { get; private set; }

    public Dictionary<ColorType, float> ObjectiveChances { get; private set; }
    public Dictionary<ColorType, int> CollectionObjectives { get; private set; }
    

    [SerializeField] private float xPadding = 2f;
    [SerializeField] private float zPadding = 2f;
    [SerializeField] private float slideSpacing = 3f;


    [SerializeField] private Transform startPoint, slidesParent;
    private readonly Dictionary<ColorType, float> baseSpawnWeights = new();
    private readonly Dictionary<ColorType, List<GameObject>> prefabsByColor = new();

    private const float neutralBaseWeight = 1f;
    private const float objectiveBaseWeight = 0.4f;
    private const string slidesParentTag = "SlidesParent";
    private const string colorObjectSpawnPointTag = "SpawnPoint";

    private void Awake()
    {
        Instance = Instance.SetSingleton(this);
    }

    private void OnEnable()
    {
        EventManager.OnSlideUsed += SlideUsed;
        EventManager.OnObjectiveExpired += ObjectiveExpired;
    }

    private void OnDisable()
    {
        EventManager.OnSlideUsed -= SlideUsed;
        EventManager.OnObjectiveExpired -= ObjectiveExpired;
    }

    private void SlideUsed(Slide slide, List<ColorObject> collected)
    {
        HashSet<int> affectedColumns = new();
        foreach (ColorObject obj in collected)
        {
            ColorObjects[obj.RowIndex, obj.ColumnIndex] = null;

            affectedColumns.Add(obj.ColumnIndex);
            obj.ColumnIndex = obj.RowIndex = -1;
        }

        foreach (int col in affectedColumns)
        {
            RebuildColumn(col);
        }
    }

    private void ObjectiveExpired(ColorObject expiredObject)
    {
        ColorObjects[expiredObject.RowIndex, expiredObject.ColumnIndex] = null;
        expiredObject.gameObject.ReturnToPool();

        RebuildColumn(expiredObject.ColumnIndex);
    }

    public void Initialize(LevelDataSO levelData)
    {
        LevelData = levelData;

        InputControllerManager.Instance.InputAttempt = levelData.ClickAttempts;

        SetupObjectives();
        BuildPrefabCache();
        BuildBaseSpawnWeights();

        SpawnSlides();
        GenerateInitialGrid();
    }

    private void SetupObjectives()
    {
        ObjectiveChances = new Dictionary<ColorType, float>();
        CollectionObjectives = new Dictionary<ColorType, int>();

        foreach (ObjectiveData obj in LevelData.Objectives)
        {
            CollectionObjectives[obj.Color] = obj.TargetAmount;
            ObjectiveChances[obj.Color] = obj.SpawnChanceMultiplier;
        }
    }

    private void BuildPrefabCache()
    {
        prefabsByColor.Clear();

        foreach (var prefab in LevelData.ColorObjectPrefabs)
        {
            if (prefab == null) continue;

            if (!prefab.TryGetComponent(out ColorObject co)) continue;

            ColorType color = co.ColorType;

            if (!prefabsByColor.ContainsKey(color))
                prefabsByColor[color] = new List<GameObject>();

            prefabsByColor[color].Add(prefab);
        }
    }

    private void BuildBaseSpawnWeights()
    {
        baseSpawnWeights.Clear();

        int largestTarget = CollectionObjectives.Count > 0? CollectionObjectives.Max(x => x.Value): 0;

        foreach (KeyValuePair<ColorType, List<GameObject>> kvp in prefabsByColor)
        {
            ColorType color = kvp.Key;
            float weight = neutralBaseWeight;

            bool isObj = CollectionObjectives.ContainsKey(color);

            if (isObj && largestTarget > 0)
            {
                int target = CollectionObjectives[color];

                float ratio = Mathf.Clamp((float)target / largestTarget, 0.1f, 1f);

                weight = objectiveBaseWeight * ratio;
                weight *= Mathf.Clamp(ObjectiveChances[color], 0.1f, 3f);
                weight = Mathf.Min(weight, neutralBaseWeight * 0.9f);
            }

            baseSpawnWeights[color] = weight;
        }
    }

    private void GenerateInitialGrid()
    {
        if (startPoint == null)
            startPoint = GameObject.FindWithTag(colorObjectSpawnPointTag).transform;

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        ColorObjects = new ColorObject[rows, cols];

        bool hasObjectives = CollectionObjectives.Count > 0;
        bool ensured = false;
        for (int col = 0; col < cols; col++)
        {
            bool forceObjective = hasObjectives && !ensured && (col == cols - 1);

            ColorType color = ChooseColor(forceObjective);

            if (hasObjectives && CollectionObjectives.ContainsKey(color))
                ensured = true;

            SpawnCube(0, col, color);
        }

        int row;
        for (row = 1; row < rows; row++)
            GenerateRow(row);
    }

    private void GenerateRow(int row)
    {
        for (int col = 0; col < LevelData.ColumnCount; col++)
        {
            ColorType color = ChooseColor(false);
            SpawnCube(row, col, color);
        }
    }

    private void SpawnSlides()
    {
        if (LevelData.SlidesPrefabs == null || LevelData.SlidesPrefabs.Count == 0)
            return;

        if (slidesParent == null)
            slidesParent = GameObject.FindWithTag(slidesParentTag).transform;

        int count = LevelData.SlidesPrefabs.Count;
        float spacing = slideSpacing;

        float startX = slidesParent.position.x;
        float y = slidesParent.position.y;
        float z = slidesParent.position.z;

        float half = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = LevelData.SlidesPrefabs[i];
            if (prefab == null)
                continue;

            float xOffset = (i - half) * spacing;

            Vector3 pos = new(startX + xOffset, y, z);
            Instantiate(prefab, pos, Quaternion.identity, slidesParent);
        }
    }

    /// <summary>
    /// Columnu düzeltir boş yerleri kaydırır en son boş kalanları yeniden doldurur.
    /// </summary>
    /// <param name="currentCol">Düzenlenecek sütun.</param>
    private void RebuildColumn(int currentCol)
    {
        int rowMax = LevelData.RowCount;
        Queue<ColorObject> existing = new(rowMax);

        for (int row = 0; row < rowMax; row++)// olanlari al
        {
            ColorObject colorobject = ColorObjects[row, currentCol];

            if (colorobject == null)
                continue;

            existing.Enqueue(colorobject);
            ColorObjects[row, currentCol] = null;
        }

        int existingCount = existing.Count;
        for (int row = 0; row < existingCount; row++)// baştan sirayla tekrar doldur (aşşa kaydır)
        {
            ColorObject cube = existing.Dequeue();

            cube.RowIndex = row;
            cube.ColumnIndex = currentCol;
            ColorObjects[row, currentCol] = cube;

            Vector3 targetPos = FindGridPosition(row, currentCol);
            cube.transform.DOMove(targetPos, 0.35f);
        }

        for (int row = 0; row < rowMax; row++) // boşları doldur
        {
            if (ColorObjects[row, currentCol] != null)
                continue;

            FillEmptyCell(row, currentCol);
        }

        InputControllerManager.Instance.IsInputEnabled = true;
    }

    /// <summary>
    /// Bir gride yeni colorobject spawn eder, eğer ki boşsa.
    /// </summary>
    /// <param name="row">Hedef grid satırı.</param>
    /// <param name="col">Hedef grid sütünü.</param>
    private void FillEmptyCell(int row, int col)
    {
        if (ColorObjects[row, col] != null)
            return;

        ColorType color = ChooseColor(false);
        GameObject cube = SpawnCube(row, col, color);

        cube.transform.localScale = Vector3.zero;
        cube.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Color obje spawn etme için yardımcı method.
    /// </summary>
    /// <param name="row">Hedef grid satırı.</param>
    /// <param name="col">Hedef grid sütünü.</param>
    /// <param name="color">Prefabın spawn olacağı renk tipi.</param>
    /// <returns> spawn olan küpün <see cref="GameObject"/> ini döndürür.</returns>
    private GameObject SpawnCube(int row, int col, ColorType color)
    {
        if (!prefabsByColor.ContainsKey(color))
        {
            Debug.LogWarning($"Warning prefab for {color} cant find!");
            return null;
        }

        if (ColorObjects[row, col] != null)
        {
            Debug.LogError("Yaw yeter ne allaka nulll olmayan satıra spawn");
            return null;
        }

        GameObject prefab = prefabsByColor[color].GetRandomValue();
        GameObject cube = PoolManager.Instance.SpawnFromPool(prefab, FindGridPosition(row, col), Quaternion.identity);

        if (!cube.TryGetComponent(out ColorObject colorObject))
            return null;

        colorObject.RowIndex = row;
        colorObject.ColumnIndex = col;
        ColorObjects[row, col] = colorObject;

        return cube;
    }

    /// <summary>
    /// Renk seçme random spaw olacak rengi seçer şansa bağlı.
    /// </summary>
    /// <param name="forceObjective">Zorla hedef olacak bir renk seçilecek mi.</param>
    /// <returns> seçilen renk tipi <see cref="ColorType"/> ni döndürür.</returns>
    private ColorType ChooseColor(bool forceObjective)
    {
        Dictionary<ColorType, float> w = baseSpawnWeights;

        IEnumerable<ColorType> pool = forceObjective && CollectionObjectives.Count > 0? CollectionObjectives.Keys : w.Keys;

        float total = pool.Sum(c => w[c]);

        float roll = Random.Range(0, total);
        float running = 0;

        foreach (ColorType col in pool)
        {
            running += w[col];
            if (roll <= running)
                return col;
        }

        return pool.First();
    }

    private void UpdateColorObjectGrid(ColorObject colorObj)
    {
        Vector3 targetPos = FindGridPosition(colorObj.RowIndex, colorObj.ColumnIndex);
        colorObj.transform.DOMove(targetPos, 0.35f);
    }

    /*private void SceneChanged(Scene oldScene, Scene newScene)
    {
        //bozuk
        Debug.LogWarning("[LevelManager] Scene Changed");
        if (oldScene.name == GameManager.levelSceneName)
            ReturnToPoolAll();
    }*/

    /// <summary>
    /// Normalde gridde kalanların hepsini poola atması lazım ama belirsiz bilmiyorum.
    /// </summary>
    public void ReturnToPoolAll()
    {
        if (ColorObjects == null) return;

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                ColorObjects[row, col].gameObject.ReturnToPool();
            }
        }

        ColorObjects = null;
    }

    /// <summary>
    /// Tüm hedefler bitti mi diye kontrol yapar.
    /// </summary>
    /// <returns>Eğer hepsi topllandıysa true döner; öbür türlü, false.</returns>
    public bool AreAllObjectivesCompleted()
    {
        foreach (KeyValuePair<ColorType,int> objective in CollectionObjectives)
        {
            int target = objective.Value;
            ColorType type = objective.Key;

            if (!ScoreManager.Instance.CollectedObjectives.TryGetValue(type, out int current))
                return false;

            if (current < target)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gridin pozisyonunu bulmak için yardımcı method.
    /// </summary>
    /// <param name="row">Hedef satır.</param>
    /// <param name="col">Hedef sütün.</param>
    /// <returns> seçilen gridin pozisyonunu <see cref="Vector3"/> olarak döndürür.</returns>
    public Vector3 FindGridPosition(int row, int col)
    {
        int cols = LevelData.ColumnCount;
        float half = (cols - 1) * 0.5f;

        float x = (col - half) * xPadding;
        float z = row * zPadding;

        return startPoint.position + new Vector3(x, 0f, z);
    }
}
