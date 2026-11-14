using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.ShipSystem;
using Logger = sMPGWM.Scripts.LoggingSystem.Logger;

namespace sMPGWM.Scripts.WorldSystem;

public partial class GameWorld : Node
{
    public ItemStore Items { get; private set; } = new();
    public bool AuthoritativeItems { get; set; }
    public ShipAssetCatalogue? Assets { get; set; }
    public InventorySnapshot? InitialInventory { get; set; }
    public Action<ShipLoadout, InventoryController>? InitializeLoadout { get; set; }

    public void InitializePlayerLoadout(int peer, InventorySnapshot initial)
    {
        var player = _controllers[peer];
        if (player.InventoryInitialized) return;
        foreach (var item in initial.Slots)
            if (item != null && Items.CreateItems(player.Inventory.Container, item.DefinitionId, item.Count) != InventoryResult.Success)
                throw new InvalidOperationException("Could not initialize personal inventory.");
        if (player.ControlledShip is { } ship)
        {
            ship.Loadout.SetAuthoritative(true);
            InitializeLoadout?.Invoke(ship.Loadout, player.Inventory);
            ship.Loadout.InitializeResources();
        }
        player.InventoryInitialized = true;
    }

    private Dictionary<int, PlayerSession> _controllers = new();
    private Dictionary<int, ShipSpawnRecord> _records = new();
    private int _spawnIndex;
    private Dictionary<string, ShipController> _entities = new();
    public IReadOnlyDictionary<string, ShipController> Entities => _entities;
    public event Action<ShipController>? EntitySpawned;

    private void TrackBinding(ShipController? previous, ShipController? next)
    {
        if (previous != null) { previous.ClearInput(); _entities[previous.EntityId] = previous; }
        if (next != null) _entities[next.EntityId] = next;
    }
    private PlayerSession EnsureParticipant(int peer)
    {
        if (_controllers.TryGetValue(peer, out var existing)) return existing;
        var participant = new PlayerSession(Items) { ControllingPeerId = peer };
        participant.ControlledShipChanged += TrackBinding;
        _controllers.Add(peer, participant);
        if (IsInsideTree() && peer == Multiplayer.GetUniqueId()) LocalPlayer?.BindParticipant(participant);
        return participant;
    }

    public PlayerController? LocalPlayer { get; set; }
    public IReadOnlyDictionary<int, PlayerSession> Players => _controllers;

    public ShipSpawnRecord ApproveSpawn(int peerId, PlayerStartupData data)
    {
        if (!ShipRegistry.TryGetById(data.ShipId, out _)) throw new ArgumentException("Unknown ship: " + data.ShipId);
        if (_records.TryGetValue(peerId, out var existing))
        {
            if (existing.ShipId != data.ShipId) throw new InvalidOperationException("Conflicting ship request.");
            return existing;
        }
        var record = NewWorldState.CreatePlayer(peerId, data, _spawnIndex);
        Spawn(record);
        _spawnIndex++;
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
        PlayerSession? participant = null;
        ShipController? ship = null;
        try
        {
            ship = SpawnEntity(EntitySpawnRecord.From(record), announce: false);
            ship.Runtime.SetFaction(1);
            participant = new PlayerSession(Items) { ControllingPeerId = record.PeerId };
            participant.ControlledShipChanged += TrackBinding;
            participant.AssignShip(ship);
            ship.Restore(record.Position, record.Rotation, record.Velocity, default);
            _controllers.Add(record.PeerId, participant);
            _records.Add(record.PeerId, record);
            if (AuthoritativeItems && InitialInventory is { } initial) InitializePlayerLoadout(record.PeerId, initial);
            else if (AuthoritativeItems) ship.Loadout.InitializeResources();
            EntitySpawned?.Invoke(ship);
            if (IsInsideTree() && record.PeerId == Multiplayer.GetUniqueId()) LocalPlayer?.BindParticipant(participant);
            Logger.Info($"NET spawn peer={record.PeerId} ship={record.ShipId} entity={record.EntityId}");
        }
        catch
        {
            _controllers.Remove(record.PeerId); _records.Remove(record.PeerId);
            if (participant != null)
            {
                participant.AssignShip(null);
                participant.ControlledShipChanged -= TrackBinding;
                participant.Dispose();
            }
            if (ship != null && GodotObject.IsInstanceValid(ship)) { _entities.Remove(ship.EntityId); ship.Loadout.Dispose(); ship.Runtime.Dispose(); ship.Free(); }
            throw;
        }
    }

