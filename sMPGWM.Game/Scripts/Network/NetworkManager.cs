using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.Scripts.Network.NetworkInternal;
using sMPGWM.scripts.Network.NetworkInternal;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network;

public partial class NetworkManager : AbstractSingleton<NetworkManager>
{
    private BaseNetwork? _activeNetwork;
    private NetworkStartupData? _startup;
    private bool _stopped;
    private bool _previousObjectDecoding;
    private bool _configuredApi;
    private Callable _previousAuthCallback;
    private double _previousAuthTimeout;
    private byte[] _authPayload = Array.Empty<byte>();
    private string? _helloToken;
    private ulong _replyDeadline;
    private readonly HashSet<int> _answered = new();
    private enum JoinState { Connected, Verified, Spawning, Ready }
    private readonly Dictionary<int, JoinState> _joins = new();
    private readonly Dictionary<int, ulong> _joinDeadlines = new();
    private bool _spawnRequested;
    private bool _spawnApplied;
    private ulong JoinDeadline() => Time.GetTicksMsec() + (ulong)Math.Max(1, _startup!.ConnectionTimeoutMs);

    protected override void OnReady()
    {
        _startup = GameFlowManager.Instance.ConsumeNetworkStartup();
        // World and gameplay children finish readiness before networking starts.
        CallDeferred(nameof(CreateNetwork));
    }

    public void CreateNetwork()
    {
        if (_activeNetwork != null || _stopped || _startup == null) return;
        if (!Enum.IsDefined(_startup.Role) || !Enum.IsDefined(_startup.Type))
        {
            FailSession("Invalid network role or transport.");
            return;
        }
        try
        {
            var api = (SceneMultiplayer)Multiplayer;
            _previousObjectDecoding = api.AllowObjectDecoding;
            _previousAuthCallback = api.AuthCallback;
            _previousAuthTimeout = api.AuthTimeout;
            _configuredApi = true;
            _authPayload = Compatibility.Payload();
            api.AllowObjectDecoding = false;
            api.AuthTimeout = Math.Max(1, _startup.ConnectionTimeoutMs) / 1000.0;
            api.AuthCallback = Callable.From<long, byte[]>(ReceiveAuthentication);
            api.PeerAuthenticating += AuthenticatePeer;
            api.PeerAuthenticationFailed += AuthenticationFailed;
            api.PeerDisconnected += PeerLeft;
            api.PeerConnected += PeerJoined;
            _activeNetwork = _startup.Type == NetworkType.Lan
                ? new EnetNetwork { Startup = _startup }
                : new SteamNetwork { Startup = _startup };
            _activeNetwork.TransportReady += OnNetworkConnected;
            _activeNetwork.Failed += FailSession;
            AddChild(_activeNetwork);
            AddChild(new NetworkTicker());
            var heartbeat = new HeartbeatMonitor { Name = "HeartbeatMonitor" };
            AddChild(heartbeat);
            heartbeat.Initialize(_startup.Role == NetworkRole.Host);
            _activeNetwork.Start();
        }
        catch (Exception e) { FailSession($"Network startup failed: {e.Message}"); }
    }

    private void AuthenticatePeer(long peer)
    {
        if (_stopped) return;
        var api = (SceneMultiplayer)Multiplayer;
        if (_startup!.Role == NetworkRole.Host &&
            api.GetPeers().Length + api.GetAuthenticatingPeers().Length >= _startup!.MaxPlayers)
        {
            api.DisconnectPeer(checked((int)peer));
            return;
        }
        var result = api.SendAuth(checked((int)peer), _authPayload);
        Logger.Info($"NET00 auth_send peer={peer} result={result}");
        if (result != Error.Ok) api.DisconnectPeer(checked((int)peer));
    }

    private void ReceiveAuthentication(long peer, byte[] data)
    {
        if (_stopped) return;
        var api = (SceneMultiplayer)Multiplayer;
        if (!Compatibility.Accept(data) || (_startup!.Role == NetworkRole.Client && peer != 1))
        {
            Logger.Warning($"NET00 auth_rejected peer={peer} bytes={data.Length}");
            api.DisconnectPeer(checked((int)peer));
            return;
        }
        var result = api.CompleteAuth(checked((int)peer));
        Logger.Info($"NET00 auth_complete peer={peer} result={result}");
        if (result != Error.Ok) api.DisconnectPeer(checked((int)peer));
    }

    private void AuthenticationFailed(long peer)
    {
        Logger.Warning($"NET00 auth_failed peer={peer}");
        if (!_stopped && _startup!.Role == NetworkRole.Client)
            FailSession("Native protocol preflight failed or timed out.");
    }

