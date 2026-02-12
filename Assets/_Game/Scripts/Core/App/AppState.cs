public sealed class AppState
{
    public GameSessionState SessionState { get; private set; } = GameSessionState.Idle;
    public string ActiveLevelId { get; private set; } = string.Empty;
    public float SessionElapsedSeconds { get; private set; }
    public int TotalCoins { get; private set; }
    public int SessionCoins { get; private set; }

    public void Sync(GameSessionManager sessionManager, CoinManager coinManager)
    {
        if (sessionManager != null)
        {
            SessionState = sessionManager.State;
            ActiveLevelId = sessionManager.CurrentLevelId;
            SessionElapsedSeconds = sessionManager.ElapsedSeconds;
        }

        if (coinManager != null)
        {
            TotalCoins = coinManager.TotalCoins;
            SessionCoins = coinManager.SessionCoins;
        }
    }
}
