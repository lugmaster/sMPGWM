using System;

namespace sMPGWM.Scripts.NetworkSystem;

internal sealed class ClientJoinProtocol
{
    private readonly ulong _timeout;
    private string? _token;
    private ulong _deadline;
    public ClientJoinPhase Phase { get; private set; }
    public bool HasWorld => Phase is ClientJoinPhase.AwaitingItems or ClientJoinPhase.AwaitingCompletion or ClientJoinPhase.Ready;
    public bool Expired(ulong now) =>
        (Phase is ClientJoinPhase.AwaitingHello or ClientJoinPhase.AwaitingSnapshot or ClientJoinPhase.AwaitingItems or ClientJoinPhase.AwaitingCompletion)
        && now >= _deadline;
    public ClientJoinProtocol(int timeoutMs) => _timeout = (ulong)Math.Max(1, timeoutMs);

    public bool Begin(string token, ulong now)
    {
        if (Phase != ClientJoinPhase.AwaitingTransport) return false;
        _token = token;
        _deadline = now + _timeout;
        Phase = ClientJoinPhase.AwaitingHello;
        return true;
    }

    public bool AcceptHello(string token, ulong now)
    {
        if (Phase != ClientJoinPhase.AwaitingHello || token != _token || Expired(now)) return false;
        _token = null;
        _deadline = now + _timeout;
        Phase = ClientJoinPhase.AwaitingSnapshot;
        return true;
    }

    public bool ApplySnapshot(ulong now)
    {
        if (Phase != ClientJoinPhase.AwaitingSnapshot || Expired(now)) return false;
        Phase = ClientJoinPhase.AwaitingItems;
        return true;
    }

    public bool ApplyItems(ulong now)
    {
        if (Phase != ClientJoinPhase.AwaitingItems || Expired(now)) return false;
        Phase = ClientJoinPhase.AwaitingCompletion;
        return true;
    }

    public bool Complete(ulong now)
    {
        if (Phase != ClientJoinPhase.AwaitingCompletion || Expired(now)) return false;
        Phase = ClientJoinPhase.Ready;
        _deadline = 0;
        return true;
    }

    public void Stop() { Phase = ClientJoinPhase.Stopped; _deadline = 0; _token = null; }
}
