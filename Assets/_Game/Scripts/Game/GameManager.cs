using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private LevelRuntimeMap levelRuntimeMap;
    [SerializeField] private HiddenItemSpawner hiddenItemSpawner;
    [SerializeField] private HiddenItemCollector hiddenItemCollector;
    [SerializeField] private RemainingItems remainingItems;
    [SerializeField] private UIManager uiManager;

    [Header("Startup")]
    [SerializeField] private bool initializeOnStart = true;

    private bool initialized;

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeGame();
        }
    }

    public void InitializeGame()
    {
        if (initialized)
        {
            return;
        }

        ResolveMissingReferences();
        ReportMissingReferences();

        if (uiManager != null)
        {
            uiManager.Configure(hiddenItemSpawner, hiddenItemCollector, remainingItems);
            uiManager.Initialize();
        }

        if (levelRuntimeMap != null)
        {
            levelRuntimeMap.InitializeLevel();
        }

        initialized = true;
    }

    public void ShutdownGame()
    {
        if (!initialized)
        {
            return;
        }

        if (uiManager != null)
        {
            uiManager.Shutdown();
        }

        initialized = false;
    }

    private void OnDestroy()
    {
        ShutdownGame();
    }

    private void ResolveMissingReferences()
    {
        if (levelRuntimeMap == null)
        {
            levelRuntimeMap = FindFirstObjectByType<LevelRuntimeMap>();
        }

        if (hiddenItemSpawner == null)
        {
            hiddenItemSpawner = FindFirstObjectByType<HiddenItemSpawner>();
        }

        if (hiddenItemCollector == null)
        {
            hiddenItemCollector = FindFirstObjectByType<HiddenItemCollector>();
        }

        if (remainingItems == null)
        {
            remainingItems = FindFirstObjectByType<RemainingItems>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    private void ReportMissingReferences()
    {
        if (uiManager == null)
        {
            Debug.LogWarning("GameManager: UIManager reference is missing.");
        }

        if (levelRuntimeMap == null)
        {
            Debug.LogWarning("GameManager: LevelRuntimeMap reference is missing.");
        }

        if (hiddenItemSpawner == null)
        {
            Debug.LogWarning("GameManager: HiddenItemSpawner reference is missing.");
        }

        if (hiddenItemCollector == null)
        {
            Debug.LogWarning("GameManager: HiddenItemCollector reference is missing.");
        }

        if (remainingItems == null)
        {
            Debug.LogWarning("GameManager: RemainingItems reference is missing.");
        }
    }
}
