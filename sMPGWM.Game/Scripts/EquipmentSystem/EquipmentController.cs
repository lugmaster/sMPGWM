using sMPGWM.Scripts.GameplayDataSystem;
using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;
public sealed class EquipmentController : IDisposable, IEquipmentView
{
    private bool _disposed;
    private WeaponGroupConfiguration _weaponGroups = WeaponGroupConfiguration.Empty;
    internal Func<ItemEntity?[], Action?>? PrepareLoadoutChange { get; set; }

    public IReadOnlyList<EquipmentSlotDefinition> Definitions { get; }
    public ItemStore Items { get; }
    public ItemContainer Container { get; }
    private EquipmentSnapshot? _snapshot;
    private ItemEntity?[]? _snapshotEntities;
    public EquipmentSnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision && ReferenceEquals(snapshot.WeaponGroups, _weaponGroups) &&
        (_disposed || ReferenceEquals(_snapshotEntities, Container.Entities))
        ? snapshot : _snapshot = new EquipmentSnapshot(Container.Revision, _snapshotEntities = Container.Entities, _weaponGroups);
    public long Revision => _disposed ? Snapshot.Revision : Container.Revision;
    public event Action<EquipmentSnapshot>? Changed;
    internal TransferResult Availability => _disposed ? TransferResult.Disposed :
        Items.Publishing ? TransferResult.PublicationInProgress : TransferResult.Success;

    public EquipmentController()
        : this(new ItemStore()) { }

    public EquipmentController(ItemStore itemRequestService) : this(itemRequestService, EquipmentSlotDefinition.Defaults) { }

