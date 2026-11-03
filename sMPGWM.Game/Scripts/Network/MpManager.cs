using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ShipSystem;
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
    private readonly Dictionary<int, ShipSpawnRecord> _records = new();
    private int _spawnIndex;
    public PlayerController? LocalPlayer { get; set; }
    public IReadOnlyDictionary<int, MpController> MpControllers => _controllers;
    public IReadOnlyDictionary<int, ShipSpawnRecord> SpawnRecords => _records;

    public ShipSpawnRecord ApproveSpawn(int peerId, PlayerStartupData data)
    {
        if (!ShipRegistry.TryGetById(data.ShipId, out _)) throw new ArgumentException("Unknown ship: " + data.ShipId);
        if (_records.TryGetValue(peerId, out var existing))
        {
            if (existing.ShipId != data.ShipId) throw new InvalidOperationException("Conflicting ship request.");
            return existing;
        }
        var index = _spawnIndex++;
        var record = new ShipSpawnRecord(peerId, Guid.NewGuid().ToString("N"), data.ShipId,
            new Vector2(640 + index % 4 * 140, 360 + index / 4 * 140), 0, Vector2.Zero);
        Spawn(record);
        return record;
    }

    public void Spawn(ShipSpawnRecord record)
    {
        if (_records.TryGetValue(record.PeerId, out var existing))
        {
            if (existing != record) throw new InvalidOperationException("Conflicting spawn record.");
            return;
        }
        foreach (var known in _records.Values)
            if (known.EntityId == record.EntityId) throw new InvalidOperationException("Duplicate ship entity ID.");
        MpController? participant = null;
        ShipController? ship = null;
        try
        {
            ship = ShipFactory.Create(ShipRegistry.GetById(record.ShipId), record.EntityId,
                record.Position, record.Rotation, record.Velocity);
            participant = new MpController { Name = $"Player_{record.PeerId}", ControllingPeerId = record.PeerId };
            AddChild(participant);
            participant.AssignShip(ship);
            _controllers.Add(record.PeerId, participant);
            _records.Add(record.PeerId, record);
            if (IsInsideTree() && record.PeerId == Multiplayer.GetUniqueId()) LocalPlayer?.BindParticipant(participant);
            Logger.Info($"NET spawn peer={record.PeerId} ship={record.ShipId} entity={record.EntityId}");
        }
        catch (Exception e)
        {
            _controllers.Remove(record.PeerId); _records.Remove(record.PeerId);
            if (participant != null) participant.Free();
            if (ship != null && GodotObject.IsInstanceValid(ship)) ship.Free();
            GameFlowManager.Instance?.QuitToStartMenuBlocking($"Ship construction failed: {e.Message}");
            throw;
        }
    }

    public void Despawn(int peerId)
    {
        if (_controllers.Remove(peerId, out var controller))
        {
            if (LocalPlayer?.Participant == controller) LocalPlayer.UnbindParticipant();
            controller.RemoveShip();
            RemoveChild(controller);
            controller.QueueFree();
        }
        _records.Remove(peerId);
    }

    public void Clear()
    {
        foreach (var peerId in new List<int>(_controllers.Keys)) Despawn(peerId);
    }

    protected override void OnExitTree() => Clear();
}
