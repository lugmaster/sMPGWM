using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.Scripts.Network.NetworkInternal;

public abstract partial class BaseNetwork : Node
{
    public event Action<string>? TransportReady;
    public event Action<string>? StartupFailed;
    public event Action<string>? ConnectionLost;
    public NetworkStartupData Startup { get; init; } = null!;
    protected TransportState State { get; private set; }
    private ulong _deadline;
    private readonly List<Action> _disconnectSignals = new();

    protected void Subscribe(Action connect, Action disconnect)
    {
        connect();
        _disconnectSignals.Add(disconnect);
    }

    protected bool Begin()
    {
        if (State != TransportState.Created) return false;
        State = TransportState.Starting;
        _deadline = Time.GetTicksMsec() + (ulong)Math.Max(1, Startup.ConnectionTimeoutMs);
        return true;
    }

    protected void ReadyTransport(string reason = "Connected")
    {
        if (State != TransportState.Starting) return;
        State = TransportState.Ready;
        TransportReady?.Invoke(reason);
    }

    protected void FailTransport(string reason)
    {
        var previous = State;
        if (previous is TransportState.Failed or TransportState.Stopped) return;
        State = TransportState.Failed;
        if (previous == TransportState.Ready) ConnectionLost?.Invoke(reason);
        else StartupFailed?.Invoke(reason);
    }

    protected abstract string TimeoutReason { get; }
    public override void _PhysicsProcess(double delta)
    {
        if (State == TransportState.Starting && Time.GetTicksMsec() >= _deadline)
            FailTransport(TimeoutReason);
    }

    public void Cleanup()
    {
        if (State == TransportState.Stopped) return;
        State = TransportState.Stopped;
        SetPhysicsProcess(false);
        TransportReady = null;
        StartupFailed = null;
        ConnectionLost = null;
        foreach (var disconnect in _disconnectSignals) disconnect();
        _disconnectSignals.Clear();
        ReleaseResources();
    }

    protected abstract void ReleaseResources();
    public override void _ExitTree() => Cleanup();
}
