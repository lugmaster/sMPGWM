using System;
using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<string, GameplayDeltaState> _gameplayPublishers = new(WorldTopology.MaxEntities, StringComparer.Ordinal);
    private readonly GameplayDeltaReceiver _gameplayReceiver = new();
    private readonly Array _gameplayDeltaRows = new();
    private Func<int, string?>? _resolveGameplayEntity;
    private readonly string?[] _removedGameplayEntities = new string?[WorldTopology.MaxEntities];

    private void PrepareGameplayReplication(sMPGWM.Scripts.ShipSystem.ShipController ship)
    {
        if (Startup.Role != NetworkRole.Host) return;
        if (!_gameplayPublishers.ContainsKey(ship.EntityId)) _gameplayPublishers.Add(ship.EntityId, new());
    }

    private void PruneGameplayDeltas()
    {
        var count = 0;
        foreach (var entity in _gameplayPublishers.Keys)
            if (!World.Entities.ContainsKey(entity)) _removedGameplayEntities[count++] = entity;
        for (var i = 0; i < count; i++)
        {
            var entity = _removedGameplayEntities[i]!;
            _gameplayPublishers[entity].Dispose(); _gameplayPublishers.Remove(entity); _removedGameplayEntities[i] = null;
        }
        _gameplayReceiver.Prune(World);
    }

    private void PublishGameplayDeltas()
    {
        _gameplayDeltaRows.Clear();
        foreach (var ship in World.Ships)
        {
            var publisher = _gameplayPublishers[ship.EntityId];
            if (publisher.Encode(GetWeaponEntityIndex(ship.EntityId), ship.Runtime) is { } row) _gameplayDeltaRows.Add(row);
        }
        if (_gameplayDeltaRows.Count == 0 && !_gameplayDirty) return;
        Array? baseline = null;
        foreach (var (peer, join) in _joins.Snapshot())
        {
            if (!join.ReceivesWorld || !CanSendToPeer(peer)) continue;
            var result = join.Phase == PeerJoinPhase.Ready
                ? RpcId(peer, MethodName.ReceiveGameplayDeltas, (ReadOnlySpan<Variant>)[Synchronization!.SessionWireIdentity, _gameplayDeltaRows])
                : RpcId(peer, MethodName.ReceiveGameplay, (ReadOnlySpan<Variant>)[Synchronization!.SessionWireIdentity, baseline ??= GameplaySnapshot()]);
            if (result != Error.Ok) { FailSession("Gameplay update send failed."); break; }
        }
        baseline?.Dispose();
        _gameplayDirty = false;
    }

    private void SeedGameplayDeltas(GameplaySnapshotEntry[] entries)
    {
        foreach (var entry in entries)
            if (World.Entities.TryGetValue(entry.Entity, out var ship)) _gameplayReceiver.Seed(entry.Entity, ship.Runtime);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveGameplayDeltas(string session, Array rows)
    {
        if (!IsHostMessage() || _clientJoin.Phase != ClientJoinPhase.Ready || session != Synchronization!.SessionWireIdentity) return;
        if (!_gameplayReceiver.Apply(rows, World, Abilities.Definitions, _resolveGameplayEntity ??= ResolveNetworkEntity))
            FailSession("Invalid gameplay update.");
    }

    private void ClearGameplayDeltas()
    {
        foreach (var publisher in _gameplayPublishers.Values) publisher.Dispose();
        _gameplayPublishers.Clear(); _gameplayReceiver.Dispose(); _gameplayDeltaRows.Dispose();
    }
}