    public void Despawn(int peerId)
    {
        if (_controllers.Remove(peerId, out var controller))
        {
            if (LocalPlayer?.Participant == controller) LocalPlayer.UnbindParticipant();
            var ship = controller.ControlledShip;
            controller.AssignShip(null);
            if (ship != null) DestroyEntity(ship.EntityId);
            controller.ControlledShipChanged -= TrackBinding;
            controller.Dispose();
        }
        _records.Remove(peerId);
    }

    internal ShipSpawnRecord CaptureConstructionRecord(int peer)
    {
        var ship = _controllers[peer].ControlledShip!;
        var record = new ShipSpawnRecord(peer, ship.EntityId, ship.Data.Name,
            ship.GlobalPosition, ship.GlobalRotation, ship.Velocity);
        _records[peer] = record;
        return record;
    }


    internal void RefreshConstructionRecords()
    {
        foreach (var (peer, owner) in _controllers)
        {
            if (owner.ControlledShip == null) _records.Remove(peer);
            else if (!_records.TryGetValue(peer, out var record) || record.EntityId != owner.ControlledShip.EntityId)
                CaptureConstructionRecord(peer);
        }
    }

    /// <summary>Creates a world-owned ship without requiring a player connection.</summary>
    public ShipController SpawnEntity(EntitySpawnRecord record) => SpawnEntity(record, announce: true);

    private ShipController SpawnEntity(EntitySpawnRecord record, bool announce)
    {
        if (_entities.ContainsKey(record.EntityId))
            throw new InvalidOperationException("Duplicate entity ID.");
        var ship = ShipFactory.Create(ShipRegistry.GetById(record.ShipId), record.EntityId,
            record.Position, record.Rotation, record.Velocity, Items, AuthoritativeItems, Assets);
        try
        {
            AddChild(ship);
            ship.Restore(record.Position, record.Rotation, record.Velocity, default);
            _entities.Add(record.EntityId, ship);
            if (announce)
            {
                if (AuthoritativeItems) ship.Loadout.InitializeResources();
                EntitySpawned?.Invoke(ship);
            }
            return ship;
        }
        catch { _entities.Remove(record.EntityId); ship.Loadout.Dispose(); ship.Runtime.Dispose(); ship.Free(); throw; }
    }

    public void DestroyEntity(string entity)
    {
        if (!_entities.TryGetValue(entity, out var ship)) return;
        foreach (var owner in _controllers.Values)
            if (owner.ControlledShip == ship) { owner.AssignShip(null); _records.Remove(owner.ControllingPeerId); }
        _entities.Remove(entity);
        if (GodotObject.IsInstanceValid(ship)) { ship.Loadout.Dispose(); ship.Runtime.Dispose(); ship.QueueFree(); }
    }

    public bool MatchesTopology(WorldTopology topology)
    {
        if (topology.Participants.Length != _controllers.Count || topology.Entities.Length != _entities.Count) return false;
        foreach (var binding in topology.Participants)
            if (!_controllers.TryGetValue(binding.Peer, out var owner) || owner.ControlGeneration != binding.Generation ||
                (owner.ControlledShip?.EntityId ?? "") != binding.Entity) return false;
        foreach (var record in topology.Entities)
            if (!_entities.TryGetValue(record.EntityId, out var ship) || !GodotObject.IsInstanceValid(ship) || ship.IsQueuedForDeletion()) return false;
        return true;
    }

