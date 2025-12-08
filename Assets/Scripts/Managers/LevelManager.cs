using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

using MEC;

using UnityEngine;

using static Assets.PublicEnums;

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

    private const float defaultBaseChance = 1f;
    private const float objectiveBaseChance = 0.4f;
    private const string slidesParentTag = "SlidesParent";
    private const string colorObjectSpawnPointTag = "SpawnPoint";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        if (collected == null || collected.Count == 0)
            goto check;

        List<ColorObject> localCollected = new(collected);

        foreach (ColorObject obj in localCollected)
        {
            ScoreManager.Instance.Score++;

            if (CollectionObjectives.ContainsKey(obj.ColorType))
                ScoreManager.Instance.AddObjective(obj.ColorType);
        }

        HashSet<int> affectedColumns = new();

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        foreach (ColorObject obj in localCollected)
        {
            int row = obj.RowIndex;
            int col = obj.ColumnIndex;

            if (row >= 0 && row < rows && col >= 0 && col < cols)
            {
                if (ReferenceEquals(ColorObjects[row, col], obj))
                {
                    ColorObjects[row, col] = null;
                    affectedColumns.Add(col);
                }
            }

            obj.DetachFromGrid();
        }

        foreach (int col in affectedColumns)
        {
            RebuildColumn(col);
        }

        Timing.CallDelayed(3f, () =>
        {
            foreach (ColorObject obj in localCollected)
            {
                obj.ReturnToPool();
            }
        });

        check:
        GameManager.Instance.CheckGame(null, null);
    }

    private void ObjectiveExpired(ColorObject expiredObject)
    {
        int row = expiredObject.RowIndex;
        int col = expiredObject.ColumnIndex;

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        if (row < 0 || row >= rows || col < 0 || col >= cols)
        {
            expiredObject.ReturnToPool();
            return;
        }

        ColorObject current = ColorObjects[row, col];

        if (!ReferenceEquals(current, expiredObject))
        {
            expiredObject.ReturnToPool();
            return;
        }

        ColorObjects[row, col] = null;
        FillEmptyCell(row, col);
        expiredObject.ReturnToPool();

        //if (!GameManager.Instance.GameEnded)
            //InputControllerManager.Instance.IsInputEnabled = true;
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

        foreach (GameObject prefab in LevelData.ColorObjectPrefabs)
        {
            if (prefab == null)
                continue;

            if (!prefab.TryGetComponent(out ColorObject co))
                continue;

            ColorType color = co.ColorType;

            if (!prefabsByColor.ContainsKey(color))
                prefabsByColor[color] = new List<GameObject>();

            prefabsByColor[color].Add(prefab);
        }
    }

    private void BuildBaseSpawnWeights()
    {
        baseSpawnWeights.Clear();

        int largestTarget = CollectionObjectives.Count > 0 ? CollectionObjectives.Max(x => x.Value) : 0;

        foreach (KeyValuePair<ColorType, List<GameObject>> kvp in prefabsByColor)
        {
            ColorType color = kvp.Key;
            float weight = defaultBaseChance;

            bool isObj = CollectionObjectives.ContainsKey(color);

            if (isObj && largestTarget > 0)
            {
                int target = CollectionObjectives[color];

                float ratio = Mathf.Clamp((float)target / largestTarget, 0.1f, 1f);

                weight = objectiveBaseChance * ratio;
                weight *= Mathf.Clamp(ObjectiveChances[color], 0.1f, 3f);
                weight = Mathf.Min(weight, defaultBaseChance * 0.9f);
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

        for (int row = 1; row < rows; row++)
        {
            GenerateRow(row);
        }
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

        for (int row = 0; row < rowMax; row++)// mevcutları sırayla topla
        {
            ColorObject colorobject = ColorObjects[row, currentCol];

            if (colorobject == null)
                continue;

            existing.Enqueue(colorobject);
            ColorObjects[row, currentCol] = null;
        }

        int existingCount = existing.Count;

        Sequence colSlide = DOTween.Sequence();

        for (int row = 0; row < existingCount; row++)// yukarıdan alta doğru sıkıştır
        {
            ColorObject cube = existing.Dequeue();

            cube.RowIndex = row;
            cube.ColumnIndex = currentCol;
            ColorObjects[row, currentCol] = cube;

            Vector3 targetPos = FindGridPosition(row, currentCol);
            colSlide.Join(cube.transform.DOMove(targetPos, 0.35f));
        }

        colSlide.Play();

        for (int row = 0; row < rowMax; row++)// boş slotları yeni spawn ile doldur
        {
            if (ColorObjects[row, currentCol] != null)
                continue;

            FillEmptyCell(row, currentCol);
        }

        //if (!GameManager.Instance.GameEnded)
            //InputControllerManager.Instance.IsInputEnabled = true;
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

        if (cube == null)
            return;

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
            Debug.LogWarning("Warning prefab for " + color + " cant find!");
            return null;
        }

        if (ColorObjects[row, col] != null)
        {
            Debug.LogError("Spawn error: null olmayan satıra spawn denemesi.");
            return null;
        }

        List<GameObject> list = prefabsByColor[color];
        GameObject prefab = list.GetRandomValue();

        GameObject cube = PoolManager.Instance.SpawnFromPool(prefab, FindGridPosition(row, col), Quaternion.identity);

        if (!cube.TryGetComponent(out ColorObject colorObject))
            return cube;

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
        Dictionary<ColorType, float> defaultWeights = baseSpawnWeights;
        IEnumerable<ColorType> pool = forceObjective && CollectionObjectives.Count > 0? CollectionObjectives.Keys : defaultWeights.Keys;

        float total = 0f;
        foreach (ColorType type in pool)
        {
            total += defaultWeights[type];
        }

        float roll = Random.Range(0f, total);
        float running = 0f;

        foreach (ColorType col in pool)
        {
            running += defaultWeights[col];
            if (roll <= running)
                return col;
        }

        foreach (ColorType col in pool)
            return col;

        return 0;
    }

    /// <summary>
    /// Normalde gridde kalanların hepsini poola atması lazım ama belirsiz bilmiyorum.
    /// </summary>
    public void ReturnToPoolAll()
    {
        if (ColorObjects == null)
            return;

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (ColorObjects[row, col] != null)
                {
                    ColorObjects[row, col].ReturnToPool();
                    ColorObjects[row, col] = null;
                }
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
        foreach (KeyValuePair<ColorType, int> objective in CollectionObjectives)
        {
            int target = objective.Value;
            ColorType type = objective.Key;

            int current;
            if (!ScoreManager.Instance.CollectedObjectives.TryGetValue(type, out current))
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
