using System;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private LevelRuntimeMap levelRuntimeMap;
    [SerializeField] private HiddenItemSpawner hiddenItemSpawner;
    [SerializeField] private HiddenItemCollector hiddenItemCollector;
    [SerializeField] private RemainingItems remainingItems;
    [FormerlySerializedAs("uiManager")]
    [SerializeField] private GameUI gameUI;
    [SerializeField] private HiddenItemInput hiddenItemInput;
    [SerializeField] private CameraRigController cameraRigController;

    [Header("Startup")]
    [SerializeField] private bool initializeOnStart = true;

    private bool initialized;
    private bool sessionBound;
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

        if (gameUI != null)
        {
            gameUI.Configure(hiddenItemSpawner, hiddenItemCollector, remainingItems);
            gameUI.Initialize();
        }

        if (levelRuntimeMap != null)
        {
            levelRuntimeMap.InitializeLevel();
        }

        if (remainingItems != null)
        {
            remainingItems.OnAllItemsCollected += HandleAllItemsCollected;
        }

        BindSessionState();
        StartSession();

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

        if (gameUI != null)
        {
            gameUI.Shutdown();
        }

        if (remainingItems != null)
        {
            remainingItems.OnAllItemsCollected -= HandleAllItemsCollected;
        }

        UnbindSessionState();

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
    }

    public void PauseGame()
    {
        if (App.Sessions == null)
        {
            return;
        }

        App.Sessions.PauseSession();
    }

    public void ResumeGame()
    {
        if (App.Sessions == null)
        {
            return;
        }

        App.Sessions.ResumeSession();
    }

    public void CompleteLevel()
    {
        if (App.Sessions == null)
        {
            return;
        }

        App.Sessions.CompleteSession();
    }

    public void ReturnToMain()
    {
        if (App.Sessions != null)
        {
            App.Sessions.AbortToMain();
            App.Sessions.ResetToIdle();
        }

        if (App.Scenes != null)
        {
            App.Scenes.LoadMain();
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
        CompleteLevel();
    }

    private void StartSession()
    {
        if (App.Sessions == null || App.Config == null)
        {
            return;
        }

        App.Sessions.StartSession(App.Config.DefaultLevelId);
        ApplySessionState(App.Sessions.State);
    }

    private void BindSessionState()
    {
        if (sessionBound || App.Sessions == null)
        {
            return;
        }

        App.Sessions.OnStateChanged += HandleSessionStateChanged;
        sessionBound = true;
    }

    private void UnbindSessionState()
    {
        if (!sessionBound)
        {
            return;
        }

        if (App.Sessions != null)
        {
            App.Sessions.OnStateChanged -= HandleSessionStateChanged;
        }

        sessionBound = false;
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState newState)
    {
        ApplySessionState(newState);
    }

    private void ApplySessionState(GameSessionState state)
    {
        bool shouldPause = state == GameSessionState.Paused || state == GameSessionState.Completed;
        SetPaused(shouldPause);

        if (App.Popups == null)
        {
            return;
        }

        switch (state)
        {
            case GameSessionState.Paused:
                App.Popups.Open(PopupType.Pause);
                App.Popups.Close(PopupType.Win);
                break;
            case GameSessionState.Completed:
                App.Popups.Close(PopupType.Pause);
                App.Popups.Open(PopupType.Win);
                break;
            case GameSessionState.Running:
                App.Popups.Close(PopupType.Pause);
                App.Popups.Close(PopupType.Win);
                break;
            default:
                App.Popups.CloseAll();
                break;
        }
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
        if (App.Sessions != null && App.Sessions.State != GameSessionState.Idle)
        {
            App.Sessions.AbortToMain();
            App.Sessions.ResetToIdle();
        }

        SetPaused(false);
        ShutdownGame();
    }
}
