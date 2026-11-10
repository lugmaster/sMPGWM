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
        items.Bind(World.Players[peer]);
        _playerItems.Add(peer, items);
        return items;
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
            SendItems(peer, request, TransferResult.PublicationInProgress);
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
        SendItems(peer, request, result);
    }

    private static bool ValidCell(int index, string slot) =>
        index >= 0 && index < ItemSnapshotCodec.MaxInventorySlots && slot.Length == 0 ||
        index == -1 && slot.Length is > 0 and <= 64;
    private static ItemCellAddress Cell(int index, string slot) => index >= 0 ? ItemCellAddress.Inventory(index) : ItemCellAddress.Equipment(slot);

    private void SendItems(int peer, long request = 0, TransferResult result = TransferResult.Success) =>
        RpcId(peer, nameof(ReceiveItems), Synchronization!.Session.ToString("N"), request, (int)result, ItemSnapshotCodec.Encode(_playerItems[peer]));

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceiveItems(string session, long request, int result, Godot.Collections.Array rows)
    {
        if (!IsHostMessage() || Synchronization == null || session != Synchronization.Session.ToString("N") || !_clientJoin.HasWorld) return;
        if (!Enum.IsDefined((TransferResult)result) || !ItemSnapshotCodec.Apply(rows, LocalItems))
        {
            FailSession("Invalid inventory snapshot.");
            return;
        }
        if (World.LocalPlayer?.Participant is { } owner) LocalItems.Bind(owner);
        _hasItemBaseline = true;
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
    }

    private void ClearItems()
    {
        foreach (var items in _playerItems.Values)
            if (items != LocalItems) items.Dispose();
        _playerItems.Clear();
        _itemRequests.Clear();
        _itemRequestTimes.Clear();
        _pendingItemRequest = 0;
        _hasItemBaseline = false;
        ItemFeedback = null;
    }
}
