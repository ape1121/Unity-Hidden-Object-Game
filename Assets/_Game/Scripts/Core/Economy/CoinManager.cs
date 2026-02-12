using System;
using UnityEngine;

public sealed class CoinManager
{
    public event Action<int, int, int> OnCoinsChanged;

    public int TotalCoins { get; private set; }
    public int SessionCoins { get; private set; }

    public float RewardIntervalSeconds { get; }
    public int RewardPerInterval { get; }

    private readonly GameSessionManager sessionManager;
    private float intervalAccumulator;

    public CoinManager(AppConfig appConfig, GameSessionManager sessionManager)
    {
        if (appConfig == null)
        {
            throw new ArgumentNullException(nameof(appConfig));
        }

        this.sessionManager = sessionManager;
        RewardIntervalSeconds = Mathf.Max(0.1f, appConfig.CoinRewardIntervalSeconds);
        RewardPerInterval = Mathf.Max(1, appConfig.CoinRewardPerInterval);

        if (this.sessionManager != null)
        {
            this.sessionManager.OnSessionStarted += HandleSessionStarted;
        }
    }

    public void Shutdown()
    {
        if (sessionManager != null)
        {
            sessionManager.OnSessionStarted -= HandleSessionStarted;
        }
    }

    private void HandleSessionStarted(string levelId)
    {
        BeginSession();
    }

    public void BeginSession()
    {
        SessionCoins = 0;
        intervalAccumulator = 0f;
        OnCoinsChanged?.Invoke(TotalCoins, SessionCoins, 0);
    }

    public int Tick(float deltaTime)
    {
        if (sessionManager == null || sessionManager.State != GameSessionState.Running || deltaTime <= 0f)
        {
            return 0;
        }

        intervalAccumulator += deltaTime;
        int awardedCoins = 0;
        while (intervalAccumulator >= RewardIntervalSeconds)
        {
            intervalAccumulator -= RewardIntervalSeconds;
            awardedCoins += RewardPerInterval;
        }

        if (awardedCoins <= 0)
        {
            return 0;
        }

        TotalCoins += awardedCoins;
        SessionCoins += awardedCoins;
        OnCoinsChanged?.Invoke(TotalCoins, SessionCoins, awardedCoins);
        return awardedCoins;
    }
}
