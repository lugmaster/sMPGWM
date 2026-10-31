using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.scripts.Network;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Network;

public partial class SessionProtocolNode : Node
{
    public string HostSession { get; } = Guid.NewGuid().ToString("N");
    public event Action<DiagnosticResult>? Completed;
    private readonly DiagnosticExchange _exchange = new();
    private readonly HashSet<int> _answered = new(10);
    private ulong _deadline;
    private bool _waiting;
    private bool _started;
    private bool _stopped;
    private bool _subscribed;

    public override void _Ready()
    {
        Multiplayer.PeerDisconnected += PeerLeft;
        _subscribed = true;
    }
    private void PeerLeft(long peer) => _answered.Remove((int)peer);

    public void SendHello()
    {
        if (_started || _stopped) return;
        _started = true;
        _waiting = true;
        _deadline = Time.GetTicksMsec() + 10000;
        Logger.Info($"Client sender={(Multiplayer.HasMultiplayerPeer() ? Multiplayer.GetUniqueId() : 0)} DiagnosticHello correlation={_exchange.Correlation} payload={_exchange.Payload}");
        var result = RpcId(1, nameof(DiagnosticHello), DiagnosticExchange.ProtocolVersion, _exchange.Correlation, _exchange.Payload);
        if (result != Error.Ok)
        {
            Logger.Warning($"Diagnostic send failed: {result}");
            Finish(false, $"Diagnostic send failed: {result}", "");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DiagnosticHello(int version, string correlation, string payload)
    {
        var sender = Multiplayer.GetRemoteSenderId();
        if (_stopped || !NetworkManager.Instance.IsHost() || !DiagnosticExchange.IsValid(version, correlation, payload)) return;
        var member = false;
        foreach (var peer in Multiplayer.GetPeers()) if (peer == sender) member = true;
        if (!member || _answered.Count >= 9 || !_answered.Add(sender)) return;
        Logger.Info($"Host sender={sender} DiagnosticHello correlation={correlation} payload={payload} session={HostSession} tick={NetworkTicker.GetTicksSinceStart()}");
        var result = RpcId(sender, nameof(DiagnosticReply), version, correlation, payload, HostSession);
        if (result != Error.Ok) Logger.Error($"Diagnostic reply failed: {result}");
        else Logger.Info($"Host sender=1 DiagnosticReply correlation={correlation} payload={payload} session={HostSession}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DiagnosticReply(int version, string correlation, string payload, string session)
    {
        var sender = Multiplayer.GetRemoteSenderId();
        if (_stopped || !_waiting || Time.GetTicksMsec() >= _deadline ||
            !_exchange.Accept(sender, version, correlation, payload, session)) return;
        Logger.Info($"Client sender={sender} DiagnosticReply correlation={correlation} payload={payload} session={session}");
        Finish(true, "MessageExchangeSucceeded", session);
    }

    private void Finish(bool success, string reason, string session)
    {
        _waiting = false;
        _exchange.Cancel();
        Completed?.Invoke(new DiagnosticResult(success, reason, _exchange.Correlation, session));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_waiting && Time.GetTicksMsec() >= _deadline) Finish(false, "Diagnostic reply timeout.", "");
    }

    public void Stop()
    {
        if (_subscribed)
        {
            Multiplayer.PeerDisconnected -= PeerLeft;
            _subscribed = false;
        }
        _stopped = true;
        _waiting = false;
        _exchange.Cancel();
        _answered.Clear();
        Completed = null;
        SetPhysicsProcess(false);
    }

    public override void _ExitTree()
    {
        Stop();
    }
}