    private void OnNetworkConnected()
    {
        if (_stopped) return;
        if (_startup!.Role == NetworkRole.Host)
        {
            Logger.Info("Network Host: Listening");
            MpManager.Instance.Spawn(1, _startup.Player);
            GameFlowManager.Instance.CompleteLoading();
            return;
        }
        if (_helloToken != null) return;
        _helloToken = Guid.NewGuid().ToString("N");
        _replyDeadline = Time.GetTicksMsec() + (ulong)Math.Max(1, _startup.ConnectionTimeoutMs);
        Logger.Info("Network Client: waiting for host reply");
        var result = RpcId(1, nameof(Hello), _helloToken);
        if (result != Error.Ok) FailSession($"Hello send failed: {result}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Hello(string token)
    {
        var sender = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !Guid.TryParseExact(token, "N", out _) ||
            Array.IndexOf(Multiplayer.GetPeers(), sender) < 0 || !_answered.Add(sender)) return;
        _joins[sender] = JoinState.Verified;
        _joinDeadlines[sender] = JoinDeadline();
        var result = RpcId(sender, nameof(HelloReply), token);
        if (result != Error.Ok)
        {
            Logger.Warning($"Hello reply failed for peer {sender}: {result}");
            ((SceneMultiplayer)Multiplayer).DisconnectPeer(sender);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void HelloReply(string token)
    {
        if (_stopped || _helloToken == null || Multiplayer.GetRemoteSenderId() != 1 ||
            token != _helloToken || Time.GetTicksMsec() >= _replyDeadline) return;
        _helloToken = null;
        _replyDeadline = JoinDeadline();
        Logger.Info("Network Client: host round trip complete");
        _spawnRequested = true;
        Logger.Info($"NET spawn_request message='{_startup!.Player.Message}'");
        if (RpcId(1, nameof(RequestSpawn), _startup.Player.ToVariant()) != Error.Ok)
            FailSession("Spawn request failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestSpawn(Variant payload)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !_joins.TryGetValue(peer, out var state) || state != JoinState.Verified) return;
        if (!PlayerStartupData.TryFromVariant(payload, out var data))
        {
            RejectJoin(peer, "Invalid player startup payload.");
            return;
        }
        try
        {
            MpManager.Instance.Spawn(peer, data);
            // All spawn messages share the reliable channel: snapshot, later additions, then completion.
            var snapshot = new Godot.Collections.Dictionary<int, string>();
            foreach (var entry in MpManager.Instance.StartupData) snapshot.Add(entry.Key, entry.Value.Message);
            if (RpcId(peer, nameof(ReceiveInitialSpawns), snapshot) != Error.Ok)
            {
                RejectJoin(peer, "Initial spawn send failed.");
                return;
            }
            _joins[peer] = JoinState.Spawning;
            _joinDeadlines[peer] = JoinDeadline();
            foreach (var other in new List<int>(_joins.Keys))
                if (other != peer && _joins[other] is JoinState.Spawning or JoinState.Ready)
                    if (RpcId(other, nameof(ReceiveSpawn), peer, data.ToVariant()) != Error.Ok)
                        RejectJoin(other, "Spawn announcement failed.");
        }
        catch (Exception e) { RejectJoin(peer, $"Spawning failed: {e.Message}"); }
    }

    private bool IsHostMessage() => !_stopped && _startup?.Role == NetworkRole.Client &&
        Multiplayer.GetRemoteSenderId() == 1;

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveInitialSpawns(Godot.Collections.Dictionary<int, string> snapshot)
    {
        if (!IsHostMessage() || !_spawnRequested || _spawnApplied) return;
        var localPeer = Multiplayer.GetUniqueId();
        if (!snapshot.ContainsKey(1) || !snapshot.TryGetValue(localPeer, out var ownMessage) ||
            ownMessage != _startup!.Player.Message || snapshot.Count > 10)
        {
            FailSession("Invalid initial spawn snapshot.");
            return;
        }
        try
        {
            foreach (var entry in snapshot)
            {
                if (entry.Key <= 0 || !PlayerStartupData.TryFromVariant(entry.Value, out var data))
                    throw new InvalidOperationException("Invalid spawn record.");
                MpManager.Instance.Spawn(entry.Key, data);
            }
            _spawnApplied = true;
            if (RpcId(1, nameof(ConfirmSpawnReady)) != Error.Ok) FailSession("Spawn acknowledgement failed.");
        }
        catch (Exception e) { FailSession($"Applying initial spawns failed: {e.Message}"); }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveSpawn(int peer, Variant payload)
    {
        if (!IsHostMessage() || !_spawnApplied) return;
        try
        {
            if (peer <= 0 || !PlayerStartupData.TryFromVariant(payload, out var data))
                throw new InvalidOperationException("Invalid spawn record.");
            MpManager.Instance.Spawn(peer, data);
        }
        catch (Exception e) { FailSession($"Applying spawn failed: {e.Message}"); }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ConfirmSpawnReady()
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !_joins.TryGetValue(peer, out var state) || state != JoinState.Spawning) return;
        if (RpcId(peer, nameof(JoinComplete), GameConfig.LobbyName) != Error.Ok)
        {
            RejectJoin(peer, "Join completion send failed.");
            return;
        }
        _joins[peer] = JoinState.Ready;
        _joinDeadlines.Remove(peer);
        MpManager.Instance.AcceptReady(peer);
        Logger.Info($"NET join_ready peer={peer}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void JoinComplete(string gameName)
    {
        if (!IsHostMessage() || !_spawnRequested || !_spawnApplied || Time.GetTicksMsec() >= _replyDeadline) return;
        _spawnRequested = false;
        HeartbeatMonitor.Instance.ActivateClient();
        _replyDeadline = 0;
        Logger.Info("NET local spawn complete; entering game");
        GameFlowManager.Instance.CompleteLoading();
        MpManager.Instance.NotifyLocalJoin(gameName);
    }

    private void PeerJoined(long peer)
    {
        if (!IsHost()) return;
        MpManager.Instance.BeginPeer((int)peer);
        _joins[(int)peer] = JoinState.Connected;
        _joinDeadlines[(int)peer] = JoinDeadline();
    }

    private void RejectJoin(int peer, string reason)
    {
        Logger.Warning($"NET join_rejected peer={peer}: {reason}");
        MpManager.Instance.Depart(peer, reason);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_stopped && _replyDeadline != 0 && Time.GetTicksMsec() >= _replyDeadline)
            FailSession(_spawnRequested ? "Player spawning timed out." : "Host reply timed out.");
        if (!IsHost()) return;
        foreach (var peer in new List<int>(_joinDeadlines.Keys))
            if (_joinDeadlines.TryGetValue(peer, out var deadline) && Time.GetTicksMsec() >= deadline)
                RejectJoin(peer, "Player join timed out.");
    }

    private void PeerLeft(long peer)
    {
        if (_stopped) return;
        if (IsHost()) MpManager.Instance.Depart((int)peer, "Disconnected");
        else if (peer == 1) MpManager.Instance.HostLost("Host disconnected");
    }

    public void RemovePeer(int peer)
    {
        _answered.Remove(peer);
        _joins.Remove(peer);
        _joinDeadlines.Remove(peer);
        if (Array.IndexOf(Multiplayer.GetPeers(), peer) >= 0)
            ((SceneMultiplayer)Multiplayer).DisconnectPeer(peer);
        MpManager.Instance.Despawn(peer);
        foreach (var other in _joins)
            if (other.Value is JoinState.Spawning or JoinState.Ready)
                RpcId(other.Key, nameof(ReceiveDespawn), peer);
    }

    public void AnnounceMembership(int peer, bool joined, string reason)
    {
        foreach (var other in _joins)
            if (other.Key != peer && other.Value == JoinState.Ready)
                RpcId(other.Key, nameof(ReceiveMembership), peer, joined, reason);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveMembership(int peer, bool joined, string reason)
    {
        if (IsHostMessage() && !_spawnRequested && _spawnApplied)
            MpManager.Instance.NotifyMembership(peer, joined, reason);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveDespawn(int peer)
    {
        if (IsHostMessage() && _spawnApplied) MpManager.Instance.Despawn(peer);
    }

    public void FailSession(string reason)
    {
        if (_stopped) return;
        MpManager.Instance.HostLost(reason);
    }

    public void StopSession()
    {
        if (_stopped) return;
        _stopped = true;
        HeartbeatMonitor.Instance?.Stop();
        MpManager.Instance?.StopMembership();
        GameFlowManager.Instance?.BeginDisconnecting();
        ProcessMode = ProcessModeEnum.Disabled;
        _helloToken = null;
        _replyDeadline = 0;
        _answered.Clear();
        _joins.Clear();
        _joinDeadlines.Clear();
        _spawnRequested = false;
        _spawnApplied = false;
        MpManager.Instance?.Clear();
        try
        {
            _activeNetwork?.Cleanup();
        }
        catch (Exception e)
        {
            Logger.Error($"Network cleanup failed: {e.Message}");
        }
        finally
        {
            if (_configuredApi && Multiplayer is SceneMultiplayer api)
            {
                api.PeerAuthenticating -= AuthenticatePeer;
                api.PeerAuthenticationFailed -= AuthenticationFailed;
                api.PeerDisconnected -= PeerLeft;
                api.PeerConnected -= PeerJoined;
                api.AuthCallback = _previousAuthCallback;
                api.AuthTimeout = _previousAuthTimeout;
                api.AllowObjectDecoding = _previousObjectDecoding;
                _configuredApi = false;
            }
        }
        Logger.Info("Network session stopped");
    }

    protected override void OnExitTree() => StopSession();
    public bool IsHost() => !_stopped && _startup?.Role == NetworkRole.Host && IsConnected();
    public bool IsConnected() => !_stopped && Multiplayer.HasMultiplayerPeer() &&
        Multiplayer.MultiplayerPeer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
}
