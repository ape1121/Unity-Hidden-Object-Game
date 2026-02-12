using System;
using UnityEngine.SceneManagement;

public sealed class SceneFlowManager
{
    private readonly string loaderSceneName;
    private readonly string mainSceneName;
    private readonly string gameSceneName;

    public event Action<Scene, LoadSceneMode> OnSceneLoaded;

    public string LoaderSceneName => loaderSceneName;
    public string MainSceneName => mainSceneName;
    public string GameSceneName => gameSceneName;

    public SceneFlowManager(string loaderSceneName, string mainSceneName, string gameSceneName)
    {
        this.loaderSceneName = loaderSceneName;
        this.mainSceneName = mainSceneName;
        this.gameSceneName = gameSceneName;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public void Shutdown()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void LoadLoader()
    {
        LoadScene(AppScene.Loader);
    }

    public void LoadMain()
    {
        LoadScene(AppScene.Main);
    }

    public void LoadGame()
    {
        LoadScene(AppScene.Game);
    }

    public void ReloadGame()
    {
        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void LoadScene(AppScene scene)
    {
        string sceneName = GetSceneName(scene);
        if (IsActiveScene(scene))
        {
            return;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public bool IsActiveScene(AppScene scene)
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        return IsSceneName(activeSceneName, scene);
    }

    public bool IsSceneName(string sceneName, AppScene scene)
    {
        return string.Equals(sceneName, GetSceneName(scene), StringComparison.Ordinal);
    }

    private string GetSceneName(AppScene scene)
    {
        switch (scene)
        {
            case AppScene.Loader:
                return loaderSceneName;
            case AppScene.Main:
                return mainSceneName;
            case AppScene.Game:
                return gameSceneName;
            default:
                return mainSceneName;
        }
    }

    private void HandleSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        if (IsSceneName(loadedScene.name, AppScene.Main) && App.Popups != null)
        {
            App.Popups.CloseAll(true);
        }

        OnSceneLoaded?.Invoke(loadedScene, loadMode);
    }
}
