using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.Network;
using sMPGWM.scripts.Network.NetworkInternal;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network;

public partial class NetworkManager : AbstractSingleton<NetworkManager>
{
    public event Action? OnNetworkReady;
    private bool _roleAssigned;
    public NetworkSession Session { get; } = new();
    public bool CanPlay => Session.State == SessionState.Ready && !DiagnosticMode;
    public ulong? LobbyId => (_activeNetwork as SteamNetwork)?.LobbyId;
    public event Action<DiagnosticResult>? DiagnosticCompleted;
    private IActiveNetwork? _activeNetwork;
    private SessionProtocolNode? _protocol;
    private NetworkTicker? _ticker;
    private NetworkStartupData? _startup;
    private bool _stopped;
    private bool _previousObjectDecoding;
    private bool _configuredApi;
    private Callable _previousAuthCallback;
    private double _previousAuthTimeout;
    private byte[] _authPayload = Array.Empty<byte>();
    public string HostSession { get; private set; } = Guid.NewGuid().ToString("N");
    public bool SessionStopped => _stopped;
    public bool DiagnosticMode => _startup?.Diagnostic == true;
    public NetworkRole NetworkRole { get; private set; }
    public NetworkType NetworkType { get; private set; }
    public DiagnosticResult? DiagnosticResult { get; private set; }

    protected override void OnReady()
    {
        _startup = GameFlowManager.Instance.ConsumeNetworkStartup();
        if (_startup == null) return;
        NetworkRole = _startup.Role;
        NetworkType = _startup.Type;
        if (DiagnosticMode && NetworkType == NetworkType.Steam)
            AddChild(new NetworkDiagnosticPanel { SessionManager = this });
        {
            _protocol = new SessionProtocolNode { Name = "SessionProtocol" };
            _protocol.Completed += CompleteDiagnostic;
            AddChild(_protocol);
            _ticker = new NetworkTicker { Name = "NetworkTicker" };
            AddChild(_ticker);
        }
        // Defer until the main-game scene and all its children have finished readiness.
        CallDeferred(nameof(CreateNetwork));
    }

    public void CreateNetwork()
    {
        if (_activeNetwork != null || _stopped) return;
        if (_startup == null) return;
        if (!Session.TryStart()) return;
        NetworkRole = _startup.Role;
        NetworkType = _startup.Type;
        Logger.Info($"Network startup role={NetworkRole} transport={NetworkType} build={typeof(NetworkManager).Module.ModuleVersionId:N} protocol={DiagnosticExchange.ProtocolVersion} version={sMPGWM.Scripts.GameConfigSystem.GameConfig.Version}");
        if (!Enum.IsDefined(NetworkRole) || !Enum.IsDefined(NetworkType))
        {
            FailSession("Invalid network role or transport.");
            return;
        }
        if (Multiplayer is SceneMultiplayer api)
        {
            _previousObjectDecoding = api.AllowObjectDecoding;
            api.AllowObjectDecoding = false;
            _configuredApi = true;
            {
                _previousAuthCallback = api.AuthCallback;
                _previousAuthTimeout = api.AuthTimeout;
                _authPayload = System.Text.Encoding.UTF8.GetBytes($"sMPGWM:preflight:{typeof(NetworkManager).Module.ModuleVersionId:N}:{DiagnosticExchange.ProtocolVersion}:{sMPGWM.Scripts.GameConfigSystem.GameConfig.Version}");
                api.AuthTimeout = 10;
                api.AuthCallback = Callable.From<long, byte[]>(ReceiveAuthentication);
                api.PeerAuthenticating += AuthenticatePeer;
                api.PeerAuthenticationFailed += AuthenticationFailed;
            }
        }
        if (NetworkType == NetworkType.Lan)
        {
            var network = new EnetNetwork { Startup = _startup };
            _activeNetwork = network;
        }
        else
        {
            _activeNetwork = new SteamNetwork { Startup = _startup };
        }
        _activeNetwork.TransportReady += OnNetworkConnected;
        _activeNetwork.StartupFailed += FailSession;
        _activeNetwork.ConnectionLost += FailSession;
        try
        {
            AddChild(_activeNetwork.GetNode());
            AssignRole();
        }
        catch (Exception e) { FailSession($"Network startup failed: {e.Message}"); }
    }

