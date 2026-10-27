using System;
using sMPGWM.scripts.Autoloads;
using sMPGWM.scripts.Constants;
using sMPGWM.scripts.Helpers;
using sMPGWM.scripts.Network.NetworkInternal;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using static Godot.MultiplayerPeer;
using BaseNetwork = sMPGWM.Scripts.Network.NetworkInternal.BaseNetwork;

namespace sMPGWM.scripts.Network;

public partial class NetworkManager : Node
{
    public static event Action OnNetworkReady;
    private IActiveNetwork _activeNetwork;
    private const int LogClassLevel = 0;

    public override void _Ready()
    {
        Logger.Log($"Creating NetworkManager", LogClassLevel);
        BaseNetwork.OnConnected += OnNetworkConnected;
    }

    private void OnNetworkConnected(bool connected, string reason)
    {
        Logger.Log($"OnNetworkConnected: {connected}, reason: {reason}", LogClassLevel);
        if (_activeNetwork != null && _activeNetwork.GetNode().IsInsideTree() && connected)
        {
            Initialize();
        }
        else
        {
            GameFlowManager.Instance.QuitToStartMenuBlocking(reason);
        }
    }

    public void CreateNetwork()
    {
        Logger.Log($"Creating Network", LogClassLevel);
        switch (GameFlowManager.Instance.NetworkType)
        {
            case NetworkType.Steam:
                _activeNetwork = new SteamNetwork();
                Logger.Log("Creating Steam Network", LogClassLevel);
                break;
            case NetworkType.Lan:
                _activeNetwork = new EnetNetwork();
                Logger.Log("Creating Enet Network", LogClassLevel);
                break;
            default:
                GameFlowManager.Instance.QuitToStartMenuBlocking($"Invalid NetworkType: {GameFlowManager.Instance.NetworkType}");
                break;
        }
        AddChild(_activeNetwork.GetNode());
        CallDeferred(nameof(AssignRole));
    }

    public void AssignRole()
    {
        Logger.Log($"Assigning Role", LogClassLevel);
        switch (GameFlowManager.Instance.NetworkRole)
        {
            case NetworkRole.Host:
                Logger.Log("Setting MultiplayerRole to Host", LogClassLevel);
                _activeNetwork.BecomeHost(GameConfig.MaxPlayers);
                break;
            case NetworkRole.Client:
                Logger.Log("Setting MultiplayerRole to Client", LogClassLevel);
                _activeNetwork.BecomeClient(GameFlowManager.Instance.LobbyId);
                break;
            default:
                GameFlowManager.Instance.QuitToStartMenuBlocking($"Invalid MultiplayerRole: {GameFlowManager.Instance.NetworkRole}");
                break;
        }
    }

    private void Initialize()
    {
        Logger.Log($"Initializing NetworkManager", LogClassLevel);
        if (GameFlowManager.Instance.NetworkRole == NetworkRole.Host && !IsHost())
        {
            GameFlowManager.Instance.QuitToStartMenuBlocking($"{nameof(NetworkManager)}.{nameof(Initialize)}: Network Role failed. Exiting...");
            return;
        }
        InvokeNetworkReady();
    }

    public bool IsHost()
    {
        var peer = Multiplayer?.MultiplayerPeer;
        return peer switch
        {
            null or OfflineMultiplayerPeer => false,
            _ => peer.GetConnectionStatus() == ConnectionStatus.Connected && Multiplayer.IsServer()
        };
    }

    public ConnectionStatus GetCurrentConnectionStatus()
    {
        return _activeNetwork?.GetConnectionStatus() ?? ConnectionStatus.Disconnected;
    }

    public bool IsConnected()
    {
        return _activeNetwork?.IsConnected() ?? false;
    }

    private static void InvokeNetworkReady()
    {
        Logger.Log("Invoking NetworkReady", LogClassLevel);
        OnNetworkReady?.Invoke();
    }

    public void Cleanup()
    {
        Logger.Log($"Cleaning up activeNetwork", LogClassLevel);
        _activeNetwork?.Cleanup();
    }
}