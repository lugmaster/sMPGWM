using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    private readonly Dictionary<int, (long Request, TransferResult Result)> _groupOutcomes = new();
    public bool WeaponGroupRequestPending => _pendingItemRequest != 0;
    public event Action? WeaponGroupRequestFinished;
    private readonly Dictionary<int, long> _itemRequests = new();
    private readonly Dictionary<int, ulong> _itemRequestTimes = new();
    private readonly Dictionary<int, (long Generation, long Inventory, long Equipment)> _publishedItemRevisions = new();
    private readonly Dictionary<(int Recipient, int Owner), (long Generation, long Revision)> _groupPublished = new();
    private long _nextItemRequest;
    private long _pendingItemRequest;
    private bool _hasItemBaseline;
    public bool ItemBaselineReady => IsHost() || _hasItemBaseline;
    public event Action? ItemBindingChanged;
    public event Action<string>? ItemFeedback;

    internal void ResetItemBinding()
    {
        _hasItemBaseline = false;
        _pendingItemRequest = 0;
        ItemBindingChanged?.Invoke();
    }

    public TransferResult RequestItemTransfer(TransferRequest request)
    {
        if (_stopped || Synchronization == null || World.LocalPlayer?.Participant is not { } player ||
            (IsHost() ? !MovementReady(player.ControllingPeerId) : _clientJoin.Phase != ClientJoinPhase.Ready || !_hasItemBaseline))
            return TransferResult.InvalidSession;
        if (IsHost())
        {
            return Simulation.QueueTransfer(player.ControllingPeerId, player.ControlledShip?.EntityId ?? "",
                player.ControlGeneration, request, result =>
                {
                    if (result is not (TransferResult.Success or TransferResult.NoChange))
                        ItemFeedback?.Invoke("Item transfer rejected: " + result);
                }) ? TransferResult.Pending : TransferResult.PublicationInProgress;
        }
        if (_pendingItemRequest != 0) return TransferResult.Pending;
        var entityIndex = player.ControlledShip is { } controlled ? GetWeaponEntityIndex(controlled.EntityId) : 0;
        if (player.ControlledShip != null && entityIndex == 0) return TransferResult.InvalidSession;
        _pendingItemRequest = ++_nextItemRequest;
        var result = RpcId(1, nameof(TransferItems), Synchronization.SessionWireIdentity, _pendingItemRequest,
            entityIndex, player.ControlGeneration,
            request.Source.InventoryAddress ?? -1, request.Source.EquipmentSlotKey ?? "", checked((long)request.SourceEntity.Value),
            request.Destination.InventoryAddress ?? -1, request.Destination.EquipmentSlotKey ?? "",
            request.DestinationEntity is { } target ? checked((long)target.Value) : 0L,
            request.InventoryRevision, request.EquipmentRevision);
        if (result == Error.Ok) return TransferResult.Pending;
        _pendingItemRequest = 0;
        return TransferResult.InvalidSession;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TransferItems(string session, long request, int entity, long generation, int sourceIndex, string sourceSlot, long sourceId,
        int targetIndex, string targetSlot, long targetId, long inventoryRevision, long equipmentRevision)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.SessionWireIdentity ||
            request <= 0 || generation < 0 || entity < 0 || !World.Players.TryGetValue(peer, out var owner)) return;
        var entityName = entity == 0 ? "" : ResolveNetworkEntity(entity);
        if (entityName == null || (owner.ControlledShip?.EntityId ?? "") != entityName || owner.ControlGeneration != generation) return;
        if (!AllowRequest(_itemRequestTimes, peer, 30))
        {
            ReplyItemTransfer(peer, request, TransferResult.PublicationInProgress, entity, generation);
            return;
        }
        var result = TransferResult.StaleRevision;
        if (!_itemRequests.TryGetValue(peer, out var previous) || request > previous)
        {
            _itemRequests[peer] = request;
            if (sourceId <= 0 || targetId < 0 || !ValidCell(sourceIndex, sourceSlot) || !ValidCell(targetIndex, targetSlot))
                result = TransferResult.InvalidLocation;
            else
            {
                var transfer = new TransferRequest(0, Cell(sourceIndex, sourceSlot), ItemEntityId.FromNetwork(sourceId),
                    Cell(targetIndex, targetSlot), targetId == 0 ? null : ItemEntityId.FromNetwork(targetId), inventoryRevision, equipmentRevision);
                if (Simulation.QueueTransfer(peer, entityName, generation, transfer,
                    outcome => ReplyItemTransfer(peer, request, outcome, entity, generation))) return;
                result = TransferResult.PublicationInProgress;
            }
        }
        ReplyItemTransfer(peer, request, result, entity, generation);
    }

    public TransferResult RequestWeaponGroups(int slot, long identity, long revision, bool primary, bool secondary)
    {
        if (_stopped || !ItemBaselineReady || World.LocalPlayer?.Participant is not { } player || player.ControlledShip is not { } ship)
            return TransferResult.InvalidSession;
        if (IsHost()) return ship.Equipment.SetWeaponGroups(slot, ItemEntityId.FromNetwork(identity), revision, primary, secondary);
        if (_pendingItemRequest != 0) return TransferResult.Pending;
        _pendingItemRequest = ++_nextItemRequest;
        if (RpcId(1, nameof(ChangeWeaponGroups), Synchronization!.SessionWireIdentity, _pendingItemRequest,
                player.ControlGeneration, slot, identity, revision, primary, secondary) == Error.Ok) return TransferResult.Pending;
        _pendingItemRequest = 0;
        return TransferResult.InvalidSession;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ChangeWeaponGroups(string session, long request, long generation, int slot, long identity, long revision, bool primary, bool secondary)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.SessionWireIdentity ||
            !World.Players.TryGetValue(peer, out var player) || player.ControlledShip is not { } ship || request <= 0) return;
        if (_groupOutcomes.TryGetValue(peer, out var cached) && cached.Request == request)
        {
            ReplyItemTransfer(peer, request, cached.Result, GetWeaponEntityIndex(ship.EntityId), player.ControlGeneration);
            return;
        }
        var result = TransferResult.StaleRevision;
        if (AllowRequest(_itemRequestTimes, peer, 30) && (!_itemRequests.TryGetValue(peer, out var previous) || request > previous))
        {
            _itemRequests[peer] = request;
            if (generation == player.ControlGeneration && identity > 0)
                result = ship.Equipment.SetWeaponGroups(slot, ItemEntityId.FromNetwork(identity), revision, primary, secondary);
        }
        _groupOutcomes[peer] = (request, result);
        ReplyItemTransfer(peer, request, result, GetWeaponEntityIndex(ship.EntityId), player.ControlGeneration);
    }

    private static bool ValidCell(int index, string slot) =>
        index >= 0 && index < ItemSnapshotCodec.MaxInventorySlots && slot.Length == 0 ||
        index == -1 && slot.Length is > 0 and <= 64;
    private static ItemCellAddress Cell(int index, string slot) => index >= 0 ? ItemCellAddress.Inventory(index) : ItemCellAddress.Equipment(slot);

    private bool SendItems(int peer)
    {
        var player = World.Players[peer];
        PrepareWeaponReplication(player);
        using var payload = ItemSnapshotCodec.Encode(player);
        payload[2] = player.ControlledShip is { } ship ? GetWeaponEntityIndex(ship.EntityId) : 0;
        if (RpcId(peer, nameof(ReceiveItems), Synchronization!.SessionWireIdentity, payload) != Error.Ok)
        {
            FailSession("Inventory snapshot send failed.");
            return false;
        }
        _publishedItemRevisions[peer] = (player.ControlGeneration, player.Inventory.Revision, player.ControlledShip?.Equipment.Revision ?? 0);
        return true;
    }

    private void PublishItemChanges()
    {
        foreach (var participant in World.Participants)
        {
            if (participant.ControlledShip is not { } ship) continue;
            var owner = participant.ControllingPeerId;
            var version = (participant.ControlGeneration, ship.Equipment.Revision);
            Godot.Collections.Array? slots = null, groups = null;
            Variant slotsValue = default, groupsValue = default;
            try
            {
                foreach (var recipientPlayer in World.Participants)
                {
                    var recipient = recipientPlayer.ControllingPeerId;
                    if (recipient == 1 || recipient == owner ||
                        !(_joins.Is(recipient, PeerJoinPhase.Spawning) || _joins.Is(recipient, PeerJoinPhase.Ready))) continue;
                    var key = (recipient, owner);
                    if (_groupPublished.TryGetValue(key, out var previous) && previous == version) continue;
                    if (slots == null)
                    {
                        var snapshot = ship.Equipment.Snapshot;
                        slots = ItemSnapshotCodec.EncodeSlots(snapshot.Slots);
                        groups = WeaponGroupSnapshotCodec.Encode(snapshot.WeaponGroups);
                        slotsValue = slots;
                        groupsValue = groups;
                    }
                    if (RpcId(recipient, nameof(ReceiveRemoteEquipment), (ReadOnlySpan<Variant>)[
                            Synchronization!.SessionWireIdentity, owner, participant.ControlGeneration,
                            ship.Equipment.Revision, slotsValue, groupsValue]) != Error.Ok)
                    { FailSession("Equipment snapshot send failed."); return; }
                    _groupPublished[key] = version;
                }
            }
            finally
            {
                slotsValue.Dispose(); groupsValue.Dispose();
                slots?.Dispose(); groups?.Dispose();
            }
        }

        foreach (var player in World.Participants)
        {
            var peer = player.ControllingPeerId;
            if (peer == 1 || !(_joins.Is(peer, PeerJoinPhase.Spawning) || _joins.Is(peer, PeerJoinPhase.Ready))) continue;
            if (!_publishedItemRevisions.TryGetValue(peer, out var published) ||
                published != (player.ControlGeneration, player.Inventory.Revision, player.ControlledShip?.Equipment.Revision ?? 0))
                if (!SendItems(peer)) return;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveRemoteEquipment(string session, int owner, long generation, long revision, Godot.Collections.Array rows,
        Godot.Collections.Array groupRows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.SessionWireIdentity || !_clientJoin.HasWorld ||
            !World.Players.TryGetValue(owner, out var player) || generation != player.ControlGeneration || player.ControlledShip is not { } ship ||
            rows.Count != ship.Equipment.Definitions.Count || revision < ship.Equipment.Revision || revision == long.MaxValue) return;
        if (!ItemSnapshotCodec.DecodeSlots(rows, new HashSet<long>(), out var slots) ||
            !WeaponGroupSnapshotCodec.TryDecode(groupRows, out var groups))
        {
            FailSession("Invalid equipment snapshot.");
            return;
        }
        Action installGroups;
        try { installGroups = ship.Equipment.PrepareWeaponGroupConfirmation(slots, revision, groups); }
        catch (ArgumentException) { FailSession("Invalid weapon group snapshot."); return; }
        if (!World.Items.Confirm(new[] { new ItemContainerConfirmation(ship.Equipment.Container, slots, revision) }, installGroups))
            FailSession("Invalid equipment snapshot.");
    }

    private void ReplyItemTransfer(int peer, long request, TransferResult result, int entity, long generation)
    {
        if (!World.Players.ContainsKey(peer) || _stopped) return;
        PublishWorldTopology();
        // The reliable owner snapshot precedes the acknowledgement, including stale-request recovery.
        if (SendItems(peer) && RpcId(peer, nameof(ItemTransferResult), Synchronization!.SessionWireIdentity,
                request, (int)result, entity, generation) != Error.Ok) FailSession("Item transfer acknowledgement failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveItems(string session, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.SessionWireIdentity || !_clientJoin.HasWorld) return;
        if (World.LocalPlayer?.Participant is not { } owner) return;
        if (ItemSnapshotCodec.IsOldBinding(rows, owner)) return;
        if (rows.Count != 10 || rows[2].VariantType != Variant.Type.Int || rows[2].AsInt64() is < 0 or > int.MaxValue)
        { FailSession("Invalid inventory binding."); return; }
        var index = rows[2].AsInt32();
        var entity = index == 0 ? "" : ResolveNetworkEntity(index);
        if (entity == null) { FailSession("Unknown inventory binding."); return; }
        rows[2] = entity;
        if (!ItemSnapshotCodec.Apply(rows, owner))
        {
            FailSession("Invalid inventory snapshot.");
            return;
        }
        PrepareWeaponReplication(owner);
        var wasReady = _hasItemBaseline;
        _hasItemBaseline = true;
        if (!wasReady) ItemBindingChanged?.Invoke();
        if (_stopped || !_clientJoin.ApplyItems(Time.GetTicksMsec())) return;
        if (RpcId(1, nameof(ConfirmSpawnReady), session, owner.ControlledShip is { } controlled ? GetWeaponEntityIndex(controlled.EntityId) : 0,
                owner.ControlGeneration) != Error.Ok) FailSession("Spawn acknowledgement failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ItemTransferResult(string session, long request, int result, int entity, long generation)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.SessionWireIdentity ||
            !_hasItemBaseline || !Enum.IsDefined((TransferResult)result)) return;
        if (request != _pendingItemRequest || request == 0 || World.LocalPlayer?.Participant is not { } player ||
            player.ControlGeneration != generation || entity < 0 ||
            (player.ControlledShip?.EntityId ?? "") != (entity == 0 ? "" : ResolveNetworkEntity(entity))) return;
        _pendingItemRequest = 0;
        WeaponGroupRequestFinished?.Invoke();
        if ((TransferResult)result is not (TransferResult.Success or TransferResult.NoChange))
            ItemFeedback?.Invoke("Item transfer rejected: " + (TransferResult)result);
    }

    private void RemovePlayerItems(int peer)
    {
        _publishedWeaponStateTicks.Remove(peer);
        _groupOutcomes.Remove(peer);
        foreach (var key in _groupPublished.Keys)
            if (key.Recipient == peer || key.Owner == peer) _groupPublished.Remove(key);
        _itemRequests.Remove(peer);
        _itemRequestTimes.Remove(peer);
        _publishedItemRevisions.Remove(peer);
    }

    private void ClearItems()
    {
        _groupOutcomes.Clear();
        _groupPublished.Clear();
        _itemRequests.Clear();
        _itemRequestTimes.Clear();
        _publishedItemRevisions.Clear();
        _pendingItemRequest = 0;
        _hasItemBaseline = false;
        WeaponGroupRequestFinished = null;
        ItemFeedback = null;
        ItemBindingChanged = null;
    }
}
