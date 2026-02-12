using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9500)]
public sealed class App : MonoBehaviour
{
    public static App Instance { get; private set; }
    public static AppConfig Config { get; private set; }

    public static SceneFlowManager Scenes { get; private set; }
    public static GameSessionManager Sessions { get; private set; }
    public static CoinManager Coins { get; private set; }

    private GameManager activeGameManager;
    private bool initialized;

    public static App EnsureInstance(AppConfig appConfig)
    {
        App app = Instance;
        if (app == null)
        {
            app = FindFirstObjectByType<App>();
        }

        if (app == null)
        {
            var appGameObject = new GameObject("App");
            app = appGameObject.AddComponent<App>();
        }

        app.Configure(appConfig);
        return app;
    }

    public void Configure(AppConfig appConfig)
    {
        if (appConfig == null)
        {
            Debug.LogError("App: AppConfig is required but was null.");
            return;
        }

        if (initialized)
        {
            Debug.LogWarning("App: Configure called after initialization. Restart Play Mode to apply AppConfig changes.");
            return;
        }

        Config = appConfig;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        TryAdvanceFromLoader();
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        Sessions.Tick(deltaTime);
        Coins.Tick(deltaTime);
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        if (Sessions != null)
        {
            Sessions.OnStateChanged -= HandleSessionStateChanged;
        }

        if (Coins != null)
        {
            Coins.Shutdown();
        }

        UnbindGameScene();

        Scenes = null;
        Sessions = null;
        Coins = null;
        Config = null;
        Instance = null;
    }

    public void StartGame()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (Scenes.IsActiveScene(AppScene.Game))
        {
            Scenes.ReloadGame();
            return;
        }

        Scenes.LoadGame();
    }

    public void ReturnToMain()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        Sessions.AbortToMain();
        Scenes.LoadMain();
    }

    public void PauseGame()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        Sessions.PauseSession();
    }

    public void ResumeGame()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        Sessions.ResumeSession();
    }

    public void CompleteLevel()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        Sessions.CompleteSession();
    }

    public void TryAdvanceFromLoader()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (!Config.AutoAdvanceFromLoader || !Scenes.IsActiveScene(AppScene.Loader))
        {
            return;
        }

        Scenes.LoadMain();
    }

    private bool EnsureInitialized()
    {
        if (initialized)
        {
            return true;
        }

        if (!ValidateConfig())
        {
            return false;
        }

        Scenes = new SceneFlowManager(Config.LoaderSceneName, Config.MainSceneName, Config.GameSceneName);
        Sessions = new GameSessionManager();
        Coins = new CoinManager(Config, Sessions);

        Sessions.OnStateChanged += HandleSessionStateChanged;

        initialized = true;
        return true;
    }

    private bool ValidateConfig()
    {
        if (Config == null)
        {
            Debug.LogError("App: Missing AppConfig. Assign AppConfig in AppBootstrap.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Config.LoaderSceneName)
            || string.IsNullOrWhiteSpace(Config.MainSceneName)
            || string.IsNullOrWhiteSpace(Config.GameSceneName)
            || string.IsNullOrWhiteSpace(Config.DefaultLevelId))
        {
            Debug.LogError("App: AppConfig has empty required fields.");
            return false;
        }

        return true;
    }

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (Scenes.IsSceneName(loadedScene.name, AppScene.Loader))
        {
            TryAdvanceFromLoader();
            return;
        }

        if (Scenes.IsSceneName(loadedScene.name, AppScene.Game))
        {
            BindGameScene();
            return;
        }

        if (Scenes.IsSceneName(loadedScene.name, AppScene.Main))
        {
            UnbindGameScene();
            Sessions.AbortToMain();
            Sessions.ResetToIdle();
        }
    }

    private void BindGameScene()
    {
        UnbindGameScene();

        activeGameManager = FindFirstObjectByType<GameManager>();
        if (activeGameManager == null)
        {
            Debug.LogWarning("App: Game scene loaded but no GameManager was found.");
            return;
        }

        activeGameManager.LevelCompleted += HandleLevelCompleted;
        activeGameManager.InitializeGame();

        Sessions.StartSession(Config.DefaultLevelId);
        ApplySessionStateToGame(Sessions.State);
    }

    private void UnbindGameScene()
    {
        if (activeGameManager == null)
        {
            return;
        }

        activeGameManager.LevelCompleted -= HandleLevelCompleted;
        activeGameManager.SetPaused(false);
        activeGameManager = null;
    }

    private void HandleLevelCompleted()
    {
        Sessions.CompleteSession();
    }

    private void HandleSessionStateChanged(GameSessionState previousState, GameSessionState newState)
    {
        ApplySessionStateToGame(newState);
    }

    private void ApplySessionStateToGame(GameSessionState state)
    {
        if (activeGameManager == null)
        {
            return;
        }

        bool shouldPause = state == GameSessionState.Paused || state == GameSessionState.Completed;
        activeGameManager.SetPaused(shouldPause);
    }
}
