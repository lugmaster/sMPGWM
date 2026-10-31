using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public class SteamMultiplayerPeerWrapper
{
    private GodotObject _nativePeer;

    public SteamMultiplayerPeerWrapper()
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)} creating Steam multiplayer peer wrapper.");
        _nativePeer = (GodotObject)ClassDB.Instantiate("SteamMultiplayerPeer");
        if (_nativePeer == null)
        {
            throw new InvalidOperationException("Failed to create SteamMultiplayerPeer instance!");
        }
    }

    public Error CreateHost(int port)
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(CreateHost)} creating host with virtual {nameof(port)}={port}.");
        var result = _nativePeer.Call("create_host", port);

        if (result.VariantType == Variant.Type.Int)
        {
            return (Error)result.AsInt32();
        }

        Logger.Error(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(CreateHost)} unexpected return type from create_host with {nameof(result.VariantType)}={result.VariantType}; expected an integer.");
        return Error.Failed;
    }

    public Error CreateClient(ulong steamId, int port)
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(CreateClient)} creating client with {nameof(steamId)}={steamId}, virtual {nameof(port)}={port}.");
        var result = _nativePeer.Call("create_client", steamId, port);

        if (result.VariantType == Variant.Type.Int)
        {
            return (Error)result.AsInt32();
        }

        Logger.Error(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(CreateClient)} unexpected return type from create_client with {nameof(result.VariantType)}={result.VariantType}; expected an integer.");
        return Error.Failed;
    }

    public int GetConnectionStatus()
    {
        return (int)_nativePeer.Call("get_connection_status");
    }

    public GodotObject GetPeer()
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(GetPeer)} returning {nameof(_nativePeer)}.");
        return _nativePeer;
    }

    public void Cleanup()
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(Cleanup)} cleanup started.");

        if (_nativePeer?.HasMethod("close") == true)
        {
            Logger.Debug(
                $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(Cleanup)} calling close() on native peer.");
            _nativePeer.Call("close");
        }

        _nativePeer?.Dispose();
        _nativePeer = null;

        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(Cleanup)} cleanup finished.");
    }
}
