public readonly struct GameSessionSummary
{
    public string LevelId { get; }
    public float ElapsedSeconds { get; }
    public bool Completed { get; }

    public GameSessionSummary(string levelId, float elapsedSeconds, bool completed)
    {
        LevelId = levelId;
        ElapsedSeconds = elapsedSeconds;
        Completed = completed;
    }
}
