using System;
using System.Collections.Generic;
using sMPGWM.scripts.Network;
using sMPGWM.Scripts.GameFlowSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.Util;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.Network;

public partial class MpManager : AbstractSingleton<MpManager>
{
    private readonly HashSet<int> _ready = new();
    private readonly HashSet<int> _departed = new();
    private bool _stopped;
    public void BeginPeer(int peer) => _departed.Remove(peer);
    public void AcceptReady(int peer)
    {
        if (_stopped || !_ready.Add(peer)) return;
        HeartbeatMonitor.Instance.Track(peer);
        NotifyMembership(peer, true, "");
        NetworkManager.Instance.AnnounceMembership(peer, true, "");
    }
    public void Depart(int peer, string reason)
    {
        if (_stopped || peer <= 1 || !_departed.Add(peer)) return;
        var joined = _ready.Remove(peer);
        HeartbeatMonitor.Instance?.Remove(peer);
        NetworkManager.Instance.RemovePeer(peer);
        if (!joined) return;
        NotifyMembership(peer, false, reason);
        NetworkManager.Instance.AnnounceMembership(peer, false, reason);
    }
    public void StopMembership() { _stopped = true; _ready.Clear(); _departed.Clear(); }
    public void HostLost(string reason)
    {
        if (_stopped) return;
        _stopped = true;
        HeartbeatMonitor.Instance?.Stop();
        GameFlowManager.Instance.QuitToStartMenuBlocking(reason);
    }
    public void NotifyMembership(int peer, bool joined, string reason)
    {
        var text = $"Player {peer} {(joined ? "joined" : "left")}" +
            (string.IsNullOrEmpty(reason) ? "." : $": {reason}");
        Logger.Info($"NET membership {text}");
        var messages = GamePlayManager.Instance?.MessageController;
        if (messages == null) return;
        messages.RegisterSender(new MessageSender("network", "Session"));
        messages.Receive(Guid.NewGuid().ToString("N"), "network", text, text, MessageType.NonBlocking);
    }

    public void NotifyLocalJoin(string gameName)
    {
        if (_stopped) return;
        var text = string.IsNullOrWhiteSpace(gameName)
            ? "You joined the game."
            : $"You joined game {gameName}.";
        Logger.Info($"NET local_join {text}");
        var messages = GamePlayManager.Instance?.MessageController;
        if (messages == null) return;
        messages.RegisterSender(new MessageSender("network", "Session"));
        messages.Receive("network-local-join", "network", text, text, MessageType.NonBlocking);
    }

    private readonly Dictionary<int, MpController> _controllers = new();
    private readonly Dictionary<int, PlayerStartupData> _startupData = new();
    public IReadOnlyDictionary<int, MpController> MpControllers => _controllers;
    public IReadOnlyDictionary<int, PlayerStartupData> StartupData => _startupData;

    public void Spawn(int peerId, PlayerStartupData data)
    {
        if (_controllers.ContainsKey(peerId)) return;
        var controller = new MpController { Name = $"Player_{peerId}" };
        AddChild(controller);
        _controllers.Add(peerId, controller);
        _startupData.Add(peerId, data);
        Logger.Info($"NET spawn peer={peerId} message='{data.Message}'");
    }

    public void Despawn(int peerId)
    {
        if (_controllers.Remove(peerId, out var controller))
        {
            RemoveChild(controller);
            controller.QueueFree();
        }
        _startupData.Remove(peerId);
    }

    public void Clear()
    {
        foreach (var peerId in new List<int>(_controllers.Keys)) Despawn(peerId);
    }

    protected override void OnExitTree() => Clear();
}
