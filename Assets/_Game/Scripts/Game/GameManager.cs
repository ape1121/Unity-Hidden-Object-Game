using System;
using System.Collections;
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
    [SerializeField] private BoosterManager boosterManager;
    [SerializeField] private HiddenItemInput hiddenItemInput;
    [SerializeField] private CameraRigController cameraRigController;

    [Header("Startup")]
    [SerializeField] private bool initializeOnStart = true;

    private bool initialized;
    private bool sessionBound;
    private bool paused;
    private bool levelCompletedRaised;
    private Coroutine uiExitToMainCoroutine;
    private Coroutine uiExitToWinCoroutine;

    public event Action LevelCompleted;
    public HiddenItemSpawner HiddenItemSpawner => hiddenItemSpawner;
    public HiddenItemCollector HiddenItemCollector => hiddenItemCollector;
    public RemainingItems RemainingItems => remainingItems;
    public CameraRigController CameraRigController => cameraRigController;
    public RectTransform UiOverlayRoot => gameUI != null ? gameUI.transform as RectTransform : null;

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
        InitializeBoosterManager();

        if (gameUI != null)
        {
            gameUI.BindGameManager(this);
            gameUI.BindGameplayActions(HandlePauseRequested, HandleReturnToMainRequested, HandleBoosterRequested);
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

        StopUiExitCoroutines();

        if (gameUI != null)
        {
            gameUI.BindGameplayActions(null, null, null);
            gameUI.Shutdown();
            gameUI.BindGameManager(null);
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

    public bool UseBooster()
    {
        if (!initialized || boosterManager == null)
        {
            return false;
        }

        if (App.Sessions == null || App.Sessions.State != GameSessionState.Running)
        {
            return false;
        }

        return boosterManager.TryUseBooster();
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

    private void HandlePauseRequested()
    {
        PauseGame();
    }

    private void HandleReturnToMainRequested()
    {
        if (uiExitToMainCoroutine != null)
        {
            return;
        }

        if (!initialized)
        {
            ReturnToMain();
            return;
        }

        uiExitToMainCoroutine = StartCoroutine(ExitUiThenReturnToMain());
    }

    private void HandleBoosterRequested()
    {
        UseBooster();
    }

    private void StartSession()
    {
        if (App.Sessions == null || App.Config == null)
        {
            return;
        }

        string levelId = App.Config.DefaultLevelId;
        if (App.Saves != null)
        {
            levelId = App.Saves.GetCurrentLevelOrDefault();
        }

        App.Sessions.StartSession(levelId);
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

        switch (state)
        {
            case GameSessionState.Paused:
                if (App.Popups != null)
                {
                    App.Popups.Open(PopupType.Pause);
                    App.Popups.Close(PopupType.Win);
                }
                break;
            case GameSessionState.Completed:
                if (App.Popups != null)
                {
                    App.Popups.Close(PopupType.Pause);
                }

                if (uiExitToWinCoroutine == null)
                {
                    uiExitToWinCoroutine = StartCoroutine(ExitUiThenOpenWinPopup());
                }
                break;
            case GameSessionState.Running:
                if (App.Popups != null)
                {
                    App.Popups.Close(PopupType.Pause);
                    App.Popups.Close(PopupType.Win);
                }
                break;
            default:
                if (App.Popups != null)
                {
                    App.Popups.CloseAll();
                }
                break;
        }
    }

    private void ResolveOptionalReferences()
    {
        if (hiddenItemInput == null)
        {
            hiddenItemInput = GetComponent<HiddenItemInput>();
        }

        if (cameraRigController == null)
        {
            cameraRigController = GetComponent<CameraRigController>();
        }

        if (boosterManager == null)
        {
            boosterManager = GetComponent<BoosterManager>();
        }
    }

    private void InitializeBoosterManager()
    {
        ResolveOptionalReferences();
        if (boosterManager == null)
        {
            boosterManager = gameObject.AddComponent<BoosterManager>();
        }

        boosterManager.Configure(this);
    }

    private void OnDestroy()
    {
        StopUiExitCoroutines();

        if (App.Sessions != null && App.Sessions.State != GameSessionState.Idle)
        {
            App.Sessions.AbortToMain();
            App.Sessions.ResetToIdle();
        }

        SetPaused(false);
        ShutdownGame();
    }

    private IEnumerator ExitUiThenReturnToMain()
    {
        if (uiExitToWinCoroutine != null)
        {
            StopCoroutine(uiExitToWinCoroutine);
            uiExitToWinCoroutine = null;
        }

        SetPaused(true);
        yield return PlayGameUiExitTransition();
        ReturnToMain();
        uiExitToMainCoroutine = null;
    }

    private IEnumerator ExitUiThenOpenWinPopup()
    {
        yield return PlayGameUiExitTransition();

        if (App.Sessions != null && App.Sessions.State == GameSessionState.Completed && App.Popups != null)
        {
            App.Popups.Open(PopupType.Win);
        }

        uiExitToWinCoroutine = null;
    }

    private IEnumerator PlayGameUiExitTransition()
    {
        if (gameUI == null)
        {
            yield break;
        }

        gameUI.Exit();
        float exitDuration = gameUI.GetExitTransitionDuration();
        if (exitDuration <= 0f)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(exitDuration);
    }

    private void StopUiExitCoroutines()
    {
        if (uiExitToMainCoroutine != null)
        {
            StopCoroutine(uiExitToMainCoroutine);
            uiExitToMainCoroutine = null;
        }

        if (uiExitToWinCoroutine != null)
        {
            StopCoroutine(uiExitToWinCoroutine);
            uiExitToWinCoroutine = null;
        }
    }
}
