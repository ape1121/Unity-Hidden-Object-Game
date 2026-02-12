
using UnityEngine;

public sealed class SaveManager
{
    private const string SaveKey = "searchit_save_v1";

    private readonly GameSessionManager sessionManager;
    private readonly CoinManager coinManager;
    private readonly string defaultLevelId;

    private SaveData saveData;

    public int TotalGold => saveData != null ? saveData.totalGold : 0;
    public string CurrentLevelId => saveData != null ? saveData.currentLevelId : defaultLevelId;

    public SaveManager(AppConfig config, GameSessionManager sessionManager, CoinManager coinManager)
    {
        this.sessionManager = sessionManager;
        this.coinManager = coinManager;
        defaultLevelId = ResolveDefaultLevelId(config);

        saveData = LoadOrCreate(defaultLevelId);
        ApplyLoadedData();
        Bind();
        Save();
    }

    public void Shutdown()
    {
        Save();
        Unbind();
    }

    public string GetCurrentLevelOrDefault()
    {
        string levelId = CurrentLevelId;
        return string.IsNullOrWhiteSpace(levelId) ? defaultLevelId : levelId;
    }

    public void SetCurrentLevel(string levelId)
    {
        EnsureData();
        saveData.currentLevelId = string.IsNullOrWhiteSpace(levelId) ? defaultLevelId : levelId;
        Save();
    }

    public void Save()
    {
        EnsureData();

        if (coinManager != null)
        {
            saveData.totalGold = Mathf.Max(0, coinManager.TotalCoins);
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void Bind()
    {
        if (coinManager != null)
        {
            coinManager.OnCoinsChanged -= HandleCoinsChanged;
            coinManager.OnCoinsChanged += HandleCoinsChanged;
        }

        if (sessionManager != null)
        {
            sessionManager.OnSessionStarted -= HandleSessionStarted;
            sessionManager.OnSessionStarted += HandleSessionStarted;
        }
    }

    private void Unbind()
    {
        if (coinManager != null)
        {
            coinManager.OnCoinsChanged -= HandleCoinsChanged;
        }

        if (sessionManager != null)
        {
            sessionManager.OnSessionStarted -= HandleSessionStarted;
        }
    }

    private void HandleCoinsChanged(int totalCoins, int sessionCoins, int deltaCoins)
    {
        EnsureData();
        saveData.totalGold = Mathf.Max(0, totalCoins);
        Save();
    }

    private void HandleSessionStarted(string levelId)
    {
        EnsureData();
        saveData.currentLevelId = string.IsNullOrWhiteSpace(levelId) ? defaultLevelId : levelId;
        Save();
    }

    private void ApplyLoadedData()
    {
        EnsureData();

        if (coinManager != null)
        {
            coinManager.SetTotalCoins(saveData.totalGold, notify: false);
        }
    }

    private void EnsureData()
    {
        if (saveData == null)
        {
            saveData = CreateDefaultData(defaultLevelId);
        }

        saveData.totalGold = Mathf.Max(0, saveData.totalGold);
        if (string.IsNullOrWhiteSpace(saveData.currentLevelId))
        {
            saveData.currentLevelId = defaultLevelId;
        }
    }

    private static SaveData LoadOrCreate(string defaultLevelId)
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return CreateDefaultData(defaultLevelId);
        }

        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return CreateDefaultData(defaultLevelId);
        }

        SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
        if (loadedData == null)
        {
            return CreateDefaultData(defaultLevelId);
        }

        loadedData.totalGold = Mathf.Max(0, loadedData.totalGold);
        if (string.IsNullOrWhiteSpace(loadedData.currentLevelId))
        {
            loadedData.currentLevelId = defaultLevelId;
        }

        return loadedData;
    }

    private static SaveData CreateDefaultData(string defaultLevelId)
    {
        return new SaveData
        {
            totalGold = 0,
            currentLevelId = defaultLevelId
        };
    }

    private static string ResolveDefaultLevelId(AppConfig config)
    {
        if (config == null || string.IsNullOrWhiteSpace(config.DefaultLevelId))
        {
            return "level_01";
        }

        return config.DefaultLevelId;
    }
}