    private void AuthenticatePeer(long peer)
    {
        if (_stopped) return;
        var api = (SceneMultiplayer)Multiplayer;
        if (NetworkRole == NetworkRole.Host &&
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
        if (!data.AsSpan().SequenceEqual(_authPayload) || (NetworkRole == NetworkRole.Client && peer != 1))
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
        if (!_stopped && NetworkRole == NetworkRole.Client)
            FailSession("Native protocol preflight failed or timed out.");
    }

    public void AssignRole()
    {
        if (_activeNetwork == null || _stopped || _roleAssigned) return;
        _roleAssigned = true;
        if (NetworkRole == NetworkRole.Host) _activeNetwork.BecomeHost(_startup!.MaxPlayers);
        else _activeNetwork.BecomeClient(_startup!.SteamTarget?.LobbyId ?? 0);
    }

    private void OnNetworkConnected(string reason)
    {
        if (_stopped) return;
        if (!Session.TryReady()) return;
        Logger.Info($"Network {NetworkRole}: {reason}");
        if (DiagnosticMode)
        {
            if (NetworkRole == NetworkRole.Client) _protocol!.SendHello();
            else Logger.Info($"Listening session={_protocol!.HostSession} port={_startup!.LanTarget?.Port} maxPlayers={_startup.MaxPlayers}");
        }
        else OnNetworkReady?.Invoke();
    }

    private void CompleteDiagnostic(DiagnosticResult result)
    {
        if (_stopped || DiagnosticResult != null) return;
        DiagnosticResult = result;
        DiagnosticCompleted?.Invoke(result);
        Logger.Info($"Diagnostic result={result.Reason} correlation={result.Correlation} session={result.HostSession} tick={NetworkTicker.GetTicksSinceStart()}");
        if (!result.Succeeded) FailSession(result.Reason);
    }

    public override void _PhysicsProcess(double delta) => _protocol?.TickHealth();

    public PeerHealthMonitor? GetPeerHealth(int peer) => _protocol?.GetHealth(peer);

    public void RemoveUnhealthyPeer(int peer)
    {
        if (_stopped) return;
        if (IsHost()) ((SceneMultiplayer)Multiplayer).DisconnectPeer(peer);
        else FailSession("Server health response timed out or failed.");
    }

    public void FailSession(string reason)
    {
        if (_stopped) return;
        DiagnosticResult ??= new DiagnosticResult(false, reason, "", "");
        Session.TryStop(reason);
        StopSession();
        GameFlowManager.Instance.QuitToStartMenuBlocking(reason);
    }

    public void StopSession()
    {
        if (_stopped) return;
        _stopped = true;
        Session.TryStop();
        _ticker?.SetPhysicsProcess(false);
        GamePlayManager.Instance?.StopSession();
        if (_protocol != null)
        {
            _protocol.Completed -= CompleteDiagnostic;
            _protocol.Stop();
        }
        if (_activeNetwork != null)
        {
            _activeNetwork.TransportReady -= OnNetworkConnected;
            _activeNetwork.StartupFailed -= FailSession;
            _activeNetwork.ConnectionLost -= FailSession;
        }
        _activeNetwork?.Cleanup();
        _activeNetwork = null;
        if (_configuredApi && Multiplayer is SceneMultiplayer api)
        {
            {
                api.PeerAuthenticating -= AuthenticatePeer;
                api.PeerAuthenticationFailed -= AuthenticationFailed;
                api.AuthCallback = _previousAuthCallback;
                api.AuthTimeout = _previousAuthTimeout;
                _authPayload = Array.Empty<byte>();
            }
            api.AllowObjectDecoding = _previousObjectDecoding;
            _configuredApi = false;
        }
        Session.CompleteStop();
        Logger.Info($"Network session stopped role={NetworkRole} tick={NetworkTicker.GetTicksSinceStart()}");
    }

    protected override void OnExitTree()
    {
        StopSession();
        HostSession = "";
        _startup = null;
        _protocol = null;
        _ticker = null;
        DiagnosticResult = null;
        OnNetworkReady = null;
        DiagnosticCompleted = null;
    }

    public bool IsHost() => !_stopped && _activeNetwork?.IsConnected() == true && Multiplayer.IsServer();
    public MultiplayerPeer.ConnectionStatus GetCurrentConnectionStatus() =>
        _activeNetwork?.GetConnectionStatus() ?? MultiplayerPeer.ConnectionStatus.Disconnected;
    public bool IsConnected() => _activeNetwork?.IsConnected() == true;
}