    public WorldTopology CaptureTopology()
    {
        foreach (var id in new List<string>(_entities.Keys))
            if (!GodotObject.IsInstanceValid(_entities[id]) || _entities[id].IsQueuedForDeletion()) _entities.Remove(id);
        RefreshConstructionRecords();
        var participants = new ParticipantBinding[_controllers.Count];
        var index = 0;
        foreach (var (peer, owner) in _controllers)
            participants[index++] = new ParticipantBinding(peer, owner.ControlGeneration, owner.ControlledShip?.EntityId ?? "");
        var entities = new EntitySpawnRecord[_entities.Count];
        index = 0;
        foreach (var ship in _entities.Values)
            entities[index++] = new EntitySpawnRecord(ship.EntityId, ship.Data.Name, ship.GlobalPosition, ship.GlobalRotation, ship.Velocity);
        var topology = new WorldTopology(participants, entities);
        if (!topology.IsValid()) throw new InvalidOperationException("Invalid world topology.");
        return topology;
    }

    public int[] ApplyTopology(WorldTopology topology)
    {
        if (!topology.IsValid()) throw new InvalidOperationException("Invalid world topology.");
        // Reject stale or conflicting generations before mutating any existing association.
        foreach (var p in topology.Participants)
            if (_controllers.TryGetValue(p.Peer, out var owner) &&
                (p.Generation < owner.ControlGeneration || (p.Generation == owner.ControlGeneration &&
                p.Entity != (owner.ControlledShip?.EntityId ?? ""))))
                throw new InvalidOperationException("Stale control topology.");
        var changed = new List<int>(topology.Participants.Length + _controllers.Count);
        foreach (var participant in topology.Participants)
            if (!_controllers.TryGetValue(participant.Peer, out var owner) ||
                owner.ControlGeneration != participant.Generation || (owner.ControlledShip?.EntityId ?? "") != participant.Entity)
                changed.Add(participant.Peer);
        foreach (var peer in _controllers.Keys)
            if (!HasParticipant(topology, peer)) changed.Add(peer);
        // Detach every changed owner before assigning any ship: transfers and swaps preserve instances.
        foreach (var peer in changed)
            if (_controllers.TryGetValue(peer, out var owner)) owner.AssignShip(null);
        foreach (var record in topology.Entities)
        {
            if (_entities.ContainsKey(record.EntityId)) continue;
            SpawnEntity(record);
        }
        foreach (var p in topology.Participants)
        {
            var owner = EnsureParticipant(p.Peer);
            owner.AssignShip(p.Entity == "" ? null : _entities[p.Entity]);
            owner.ApplyGeneration(p.Generation);
        }
        // Released entities remain frozen, so their final authoritative placement travels with topology.
        foreach (var record in topology.Entities)
            if (!HasController(topology, record.EntityId))
            {
                var ship = _entities[record.EntityId];
                ship.Restore(record.Position, record.Rotation, record.Velocity, default);
                ship.Presentation.Snap();
            }
        foreach (var peer in new List<int>(_controllers.Keys))
            if (!HasParticipant(topology, peer)) Despawn(peer);
        foreach (var entity in new List<string>(_entities.Keys))
            if (!HasEntity(topology, entity)) DestroyEntity(entity);
        _records.Clear();
        foreach (var p in topology.Participants)
            if (p.Entity != "") CaptureConstructionRecord(p.Peer);
        return changed.ToArray();
    }

    private static bool HasParticipant(WorldTopology topology, int peer)
    {
        foreach (var participant in topology.Participants) if (participant.Peer == peer) return true;
        return false;
    }

    private static bool HasController(WorldTopology topology, string entity)
    {
        foreach (var participant in topology.Participants) if (participant.Entity == entity) return true;
        return false;
    }

    private static bool HasEntity(WorldTopology topology, string entity)
    {
        foreach (var record in topology.Entities) if (record.EntityId == entity) return true;
        return false;
    }

    public void Clear()
    {
        foreach (var peerId in new List<int>(_controllers.Keys)) Despawn(peerId);
        foreach (var entity in new List<string>(_entities.Keys)) DestroyEntity(entity);
    }

    public override void _ExitTree() => Clear();
}
