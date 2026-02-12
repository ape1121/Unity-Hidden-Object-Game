using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private AppConfig appConfig;

    private void Awake()
    {
        if (appConfig == null)
        {
            Debug.LogError("AppBootstrap: AppConfig is required. Assign an AppConfig asset in the inspector.", this);
            return;
        }

        App.EnsureInstance(appConfig);
    }
}
