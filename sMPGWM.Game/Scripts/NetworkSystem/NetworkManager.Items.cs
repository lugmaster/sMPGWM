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
        _pendingItemRequest = ++_nextItemRequest;
        var result = RpcId(1, nameof(TransferItems), Synchronization.Session.ToString("N"), _pendingItemRequest,
            player.ControlledShip?.EntityId ?? "", player.ControlGeneration,
            request.Source.InventoryAddress ?? -1, request.Source.EquipmentSlotKey ?? "", checked((long)request.SourceEntity.Value),
            request.Destination.InventoryAddress ?? -1, request.Destination.EquipmentSlotKey ?? "",
            request.DestinationEntity is { } target ? checked((long)target.Value) : 0L,
            request.InventoryRevision, request.EquipmentRevision);
        if (result == Error.Ok) return TransferResult.Pending;
        _pendingItemRequest = 0;
        return TransferResult.InvalidSession;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TransferItems(string session, long request, string entity, long generation, int sourceIndex, string sourceSlot, long sourceId,
        int targetIndex, string targetSlot, long targetId, long inventoryRevision, long equipmentRevision)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            request <= 0 || generation < 0 || entity.Length is not (0 or 32) || !World.Players.ContainsKey(peer)) return;
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
                if (Simulation.QueueTransfer(peer, entity, generation, transfer,
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
        if (RpcId(1, nameof(ChangeWeaponGroups), Synchronization!.Session.ToString("N"), _pendingItemRequest,
                player.ControlGeneration, slot, identity, revision, primary, secondary) == Error.Ok) return TransferResult.Pending;
        _pendingItemRequest = 0;
        return TransferResult.InvalidSession;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ChangeWeaponGroups(string session, long request, long generation, int slot, long identity, long revision, bool primary, bool secondary)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            !World.Players.TryGetValue(peer, out var player) || player.ControlledShip is not { } ship || request <= 0) return;
        if (_groupOutcomes.TryGetValue(peer, out var cached) && cached.Request == request)
        {
            ReplyItemTransfer(peer, request, cached.Result, ship.EntityId, player.ControlGeneration);
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
        ReplyItemTransfer(peer, request, result, ship.EntityId, player.ControlGeneration);
    }

    private static bool ValidCell(int index, string slot) =>
        index >= 0 && index < ItemSnapshotCodec.MaxInventorySlots && slot.Length == 0 ||
        index == -1 && slot.Length is > 0 and <= 64;
    private static ItemCellAddress Cell(int index, string slot) => index >= 0 ? ItemCellAddress.Inventory(index) : ItemCellAddress.Equipment(slot);

    private bool SendItems(int peer)
    {
        var player = World.Players[peer];
        if (RpcId(peer, nameof(ReceiveItems), Synchronization!.Session.ToString("N"), ItemSnapshotCodec.Encode(player)) != Error.Ok)
        {
            FailSession("Inventory snapshot send failed.");
            return false;
        }
        _publishedItemRevisions[peer] = (player.ControlGeneration, player.Inventory.Revision, player.ControlledShip?.Equipment.Revision ?? 0);
        return true;
    }

    private void PublishItemChanges()
    {
        foreach (var (recipient, _) in World.Players)
        {
            if (recipient == 1 || !(_joins.Is(recipient, PeerJoinPhase.Spawning) || _joins.Is(recipient, PeerJoinPhase.Ready))) continue;
            foreach (var (owner, participant) in World.Players)
            {
                if (owner == recipient || participant.ControlledShip is not { } ship) continue;
                var key = (recipient, owner);
                var version = (participant.ControlGeneration, ship.Equipment.Revision);
                if (_groupPublished.TryGetValue(key, out var previous) && previous == version) continue;
                if (RpcId(recipient, nameof(ReceiveRemoteEquipment), Synchronization!.Session.ToString("N"), owner,
                        participant.ControlGeneration, ship.Equipment.Revision, ItemSnapshotCodec.EncodeSlots(ship.Equipment.Snapshot.Slots),
                        WeaponGroupSnapshotCodec.Encode(ship.Equipment.Snapshot.WeaponGroups)) != Error.Ok)
                { FailSession("Equipment snapshot send failed."); return; }
                _groupPublished[key] = version;
            }
        }

        foreach (var (peer, player) in World.Players)
        {
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
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") || !_clientJoin.HasWorld ||
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

    private void ReplyItemTransfer(int peer, long request, TransferResult result, string entity, long generation)
    {
        if (!World.Players.ContainsKey(peer) || _stopped) return;
        PublishWorldTopology();
        // The reliable owner snapshot precedes the acknowledgement, including stale-request recovery.
        if (SendItems(peer) && RpcId(peer, nameof(ItemTransferResult), Synchronization!.Session.ToString("N"),
                request, (int)result, entity, generation) != Error.Ok) FailSession("Item transfer acknowledgement failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveItems(string session, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") || !_clientJoin.HasWorld) return;
        if (World.LocalPlayer?.Participant is not { } owner) return;
        if (ItemSnapshotCodec.IsOldBinding(rows, owner)) return;
        if (!ItemSnapshotCodec.Apply(rows, owner))
        {
            FailSession("Invalid inventory snapshot.");
            return;
        }
        var wasReady = _hasItemBaseline;
        _hasItemBaseline = true;
        if (!wasReady) ItemBindingChanged?.Invoke();
        if (_stopped || !_clientJoin.ApplyItems(Time.GetTicksMsec())) return;
        if (RpcId(1, nameof(ConfirmSpawnReady), session, owner.ControlledShip?.EntityId ?? "",
                owner.ControlGeneration) != Error.Ok) FailSession("Spawn acknowledgement failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ItemTransferResult(string session, long request, int result, string entity, long generation)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            !_hasItemBaseline || !Enum.IsDefined((TransferResult)result)) return;
        if (request != _pendingItemRequest || request == 0 || World.LocalPlayer?.Participant is not { } player ||
            player.ControlGeneration != generation || (player.ControlledShip?.EntityId ?? "") != entity) return;
        _pendingItemRequest = 0;
        WeaponGroupRequestFinished?.Invoke();
        if ((TransferResult)result is not (TransferResult.Success or TransferResult.NoChange))
            ItemFeedback?.Invoke("Item transfer rejected: " + (TransferResult)result);
    }

    private void RemovePlayerItems(int peer)
    {
        _publishedWeaponStateTicks.Remove(peer);
        _groupOutcomes.Clear();
        _groupPublished.Clear();
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
