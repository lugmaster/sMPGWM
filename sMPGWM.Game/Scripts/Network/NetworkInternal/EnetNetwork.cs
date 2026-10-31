using System;
using System.Net;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;

namespace sMPGWM.scripts.Network.NetworkInternal;

public partial class EnetNetwork : BaseNetwork, IActiveNetwork
{
    private ENetMultiplayerPeer? _peer;
    public NetworkStartupData? Startup { get; init; }
    private bool _finished;
    private bool _cleaned;
    private bool _subscribed;
    private void Report(bool success, string reason = "Connected")
    {
        if (_cleaned || _finished) return;
        _finished = true;
        EmitConnected(success, reason);
    }
    private bool _started;
    private bool _waiting;
    private ulong _deadline;

    public void BecomeHost(int maxConnections)
    {
        if (_started) return;
        _started = true;
        if (maxConnections < 1 || maxConnections > 10)
        {
            Report(false, "ENet diagnostic hosting requires 1-10 player capacity.");
            return;
        }
        _peer = new ENetMultiplayerPeer();
        var result = _peer.CreateServer(Startup?.Port ?? GameConfig.Port, Math.Max(1, maxConnections - 1));
        if (result != Error.Ok) { Report(false, $"CreateServer: {result}"); return; }
        _peer.RefuseNewConnections = maxConnections == 1;
        Multiplayer.MultiplayerPeer = _peer;
        Report(true, "Listening");
    }

    public void BecomeClient(ulong lobbyId = 0)
    {
        if (_started) return;
        _started = true;
        if (!IPAddress.TryParse(Startup?.Address ?? GameConfig.ServerIpAddress, out _) || (Startup?.Port ?? GameConfig.Port) is < 1 or > 65535)
        {
            Report(false, "Invalid LAN endpoint.");
            return;
        }
        _subscribed = true;
        Multiplayer.ConnectedToServer += Connected;
        Multiplayer.ConnectionFailed += Failed;
        Multiplayer.ServerDisconnected += Disconnected;
        _peer = new ENetMultiplayerPeer();
        var result = _peer.CreateClient(Startup?.Address ?? GameConfig.ServerIpAddress, Startup?.Port ?? GameConfig.Port);
        if (result != Error.Ok) { Report(false, $"CreateClient: {result}"); return; }
        _waiting = true;
        _deadline = Time.GetTicksMsec() + (ulong)Math.Max(1, Startup?.ConnectionTimeoutMs ?? ConnectionTimeout);
        Multiplayer.MultiplayerPeer = _peer;
    }

    private void Connected() { if (!_waiting) return; _waiting = false; Report(true); }
    private void Failed() { _waiting = false; Report(false, "ENet connection failed or disconnected."); }
    private void Disconnected() { if (!_cleaned) EmitConnected(false, "Server disconnected."); }
    public override void _PhysicsProcess(double delta)
    {
        if (_waiting && Time.GetTicksMsec() >= _deadline) Failed();
    }
    public bool IsConnected() => GetConnectionStatus() == ConnectionStatus.Connected;
    public ConnectionStatus GetConnectionStatus() => _peer?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    public Node GetNode() => this;
    public override void Cleanup()
    {
        if (_cleaned) return;
        _cleaned = true;
        _waiting = false;
        if (_subscribed)
        {
            _subscribed = false;
            Multiplayer.ConnectedToServer -= Connected;
            Multiplayer.ConnectionFailed -= Failed;
            Multiplayer.ServerDisconnected -= Disconnected;
        }
        base.Cleanup();
        if (_peer == null) return;
        if (Multiplayer.MultiplayerPeer == _peer) Multiplayer.MultiplayerPeer = null;
        _peer.Close();
        _peer.Dispose();
        _peer = null;
    }
}
