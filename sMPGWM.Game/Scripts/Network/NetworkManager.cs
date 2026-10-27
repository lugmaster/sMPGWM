using System;
using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.Network;
using sMPGWM.scripts.Network.NetworkInternal;
using static Godot.MultiplayerPeer;
using BaseNetwork = sMPGWM.Scripts.Network.NetworkInternal.BaseNetwork;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network;

public partial class NetworkManager : Node
{
    public static event Action OnNetworkReady;
    private IActiveNetwork _activeNetwork;

    public override void _Ready()
    {
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(_Ready)} creating network manager.");
        BaseNetwork.OnConnected += OnNetworkConnected;
    }

    private void OnNetworkConnected(bool connected, string reason)
    {
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(OnNetworkConnected)} connection result with {nameof(connected)}={connected}, {nameof(reason)}={reason}.");
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
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(CreateNetwork)} creating network.");
        switch (GameFlowManager.Instance.NetworkType)
        {
            case NetworkType.Steam:
                _activeNetwork = new SteamNetwork();
                Logger.Debug(
                    $"{nameof(NetworkManager)}.{nameof(CreateNetwork)} creating Steam network.");
                break;
            case NetworkType.Lan:
                _activeNetwork = new EnetNetwork();
                Logger.Debug(
                    $"{nameof(NetworkManager)}.{nameof(CreateNetwork)} creating ENet network.");
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
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(AssignRole)} assigning role.");
        switch (GameFlowManager.Instance.NetworkRole)
        {
            case NetworkRole.Host:
                Logger.Debug(
                    $"{nameof(NetworkManager)}.{nameof(AssignRole)} setting role to host.");
                _activeNetwork.BecomeHost(GameConfig.MaxPlayers);
                break;
            case NetworkRole.Client:
                Logger.Debug(
                    $"{nameof(NetworkManager)}.{nameof(AssignRole)} setting role to client.");
                _activeNetwork.BecomeClient(GameFlowManager.Instance.LobbyId);
                break;
            default:
                GameFlowManager.Instance.QuitToStartMenuBlocking($"Invalid MultiplayerRole: {GameFlowManager.Instance.NetworkRole}");
                break;
        }
    }

    private void Initialize()
    {
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(Initialize)} initializing network manager.");
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
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(InvokeNetworkReady)} invoking {nameof(OnNetworkReady)}.");
        OnNetworkReady?.Invoke();
    }

    public void Cleanup()
    {
        Logger.Debug(
            $"{nameof(NetworkManager)}.{nameof(Cleanup)} cleaning up {nameof(_activeNetwork)}.");
        _activeNetwork?.Cleanup();
    }
}