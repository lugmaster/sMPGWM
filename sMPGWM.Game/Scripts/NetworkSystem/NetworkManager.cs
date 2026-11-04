using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.NetworkSystem.NetworkInternal;
using sMPGWM.Scripts.Util;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.World;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager : AbstractSingleton<NetworkManager>
{
    private BaseNetwork? _activeNetwork;
    private NetworkStartupData? _startup;
    private bool _stopped;
    private bool _previousObjectDecoding;
    private bool _configuredApi;
    private MultiplayerPeer? _previousPeer;
    private Callable _previousAuthCallback;
    private double _previousAuthTimeout;
    private byte[] _authPayload = Array.Empty<byte>();
    private HostJoinProtocol _joins = null!;
    private ClientJoinProtocol _clientJoin = null!;
    private HeartbeatMonitor? _heartbeat;
    public SynchronizationCoordinator? Synchronization { get; set; }
    public GameWorld World { get; init; } = null!;
    public NetworkStartupData Startup { get; init; } = null!;
    public event Action<string>? Failed;
    public event Action? SessionReady;
    public event Action<int, bool, string>? MembershipChanged;
    public event Action<string>? LocalJoined;

    protected override void OnReady()
    {
        _startup = Startup;
        _joins = new HostJoinProtocol(Startup.ConnectionTimeoutMs);
        _clientJoin = new ClientJoinProtocol(Startup.ConnectionTimeoutMs);
        // The composition root explicitly starts networking after gameplay initialization.
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
            _previousPeer = api.MultiplayerPeer;
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

            _heartbeat = new HeartbeatMonitor { Name = "HeartbeatMonitor" };
            _heartbeat.Failed += FailSession;
            _heartbeat.PeerTimedOut += DepartPeer;
            AddChild(_heartbeat);
            _heartbeat.Initialize(_startup.Role == NetworkRole.Host);
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
            try
            {
                World.ApproveSpawn(1, _startup.Player);
                SessionReady?.Invoke();
            }
            catch (Exception error) { FailSession($"Host spawning failed: {error.Message}"); }
            return;
        }
        var token = Guid.NewGuid().ToString("N");
        if (!_clientJoin.Begin(token, Time.GetTicksMsec())) return;
        Logger.Info("Network Client: waiting for host reply");
        var result = RpcId(1, nameof(Hello), token);
        if (result != Error.Ok) FailSession($"Hello send failed: {result}");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Hello(string token)
    {
        var sender = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !Guid.TryParseExact(token, "N", out _) ||
            Array.IndexOf(Multiplayer.GetPeers(), sender) < 0 ||
            !_joins.Advance(sender, PeerJoinPhase.Connected, PeerJoinPhase.Verified, Time.GetTicksMsec())) return;
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
        if (!IsHostMessage() || !_clientJoin.AcceptHello(token, Time.GetTicksMsec())) return;
        Logger.Info("Network Client: host round trip complete");
        Logger.Info($"NET spawn_request message='{_startup!.Player.ShipId}'");
        if (RpcId(1, nameof(RequestSpawn), _startup.Player.ToVariant()) != Error.Ok)
            FailSession("Spawn request failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestSpawn(Variant payload)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !_joins.Is(peer, PeerJoinPhase.Verified)) return;
        if (_joins.Expired(peer, Time.GetTicksMsec()))
        {
            RejectJoin(peer, "Player join timed out.");
            return;
        }
        if (!PlayerStartupData.TryFromVariant(payload, out var data) || !ShipRegistry.TryGetById(data.ShipId, out _))
        {
            RejectJoin(peer, "Invalid player startup payload.");
            return;
        }
        try
        {
            World.ApproveSpawn(peer, data);
            var snapshot = World.CaptureTopology().ToVariant();
            if (RpcId(peer, nameof(ReceiveInitialSpawns), snapshot, Synchronization!.Baseline()) != Error.Ok)
            {
                RejectJoin(peer, "Initial spawn send failed.");
                return;
            }
            if (!_joins.Advance(peer, PeerJoinPhase.Verified, PeerJoinPhase.Spawning, Time.GetTicksMsec()))
            {
                RejectJoin(peer, "Player join timed out.");
                return;
            }
            foreach (var other in _joins.Snapshot())
                if (other.Key != peer && other.Value.ReceivesWorld)
                    if (RpcId(other.Key, nameof(ReceiveWorldTopology), Synchronization!.Session.ToString("N"), snapshot) != Error.Ok)
                        RejectJoin(other.Key, "Spawn announcement failed.");
        }
        catch (Exception e) { FailSession($"Spawning failed: {e.Message}"); }
    }

    private bool IsHostMessage() => !_stopped && _startup?.Role == NetworkRole.Client &&
        Multiplayer.GetRemoteSenderId() == 1;

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveInitialSpawns(Godot.Collections.Array snapshot, byte[] movement)
    {
        if (!IsHostMessage() || _clientJoin.Phase != ClientJoinPhase.AwaitingSnapshot || _clientJoin.Expired(Time.GetTicksMsec())) return;
        try
        {
            if (!WorldTopology.TryRead(snapshot, out var topology) || !topology.Participants.Any(p => p.Peer == 1))
                throw new InvalidOperationException("Invalid initial world topology.");
            var own = topology.Participants.SingleOrDefault(p => p.Peer == Multiplayer.GetUniqueId());
            if (own == null || !topology.Entities.Any(e => e.EntityId == own.Entity && e.ShipId == _startup!.Player.ShipId))
                throw new InvalidOperationException("Missing local ship.");
            World.ApplyTopology(topology);
            if (World.LocalPlayer?.Participant?.ControlledShip == null)
                throw new InvalidOperationException("Local controls were not bound.");
            if (Synchronization == null || !Synchronization.Receive(movement, true))
                throw new InvalidOperationException("Invalid initial movement baseline.");
            if (!_clientJoin.ApplySnapshot(Time.GetTicksMsec()))
                throw new InvalidOperationException("Player spawning timed out.");
            if (RpcId(1, nameof(ConfirmSpawnReady)) != Error.Ok) FailSession("Spawn acknowledgement failed.");
        }
        catch (Exception e) { FailSession($"Applying initial spawns failed: {e.Message}"); }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ConfirmSpawnReady()
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !_joins.Is(peer, PeerJoinPhase.Spawning)) return;
        var now = Time.GetTicksMsec();
        if (_joins.Expired(peer, now))
        {
            RejectJoin(peer, "Player join timed out.");
            return;
        }
        if (RpcId(peer, nameof(JoinComplete), GameConfig.LobbyName) != Error.Ok)
        {
            RejectJoin(peer, "Join completion send failed.");
            return;
        }
        if (!_joins.Advance(peer, PeerJoinPhase.Spawning, PeerJoinPhase.Ready, now))
        {
            RejectJoin(peer, "Player join timed out.");
            return;
        }
        _heartbeat!.Track(peer);
        MembershipChanged?.Invoke(peer, true, "");
        AnnounceMembership(peer, true, "");
        Logger.Info($"NET join_ready peer={peer}");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void JoinComplete(string gameName)
    {
        if (!IsHostMessage() || !_clientJoin.Complete(Time.GetTicksMsec())) return;
        _heartbeat!.ActivateClient();
        Logger.Info("NET local spawn complete; entering game");
        SessionReady?.Invoke();
        LocalJoined?.Invoke(gameName);
    }

    private void PeerJoined(long peer)
    {
        if (!IsHost()) return;
        _joins.Begin((int)peer, Time.GetTicksMsec());
    }

    private void RejectJoin(int peer, string reason)
    {
        Logger.Warning($"NET join_rejected peer={peer}: {reason}");
        if (!_joins.Reject(peer, Time.GetTicksMsec())) return;
        _heartbeat?.Remove(peer);
        RpcId(peer, nameof(JoinRejected), reason);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void JoinRejected(string reason)
    {
        if (IsHostMessage()) FailSession(reason);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_stopped) return;
        var now = Time.GetTicksMsec();
        if (_clientJoin.Expired(now))
            FailSession(_clientJoin.Phase == ClientJoinPhase.AwaitingHello ? "Host reply timed out." : "Player spawning timed out.");
        if (!IsHost()) return;
        foreach (var (peer, join) in _joins.Snapshot())
        {
            if (!join.Expired(now)) continue;
            if (join.Phase == PeerJoinPhase.Rejected) DepartPeer(peer, "Join rejected");
            else RejectJoin(peer, "Player join timed out.");
        }
    }

    private void PeerLeft(long peer)
    {
        if (_stopped) return;
        if (IsHost()) DepartPeer((int)peer, "Disconnected");
        else if (peer == 1) FailSession("Host disconnected");
    }

    private void DepartPeer(int peer, string reason)
    {
        // Remove the protocol entry first so synchronous disconnect callbacks are harmless.
        if (_stopped || !_joins.Remove(peer, out var wasReady)) return;
        _heartbeat?.Remove(peer);
        if (Array.IndexOf(Multiplayer.GetPeers(), peer) >= 0)
            ((SceneMultiplayer)Multiplayer).DisconnectPeer(peer);
        _baselineRequests.Remove(peer); _timingRequests.Remove(peer);
        World.Despawn(peer);
        PublishWorldTopology();
        if (!wasReady) return;
        MembershipChanged?.Invoke(peer, false, reason);
        AnnounceMembership(peer, false, reason);
    }

    private void AnnounceMembership(int peer, bool joined, string reason)
    {
        foreach (var other in _joins.Snapshot())
            if (other.Key != peer && other.Value.Phase == PeerJoinPhase.Ready)
                RpcId(other.Key, nameof(ReceiveMembership), peer, joined, reason);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveMembership(int peer, bool joined, string reason)
    {
        if (IsHostMessage() && _clientJoin.Phase == ClientJoinPhase.Ready)
            MembershipChanged?.Invoke(peer, joined, reason);
    }

    private string? _publishedTopology;
    public void PublishWorldTopology()
    {
        if (!IsHost()) return;
        var topology = World.CaptureTopology();
        if (topology.Identity == _publishedTopology) return;
        _publishedTopology = topology.Identity;
        var payload = topology.ToVariant();
        foreach (var (peer, join) in _joins.Snapshot())
            if (join.ReceivesWorld)
                RpcId(peer, nameof(ReceiveWorldTopology), Synchronization!.Session.ToString("N"), payload);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWorldTopology(string session, Godot.Collections.Array payload)
    {
        if (!IsHostMessage() || !_clientJoin.HasWorld || session != Synchronization!.Session.ToString("N")) return;
        try
        {
            if (!WorldTopology.TryRead(payload, out var topology)) throw new InvalidOperationException("Invalid world topology.");
            foreach (int peer in World.ApplyTopology(topology)) Synchronization.BindingChanged(peer);
        }
        catch (Exception error) { FailSession($"Applying world topology failed: {error.Message}"); }
    }

    public bool MovementReady(int peer) => peer == 1 || _joins.Is(peer, PeerJoinPhase.Ready);

    public void PublishMovement(byte[] payload)
    {
        if (!IsHost()) return;
        foreach (var (peer, join) in _joins.Snapshot())
            if (join.Phase == PeerJoinPhase.Ready) RpcId(peer, nameof(ReceiveMovement), payload);
    }
    public void SendMovementCommands(byte[] payload)
    {
        if (!_stopped) RpcId(1, nameof(ReceiveCommands), payload);
    }
    public void SendTimingProbe(long token)
    {
        if (!_stopped) RpcId(1, nameof(TimingProbe), token);
    }
    public void RequestMovementBaseline(long request) => RpcId(1, nameof(RequestBaseline), Synchronization!.Session.ToString("N"), request);
    private readonly Dictionary<int, ulong> _baselineRequests = new();
    private readonly Dictionary<int, ulong> _timingRequests = new();

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveCommands(byte[] payload)
    {
        if (IsHost()) Synchronization?.AcceptCommands(Multiplayer.GetRemoteSenderId(), payload);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReceiveMovement(byte[] payload)
    {
        if (IsHostMessage() && _clientJoin.Phase == ClientJoinPhase.Ready) Synchronization?.Receive(payload);
    }
    private bool AllowRequest(Dictionary<int, ulong> requests, int peer, ulong interval)
    {
        ulong now = Time.GetTicksMsec();
        if (requests.TryGetValue(peer, out var last) && now - last < interval) return false;
        requests[peer] = now; return true;
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void TimingProbe(long token)
    {
        int peer = Multiplayer.GetRemoteSenderId();
        if (IsHost() && MovementReady(peer) && AllowRequest(_timingRequests, peer, 500))
            RpcId(peer, nameof(TimingResponse), token, Synchronization!.CompletedTick);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void TimingResponse(long token, long tick)
    {
        if (IsHostMessage()) Synchronization?.TimingReply(token, tick);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestBaseline(string session, long request)
    {
        int peer = Multiplayer.GetRemoteSenderId();
        if (IsHost() && session == Synchronization!.Session.ToString("N") && MovementReady(peer) && request > 0 && AllowRequest(_baselineRequests, peer, 500))
        {
            PublishWorldTopology();
            Synchronization!.ResetCommands(peer);
            RpcId(peer, nameof(ReceiveBaseline), request, Synchronization.Baseline());
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveBaseline(long request, byte[] payload)
    {
        if (IsHostMessage() && _clientJoin.Phase == ClientJoinPhase.Ready)
            Synchronization?.Receive(payload, true, request);
    }
    public void FailSession(string reason)
    {
        if (_stopped) return;
        Failed?.Invoke(reason);
    }

    public void StopSession()
    {
        if (_stopped) return;
        _stopped = true;
        Synchronization?.Stop();
        _baselineRequests.Clear(); _timingRequests.Clear();
        if (_heartbeat != null)
        {
            _heartbeat.Stop();
            _heartbeat.Failed -= FailSession;
            _heartbeat.PeerTimedOut -= DepartPeer;
        }
        ProcessMode = ProcessModeEnum.Disabled;
        _joins?.Clear();
        _clientJoin?.Stop();
        Failed = null;
        SessionReady = null;
        MembershipChanged = null;
        LocalJoined = null;
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
                if (!api.HasMultiplayerPeer() && GodotObject.IsInstanceValid(_previousPeer))
                    api.MultiplayerPeer = _previousPeer;
                _previousPeer = null;
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
