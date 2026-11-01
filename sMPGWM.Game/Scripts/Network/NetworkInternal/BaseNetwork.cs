using System;
using System.Collections.Generic;
using Godot;

namespace sMPGWM.Scripts.Network.NetworkInternal;

public abstract partial class BaseNetwork : Node
{
    public event Action? TransportReady;
    public event Action<string>? Failed;
    public NetworkStartupData Startup { get; init; } = null!;
    protected MultiplayerPeer? Peer;
    protected bool Stopped { get; private set; }
    protected bool Starting => !Stopped && _deadline != 0;
    private bool _started;
    private ulong _deadline;
    private readonly List<Action> _disconnectSignals = new();

    protected void Subscribe(Action connect, Action disconnect)
    {
        connect();
        _disconnectSignals.Add(disconnect);
    }

    public void Start()
    {
        if (_started || Stopped) return;
        _started = true;
        _deadline = Time.GetTicksMsec() + (ulong)Math.Max(1, Startup.ConnectionTimeoutMs);
        if (Startup.MaxPlayers is < 1 or > 10)
        {
            FailTransport("Hosting requires 1-10 player capacity.");
            return;
        }
        StartTransport();
    }

    protected abstract void StartTransport();

    protected void ReadyTransport()
    {
        if (!Starting) return;
        _deadline = 0;
        TransportReady?.Invoke();
    }

    protected void FailTransport(string reason)
    {
        if (Stopped) return;
        Failed?.Invoke(reason);
        Cleanup();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Starting && Time.GetTicksMsec() >= _deadline) FailTransport("Connection timed out.");
    }

    public void Cleanup()
    {
        if (Stopped) return;
        Stopped = true;
        SetPhysicsProcess(false);
        TransportReady = null;
        Failed = null;
        foreach (var disconnect in _disconnectSignals) disconnect();
        _disconnectSignals.Clear();
        try { ReleaseResources(); }
        finally
        {
            if (Peer != null)
            {
                if (Multiplayer.MultiplayerPeer == Peer) Multiplayer.MultiplayerPeer = null;
                Peer.Close();
                Peer.Dispose();
                Peer = null;
            }
        }
    }

    protected virtual void ReleaseResources() { }
    public override void _ExitTree() => Cleanup();
}
