using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "SearchIt/App Config", order = 1)]
public class AppConfig : ScriptableObject
{
    [Header("Scenes")]
    [SerializeField] private string loaderSceneName = "Loader";
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private string gameSceneName = "Game";

    [Header("Startup")]
    [SerializeField] private string defaultLevelId = "level_01";

    [Header("Economy")]
    [SerializeField, Min(0.1f)] private float coinRewardIntervalSeconds = 5f;
    [SerializeField, Min(1)] private int coinRewardPerInterval = 1;

    [Header("Popups")]
    [SerializeField] private Canvas popupCanvasPrefab;
    [SerializeField] private PopupView[] popupPrefabs = Array.Empty<PopupView>();

    public string LoaderSceneName => loaderSceneName;
    public string MainSceneName => mainSceneName;
    public string GameSceneName => gameSceneName;
    public string DefaultLevelId => defaultLevelId;
    public float CoinRewardIntervalSeconds => coinRewardIntervalSeconds;
    public int CoinRewardPerInterval => coinRewardPerInterval;
    public Canvas PopupCanvasPrefab => popupCanvasPrefab;
    public IReadOnlyList<PopupView> PopupPrefabs => popupPrefabs;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(loaderSceneName))
        {
            loaderSceneName = "Loader";
        }

        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            mainSceneName = "Main";
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            gameSceneName = "Game";
        }

        if (string.IsNullOrWhiteSpace(defaultLevelId))
        {
            defaultLevelId = "level_01";
        }

        coinRewardIntervalSeconds = Mathf.Max(0.1f, coinRewardIntervalSeconds);
        coinRewardPerInterval = Mathf.Max(1, coinRewardPerInterval);
        popupPrefabs ??= Array.Empty<PopupView>();
    }
#endif
}
