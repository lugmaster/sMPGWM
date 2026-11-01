using System;

namespace sMPGWM.Scripts.Steam;

// The native list signal has no request ID. Drain canceled searches before reusing it.
internal sealed class PendingLobbySearch
{
    private Action<long[]>? _completion;
    internal bool Pending { get; private set; }

    internal bool Submit(Action<long[]> completion, Action request)
    {
        if (Pending) return false;
        Pending = true;
        _completion = completion;
        try { request(); }
        catch
        {
            Pending = false;
            _completion = null;
            throw;
        }
        return true;
    }

    internal void Cancel(Action<long[]> completion)
    {
        if (_completion == completion) _completion = null;
    }

    internal void Complete(long[] lobbies)
    {
        if (!Pending) return;
        var completion = _completion;
        Pending = false;
        _completion = null;
        completion?.Invoke(lobbies);
    }
}
