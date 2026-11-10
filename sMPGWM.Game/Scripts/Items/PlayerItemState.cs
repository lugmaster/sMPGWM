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

    public PlayerItemState()
    {
        Inventory = new(Store);
        Equipment = new(Store);
        _sources = new string?[Equipment.Definitions.Count];
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
        }
    }

    private void EquipmentChanged(EquipmentSnapshot snapshot)
    {
        RemoveGrants();
        if (_ship == null) return;
        for (var i = 0; i < snapshot.Slots.Count; i++)
        {
            if (snapshot.Slots[i] is not { } item || item.Definition.AbilityIds.Count == 0) continue;
            var source = "item:" + item.Id;
            _ship.Gameplay.Grants.SetSource(source, item.Definition.AbilityIds);
            _sources[i] = source;
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
        var sourceIndex = request.Source.InventoryAddress ?? Equipment.FindSlot(request.Source.EquipmentSlotKey!);
        var targetIndex = request.Destination.InventoryAddress ?? Equipment.FindSlot(request.Destination.EquipmentSlotKey!);
        var source = request.Source.InventoryAddress.HasValue ? Inventory.Container : Equipment.Container;
        var target = request.Destination.InventoryAddress.HasValue ? Inventory.Container : Equipment.Container;
        return Store.TransferItem(request.SourceEntity, new(source.Id, sourceIndex), new(target.Id, targetIndex),
            source == Inventory.Container ? request.InventoryRevision : request.EquipmentRevision,
            target == Inventory.Container ? request.InventoryRevision : request.EquipmentRevision, request.DestinationEntity);
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
