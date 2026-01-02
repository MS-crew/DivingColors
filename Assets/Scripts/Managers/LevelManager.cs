using System;
using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

using UnityEngine;

using static Assets.PublicEnums;

using Random = UnityEngine.Random;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public LevelDataSO LevelData { get; private set; }

    public List<Slide> Slides { get; set; } = new();
    public ColorObject[,] ColorObjects { get; private set; }

    public Dictionary<ColorType, Slide> SlideCache { get; private set; } = new();
    public Dictionary<ColorType, float> ObjectiveChances { get; private set; } = new();
    public Dictionary<ColorType, int> CollectionObjectives { get; private set; } = new();

    private readonly Dictionary<string, int> specialObjectCounts = new();

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

    private Queue<Action> gridOperationQueue = new();
    private bool isGridBusy = false;

    public bool IsSystemBusy => isGridBusy || gridOperationQueue.Count > 0;

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        EventManager.OnSlideUsing += QueueSlideOperation;
        EventManager.OnBombExploded += QueueBombOperation;
        EventManager.OnObjectiveExpired += QueueExpiredOperation;
    }

    private void OnDisable()
    {
        EventManager.OnSlideUsing -= QueueSlideOperation;
        EventManager.OnBombExploded -= QueueBombOperation;
        EventManager.OnObjectiveExpired -= QueueExpiredOperation;
    }

    private void ProcessNextOperation()
    {
        if (isGridBusy || gridOperationQueue.Count == 0) return;

        isGridBusy = true;
        Action operation = gridOperationQueue.Dequeue();

        try
        {
            operation?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Grid Action Hatası: {e.Message}");
            OnOperationComplete();
        }
    }

    private void OnOperationComplete()
    {
        isGridBusy = false;
        ProcessNextOperation();
    }

    private void QueueSlideOperation(Slide slide, List<ColorObject> collected)
    {
        gridOperationQueue.Enqueue(() => ExecuteSlide(slide, collected));
        ProcessNextOperation();
    }

    private void QueueBombOperation(BombObject bomb)
    {
        gridOperationQueue.Enqueue(() => ExecuteBomb(bomb));
        ProcessNextOperation();
    }

    private void QueueExpiredOperation(ColorObject expiredObject)
    {
        gridOperationQueue.Enqueue(() => ExecuteExpired(expiredObject));
        ProcessNextOperation();
    }

    private void ExecuteSlide(Slide slide, List<ColorObject> collected)
    {
        if (collected == null || collected.Count == 0)
        {
            EventManager.SlideUsed(null, null);
            OnOperationComplete();
            return;
        }

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

            UnregisterSpecialObject(obj);
            obj.OnCollected();
        }

        Sequence masterSequence = DOTween.Sequence();

        foreach (int col in affectedColumns)
        {
            masterSequence.Join(RebuildColumn(col));
        }

        masterSequence.OnComplete(() =>
        {
            foreach (ColorObject obj in localCollected)
                obj.ReturnToPool();

            EventManager.SlideUsed(null, null);
            GameManager.Instance.CheckGame(null, null);

            OnOperationComplete();
        });
    }

    private void ExecuteBomb(BombObject bomb)
    {
        if (bomb == null || !bomb.gameObject.activeInHierarchy)
        {
            OnOperationComplete();
            return;
        }

        int centerRow = bomb.RowIndex;
        int centerCol = bomb.ColumnIndex;
        int radius = bomb.ExplodeRadius;
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        ColorObject current = ColorObjects[centerRow, centerCol];
        if (centerRow < 0 || centerRow >= rows || centerCol < 0 || centerCol >= cols || !ReferenceEquals(current, bomb))
        {
            UnregisterSpecialObject(bomb);
            bomb.ReturnToPool();
            OnOperationComplete();
            return;
        }

        HashSet<int> affectedColumns = new();
        int startRow = Mathf.Max(0, centerRow - radius);
        int startCol = Mathf.Max(0, centerCol - radius);
        int endCol = Mathf.Min(cols - 1, centerCol + radius);
        int endRow = Mathf.Min(rows - 1, centerRow + radius);

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                ColorObject target = ColorObjects[r, c];
                if (target != null)
                {
                    ColorObjects[r, c] = null;

                    target.transform.DOKill();

                    UnregisterSpecialObject(target);
                    target.DetachFromGrid();
                    target.ReturnToPool();
                    affectedColumns.Add(c);
                }
            }
        }

        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.Shake(0.7f, 0.4f, true);

        Sequence masterSequence = DOTween.Sequence();

        foreach (int col in affectedColumns)
        {
            masterSequence.Join(RebuildColumn(col));
        }

        masterSequence.OnComplete(OnOperationComplete);
    }

    private void ExecuteExpired(ColorObject expiredObject)
    {
        int row = expiredObject.RowIndex;
        int col = expiredObject.ColumnIndex;
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        ColorObject current = ColorObjects[row, col];
        if (row < 0 || row >= rows || col < 0 || col >= cols || !ReferenceEquals(current, expiredObject))
        {
            UnregisterSpecialObject(expiredObject);
            expiredObject.ReturnToPool();
            OnOperationComplete();
            return;
        }

        ColorObjects[row, col] = null;
        UnregisterSpecialObject(expiredObject);
        expiredObject.ReturnToPool();

        Tween fillTween = FillEmptyCell(row, col);

        if (fillTween != null)
            DOTween.Sequence().Append(fillTween).OnComplete(OnOperationComplete);
        else
            OnOperationComplete();
    }

    private Sequence RebuildColumn(int currentCol)
    {
        int rowMax = LevelData.RowCount;
        Queue<ColorObject> existing = new(rowMax);

        for (int row = 0; row < rowMax; row++)
        {
            ColorObject colorobject = ColorObjects[row, currentCol];
            if (colorobject == null)
                continue;

            existing.Enqueue(colorobject);
            ColorObjects[row, currentCol] = null;
        }

        int existingCount = existing.Count;
        Sequence colSlide = DOTween.Sequence();

        for (int row = 0; row < existingCount; row++)
        {
            ColorObject cube = existing.Dequeue();

            cube.RowIndex = row;
            cube.ColumnIndex = currentCol;
            ColorObjects[row, currentCol] = cube;

            Vector3 targetPos = FindGridPosition(row, currentCol);

            cube.transform.DOKill();
            colSlide.Join(cube.transform.DOMove(targetPos, 0.35f));
        }

        colSlide.Play();

        for (int row = 0; row < rowMax; row++)
        {
            if (ColorObjects[row, currentCol] != null)
                continue;

            Tween t = FillEmptyCell(row, currentCol);
            if (t != null) 
                colSlide.Join(t);
        }

        return colSlide;
    }

    private Tween FillEmptyCell(int row, int col)
    {
        if (ColorObjects[row, col] != null)
            return null;

        foreach (SpecialObject special in LevelData.SpecialObjects)
        {
            if (special.Prefab == null)
                continue;

            string tagKey = special.Prefab.tag;
            if (string.IsNullOrEmpty(tagKey) || tagKey == "Untagged")
                continue;

            specialObjectCounts.TryGetValue(tagKey, out int currentCount);

            if (currentCount >= special.MaxOnSameTime)
                continue;

            if (Random.value > special.SpawnChance)
                continue;

            GameObject specialObj = SpawnSpecialObject(special, row, col);
            if (specialObj != null)
            {
                specialObj.transform.localScale = Vector3.zero;

                return specialObj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
        }

        ColorType color = ChooseColor(false);
        GameObject cube = SpawnCube(row, col, color);
        if (cube == null)
            return null;

        cube.transform.localScale = Vector3.zero;

        return cube.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void UnregisterSpecialObject(ColorObject obj)
    {
        string tagKey = obj.gameObject.tag;
        if (!string.IsNullOrEmpty(tagKey) && specialObjectCounts.ContainsKey(tagKey))
        {
            specialObjectCounts[tagKey] = Mathf.Max(0, specialObjectCounts[tagKey] - 1);
        }
    }

    private GameObject SpawnSpecialObject(SpecialObject specialData, int row, int col)
    {
        if (specialData.Prefab == null) return null;
        Vector3 spawnPos = FindGridPosition(row, col);
        GameObject obj = PoolManager.Instance.SpawnFromPool(specialData.Prefab, spawnPos, specialData.Prefab.transform.rotation);

        if (obj.TryGetComponent(out ColorObject colorObj))
        {
            colorObj.RowIndex = row;
            colorObj.ColumnIndex = col;
            ColorObjects[row, col] = colorObj;

            string tagKey = obj.tag;
            if (!specialObjectCounts.ContainsKey(tagKey)) specialObjectCounts[tagKey] = 0;
            specialObjectCounts[tagKey]++;
            return obj;
        }

        obj.ReturnToPool();
        return null;
    }

    public void Initialize(LevelDataSO levelData)
    {
        LevelData = levelData;
        InputControllerManager.Instance.InputAttempt = levelData.ClickAttempts;

        specialObjectCounts.Clear();
        gridOperationQueue.Clear();
        isGridBusy = false;

        if (LevelData.SpecialObjects != null)
        {
            foreach (SpecialObject special in LevelData.SpecialObjects)
            {
                if (special.Prefab != null && !string.IsNullOrEmpty(special.Prefab.tag))
                    specialObjectCounts[special.Prefab.tag] = 0;
            }
        }

        SetupObjectives();
        BuildPrefabCache();
        BuildBaseSpawnWeights();
        SpawnSlides();
        GenerateInitialGrid();
    }

    private GameObject SpawnCube(int row, int col, ColorType color)
    {
        if (!prefabsByColor.ContainsKey(color))
            return null;

        if (ColorObjects[row, col] != null)
            return null;

        List<GameObject> list = prefabsByColor[color];
        GameObject prefab = list.GetRandomValue();
        GameObject cube = PoolManager.Instance.SpawnFromPool(prefab, FindGridPosition(row, col), prefab.transform.rotation);

        if (!cube.TryGetComponent(out ColorObject colorObject))
            return cube;

        colorObject.RowIndex = row;
        colorObject.ColumnIndex = col;
        ColorObjects[row, col] = colorObject;

        return cube;
    }

    private ColorType ChooseColor(bool forceObjective)
    {
        Dictionary<ColorType, float> defaultWeights = baseSpawnWeights;
        IEnumerable<ColorType> pool = forceObjective && CollectionObjectives.Count > 0 ? CollectionObjectives.Keys : defaultWeights.Keys;
        List<ColorType> validPool = pool.Where(c => SlideCache.ContainsKey(c)).ToList();
        if (validPool.Count == 0)
            validPool = SlideCache.Keys.ToList();

        if (validPool.Count == 0)
            return ColorType.Blue;

        float total = 0f;
        foreach (ColorType type in validPool)
            total += defaultWeights.ContainsKey(type) ? defaultWeights[type] : 1f;

        float roll = Random.Range(0f, total);
        float running = 0f;
        foreach (ColorType col in validPool)
        {
            running += defaultWeights.ContainsKey(col) ? defaultWeights[col] : 1f;
            if (roll <= running)
                return col;
        }

        return validPool[0];
    }

    public bool HasAnyPlayableMove()
    {
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                ColorObject obj = ColorObjects[r, c];
                if (obj != null)
                {
                    if (obj.CanBeClicable)
                        return true;

                    break;
                }
            }
        }

        return false;
    }

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
        specialObjectCounts.Clear();
    }

    public bool AreAllObjectivesCompleted()
    {
        foreach (KeyValuePair<ColorType, int> objective in CollectionObjectives)
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

    public Vector3 FindGridPosition(int row, int col)
    {
        int cols = LevelData.ColumnCount;
        float half = (cols - 1) * 0.5f;
        float x = (col - half) * xPadding;
        float z = row * zPadding;

        return startPoint.position + new Vector3(x, 0f, z);
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

        bool ensured = false;
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;
        bool hasObjectives = CollectionObjectives.Count > 0;
        ColorObjects = new ColorObject[rows, cols];
        for (int col = 0; col < cols; col++)
        {
            bool forceObjective = hasObjectives && !ensured && (col == cols - 1);
            ColorType color = ChooseColor(forceObjective);
            if (hasObjectives && CollectionObjectives.ContainsKey(color))
                ensured = true;

            SpawnCube(0, col, color);
        }

        for (int row = 1; row < rows; row++)
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
        SlideCache.Clear();
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
            GameObject slideGo = Instantiate(prefab, pos, Quaternion.identity, slidesParent);
            if (slideGo.TryGetComponent(out Slide slide))
                SlideCache[slide.Color] = slide;
        }
    }
}
/*using System;
using System.Collections.Generic;
using System.Linq;

using DG.Tweening;

using MEC;

using UnityEngine;

using static Assets.PublicEnums;

using Random = UnityEngine.Random;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public LevelDataSO LevelData { get; private set; }

    public List<Slide> Slides { get; set; } = new();
    public ColorObject[,] ColorObjects { get; private set; }

    public Dictionary<ColorType, Slide> SlideCache { get; private set; } = new();
    public Dictionary<ColorType, float> ObjectiveChances { get; private set; } = new();
    public Dictionary<ColorType, int> CollectionObjectives { get; private set; } = new();

    private readonly Dictionary<string, int> specialObjectCounts = new();

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

    private bool isGridBusy = false;
    private Queue<Action> gridOperationQueue = new Queue<Action>();

    private void Awake() => Instance = Instance.SetSingleton(this);

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        EventManager.OnSlideUsing += SlideUsed;
        EventManager.OnBombExploded += BombExploded;
        EventManager.OnObjectiveExpired += ObjectiveExpired;
    }

    private void OnDisable()
    {
        EventManager.OnSlideUsing -= SlideUsed;
        EventManager.OnBombExploded -= BombExploded;
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

            UnregisterSpecialObject(obj);
            obj.OnCollected();
        }

        foreach (int col in affectedColumns)
        {
            RebuildColumn(col);
        }

        Timing.CallDelayed(3f, () =>
        {
            foreach (ColorObject obj in localCollected)
                obj.ReturnToPool();
        });

    check:
        EventManager.SlideUsed(null, null);
    }

    private void BombExploded(BombObject bomb)
    {
        if (bomb == null || !bomb.gameObject.activeInHierarchy)
            return;

        int centerRow = bomb.RowIndex;
        int centerCol = bomb.ColumnIndex;
        int radius = bomb.ExplodeRadius;

        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        if (centerRow < 0 || centerRow >= rows || centerCol < 0 || centerCol >= cols)
        {
            UnregisterSpecialObject(bomb);
            bomb.ReturnToPool();
            return;
        }

        ColorObject current = ColorObjects[centerRow, centerCol];
        if (!ReferenceEquals(current, bomb))
        {
            UnregisterSpecialObject(bomb);
            bomb.ReturnToPool();
            return;
        }

        HashSet<int> affectedColumns = new();
        int startRow = Mathf.Max(0, centerRow - radius);
        int startCol = Mathf.Max(0, centerCol - radius);
        int endRow = Mathf.Min(rows - 1, centerRow + radius);
        int endCol = Mathf.Min(cols - 1, centerCol + radius);

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                ColorObject target = ColorObjects[r, c];
                if (target == null)
                    continue;

                ColorObjects[r, c] = null;

                UnregisterSpecialObject(target);

                target.transform.DOKill();

                target.DetachFromGrid();
                target.ReturnToPool();
                affectedColumns.Add(c);
            }
        }

        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.Shake(bomb.ShakeInsentity, bomb.ShakeDuration, true);

        foreach (int col in affectedColumns)
        {
            RebuildColumn(col);
        }
    }

    private void ObjectiveExpired(ColorObject expiredObject)
    {
        int row = expiredObject.RowIndex;
        int col = expiredObject.ColumnIndex;
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        if (row < 0 || row >= rows || col < 0 || col >= cols)
        {
            UnregisterSpecialObject(expiredObject);
            expiredObject.ReturnToPool();
            return;
        }

        ColorObject current = ColorObjects[row, col];
        if (!ReferenceEquals(current, expiredObject))
        {
            UnregisterSpecialObject(expiredObject);
            expiredObject.ReturnToPool();
            return;
        }

        ColorObjects[row, col] = null;

        UnregisterSpecialObject(expiredObject);

        FillEmptyCell(row, col);
        expiredObject.ReturnToPool();
    }

    public void Initialize(LevelDataSO levelData)
    {
        LevelData = levelData;
        InputControllerManager.Instance.InputAttempt = levelData.ClickAttempts;

        specialObjectCounts.Clear();
        if (LevelData.SpecialObjects != null)
        {
            foreach (SpecialObject special in LevelData.SpecialObjects)
            {
                if (special.Prefab != null)
                    specialObjectCounts[special.Prefab.name] = 0;
            }
        }

        SetupObjectives();
        BuildPrefabCache();
        BuildBaseSpawnWeights();
        SpawnSlides();
        GenerateInitialGrid();
    }

    private void FillEmptyCell(int row, int col)
    {
        if (ColorObjects[row, col] != null) return;

        foreach (SpecialObject special in LevelData.SpecialObjects)
        {
            if (special.Prefab == null)
                continue;

            specialObjectCounts.TryGetValue(special.Prefab.tag, out int currentCount);

            if (currentCount >= special.MaxOnSameTime)
                continue;

            if (Random.value > special.SpawnChance)
                continue;

            GameObject specialObj = SpawnSpecialObject(special.Prefab, row, col);

            if (specialObj != null)
            {
                specialObj.transform.localScale = Vector3.zero;
                specialObj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                return;
            }
        }

        ColorType color = ChooseColor(false);
        GameObject cube = SpawnCube(row, col, color);

        if (cube == null) return;

        cube.transform.localScale = Vector3.zero;
        cube.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void UnregisterSpecialObject(ColorObject obj)
    {
        string tag = obj.gameObject.tag;
        if (specialObjectCounts.TryGetValue(tag, out int value))
        {
            specialObjectCounts[tag] = Mathf.Max(0, value - 1);
        }
    }

    private GameObject SpawnSpecialObject(GameObject prefab, int row, int col)
    {
        Vector3 spawnPos = FindGridPosition(row, col);
        GameObject obj = PoolManager.Instance.SpawnFromPool(prefab, spawnPos, prefab.transform.rotation);

        if (!obj.TryGetComponent(out ColorObject colorObj))
        {
            obj.ReturnToPool();
            return null;
        }

        colorObj.RowIndex = row;
        colorObj.ColumnIndex = col;
        ColorObjects[row, col] = colorObj;

        string tag = obj.tag;
        if (!specialObjectCounts.ContainsKey(tag))
            specialObjectCounts[tag] = 0;

        specialObjectCounts[tag]++;

        return obj;
    }

    private void RebuildColumn(int currentCol)
    {
        int rowMax = LevelData.RowCount;
        Queue<ColorObject> existing = new(rowMax);

        for (int row = 0; row < rowMax; row++)
        {
            ColorObject colorobject = ColorObjects[row, currentCol];
            if (colorobject == null)
                continue;

            existing.Enqueue(colorobject);
            ColorObjects[row, currentCol] = null;
        }

        int existingCount = existing.Count;
        Sequence colSlide = DOTween.Sequence();

        for (int row = 0; row < existingCount; row++)
        {
            ColorObject cube = existing.Dequeue();

            cube.RowIndex = row;
            cube.ColumnIndex = currentCol;
            ColorObjects[row, currentCol] = cube;

            Vector3 targetPos = FindGridPosition(row, currentCol);

            Vector3 currentPos = cube.transform.position;
            cube.transform.position = new Vector3(targetPos.x, currentPos.y, currentPos.z);

            colSlide.Join(cube.transform.DOMove(targetPos, 0.35f));
        }

        colSlide.Play();

        for (int row = 0; row < rowMax; row++)
        {
            if (ColorObjects[row, currentCol] != null)
                continue;

            FillEmptyCell(row, currentCol);
        }
    }

    private GameObject SpawnCube(int row, int col, ColorType color)
    {
        if (!prefabsByColor.ContainsKey(color))
            return null;

        if (ColorObjects[row, col] != null)
            return null;

        List<GameObject> list = prefabsByColor[color];
        GameObject prefab = list.GetRandomValue();
        GameObject cube = PoolManager.Instance.SpawnFromPool(prefab, FindGridPosition(row, col), prefab.transform.rotation);

        if (!cube.TryGetComponent(out ColorObject colorObject))
            return cube;

        colorObject.RowIndex = row;
        colorObject.ColumnIndex = col;
        ColorObjects[row, col] = colorObject;

        return cube;
    }

    private ColorType ChooseColor(bool forceObjective)
    {
        Dictionary<ColorType, float> defaultWeights = baseSpawnWeights;
        IEnumerable<ColorType> pool = forceObjective && CollectionObjectives.Count > 0 ? CollectionObjectives.Keys : defaultWeights.Keys;

        float total = 0f;
        foreach (ColorType type in pool)
            total += defaultWeights[type];

        float running = 0f;
        float roll = Random.Range(0f, total);
        foreach (ColorType color in pool)
        {
            running += defaultWeights[color];
            if (roll <= running)
                return color;
        }

        foreach (ColorType color in pool)
            return color;

        return 0;
    }

    public bool HasAnyPlayableMove()
    {
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                ColorObject obj = ColorObjects[r, c];
                if (obj != null)
                {
                    if (obj.CanBeClicable)
                        return true;

                    break;
                }
            }
        }

        return false;
    }

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
        specialObjectCounts.Clear();
    }

    public bool AreAllObjectivesCompleted()
    {
        foreach (KeyValuePair<ColorType, int> objective in CollectionObjectives)
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

    public Vector3 FindGridPosition(int row, int col)
    {
        int cols = LevelData.ColumnCount;
        float half = (cols - 1) * 0.5f;
        float x = (col - half) * xPadding;
        float z = row * zPadding;

        return startPoint.position + new Vector3(x, 0f, z);
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

        bool ensured = false;
        int rows = LevelData.RowCount;
        int cols = LevelData.ColumnCount;
        bool hasObjectives = CollectionObjectives.Count > 0;

        ColorObjects = new ColorObject[rows, cols];

        for (int col = 0; col < cols; col++)
        {
            bool forceObjective = hasObjectives && !ensured && (col == cols - 1);
            ColorType color = ChooseColor(forceObjective);
            if (hasObjectives && CollectionObjectives.ContainsKey(color))
                ensured = true;

            SpawnCube(0, col, color);
        }

        for (int row = 1; row < rows; row++)
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
        SlideCache.Clear();
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
            GameObject slideGo = Instantiate(prefab, pos, Quaternion.identity, slidesParent);

            if (slideGo.TryGetComponent(out Slide slide))
                SlideCache[slide.Color] = slide;
        }
    }
}*/