    public EquipmentController(ItemStore itemRequestService, IReadOnlyList<EquipmentSlotDefinition> definitions)
    {
        Items = itemRequestService;
        ArgumentNullException.ThrowIfNull(definitions);
        if (definitions.Count == 0 || definitions.Count > ItemContainer.MaxCapacity)
            throw new ArgumentException("Invalid equipment layout.", nameof(definitions));
        if (ReferenceEquals(definitions, EquipmentSlotDefinition.Defaults))
        {
            for (var i = 0; i < definitions.Count; i++)
                if (definitions[i].AcceptedCategory != "weapon") GameplayDefinitions.Current.ItemCategories.GetById(definitions[i].AcceptedCategory);
            Definitions = definitions;
        }
        else
        {
            var copy = new EquipmentSlotDefinition[definitions.Count];
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var singletonCategories = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < copy.Length; i++)
            {
                var definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Key) || definition.Key.Length > 64 || !keys.Add(definition.Key))
                    throw new ArgumentException("Invalid or duplicate equipment slot key.", nameof(definitions));
                if (definition.AcceptedCategory != "weapon") GameplayDefinitions.Current.ItemCategories.GetById(definition.AcceptedCategory);
                if (definition.AcceptedCategory is "engine" or "hull" or "shield_generator" or "reactor" or "battery" or "weapon_fixture" &&
                    !singletonCategories.Add(definition.AcceptedCategory))
                    throw new ArgumentException("Duplicate singleton component category.", nameof(definitions));
                copy[i] = definition;
            }
            Definitions = Array.AsReadOnly(copy);
        }
        Container = itemRequestService.CreateContainer(Definitions.Count, accepts: (index, definition, count) =>
            index >= 0 && index < Definitions.Count && count == 1 && Definitions[index].Accepts(definition.CategoryId));
        Container.PrepareChange = PrepareEquipmentChange;
        Container.ValidateState = slots => WeaponSlotPolicy.Used(slots) <= WeaponSlotPolicy.Capacity(slots);
        Container.PrepareTransfer = PrepareTransfer;
        Container.Subscribe(NotifyObservers);
    }

    private Action PrepareEquipmentChange(ItemEntity?[] slots)
    {
        var count = 0;
        var unchanged = true;
        for (var i = 0; i < slots.Length; i++)
        {
            if (!Definitions[i].IsWeaponSlot || slots[i] is not { } item) continue;
            if (!_weaponGroups.TryGetBinding(i, item.Id, out _)) unchanged = false;
            count++;
        }
        var groups = _weaponGroups;
        if (!unchanged || count != groups.Bindings.Count)
        {
            var bindings = new WeaponGroupBinding[count];
            var index = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                if (!Definitions[i].IsWeaponSlot || slots[i] is not { } item) continue;
                bindings[index++] = _weaponGroups.TryGetBinding(i, item.Id, out var previous)
                    ? previous : new WeaponGroupBinding(i, item.Id, true, false);
            }
            groups = count == 0 ? WeaponGroupConfiguration.Empty : new WeaponGroupConfiguration(bindings);
        }
        var installLoadout = PrepareLoadoutChange?.Invoke(slots);
        return () => { _weaponGroups = groups; installLoadout?.Invoke(); };
    }

    internal Action PrepareWeaponGroupConfirmation(ItemEntity?[] slots, long revision, WeaponGroupConfiguration groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (slots.Length != Definitions.Count || revision < Revision || revision == long.MaxValue)
            throw new ArgumentException("Invalid equipment configuration revision or layout.");
        var bindingIndex = 0;
        for (var slot = 0; slot < slots.Length; slot++)
        {
            if (!Definitions[slot].IsWeaponSlot || slots[slot] is not { } item) continue;
            if (bindingIndex >= groups.Bindings.Count || groups.Bindings[bindingIndex].Slot != slot ||
                groups.Bindings[bindingIndex].Item != item.Id)
                throw new ArgumentException("Weapon groups must match all equipped weapon bindings.");
            bindingIndex++;
        }
        if (bindingIndex != groups.Bindings.Count || revision == Revision && !_weaponGroups.SameAs(groups))
            throw new ArgumentException("Conflicting weapon configuration.");
        return () => _weaponGroups = groups;
    }

    public void ApplyWeaponGroupDefinition(string[] primarySlots, string[] secondarySlots)
    {
        var bindings = new WeaponGroupBinding[_weaponGroups.Bindings.Count];
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = _weaponGroups.Bindings[i];
            var key = Definitions[binding.Slot].Key;
            bindings[i] = binding with { Primary = Array.IndexOf(primarySlots, key) >= 0, Secondary = Array.IndexOf(secondarySlots, key) >= 0 };
        }
        var groups = new WeaponGroupConfiguration(bindings);
        if (_weaponGroups.SameAs(groups)) return;
        if (Availability != TransferResult.Success || Revision == long.MaxValue || !ConfirmGroups(groups))
            throw new InvalidOperationException("Cannot apply weapon group definition.");
    }

    private bool ConfirmGroups(WeaponGroupConfiguration groups)
    {
        var slots = Container.Entities;
        var revision = Revision + 1;
        var install = PrepareWeaponGroupConfirmation(slots, revision, groups);
        return Items.Confirm(new[] { new ItemContainerConfirmation(Container, slots, revision) }, install);
    }

    public TransferResult SetWeaponGroups(int slot, ItemEntityId identity, long revision, bool primary, bool secondary)
    {
        if (Availability != TransferResult.Success) return Availability;
        if (revision != Revision || revision == long.MaxValue) return TransferResult.StaleRevision;
        if (slot < 0 || slot >= Definitions.Count || !Definitions[slot].IsWeaponSlot) return TransferResult.InvalidLocation;
        if (Container.Entities[slot] is not { } item || item.Id != identity) return TransferResult.StaleEntity;
        if (!_weaponGroups.TryGetBinding(slot, identity, out var previous)) return TransferResult.StaleEntity;
        if (previous.Primary == primary && previous.Secondary == secondary) return TransferResult.NoChange;
        var bindings = new WeaponGroupBinding[_weaponGroups.Bindings.Count];
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = _weaponGroups.Bindings[i];
            bindings[i] = binding.Slot == slot ? binding with { Primary = primary, Secondary = secondary } : binding;
        }
        return ConfirmGroups(new WeaponGroupConfiguration(bindings)) ? TransferResult.Success : TransferResult.IncompatiblePlacement;
    }

    public int FindSlot(string key)
    {
        for (var index = 0; index < Definitions.Count; index++)
            if (Definitions[index].Key == key) return index;
        return -1;
    }

    public bool CanPlace(string key, string definitionId)
    {
        var index = FindSlot(key);
        return index >= 0 && Definitions[index].Accepts(GameplayDefinitions.Current.Items.GetById(definitionId).CategoryId);
    }

    private void PrepareTransfer(Dictionary<ItemContainer, ItemEntity?[]> changes, ItemContainer storage)
    {
        var slots = changes[Container];
        var capacity = WeaponSlotPolicy.Capacity(slots);
        if (capacity >= WeaponSlotPolicy.Capacity(Container.Entities)) return;
        var used = WeaponSlotPolicy.Used(slots);
        if (used <= capacity) return;
        if (ReferenceEquals(storage, Container) || storage.Disposed || storage.Revision == long.MaxValue)
            throw new InvalidOperationException("Cannot return weapons to inventory.");
        var inventory = changes[storage];
        for (var index = slots.Length - 1; index >= 0 && used > capacity; index--)
        {
            if (slots[index] is not { } item || !Definitions[index].IsWeaponSlot) continue;
            var destination = -1;
            for (var i = 0; i < inventory.Length; i++)
                if (inventory[i] == null && storage.Accepts(i, item.Definition, item.Count)) { destination = i; break; }
            if (destination < 0)
            {
                if (storage.GrowthChunk == 0 || inventory.Length > ItemContainer.MaxCapacity - storage.GrowthChunk)
                    throw new InvalidOperationException("Inventory cannot hold unequipped weapons.");
                destination = inventory.Length;
                Array.Resize(ref inventory, inventory.Length + storage.GrowthChunk);
                if (!storage.Accepts(destination, item.Definition, item.Count))
                    throw new InvalidOperationException("Inventory cannot accept unequipped weapons.");
                changes[storage] = inventory;
            }
            inventory[destination] = item;
            slots[index] = null;
            used -= WeaponSlotPolicy.GetCost(item.Definition.CategoryId);
        }
    }

    // Subscriber failures are programming errors: propagate them immediately.
    internal void NotifyObservers() => Changed?.Invoke(Snapshot);

    public void Dispose()
    {
        if (_disposed) return;
        _ = Snapshot;
        Items.DestroyContainer(Container);
        Container.Unsubscribe(NotifyObservers);
        _disposed = true;
        PrepareLoadoutChange = null;
        Changed = null;
    }
}
