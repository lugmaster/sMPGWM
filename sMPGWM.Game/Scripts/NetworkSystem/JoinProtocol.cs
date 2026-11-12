using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.NetworkSystem;

internal enum PeerJoinPhase { Connected, Verified, Spawning, Ready, Rejected }

internal sealed class PeerJoin
{
    public PeerJoinPhase Phase { get; private set; } = PeerJoinPhase.Connected;
    public ulong Deadline { get; private set; }
    public bool WasReady { get; private set; }
    public bool ReceivesWorld => Phase is PeerJoinPhase.Spawning or PeerJoinPhase.Ready;
    public PeerJoin(ulong deadline) => Deadline = deadline;
    public bool Expired(ulong now) => Phase != PeerJoinPhase.Ready && now >= Deadline;

    public bool Advance(PeerJoinPhase expected, PeerJoinPhase next, ulong now, ulong deadline)
    {
        if (Phase != expected || Expired(now)) return false;
        if ((expected, next) is not ((PeerJoinPhase.Connected, PeerJoinPhase.Verified) or
            (PeerJoinPhase.Verified, PeerJoinPhase.Spawning) or (PeerJoinPhase.Spawning, PeerJoinPhase.Ready)))
            return false;
        Phase = next;
        Deadline = next == PeerJoinPhase.Ready ? 0 : deadline;
        WasReady = next == PeerJoinPhase.Ready;
        return true;
    }

    public bool Reject(ulong now)
    {
        if (Phase == PeerJoinPhase.Rejected) return false;
        Phase = PeerJoinPhase.Rejected;
        // Allow reliable rejection delivery before disconnecting the transport.
        Deadline = now + 500;
        return true;
    }
}

internal sealed class HostJoinProtocol
{
    private readonly Dictionary<int, PeerJoin> _peers = new();
    private readonly ulong _timeout;
    private KeyValuePair<int, PeerJoin>[]? _snapshot;
    public HostJoinProtocol(int timeoutMs) => _timeout = (ulong)Math.Max(1, timeoutMs);
    // Membership is stable during iteration; PeerJoin objects retain their live phase state.
    public KeyValuePair<int, PeerJoin>[] Snapshot()
    {
        if (_snapshot != null) return _snapshot;
        if (_peers.Count == 0) return _snapshot = Array.Empty<KeyValuePair<int, PeerJoin>>();
        var snapshot = new KeyValuePair<int, PeerJoin>[_peers.Count];
        var index = 0;
        foreach (var peer in _peers) snapshot[index++] = peer;
        return _snapshot = snapshot;
    }
    public bool Begin(int peer, ulong now)
    {
        if (peer <= 1 || _peers.ContainsKey(peer)) return false;
        _peers.Add(peer, new PeerJoin(now + _timeout));
        _snapshot = null;
        return true;
    }
    public bool Is(int peer, PeerJoinPhase phase) => _peers.TryGetValue(peer, out var join) && join.Phase == phase;
    public bool Expired(int peer, ulong now) => _peers.TryGetValue(peer, out var join) && join.Expired(now);
    public bool Advance(int peer, PeerJoinPhase expected, PeerJoinPhase next, ulong now) =>
        _peers.TryGetValue(peer, out var join) && join.Advance(expected, next, now, now + _timeout);
    public bool Reject(int peer, ulong now) => _peers.TryGetValue(peer, out var join) && join.Reject(now);
    public bool Remove(int peer, out bool wasReady)
    {
        wasReady = false;
        if (!_peers.Remove(peer, out var join)) return false;
        _snapshot = null;
        wasReady = join.WasReady;
        return true;
    }
    public void Clear()
    {
        _peers.Clear();
        _snapshot = null;
    }
}

internal enum ClientJoinPhase { AwaitingTransport, AwaitingHello, AwaitingSnapshot, AwaitingCompletion, Ready, Stopped }

internal sealed class ClientJoinProtocol
{
    private readonly ulong _timeout;
    private string? _token;
    private ulong _deadline;
    public ClientJoinPhase Phase { get; private set; }
    public bool HasWorld => Phase is ClientJoinPhase.AwaitingCompletion or ClientJoinPhase.Ready;
    public bool Expired(ulong now) =>
        (Phase is ClientJoinPhase.AwaitingHello or ClientJoinPhase.AwaitingSnapshot or ClientJoinPhase.AwaitingCompletion)
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
