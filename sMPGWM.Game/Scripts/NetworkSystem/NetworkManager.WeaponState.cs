using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, long> _publishedWeaponStateTicks = new();
    private readonly Godot.Collections.Array _weaponStatePayload = new();
    private readonly Godot.Collections.Array?[] _weaponStateRows = new Godot.Collections.Array?[9];
    private readonly long[] _weaponStateIds = new long[9];
    private readonly double[] _weaponStateNext = new double[9];
    private readonly long[] _weaponStateSequences = new long[9];
    private readonly long[] _weaponStateLast = new long[9];
    private readonly WeaponCadence _weaponStateValidation = new();

    private void PublishWeaponStates(long tick)
    {
        foreach (var (peer, player) in World.Players)
        {
            if (peer == 1 || !_joins.Is(peer, PeerJoinPhase.Ready) || !CanSendToPeer(peer) || player.ControlledShip is not { } ship) continue;
            var lastPublished = _publishedWeaponStateTicks.GetValueOrDefault(peer, -1);
            var changed = false;
            foreach (var item in ship.Equipment.Snapshot.Slots)
                if (item?.WeaponState is { } state && state.LastAcceptedTick > lastPublished) { changed = true; break; }
            if (!changed) continue;
            _weaponStatePayload.Clear();
            var count = 0;
            foreach (var item in ship.Equipment.Snapshot.Slots)
                if (item?.WeaponState is { } state)
                {
                    var row = _weaponStateRows[count] ??= new Godot.Collections.Array { 0L, 0.0, 0L, -1L };
                    row[0] = checked((long)item.Id.Value); row[1] = state.NextEligibleTick;
                    row[2] = state.Sequence; row[3] = state.LastAcceptedTick;
                    _weaponStatePayload.Add(row); count++;
                }
            RpcId(peer, nameof(ReceiveWeaponStates), Synchronization!.Session.ToString("N"), player.ControlGeneration,
                ship.Equipment.Revision, tick, _weaponStatePayload);
            _publishedWeaponStateTicks[peer] = tick;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveWeaponStates(string session, long generation, long revision, long tick, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") || !ItemBaselineReady ||
            World.LocalPlayer?.Participant is not { } player || player.ControlGeneration != generation ||
            player.ControlledShip is not { } ship || ship.Equipment.Revision != revision || rows.Count > 9 ||
            tick < 0 || tick > SnapshotEnvelope.MaxTick) return;
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].VariantType != Variant.Type.Array) return;
            var row = rows[i].AsGodotArray();
            if (row.Count != 4 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.Float ||
                row[2].VariantType != Variant.Type.Int || row[3].VariantType != Variant.Type.Int || row[0].AsInt64() <= 0) return;
            _weaponStateIds[i] = row[0].AsInt64();
            for (var j = 0; j < i; j++) if (_weaponStateIds[j] == _weaponStateIds[i]) return;
            ItemEntity? match = null;
            foreach (var item in ship.Equipment.Snapshot.Slots)
                if (item != null && item.Id.Value == unchecked((ulong)row[0].AsInt64())) { match = item; break; }
            if (match?.WeaponState == null || row[3].AsInt64() > tick) return;
            _weaponStateNext[i] = row[1].AsDouble(); _weaponStateSequences[i] = row[2].AsInt64(); _weaponStateLast[i] = row[3].AsInt64();
            try { _weaponStateValidation.Restore(_weaponStateNext[i], _weaponStateSequences[i], _weaponStateLast[i]); }
            catch (ArgumentException) { return; }
        }
        for (var i = 0; i < rows.Count; i++)
            foreach (var item in ship.Equipment.Snapshot.Slots)
                if (item?.WeaponState is { } state && item.Id.Value == (ulong)_weaponStateIds[i] && _weaponStateSequences[i] >= state.Sequence)
                { state.Restore(_weaponStateNext[i], _weaponStateSequences[i], _weaponStateLast[i]); break; }
    }

    private void ClearWeaponStates()
    {
        _publishedWeaponStateTicks.Clear();
        _weaponStatePayload.Dispose();
        foreach (var row in _weaponStateRows) row?.Dispose();
    }
}
