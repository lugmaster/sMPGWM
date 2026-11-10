using System;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.World;

namespace sMPGWM.Scripts.Items;

public sealed class PlayerItemState : IDisposable
{
    public ItemStore Store { get; } = new();
    public InventoryController Inventory { get; }
    public EquipmentController Equipment { get; }
    private PlayerSession? _owner;
    private ShipController? _ship;
    private readonly string?[] _sources;
    private readonly ItemEntityId?[] _sourceItems;

    public PlayerItemState()
    {
        Inventory = new(Store);
        Equipment = new(Store);
        _sources = new string?[Equipment.Definitions.Count];
        _sourceItems = new ItemEntityId?[Equipment.Definitions.Count];
        Equipment.Changed += EquipmentChanged;
    }

    public void Bind(PlayerSession owner)
    {
        if (_owner == owner) return;
        if (_owner != null) _owner.ControlledShipChanged -= ShipChanged;
        _owner = owner;
        owner.ControlledShipChanged += ShipChanged;
        ShipChanged(_ship, owner.ControlledShip);
    }

    private void ShipChanged(ShipController? previous, ShipController? next)
    {
        RemoveGrants();
        _ship = next;
        EquipmentChanged(Equipment.Snapshot);
    }

    private void RemoveGrants()
    {
        for (var i = 0; i < _sources.Length; i++)
        {
            if (_sources[i] is { } source) _ship?.Gameplay.Grants.RemoveSource(source);
            _sources[i] = null;
            _sourceItems[i] = null;
        }
    }

    private void EquipmentChanged(EquipmentSnapshot snapshot)
    {
        if (_ship == null) return;
        // Remove changed sources before reinstalling them so slot swaps cannot revoke a new grant.
        for (var i = 0; i < snapshot.Slots.Count; i++)
            if (_sourceItems[i] != snapshot.Slots[i]?.Id && _sources[i] is { } previous)
                _ship.Gameplay.Grants.RemoveSource(previous);
        for (var i = 0; i < snapshot.Slots.Count; i++)
        {
            var item = snapshot.Slots[i];
            if (_sourceItems[i] == item?.Id) continue;
            _sourceItems[i] = item?.Id;
            var source = item != null && item.Definition.AbilityIds.Count != 0 ? "item:" + item.Id : null;
            _sources[i] = source;
            if (source == null) continue;
            _ship.Gameplay.Grants.SetSource(source, item!.Definition.AbilityIds);
        }
    }

    public bool Provides(string slot, long source, string ability)
    {
        var index = Equipment.FindSlot(slot);
        if (index < 0 || source <= 0 || Equipment.Snapshot.Slots[index] is not { } item ||
            item.Id.Value != (ulong)source) return false;
        foreach (var id in item.Definition.AbilityIds)
            if (id == ability) return true;
        return false;
    }

    public TransferResult Transfer(TransferRequest request)
    {
        if (!ItemCellResolver.TryResolve(request.Source, Inventory, Equipment, out var source) ||
            !ItemCellResolver.TryResolve(request.Destination, Inventory, Equipment, out var target))
            return TransferResult.InvalidLocation;
        return Store.TransferItem(request.SourceEntity, source, target,
            request.Source.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision,
            request.Destination.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision, request.DestinationEntity);
    }

    public void Dispose()
    {
        if (_owner != null) _owner.ControlledShipChanged -= ShipChanged;
        RemoveGrants();
        _owner = null;
        _ship = null;
        Equipment.Changed -= EquipmentChanged;
        Equipment.Dispose();
        Inventory.Dispose();
    }
}
