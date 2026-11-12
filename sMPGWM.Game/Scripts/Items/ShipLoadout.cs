using System;
using sMPGWM.Scripts.Items.Equipment;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.Items;

public sealed class ShipLoadout : IDisposable
{
    private readonly ShipController _ship;
    private readonly ItemEntityId?[] _sourceItems;
    private readonly StatContributionSource?[] _sources;
    private bool _authoritative;
    private bool _disposed;
    private bool _publishGameplay;
    public ItemStore Store { get; }
    public EquipmentController Equipment { get; }

    public ShipLoadout(ShipController ship, ItemStore store, bool authoritative)
    {
        _ship = ship;
        Store = store;
        Equipment = new(store, EquipmentSlotDefinition.Defaults);
        _sourceItems = new ItemEntityId?[Equipment.Definitions.Count];
        _sources = new StatContributionSource?[Equipment.Definitions.Count];
        _authoritative = authoritative;
        Equipment.Container.PrepareChange = Prepare;
        Equipment.Container.PublishDomainChange = Publish;
        ship.DefinitionChanged += DefinitionChanged;
        if (authoritative)
        {
            try { DefinitionChanged(); }
            catch { Dispose(); throw; }
        }
    }

    public void SetAuthoritative(bool authoritative)
    {
        if (_authoritative == authoritative) return;
        var previous = _authoritative;
        _authoritative = authoritative;
        if (!authoritative) return;
        Action install;
        try { install = Prepare(Equipment.Container.Entities); }
        catch { _authoritative = previous; throw; }
        install();
        Publish();
    }

    private void DefinitionChanged()
    {
        if (_disposed) return;
        Prepare(Equipment.Container.Entities)();
        Publish();
    }

    private Action Prepare(ItemEntity?[] slots)
    {
        var stats = _authoritative ? EquipmentStatCalculator.Calculate(_ship.Data.StatModifiers, _ship.Data.Mass,
            new EquipmentSnapshot(Equipment.Revision, slots), Equipment.Definitions) : default;
        var batterySlot = Equipment.FindSlot("battery");
        var battery = batterySlot >= 0 ? slots[batterySlot]?.Id.Value ?? 0 : 0;
        if (_authoritative) _ship.Runtime.ValidateEquipment(stats, battery);
        var sources = new StatContributionSource?[slots.Length];
        var grants = new Action?[slots.Length];
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] is not { } item || item.Definition.AbilityIds.Count == 0) continue;
            var source = new StatContributionSource(StatContributionKind.Equipment, checked((long)item.Id.Value));
            sources[i] = source;
            if (_sourceItems[i] != item.Id) grants[i] = _ship.Runtime.Grants.PrepareSource(source, item.Definition.AbilityIds);
        }
        return () =>
        {
            for (var i = 0; i < slots.Length; i++)
                if (_sourceItems[i] != slots[i]?.Id && _sources[i] is { } previous)
                    _ship.Runtime.Grants.RemoveSource(previous);
            for (var i = 0; i < slots.Length; i++)
            {
                if (_sourceItems[i] == slots[i]?.Id) continue;
                _sourceItems[i] = slots[i]?.Id;
                _sources[i] = sources[i];
                grants[i]?.Invoke();
            }
            if (!_authoritative) return;
            _ship.Runtime.ApplyEquipment(stats, battery, publish: false);
            _ship.RefreshMovementConfiguration();
            _publishGameplay = true;
        };
    }

    private void Publish()
    {
        if (!_publishGameplay) return;
        _publishGameplay = false;
        _ship.Runtime.PublishChanges();
    }

    public void InitializeResources() => _ship.Runtime.InitializeResources();

    public void EquipStarterLoadout(InventoryController inventory)
    {
        if (!ReferenceEquals(Store, inventory.Items)) throw new ArgumentException("Starter inventory belongs to a different store.");
        foreach (var slot in Equipment.Definitions)
        {
            var index = Equipment.FindSlot(slot.Key);
            if (Equipment.Snapshot.Slots[index] != null) continue;
            var definition = "mock_standard_" + slot.AcceptedCategory;
            var sourceIndex = Find(inventory, definition);
            if (sourceIndex < 0)
            {
                if (Store.CreateItems(inventory.Container, definition, 1) != InventoryResult.Success)
                    throw new InvalidOperationException("Could not create starter equipment: " + definition);
                sourceIndex = Find(inventory, definition);
            }
            var item = inventory.Snapshot.Slots[sourceIndex]!;
            if (Store.TransferItem(item.Id, new(inventory.Container.Id, sourceIndex), new(Equipment.Container.Id, index),
                    inventory.Revision, Equipment.Revision) != TransferResult.Success)
                throw new InvalidOperationException("Could not equip starter item: " + definition);
        }
    }

    private static int Find(InventoryController inventory, string definition)
    {
        for (var i = 0; i < inventory.Snapshot.Slots.Count; i++)
            if (inventory.Snapshot.Slots[i]?.DefinitionId == definition) return i;
        return -1;
    }

    public void EvictReplica()
    {
        if (_authoritative) throw new InvalidOperationException("Cannot evict authoritative equipment.");
        Store.EvictReplica(Equipment.Container);
    }

    public bool Provides(string slot, long source, string ability)
    {
        if (_disposed || source <= 0) return false;
        var index = Equipment.FindSlot(slot);
        if (index < 0 || Equipment.Snapshot.Slots[index] is not { } item || item.Id.Value != (ulong)source) return false;
        foreach (var id in item.Definition.AbilityIds)
            if (id == ability) return true;
        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ship.DefinitionChanged -= DefinitionChanged;
        for (var i = 0; i < _sources.Length; i++)
            if (_sources[i] is { } source) _ship.Runtime.Grants.RemoveSource(source);
        Equipment.Dispose();
    }
}
