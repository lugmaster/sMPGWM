using Godot;
using sMPGWM.Scripts.GameFlowSystem;

namespace sMPGWM.scripts.Network.NetworkInternal;

public class SteamMultiplayerPeerWrapper
{
    private GodotObject _nativePeer;

    public SteamMultiplayerPeerWrapper()
    {
        Logger.Log("Creating SteamMultiplayerPeerWrapper", 0);
        _nativePeer = (GodotObject)ClassDB.Instantiate("SteamMultiplayerPeer");
        if (_nativePeer == null)
        {
            GameFlowManager.Instance.QuitToStartMenuBlocking("Failed to create SteamMultiplayerPeer instance!");
        }
    }

    public Error CreateHost(int port)
    {
        Logger.Log($"Creating SteamMultiplayerPeerWrapper Host for virtual {port}", 0);
        var result = _nativePeer.Call("create_host", port);

        if (result.VariantType == Variant.Type.Int)
        {
            return (Error)result.AsInt32();
        }

        Logger.Error("SteamMultiplayerWrapper: Unexpected return levelEnum from create_host! Expected an integer.");
        return Error.Failed;
    }

    public Error CreateClient(ulong steamId, int port)
    {
        Logger.Log($"Creating SteamMultiplayerPeerWrapper Client for User {steamId} on virtual port {port}", 0);
        var result = _nativePeer.Call("create_client", steamId, port);

        if (result.VariantType == Variant.Type.Int)
        {
            return (Error)result.AsInt32();
        }

        Logger.Error("SteamMultiplayerWrapper: Unexpected return levelEnum from create_client! Expected an integer.");
        return Error.Failed;
    }

    public int GetConnectionStatus()
    {
        return (int)_nativePeer.Call("get_connection_status");
    }

    public GodotObject GetPeer()
    {
        Logger.Log("Return SteamMultiplayerPeerWrapper as _nativePeer", 0);
        return _nativePeer;
    }

    public void Cleanup()
    {
        Logger.Log("SteamMultiplayerPeerWrapper: Cleanup started", 0);

        if (_nativePeer?.HasMethod("close") == true)
        {
            Logger.Log("SteamMultiplayerPeerWrapper: Calling close() on native peer", 0);
            _nativePeer.Call("close");
        }

        _nativePeer = null;

        Logger.Log("SteamMultiplayerPeerWrapper: Cleanup finished", 0);
    }
}
