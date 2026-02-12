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
}
