#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayModeStartFromLoader
{
    private const string LoaderScenePath = "Assets/_Game/Scenes/Loader.unity";

    static PlayModeStartFromLoader()
    {
        ApplyLoaderStartScene(logResult: false);
    }

    [MenuItem("SearchIt/Play/Use Loader As Play Start Scene")]
    public static void ApplyLoaderStartScene()
    {
        ApplyLoaderStartScene(logResult: true);
    }

    private static void ApplyLoaderStartScene(bool logResult)
    {
        SceneAsset loaderScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoaderScenePath);
        if (loaderScene == null)
        {
            UnityEngine.Debug.LogWarning("PlayModeStartFromLoader: Loader scene not found at " + LoaderScenePath);
            return;
        }

        EditorSceneManager.playModeStartScene = loaderScene;
        if (logResult)
        {
            UnityEngine.Debug.Log("PlayModeStartFromLoader: Play Mode start scene set to " + LoaderScenePath);
        }
    }

    [MenuItem("SearchIt/Play/Clear Custom Play Start Scene")]
    public static void ClearPlayStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
        UnityEngine.Debug.Log("PlayModeStartFromLoader: Cleared custom Play Mode start scene.");
    }
}
#endif
