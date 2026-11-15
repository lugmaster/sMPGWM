using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private sealed class PublishedWeaponStates
    {
        internal long Generation, EquipmentRevision;
        internal int Ship, Count;
        internal readonly ulong[] Ids = new ulong[9];
        internal readonly long[] Revisions = new long[9];
    }

    private readonly Dictionary<int, PublishedWeaponStates> _publishedWeaponStateTicks = new(MovementSystem.MovementWire.MaxPlayers);
    private readonly Godot.Collections.Array _weaponStatePayload = new();
    private readonly Godot.Collections.Array[] _weaponStateRows = CreateWeaponStateRows();
    private readonly ulong[] _weaponStateIds = new ulong[9];
    private readonly WeaponRuntimeState?[] _weaponStateDecoded = new WeaponRuntimeState?[9];
    private readonly WeaponSupplyDefinition?[] _weaponStateSupplies = new WeaponSupplyDefinition?[9];
    private readonly WeaponRuntimeState?[] _weaponStateTargets = new WeaponRuntimeState?[9];

    private static Godot.Collections.Array[] CreateWeaponStateRows()
    {
        var rows = new Godot.Collections.Array[9];
        for (var i = 0; i < rows.Length; i++)
            rows[i] = new() { 0L, 0L, 0.0, 0L, -1L, 0, -1L, -1L };
        return rows;
    }

    private void PrepareWeaponReplication(PlayerSession player)
    {
        if (player.ControlledShip is not { } ship) return;
        ship.Weapons.Refresh();
        if (IsHost())
        {
            if (!_publishedWeaponStateTicks.ContainsKey(player.ControllingPeerId))
                _publishedWeaponStateTicks.Add(player.ControllingPeerId, new());
            return;
        }
        var slots = ship.Equipment.Snapshot.Slots;
        var count = 0;
        for (var i = 0; i < slots.Count; i++)
        {
            if (slots[i]?.WeaponState == null || slots[i]!.Definition is not WeaponItemDefinition weapon) continue;
            if (count == _weaponStateDecoded.Length) throw new InvalidOperationException("Equipped weapon state exceeds protocol capacity.");
            if (!ReferenceEquals(_weaponStateSupplies[count], weapon.Supply))
            {
                _weaponStateSupplies[count] = weapon.Supply;
                _weaponStateDecoded[count] = new(weapon.Supply);
            }
            count++;
        }
    }

    private void PublishWeaponStates(long tick)
    {
        foreach (var player in World.Participants)
        {
            var peer = player.ControllingPeerId;
            if (peer == 1 || !_joins.Is(peer, PeerJoinPhase.Ready) || !CanSendToPeer(peer) || player.ControlledShip is not { } ship) continue;
            var published = _publishedWeaponStateTicks[peer];
            var shipIndex = GetWeaponEntityIndex(ship.EntityId);
            var changed = published.Ship != shipIndex || published.Generation != player.ControlGeneration || published.EquipmentRevision != ship.Equipment.Revision;
            var count = 0;
            var slots = ship.Equipment.Snapshot.Slots;
            for (var slot = 0; slot < slots.Count; slot++)
            {
                var item = slots[slot];
                if (item?.WeaponState is { } state)
                {
                    if (count == 9) throw new InvalidOperationException("Equipped weapon state exceeds protocol capacity.");
                    changed |= published.Ids[count] != item.Id.Value || published.Revisions[count] != state.Revision;
                    count++;
                }
            }
            if (!changed && published.Count == count) continue;
            _weaponStatePayload.Clear();
            count = 0;
            for (var slot = 0; slot < slots.Count; slot++)
            {
                var item = slots[slot];
                if (item?.WeaponState is { } state)
                {
                    var row = _weaponStateRows[count];
                    row[0] = checked((long)item.Id.Value); row[1] = state.Revision; row[2] = state.NextEligibleTick;
                    row[3] = state.Sequence; row[4] = state.LastAcceptedTick; row[5] = state.Rounds;
                    row[6] = state.ReloadStartTick; row[7] = state.ReloadDeadlineTick;
                    _weaponStatePayload.Add(row); count++;
                }
            }
            if (RpcId(peer, MethodName.ReceiveWeaponStates, (ReadOnlySpan<Variant>)[Synchronization!.SessionWireIdentity,
                shipIndex, player.ControlGeneration, ship.Equipment.Revision, tick, _weaponStatePayload]) != Error.Ok) continue;
            published.Ship = shipIndex; published.Generation = player.ControlGeneration;
            published.EquipmentRevision = ship.Equipment.Revision; published.Count = count;
            for (var i = 0; i < count; i++)
            {
                published.Ids[i] = (ulong)_weaponStateRows[i]![0].AsInt64();
                published.Revisions[i] = _weaponStateRows[i]![1].AsInt64();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponStates(string session, int shipIndex, long generation, long revision, long tick, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.SessionWireIdentity || !ItemBaselineReady ||
            World.LocalPlayer?.Participant is not { } player || player.ControlGeneration != generation ||
            player.ControlledShip is not { } ship || ship.Equipment.Revision != revision || rows.Count > 9 ||
            !_weaponEntityNames.TryGetValue(shipIndex, out var entity) || entity != ship.EntityId ||
            tick < 0 || tick > SnapshotEnvelope.MaxTick) return;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].VariantType != Variant.Type.Array) return;
            var row = rows[i].AsGodotArray();
            if (row.Count != 8) return;
            for (var field = 0; field < 8; field++)
                if (row[field].VariantType != (field == 2 ? Variant.Type.Float : Variant.Type.Int)) return;
            if (row[0].AsInt64() <= 0 || row[5].AsInt64() < 0 || row[5].AsInt64() > int.MaxValue) return;
            _weaponStateIds[i] = (ulong)row[0].AsInt64();
            for (var j = 0; j < i; j++) if (_weaponStateIds[j] == _weaponStateIds[i]) return;
            ItemEntity? match = null;
            var slots = ship.Equipment.Snapshot.Slots;
            for (var slot = 0; slot < slots.Count; slot++)
                if (slots[slot] is { } item && item.Id.Value == _weaponStateIds[i]) { match = item; break; }
            if (match?.WeaponState is not { } current || match.Definition is not WeaponItemDefinition weapon ||
                row[4].AsInt64() > tick || row[6].AsInt64() > tick || row[7].AsInt64() >= 0 && row[7].AsInt64() <= tick) return;
            if (!ReferenceEquals(_weaponStateSupplies[i], weapon.Supply)) return;
            var decoded = _weaponStateDecoded[i]!;
            try { decoded.Restore(row[1].AsInt64(), row[2].AsDouble(), row[3].AsInt64(), row[4].AsInt64(),
                row[5].AsInt32(), row[6].AsInt64(), row[7].AsInt64()); }
            catch (ArgumentException) { return; }
            if (decoded.Revision == current.Revision && !decoded.SameAs(current)) return;
            if (decoded.Revision > current.Revision && (decoded.Sequence < current.Sequence ||
                decoded.LastAcceptedTick < current.LastAcceptedTick || decoded.NextEligibleTick < current.NextEligibleTick)) return;
            _weaponStateTargets[i] = current;
        }
        for (var i = 0; i < rows.Count; i++)
        {
            var state = _weaponStateTargets[i]!;
            var decoded = _weaponStateDecoded[i]!;
            if (decoded.Revision <= state.Revision) continue;
            state.Restore(decoded.Revision, decoded.NextEligibleTick, decoded.Sequence, decoded.LastAcceptedTick,
                decoded.Rounds, decoded.ReloadStartTick, decoded.ReloadDeadlineTick);
        }
    }

    private void ClearWeaponStates()
    {
        _publishedWeaponStateTicks.Clear();
        _weaponStatePayload.Dispose();
        foreach (var row in _weaponStateRows) row?.Dispose();
        Array.Clear(_weaponStateDecoded); Array.Clear(_weaponStateSupplies); Array.Clear(_weaponStateTargets);
    }
}
