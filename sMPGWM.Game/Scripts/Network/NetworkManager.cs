using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.scripts.Network.NetworkInternal;
using sMPGWM.Scripts.Network.NetworkInternal;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network;

public partial class NetworkManager : AbstractSingleton<NetworkManager>
{
    public static event Action? OnNetworkReady;
    private IActiveNetwork? _activeNetwork;
    private SessionProtocolNode? _protocol;
    private NetworkStartupData? _startup;
    private bool _stopped;
    private bool _previousObjectDecoding;
    private bool _configuredApi;
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
        if (DiagnosticMode)
        {
            _protocol = new SessionProtocolNode { Name = "SessionProtocol" };
            _protocol.Completed += CompleteDiagnostic;
            AddChild(_protocol);
            AddChild(new NetworkTicker { Name = "NetworkTicker" });
        }
        // Defer until the main-game scene and all its children have finished readiness.
        CallDeferred(nameof(CreateNetwork));
    }

    public void CreateNetwork()
    {
        if (_activeNetwork != null || _stopped) return;
        _startup ??= new NetworkStartupData(GameFlowManager.Instance.NetworkType,
            GameFlowManager.Instance.NetworkRole, GameConfig.ServerIpAddress, GameConfig.Port,
            GameConfig.MaxPlayers, GameConfig.ConnectionTimeout, LobbyId: GameFlowManager.Instance.LobbyId);
        NetworkRole = _startup.Role;
        NetworkType = _startup.Type;
        if (!Enum.IsDefined(NetworkRole) || !Enum.IsDefined(NetworkType))
        {
            CancelDiagnostic("Invalid network role or transport.");
            return;
        }
        if (Multiplayer is SceneMultiplayer api)
        {
            _previousObjectDecoding = api.AllowObjectDecoding;
            api.AllowObjectDecoding = false;
            _configuredApi = true;
        }
        if (NetworkType == NetworkType.Lan)
        {
            var network = new EnetNetwork { Startup = _startup };
            network.ConnectionChanged += OnNetworkConnected;
            _activeNetwork = network;
        }
        else
        {
            BaseNetwork.OnConnected += OnNetworkConnected;
            _activeNetwork = new SteamNetwork();
        }
        AddChild(_activeNetwork.GetNode());
        AssignRole();
    }

    public void AssignRole()
    {
        if (_activeNetwork == null || _stopped) return;
        if (NetworkRole == NetworkRole.Host) _activeNetwork.BecomeHost(_startup!.MaxPlayers);
        else _activeNetwork.BecomeClient(_startup!.LobbyId);
    }

    private void OnNetworkConnected(bool connected, string reason)
    {
        if (_stopped) return;
        if (!connected) { CancelDiagnostic(reason); return; }
        Logger.Info($"Network {NetworkRole}: {reason}");
        if (DiagnosticMode)
        {
            if (NetworkRole == NetworkRole.Client) _protocol!.SendHello();
            else Logger.Info($"Listening session={_protocol!.HostSession} port={_startup!.Port} maxPlayers={_startup.MaxPlayers}");
        }
        else OnNetworkReady?.Invoke();
    }

    private void CompleteDiagnostic(DiagnosticResult result)
    {
        if (_stopped || DiagnosticResult != null) return;
        DiagnosticResult = result;
        Logger.Info($"Diagnostic result={result.Reason} correlation={result.Correlation} session={result.HostSession} tick={NetworkTicker.GetTicksSinceStart()}");
        if (!result.Succeeded) CancelDiagnostic(result.Reason);
    }

    public void CancelDiagnostic(string reason)
    {
        if (_stopped) return;
        DiagnosticResult ??= new DiagnosticResult(false, reason, "", "");
        StopSession();
        GameFlowManager.Instance.QuitToStartMenuBlocking(reason);
    }

    public void StopSession()
    {
        if (_stopped) return;
        _stopped = true;
        if (_protocol != null)
        {
            _protocol.Completed -= CompleteDiagnostic;
            _protocol.Stop();
        }
        BaseNetwork.OnConnected -= OnNetworkConnected;
        if (_activeNetwork is EnetNetwork network) network.ConnectionChanged -= OnNetworkConnected;
        _activeNetwork?.Cleanup();
        _activeNetwork = null;
        if (_configuredApi && Multiplayer is SceneMultiplayer api)
        {
            api.AllowObjectDecoding = _previousObjectDecoding;
            _configuredApi = false;
        }
        Logger.Info($"Network session stopped role={NetworkRole} tick={NetworkTicker.GetTicksSinceStart()}");
    }

    protected override void OnExitTree()
    {
        StopSession();
        _startup = null;
        _protocol = null;
        DiagnosticResult = null;
        OnNetworkReady = null;
    }

    public bool IsHost() => !_stopped && _activeNetwork?.IsConnected() == true && Multiplayer.IsServer();
    public MultiplayerPeer.ConnectionStatus GetCurrentConnectionStatus() =>
        _activeNetwork?.GetConnectionStatus() ?? MultiplayerPeer.ConnectionStatus.Disconnected;
    public bool IsConnected() => _activeNetwork?.IsConnected() == true;
}
