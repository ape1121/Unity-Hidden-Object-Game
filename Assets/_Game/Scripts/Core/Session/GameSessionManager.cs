using System;
using UnityEngine;

public sealed class GameSessionManager
{
    public event Action<GameSessionState, GameSessionState> OnStateChanged;
    public event Action<string> OnSessionStarted;
    public event Action<GameSessionSummary> OnSessionEnded;

    public GameSessionState State { get; private set; } = GameSessionState.Idle;
    public string CurrentLevelId { get; private set; } = string.Empty;
    public float ElapsedSeconds { get; private set; }

    public void StartSession(string levelId)
    {
        if (State != GameSessionState.Idle)
        {
            AbortSession();
        }

        CurrentLevelId = string.IsNullOrWhiteSpace(levelId) ? "level_01" : levelId;
        ElapsedSeconds = 0f;

        SetState(GameSessionState.Running);
        OnSessionStarted?.Invoke(CurrentLevelId);
    }

    public void PauseSession()
    {
        if (State != GameSessionState.Running)
        {
            return;
        }

        SetState(GameSessionState.Paused);
    }

    public void ResumeSession()
    {
        if (State != GameSessionState.Paused)
        {
            return;
        }

        SetState(GameSessionState.Running);
    }

    public void CompleteSession()
    {
        if (State != GameSessionState.Running && State != GameSessionState.Paused)
        {
            return;
        }

        SetState(GameSessionState.Completed);
        OnSessionEnded?.Invoke(new GameSessionSummary(CurrentLevelId, ElapsedSeconds, true));
    }

    public void AbortSession()
    {
        if (State == GameSessionState.Idle)
        {
            return;
        }

        OnSessionEnded?.Invoke(new GameSessionSummary(CurrentLevelId, ElapsedSeconds, false));
        ResetToIdle();
    }

    public void AbortToMain()
    {
        AbortSession();
    }

    public void ResetToIdle()
    {
        CurrentLevelId = string.Empty;
        ElapsedSeconds = 0f;
        SetState(GameSessionState.Idle);
    }

    public void Tick(float deltaTime)
    {
        if (State != GameSessionState.Running || deltaTime <= 0f)
        {
            return;
        }

        ElapsedSeconds += deltaTime;
    }

    private void SetState(GameSessionState newState)
    {
        if (State == newState)
        {
            return;
        }

        GameSessionState previousState = State;
        State = newState;
        OnStateChanged?.Invoke(previousState, newState);
    }
}
