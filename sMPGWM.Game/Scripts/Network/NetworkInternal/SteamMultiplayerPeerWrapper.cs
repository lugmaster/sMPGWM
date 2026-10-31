using System;
using Godot;
using sMPGWM.Scripts.GameFlowSystem;

using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.scripts.Network.NetworkInternal;

public class SteamMultiplayerPeerWrapper
{
    private GodotObject _nativePeer;
    public bool DiagnosticTracing { get; init; }

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

    private void TraceCapabilities(string phase, Error result = Error.Ok)
    {
        if (!DiagnosticTracing && OS.GetEnvironment("NET00_CAPABILITIES") != "1")
            return;

        var peer = (MultiplayerPeer)_nativePeer;
        using var api = new SceneMultiplayer();
        Logger.Info($"NET00 phase={phase} engine={Engine.GetVersionInfo()["string"]} native={_nativePeer.GetClass()} result={result} status={peer.GetConnectionStatus()}");
        foreach (var method in new[] { "create_host", "create_client", "close", "get_steam_id_for_peer_id", "get_peer_id_for_steam_id" })
            Logger.Info($"NET00 native_method={method} available={_nativePeer.HasMethod(method)}");
        foreach (var method in new[] { "set_auth_callback", "send_auth", "complete_auth", "set_server_relay_enabled" })
            Logger.Info($"NET00 api_method={method} available={api.HasMethod(method)}");
        if (phase == "host" && result == Error.Ok)
            Logger.Info($"NET00 server_id={peer.GetUniqueId()} relay_supported={peer.IsServerRelaySupported()}");
    }

    public Error CreateHost(int port)
    {
        Logger.Debug(
            $"{nameof(SteamMultiplayerPeerWrapper)}.{nameof(CreateHost)} creating host with virtual {nameof(port)}={port}.");
        TraceCapabilities("constructed");
        var result = _nativePeer.Call("create_host", port);

        if (result.VariantType == Variant.Type.Int)
        {
            TraceCapabilities("host", (Error)result.AsInt32());
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
            TraceCapabilities("client", (Error)result.AsInt32());
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
