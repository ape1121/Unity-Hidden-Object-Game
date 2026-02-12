using UnityEngine;

[DefaultExecutionOrder(-9500)]
public sealed class App : MonoBehaviour
{
    [SerializeField] private AppConfig appConfig;

    public static App Instance { get; private set; }
    public static AppConfig Config { get; private set; }

    public static SceneFlowManager Scenes { get; private set; }
    public static GameSessionManager Sessions { get; private set; }
    public static CoinManager Coins { get; private set; }
    public static SaveManager Saves { get; private set; }
    public static PopupManager Popups { get; private set; }

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

    private void Start()
    {
        InitializeManagers();
        Scenes.LoadMain();
    }

    private void Update()
    {
        if (Sessions == null || Coins == null)
            return;
        if (Sessions.State == GameSessionState.Paused || Sessions.State == GameSessionState.Completed)
            return;
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

        if (Saves != null)
        {
            Saves.Shutdown();
        }

        if (Coins != null)
        {
            Coins.Shutdown();
        }

        if (Popups != null)
        {
            Popups.Shutdown();
        }

        if (Scenes != null)
        {
            Scenes.Shutdown();
        }

        Scenes = null;
        Sessions = null;
        Coins = null;
        Saves = null;
        Popups = null;
        Config = null;
        Instance = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || Saves == null)
        {
            return;
        }

        Saves.Save();
    }

    private void OnApplicationQuit()
    {
        if (Saves == null)
        {
            return;
        }

        Saves.Save();
    }

    private void InitializeManagers()
    {
        Config = appConfig;
        Scenes = new SceneFlowManager(appConfig.LoaderSceneName, appConfig.MainSceneName, appConfig.GameSceneName);
        Sessions = new GameSessionManager();
        Coins = new CoinManager(appConfig, Sessions);
        Saves = new SaveManager(appConfig, Sessions, Coins);
        Popups = new PopupManager(appConfig);
    }
}
