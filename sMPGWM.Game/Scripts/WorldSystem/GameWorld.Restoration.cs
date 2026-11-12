using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WorldSystem;

public partial class GameWorld
{
    private RestoredWorldState? _restoredState;
    public bool IsRestored => _restoredState != null;
    public long InitialCompletedTick { get; private set; }

    public RestoredWorldState CaptureState(long completedTick, long abilitySequence, ItemHotbarSnapshot? localHotbar = null)
    {
        if (!AuthoritativeItems) throw new InvalidOperationException("Only an authoritative world can be captured.");
        var ships = new RestoredShipState[_entities.Count];
        var index = 0;
        foreach (var ship in _entities.Values)
            ships[index++] = new RestoredShipState(new EntitySpawnRecord(ship.EntityId, ship.Data.Name,
                ship.GlobalPosition, ship.GlobalRotation, ship.Velocity), Items.Capture(ship.Equipment.Container),
                ship.Runtime.CaptureState(), ship.CaptureMovementState());
        var players = new RestoredPlayerState[_controllers.Count];
        index = 0;
        foreach (var owner in _controllers.Values)
            players[index++] = new RestoredPlayerState(new ParticipantBinding(owner.ControllingPeerId,
                owner.ControlGeneration, owner.ControlledShip?.EntityId ?? ""), Items.Capture(owner.Inventory.Container));
        return new RestoredWorldState(completedTick, abilitySequence, _spawnIndex, ships, players, localHotbar);
    }

    public void RestoreState(RestoredWorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (ReferenceEquals(state, _restoredState)) return;
        if (!AuthoritativeItems || _restoredState != null || _controllers.Count != 0 || _entities.Count != 0 || !Items.IsEmpty)
            throw new InvalidOperationException("World restoration requires a fresh authoritative world.");
        var entities = new EntitySpawnRecord[state.Ships.Count];
        for (var i = 0; i < entities.Length; i++)
        {
            var saved = state.Ships[i] ?? throw new ArgumentException("Missing restored ship.");
            entities[i] = saved.Entity;
            if (saved.Entity == null || saved.Runtime == null || saved.Runtime.Behaviours == null ||
                saved.Runtime.AuthoritativeTick > state.CompletedTick)
                throw new ArgumentException("Restored runtime is ahead of the completed world tick.");
            foreach (var behaviour in saved.Runtime.Behaviours)
                if (behaviour.RuntimeId > state.AbilitySequence || behaviour.Deadline <= state.CompletedTick)
                    throw new ArgumentException("Restored effects are expired or would reuse an effect identity.");
        }
        var bindings = new ParticipantBinding[state.Players.Count];
        for (var i = 0; i < bindings.Length; i++)
            bindings[i] = (state.Players[i] ?? throw new ArgumentException("Missing restored participant.")).Binding
                ?? throw new ArgumentException("Missing restored ownership binding.");
        if (!new WorldTopology(bindings, entities).IsValid()) throw new ArgumentException("Invalid restored world topology.");

        var store = new ItemStore();
        var ships = new Dictionary<string, ShipController>(entities.Length, StringComparer.Ordinal);
        var players = new Dictionary<int, PlayerSession>(bindings.Length);
        var records = new Dictionary<int, ShipSpawnRecord>(bindings.Length);
        var containers = new ItemContainerRestoration[entities.Length + bindings.Length];
        try
        {
            for (var i = 0; i < entities.Length; i++)
            {
                var record = entities[i];
                var ship = ShipFactory.Create(ShipRegistry.GetById(record.ShipId), record.EntityId,
                    record.Position, record.Rotation, record.Velocity, store, true);
                ships.Add(record.EntityId, ship);
                containers[i] = new ItemContainerRestoration(ship.Equipment.Container, state.Ships[i].Equipment);
            }
            for (var i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                var owner = new PlayerSession(store) { ControllingPeerId = binding.Peer };
                players.Add(binding.Peer, owner);
                owner.AssignShip(binding.Entity == "" ? null : ships[binding.Entity]);
                owner.ApplyGeneration(binding.Generation);
                owner.InventoryInitialized = true;
                containers[entities.Length + i] = new ItemContainerRestoration(owner.Inventory.Container, state.Players[i].Inventory);
                if (owner.ControlledShip is { } ship)
                    records.Add(binding.Peer, new ShipSpawnRecord(binding.Peer, ship.EntityId, ship.Data.Name,
                        ship.Position, ship.Rotation, ship.Velocity));
            }
            store.Restore(containers);
            foreach (var saved in state.Ships)
            {
                var ship = ships[saved.Entity.EntityId];
                ship.Runtime.RestoreState(saved.Runtime, GameplayDefinitions.Current);
                ship.RestoreMovementState(saved.Movement);
            }
            foreach (var ship in ships.Values) AddChild(ship);
        }
        catch
        {
            foreach (var owner in players.Values) owner.Dispose();
            foreach (var ship in ships.Values)
                if (GodotObject.IsInstanceValid(ship)) { ship.Loadout.Dispose(); ship.Runtime.Dispose(); ship.Free(); }
            throw;
        }

        Items = store;
        _entities = ships;
        _controllers = players;
        _records = records;
        _spawnIndex = state.NextSpawnIndex;
        InitialCompletedTick = state.CompletedTick;
        _restoredState = state;
        foreach (var owner in players.Values) owner.ControlledShipChanged += TrackBinding;
        foreach (var ship in ships.Values) EntitySpawned?.Invoke(ship);
        if (IsInsideTree() && players.TryGetValue(Multiplayer.GetUniqueId(), out var local)) LocalPlayer?.BindParticipant(local);
    }
}
