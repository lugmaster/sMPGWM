using System;
using System.Collections.Generic;
using Godot;
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
            _activeNetwork = _startup.Type == NetworkType.Lan
                ? new EnetNetwork { Startup = _startup }
                : new SteamNetwork { Startup = _startup };
            _activeNetwork.TransportReady += OnNetworkConnected;
            _activeNetwork.Failed += FailSession;
            AddChild(_activeNetwork);
            AddChild(new NetworkTicker());
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
        _replyDeadline = 0;
        Logger.Info("Network Client: host round trip complete");
        GameFlowManager.Instance.CompleteLoading();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_stopped && _replyDeadline != 0 && Time.GetTicksMsec() >= _replyDeadline)
            FailSession("Host reply timed out.");
    }

    private void PeerLeft(long peer) => _answered.Remove((int)peer);

    public void FailSession(string reason)
    {
        if (!_stopped) GameFlowManager.Instance.QuitToStartMenuBlocking(reason);
    }

    public void StopSession()
    {
        if (_stopped) return;
        _stopped = true;
        GameFlowManager.Instance?.BeginDisconnecting();
        ProcessMode = ProcessModeEnum.Disabled;
        _helloToken = null;
        _replyDeadline = 0;
        _answered.Clear();
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
