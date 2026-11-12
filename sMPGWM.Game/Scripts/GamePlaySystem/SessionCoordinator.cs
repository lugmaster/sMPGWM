using System;

namespace sMPGWM.Scripts.GamePlaySystem;

/// <summary>The sole owner of a session lifetime. Presentation reacts to its outcomes.</summary>
internal sealed class SessionCoordinator
{
    private readonly SessionScope _scope;
    private readonly Action _ready;
    private readonly Action<string> _failed;
    public SessionState State { get; private set; }
    public SessionInitialization Initialization { get; }

    public SessionCoordinator(Action ready, Action<string> failed, Action<Exception> cleanupError)
    {
        _ready = ready;
        _failed = failed;
        _scope = new SessionScope(cleanupError);
        Initialization = new SessionInitialization(CompleteLoading, Fail);
    }

    public void Start(Action<SessionScope> initialize)
    {
        if (State != SessionState.Created) return;
        State = SessionState.Starting;
        try { Initialization.Construct(() => initialize(_scope)); }
        catch (Exception error) { Fail($"Session initialization failed: {error.Message}"); }
    }

    public void CompleteLoading()
    {
        if (State != SessionState.Starting || Initialization.Stage != SessionInitializationStage.Ready) return;
        State = SessionState.Active;
        _ready();
    }

    public void Fail(string reason)
    {
        if (State == SessionState.Stopped) return;
        Stop();
        _failed(reason);
    }

    public void Stop()
    {
        if (State == SessionState.Stopped) return;
        // Close the gate before cleanup can trigger disconnect callbacks.
        State = SessionState.Stopped;
        Initialization.Stop();
        _scope.Dispose();
    }
}
