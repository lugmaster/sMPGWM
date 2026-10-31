using System;

namespace sMPGWM.Scripts.Network;

// One session per main-game scene. A stopped session is never reused.
public sealed class NetworkSession
{
    public SessionState State { get; private set; } = SessionState.Created;
    public string? FailureReason { get; private set; }
    public event Action<SessionState>? Changed;

    public bool TryStart() => Transition(SessionState.Created, SessionState.Starting);
    public bool TryReady() => Transition(SessionState.Starting, SessionState.Ready);
    public bool TryStop(string? failureReason = null)
    {
        if (State is SessionState.Stopping or SessionState.Stopped) return false;
        FailureReason = failureReason;
        SetState(SessionState.Stopping);
        return true;
    }
    public void CompleteStop() => Transition(SessionState.Stopping, SessionState.Stopped);

    private bool Transition(SessionState expected, SessionState next)
    {
        if (State != expected) return false;
        SetState(next);
        return true;
    }
    private void SetState(SessionState state)
    {
        State = state;
        Changed?.Invoke(state);
    }
}
