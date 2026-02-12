using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private LevelRuntimeMap levelRuntimeMap;
    [SerializeField] private HiddenItemSpawner hiddenItemSpawner;
    [SerializeField] private HiddenItemCollector hiddenItemCollector;
    [SerializeField] private RemainingItems remainingItems;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private HiddenItemInput hiddenItemInput;
    [SerializeField] private CameraRigController cameraRigController;

    [Header("Startup")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool pauseWithTimeScale = true;

    private bool initialized;
    private bool paused;
    private bool levelCompletedRaised;

    public event Action LevelCompleted;

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

        ResolveOptionalReferences();

        if (uiManager != null)
        {
            uiManager.Configure(hiddenItemSpawner, hiddenItemCollector, remainingItems);
            uiManager.Initialize();
        }

        if (levelRuntimeMap != null)
        {
            levelRuntimeMap.InitializeLevel();
        }

        if (remainingItems != null)
        {
            remainingItems.OnAllItemsCollected += HandleAllItemsCollected;
        }

        levelCompletedRaised = false;
        SetPaused(false);
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

        if (remainingItems != null)
        {
            remainingItems.OnAllItemsCollected -= HandleAllItemsCollected;
        }

        initialized = false;
    }

    public void SetPaused(bool isPaused)
    {
        if (paused == isPaused)
        {
            return;
        }

        ResolveOptionalReferences();

        paused = isPaused;
        if (hiddenItemInput != null)
        {
            hiddenItemInput.enabled = !paused;
        }

        if (cameraRigController != null)
        {
            cameraRigController.enabled = !paused;
        }

        if (pauseWithTimeScale && Application.isPlaying)
        {
            Time.timeScale = paused ? 0f : 1f;
        }
    }

    private void HandleAllItemsCollected()
    {
        if (levelCompletedRaised)
        {
            return;
        }

        levelCompletedRaised = true;
        LevelCompleted?.Invoke();
    }

    private void ResolveOptionalReferences()
    {
        if (hiddenItemInput == null)
        {
            hiddenItemInput = FindFirstObjectByType<HiddenItemInput>();
        }

        if (cameraRigController == null)
        {
            cameraRigController = FindFirstObjectByType<CameraRigController>();
        }
    }

    private void OnDestroy()
    {
        SetPaused(false);
        ShutdownGame();
    }
}
