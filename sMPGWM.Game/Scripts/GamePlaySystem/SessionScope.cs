using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.GamePlaySystem;

/// <summary>Owns acquired session resources, including a partially initialized session.</summary>
internal sealed class SessionScope : IDisposable
{
    private readonly Stack<Action> _cleanup = new();
    private readonly Action<Exception> _reportError;
    public bool IsDisposed { get; private set; }

    public SessionScope(Action<Exception> reportError) => _reportError = reportError;

    public void Defer(Action cleanup)
    {
        if (IsDisposed)
        {
            // Cancellation can occur synchronously while an acquisition is publishing events.
            cleanup();
            throw new ObjectDisposedException(nameof(SessionScope));
        }
        _cleanup.Push(cleanup);
    }

    public T Own<T>(T resource) where T : IDisposable
    {
        Defer(resource.Dispose);
        return resource;
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        while (_cleanup.TryPop(out var cleanup))
        {
            try { cleanup(); }
            catch (Exception error) { _reportError(error); }
        }
    }
}
