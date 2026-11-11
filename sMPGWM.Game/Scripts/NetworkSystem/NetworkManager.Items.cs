using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;

namespace sMPGWM.Scripts.NetworkSystem;

public partial class NetworkManager
{
    public PlayerItemState LocalItems { get; init; } = null!;
    internal InventorySnapshot InitialItems { get; init; } = null!;
    private readonly Dictionary<int, PlayerItemState> _playerItems = new();
    private readonly Dictionary<int, long> _itemRequests = new();
    private readonly Dictionary<int, ulong> _itemRequestTimes = new();
    private readonly Dictionary<int, (long Inventory, long Equipment)> _publishedItemRevisions = new();
    private long _nextItemRequest;
    private long _pendingItemRequest;
    private bool _hasItemBaseline;
    public event Action<string>? ItemFeedback;

    private PlayerItemState EnsurePlayerItems(int peer)
    {
        if (_playerItems.TryGetValue(peer, out var existing)) return existing;
        var items = peer == 1 ? LocalItems : new PlayerItemState();
        if (peer != 1)
            foreach (var item in InitialItems.Slots)
                if (item != null && items.Store.CreateItems(items.Inventory.Container, item.DefinitionId, item.Count) != InventoryResult.Success)
                    throw new InvalidOperationException("Could not create participant inventory.");
        EquipStarterLoadout(items);
        items.Bind(World.Players[peer], authoritative: true);
        _playerItems.Add(peer, items);
        return items;
    }

    private static void EquipStarterLoadout(PlayerItemState items)
    {
        foreach (var slot in items.Equipment.Definitions)
        {
            var index = items.Equipment.FindSlot(slot.Key);
            if (items.Equipment.Snapshot.Slots[index] != null) continue;
            var definition = "mock_standard_" + slot.AcceptedCategory;
            var sourceIndex = -1;
            for (var i = 0; i < items.Inventory.Snapshot.Slots.Count; i++)
                if (items.Inventory.Snapshot.Slots[i]?.DefinitionId == definition)
                { sourceIndex = i; break; }
            if (sourceIndex < 0)
            {
                if (items.Store.CreateItems(items.Inventory.Container, definition, 1) != InventoryResult.Success)
                    throw new InvalidOperationException("Could not create starter equipment: " + definition);
                for (var i = 0; i < items.Inventory.Snapshot.Slots.Count; i++)
                    if (items.Inventory.Snapshot.Slots[i]?.DefinitionId == definition)
                    { sourceIndex = i; break; }
            }
            var item = items.Inventory.Snapshot.Slots[sourceIndex]!;
            if (items.Store.TransferItem(item.Id, new(items.Inventory.Container.Id, sourceIndex),
                    new(items.Equipment.Container.Id, index), items.Inventory.Revision, items.Equipment.Revision)
                != TransferResult.Success)
                throw new InvalidOperationException("Could not equip starter item: " + definition);
        }
    }

