using System;

namespace sMPGWM.Scripts.NetworkSystem;

public enum ClientSynchronizationPhase { AwaitingBaseline, Warming, Predicting, Recovering, Stopped }

/// <summary>Client baseline identity, liveness and recovery transitions, independent of RPCs and scenes.</summary>
public sealed class ClientSynchronizationState(Func<ulong> now)
{
    public ClientSynchronizationPhase Phase { get; private set; } = ClientSynchronizationPhase.AwaitingBaseline;
    public Guid Session { get; private set; }
    public long Request { get; private set; }
    private long _lastSnapshot = -1;
    private ulong _snapshotAt, _lastFrame, _recoveryAt, _lastRequest;
    public bool HasBaseline => Phase is ClientSynchronizationPhase.Warming or ClientSynchronizationPhase.Predicting;
    public bool Recovering => Phase == ClientSynchronizationPhase.Recovering;
    public bool CanPresent => HasBaseline;

    public void Activate() { _lastFrame = now(); _snapshotAt = now(); }
    public bool AcceptsBaseline(Guid session, long request) =>
        Phase == ClientSynchronizationPhase.AwaitingBaseline ||
        (Recovering && session == Session && request == Request);
    public void ApplyBaseline(Guid session, long tick)
    {
        Session = session; _lastSnapshot = tick; _snapshotAt = now();
        Phase = ClientSynchronizationPhase.Warming;
    }
    public bool ObserveSnapshot(Guid session, long tick)
    {
        if (!HasBaseline || session != Session || tick <= _lastSnapshot) return false;
        _lastSnapshot = tick; _snapshotAt = now();
        return true;
    }
    public void FinishWarmup() { if (Phase == ClientSynchronizationPhase.Warming) Phase = ClientSynchronizationPhase.Predicting; }
    public void RestartWarmup() { if (HasBaseline) Phase = ClientSynchronizationPhase.Warming; }
    public bool NeedsRecovery()
    {
        ulong current = now();
        bool stalled = current - _lastFrame > 500;
        _lastFrame = current;
        return HasBaseline && (stalled || current - _snapshotAt > 1000);
    }
    public bool BeginRecovery()
    {
        if (!HasBaseline) return false;
        Phase = ClientSynchronizationPhase.Recovering; Request++;
        _recoveryAt = _lastRequest = now();
        return true;
    }
    public bool TimedOut => Recovering && now() - _recoveryAt > 5000;
    public bool RetryDue()
    {
        if (!Recovering || now() - _lastRequest <= 1000) return false;
        _lastRequest = now(); return true;
    }
    public void Stop() => Phase = ClientSynchronizationPhase.Stopped;
}
