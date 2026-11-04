using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;
namespace sMPGWM.Scripts.NetworkSystem;

public partial class HeartbeatMonitor : AbstractSingleton<HeartbeatMonitor>
{
    public event Action<string>? Failed;
    public event Action<int, string>? PeerTimedOut;
    [Export] public double RequestIntervalSeconds { get; set; } = 5;
    [Export] public double SilenceTimeoutSeconds { get; set; } = 20;
    private sealed class Peer
    {
        public double Seen, Next;
        public readonly Dictionary<long, double> Pending = new();
    }
    private readonly Dictionary<int, Peer> _peers = new();
    private bool _host, _running, _active;
    private double _seen;
    private long _sequence, _received;
    private static double Now => Time.GetTicksMsec() / 1000.0;
    public void Initialize(bool host)
    {
        Stop();
        _host = host;
        _running = true;
        ProcessMode = ProcessModeEnum.Always;
    }
    public void Track(int id)
    {
        if (_running && _host && id > 1)
            _peers.TryAdd(id, new Peer { Seen = Now, Next = Now + Math.Max(.1, RequestIntervalSeconds) });
    }
    public void ActivateClient() { if (_running && !_host && !_active) { _active = true; _seen = Now; } }
    public void Remove(int id) => _peers.Remove(id);
    public void Stop() { _running = _active = false; _peers.Clear(); _sequence = _received = 0; }
    public override void _Process(double delta)
    {
        if (!_running) return;
        var now = Now;
        var timeout = Math.Max(.1, SilenceTimeoutSeconds);
        if (!_host)
        {
            if (_active && now - _seen >= timeout)
            {
                Stop();
                Logger.Warning("HB host timeout");
                Failed?.Invoke("Host stopped responding");
            }
            return;
        }
        foreach (var id in new List<int>(_peers.Keys))
        {
            if (!_peers.TryGetValue(id, out var peer)) continue;
            if (now - peer.Seen >= timeout)
            {
                _peers.Remove(id);
                Logger.Warning($"HB timeout peer={id}");
                PeerTimedOut?.Invoke(id, "Heartbeat timeout");
                continue;
            }
            foreach (var seq in new List<long>(peer.Pending.Keys))
                if (now >= peer.Pending[seq]) peer.Pending.Remove(seq);
            if (now < peer.Next) continue;
            peer.Next = now + Math.Max(.1, RequestIntervalSeconds);
            if (peer.Pending.Count >= 64) continue;
            var sequence = ++_sequence;
            peer.Pending.Add(sequence, now + timeout);
            var result = RpcId(id, nameof(HeartbeatRequest), sequence);
            Logger.Info($"HB request peer={id} sequence={sequence} result={result}");
        }
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void HeartbeatRequest(long sequence)
    {
        if (!_running || _host || !_active || Multiplayer.GetRemoteSenderId() != 1 || sequence <= _received) return;
        _received = sequence;
        _seen = Now;
        var result = RpcId(1, nameof(HeartbeatResponse), sequence);
        Logger.Info($"HB response host=1 sequence={sequence} result={result}");
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void HeartbeatResponse(long sequence)
    {
        var id = Multiplayer.GetRemoteSenderId();
        if (!_running || !_host || !_peers.TryGetValue(id, out var peer) ||
            !peer.Pending.Remove(sequence, out var expires) || Now >= expires) return;
        peer.Seen = Now;
        Logger.Info($"HB accepted peer={id} sequence={sequence}");
    }
    protected override void OnExitTree() => Stop();
}