    public TransferResult RequestItemTransfer(TransferRequest request)
    {
        if (_stopped || Synchronization == null || World.LocalPlayer?.Participant is not { } player ||
            (IsHost() ? !MovementReady(player.ControllingPeerId) : _clientJoin.Phase != ClientJoinPhase.Ready || !_hasItemBaseline))
            return TransferResult.InvalidSession;
        if (IsHost()) return EnsurePlayerItems(1).Transfer(request);
        if (_pendingItemRequest != 0) return TransferResult.Pending;
        _pendingItemRequest = ++_nextItemRequest;
        var result = RpcId(1, nameof(TransferItems), Synchronization.Session.ToString("N"), _pendingItemRequest,
            request.Source.InventoryAddress ?? -1, request.Source.EquipmentSlotKey ?? "", checked((long)request.SourceEntity.Value),
            request.Destination.InventoryAddress ?? -1, request.Destination.EquipmentSlotKey ?? "",
            request.DestinationEntity is { } target ? checked((long)target.Value) : 0L,
            request.InventoryRevision, request.EquipmentRevision);
        if (result == Error.Ok) return TransferResult.Pending;
        _pendingItemRequest = 0;
        return TransferResult.InvalidSession;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TransferItems(string session, long request, int sourceIndex, string sourceSlot, long sourceId,
        int targetIndex, string targetSlot, long targetId, long inventoryRevision, long equipmentRevision)
    {
        var peer = Multiplayer.GetRemoteSenderId();
        if (!IsHost() || !MovementReady(peer) || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            request <= 0 || !_playerItems.TryGetValue(peer, out var items)) return;
        if (!AllowRequest(_itemRequestTimes, peer, 30))
        {
            ReplyItemTransfer(peer, request, TransferResult.PublicationInProgress);
            return;
        }
        var result = TransferResult.StaleRevision;
        if (!_itemRequests.TryGetValue(peer, out var previous) || request > previous)
        {
            _itemRequests[peer] = request;
            if (sourceId <= 0 || targetId < 0 || !ValidCell(sourceIndex, sourceSlot) || !ValidCell(targetIndex, targetSlot))
                result = TransferResult.InvalidLocation;
            else
                result = items.Transfer(new(0, Cell(sourceIndex, sourceSlot), ItemEntityId.FromNetwork(sourceId),
                    Cell(targetIndex, targetSlot), targetId == 0 ? null : ItemEntityId.FromNetwork(targetId), inventoryRevision, equipmentRevision));
        }
        ReplyItemTransfer(peer, request, result);
    }

    private static bool ValidCell(int index, string slot) =>
        index >= 0 && index < ItemSnapshotCodec.MaxInventorySlots && slot.Length == 0 ||
        index == -1 && slot.Length is > 0 and <= 64;
    private static ItemCellAddress Cell(int index, string slot) => index >= 0 ? ItemCellAddress.Inventory(index) : ItemCellAddress.Equipment(slot);

    private bool SendItems(int peer)
    {
        var items = _playerItems[peer];
        if (RpcId(peer, nameof(ReceiveItems), Synchronization!.Session.ToString("N"), ItemSnapshotCodec.Encode(items)) != Error.Ok)
        {
            FailSession("Inventory snapshot send failed.");
            return false;
        }
        _publishedItemRevisions[peer] = (items.Inventory.Revision, items.Equipment.Revision);
        return true;
    }

    private void PublishItemChanges()
    {
        foreach (var (peer, items) in _playerItems)
        {
            if (peer == 1 || !_joins.Is(peer, PeerJoinPhase.Ready)) continue;
            if (!_publishedItemRevisions.TryGetValue(peer, out var published) ||
                published != (items.Inventory.Revision, items.Equipment.Revision))
                if (!SendItems(peer)) return;
        }
    }

    private void ReplyItemTransfer(int peer, long request, TransferResult result)
    {
        // The reliable owner snapshot precedes the acknowledgement, including stale-request recovery.
        if (SendItems(peer) && RpcId(peer, nameof(ItemTransferResult), Synchronization!.Session.ToString("N"),
                request, (int)result) != Error.Ok) FailSession("Item transfer acknowledgement failed.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveItems(string session, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") || !_clientJoin.HasWorld) return;
        if (!ItemSnapshotCodec.Apply(rows, LocalItems))
        {
            FailSession("Invalid inventory snapshot.");
            return;
        }
        if (World.LocalPlayer?.Participant is { } owner) LocalItems.Bind(owner);
        _hasItemBaseline = true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ItemTransferResult(string session, long request, int result)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") ||
            !_hasItemBaseline || !Enum.IsDefined((TransferResult)result)) return;
        if (request != _pendingItemRequest || request == 0) return;
        _pendingItemRequest = 0;
        if ((TransferResult)result is not (TransferResult.Success or TransferResult.NoChange))
            ItemFeedback?.Invoke("Item transfer rejected: " + (TransferResult)result);
    }

    private void RemovePlayerItems(int peer)
    {
        if (_playerItems.Remove(peer, out var items) && items != LocalItems) items.Dispose();
        _itemRequests.Remove(peer);
        _itemRequestTimes.Remove(peer);
        _publishedItemRevisions.Remove(peer);
    }

    private void ClearItems()
    {
        foreach (var items in _playerItems.Values)
            if (items != LocalItems) items.Dispose();
        _playerItems.Clear();
        _itemRequests.Clear();
        _itemRequestTimes.Clear();
        _publishedItemRevisions.Clear();
        _pendingItemRequest = 0;
        _hasItemBaseline = false;
        ItemFeedback = null;
    }
}
