using System;

namespace sMPGWM.Scripts.SteamSystem;

public sealed class PendingLobbyOperation
{
    private Action<long, ulong>? _completion;
    public bool Pending { get; private set; }
    public ulong Target { get; private set; }

    public bool Submit(ulong target, Action<long, ulong> completion, Action request)
    {
        if (!Begin(target, completion)) return false;
        try
        {
            request();
            return true;
        }
        catch
        {
            // No asynchronous request was submitted, so there is nothing to drain.
            _completion = null;
            Pending = false;
            Target = 0;
            throw;
        }
    }

    public bool Begin(ulong target, Action<long, ulong> completion)
    {
        if (Pending) return false;
        Pending = true;
        Target = target;
        _completion = completion;
        return true;
    }

    public void Cancel(Action<long, ulong> completion)
    {
        if (_completion == completion) _completion = null;
    }

    public void Complete(long result, ulong lobby, Action<ulong> leave)
    {
        if (!Pending || (Target != 0 && Target != lobby)) return;
        var completion = _completion;
        _completion = null;
        Pending = false;
        Target = 0;
        if (completion == null)
        {
            if (result == 1) leave(lobby);
            return;
        }
        completion(result, lobby);
    }
}
