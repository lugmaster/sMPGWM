using System;
using System.Text;
using Godot;
using sMPGWM.scripts.Network;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Network;

// Owns the temporary SceneMultiplayer configuration for a single session.
internal sealed class SessionAuthentication : IDisposable
{
    private readonly SceneMultiplayer _api;
    private readonly NetworkStartupData _startup;
    private readonly Action<string> _fail;
    private readonly bool _previousObjectDecoding;
    private readonly Callable _previousCallback;
    private readonly double _previousTimeout;
    private readonly byte[] _payload;
    private bool _disposed;

    public SessionAuthentication(SceneMultiplayer api, NetworkStartupData startup, Action<string> fail)
    {
        _api = api;
        _startup = startup;
        _fail = fail;
        _previousObjectDecoding = api.AllowObjectDecoding;
        _previousCallback = api.AuthCallback;
        _previousTimeout = api.AuthTimeout;
        _payload = Encoding.UTF8.GetBytes($"sMPGWM:preflight:{typeof(NetworkManager).Module.ModuleVersionId:N}:{DiagnosticExchange.ProtocolVersion}:{GameConfigSystem.GameConfig.Version}");
        api.AllowObjectDecoding = false;
        api.AuthTimeout = 10;
        api.AuthCallback = Callable.From<long, byte[]>(Receive);
        api.PeerAuthenticating += Authenticate;
        api.PeerAuthenticationFailed += Failed;
    }

    private void Authenticate(long peer)
    {
        if (_disposed) return;
        if (_startup.Role == NetworkRole.Host &&
            _api.GetPeers().Length + _api.GetAuthenticatingPeers().Length >= _startup.MaxPlayers)
        {
            _api.DisconnectPeer(checked((int)peer));
            return;
        }
        var result = _api.SendAuth(checked((int)peer), _payload);
        Logger.Info($"NET00 auth_send peer={peer} result={result}");
        if (result != Error.Ok) _api.DisconnectPeer(checked((int)peer));
    }

    private void Receive(long peer, byte[] data)
    {
        if (_disposed) return;
        if (!data.AsSpan().SequenceEqual(_payload) || (_startup.Role == NetworkRole.Client && peer != 1))
        {
            Logger.Warning($"NET00 auth_rejected peer={peer} bytes={data.Length}");
            _api.DisconnectPeer(checked((int)peer));
            return;
        }
        var result = _api.CompleteAuth(checked((int)peer));
        Logger.Info($"NET00 auth_complete peer={peer} result={result}");
        if (result != Error.Ok) _api.DisconnectPeer(checked((int)peer));
    }

    private void Failed(long peer)
    {
        Logger.Warning($"NET00 auth_failed peer={peer}");
        if (!_disposed && _startup.Role == NetworkRole.Client)
            _fail("Native protocol preflight failed or timed out.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _api.PeerAuthenticating -= Authenticate;
        _api.PeerAuthenticationFailed -= Failed;
        _api.AuthCallback = _previousCallback;
        _api.AuthTimeout = _previousTimeout;
        _api.AllowObjectDecoding = _previousObjectDecoding;
    }
}
