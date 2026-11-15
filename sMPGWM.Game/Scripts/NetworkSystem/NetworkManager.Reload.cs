using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WeaponSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, ReloadHistory> _reloadResults = new();
    private long _nextReloadRequest;
    private int _pendingReloadEntity;
    private long _pendingReloadGeneration;
    private long _pendingReloadEquipment;
    public event Action<ReloadResult>? ReloadFeedback;

    public void ReloadWeapons()
    {
        if (_stopped || Synchronization == null || World.LocalPlayer?.Participant is not { } owner ||
            owner.ControlledShip is not { } ship ||
            (!IsHost() && (_clientJoin.Phase != ClientJoinPhase.Ready || !ItemBaselineReady))) return;
        var items = new Godot.Collections.Array();
        foreach (var item in ship.Equipment.Snapshot.Slots)
            if (item?.Definition is WeaponItemDefinition { Supply: AmmunitionSupplyDefinition } && item.WeaponPlan != null && item.WeaponState != null)
                items.Add(checked((long)item.Id.Value));
        if (items.Count == 0 || items.Count > 9) return;
        var entity = GetWeaponEntityIndex(ship.EntityId);
        if (entity == 0 || _nextReloadRequest >= long.MaxValue - 1) return;
        var request = ++_nextReloadRequest;
        _pendingReloadEntity = entity;
        _pendingReloadGeneration = owner.ControlGeneration;
        _pendingReloadEquipment = ship.Equipment.Revision;
        var payload = new Godot.Collections.Array { Synchronization.Session.ToString("N"), entity,
            owner.ControlGeneration, request, ship.Equipment.Revision, items };
        ReloadFeedback?.Invoke(ReloadResult.Pending);
        if (IsHost()) QueueReload(1, payload);
        else if (RpcId(1, nameof(RequestReload), payload) != Error.Ok) ReloadFeedback?.Invoke(ReloadResult.SendFailed);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestReload(Godot.Collections.Array payload) => QueueReload(Multiplayer.GetRemoteSenderId(), payload);

    private void QueueReload(int peer, Godot.Collections.Array payload)
    {
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || payload.Count != 6 ||
            payload[0].VariantType != Variant.Type.String || payload[0].AsString() != Synchronization.Session.ToString("N") ||
            payload[1].VariantType != Variant.Type.Int || payload[1].AsInt64() is <= 0 or > int.MaxValue ||
            payload[2].VariantType != Variant.Type.Int || payload[2].AsInt64() <= 0 ||
            payload[3].VariantType != Variant.Type.Int || payload[3].AsInt64() is <= 0 or long.MaxValue ||
            payload[4].VariantType != Variant.Type.Int || payload[4].AsInt64() <= 0 ||
            payload[5].VariantType != Variant.Type.Array) return;
        var values = payload[5].AsGodotArray();
        if (values.Count is < 1 or > 9) return;
        var items = new ulong[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].VariantType != Variant.Type.Int || values[i].AsInt64() <= 0) return;
            items[i] = (ulong)values[i].AsInt64();
            for (var j = 0; j < i; j++) if (items[j] == items[i]) return;
        }
        var request = payload[3].AsInt64();
        if (!_reloadResults.TryGetValue(peer, out var history))
        {
            if (_reloadResults.Count == 64) return;
            _reloadResults.Add(peer, history = new());
        }
        if (!history.Admit(request, Math.Max(0, Synchronization.CompletedTick), out var result))
        { ReplyReload(peer, request, result); return; }
        result = ReloadResult.InvalidBinding;
        if (_weaponEntityNames.TryGetValue(payload[1].AsInt32(), out var entity))
        {
            var command = new PendingReload(peer, request, entity, payload[2].AsInt64(), payload[4].AsInt64(), items);
            var commands = Simulation.Weapons.ReloadCommands;
            commands.Completed = CompleteReload;
            if (commands.ValidBinding(command))
            {
                if (commands.TryEnqueue(command)) return;
                result = ReloadResult.Busy;
            }
        }
        CompleteReload(peer, request, result);
    }

    private void CompleteReload(int peer, long request, ReloadResult result)
    {
        if (_reloadResults.TryGetValue(peer, out var history)) history.Complete(request, result);
        ReplyReload(peer, request, result);
    }

    private void ReplyReload(int peer, long request, ReloadResult result)
    {
        if (peer == 1) ApplyReloadResult(request, result);
        else if (Synchronization != null && CanSendToPeer(peer))
            RpcId(peer, nameof(ReceiveReloadResult), Synchronization.Session.ToString("N"), request, (int)result);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveReloadResult(string session, long request, int result)
    {
        if (IsHostMessage() && Synchronization != null && session == Synchronization.Session.ToString("N") &&
            Enum.IsDefined((ReloadResult)result)) ApplyReloadResult(request, (ReloadResult)result);
    }

    private void ApplyReloadResult(long request, ReloadResult result)
    {
        if (request == _nextReloadRequest && World.LocalPlayer?.Participant is { } owner &&
            owner.ControlledShip is { } ship && GetWeaponEntityIndex(ship.EntityId) == _pendingReloadEntity &&
            owner.ControlGeneration == _pendingReloadGeneration && ship.Equipment.Revision == _pendingReloadEquipment)
            ReloadFeedback?.Invoke(result);
    }

    public void CancelPendingReloads(int peer) => _simulation?.Weapons.ReloadCommands.RemovePeer(peer);

    private void ClearReloads()
    {
        _simulation?.Weapons.ReloadCommands.Clear();
        _reloadResults.Clear();
        ReloadFeedback = null;
    }
}
