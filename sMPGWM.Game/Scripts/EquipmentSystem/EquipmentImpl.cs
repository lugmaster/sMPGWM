#nullable enable

using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed class EquipmentImpl : IDisposable
{
    private readonly Action<Exception>? _reportObserverError;
    private bool _disposed;
    private bool _publishing;

    internal ItemCatalog Catalog { get; }
    public IReadOnlyList<EquipmentSlotDefinition> Definitions { get; }
    public EquipmentSnapshot Snapshot { get; private set; } = new(0, new ItemEntity?[8]);
    public long Revision => Snapshot.Revision;
    public event Action<EquipmentSnapshot>? Changed;
    internal TransferResult Availability => _disposed ? TransferResult.Disposed :
        _publishing ? TransferResult.PublicationInProgress : TransferResult.Success;

    public EquipmentImpl(ItemCatalog catalog, Action<Exception>? reportObserverError = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
        _reportObserverError = reportObserverError;
        Definitions = Array.AsReadOnly(new[]
        {
            new EquipmentSlotDefinition("engine", "Engine", ItemCategories.Engine),
            new EquipmentSlotDefinition("hull", "Hull", ItemCategories.Hull),
            new EquipmentSlotDefinition("shield_generator", "Shield Generator", ItemCategories.ShieldGenerator),
            new EquipmentSlotDefinition("reactor", "Reactor", ItemCategories.Reactor),
            new EquipmentSlotDefinition("light_weapon_1", "Light Weapon Slot 1", ItemCategories.LightWeapon),
            new EquipmentSlotDefinition("light_weapon_2", "Light Weapon Slot 2", ItemCategories.LightWeapon),
            new EquipmentSlotDefinition("heavy_weapon_1", "Heavy Weapon Slot 1", ItemCategories.HeavyWeapon),
            new EquipmentSlotDefinition("heavy_weapon_2", "Heavy Weapon Slot 2", ItemCategories.HeavyWeapon)
        });
        foreach (var definition in Definitions)
            catalog.Categories.Get(definition.AcceptedCategory);
    }

    public int FindSlot(string key)
    {
        for (var index = 0; index < Definitions.Count; index++)
            if (Definitions[index].Key == key) return index;
        return -1;
    }

    public bool CanPlace(string key, ItemDefinitionId definitionId)
    {
        var index = FindSlot(key);
        return index >= 0 && Catalog.Get(definitionId).CategoryId == Definitions[index].AcceptedCategory;
    }

    internal EquipmentSnapshot PrepareChange(ItemEntity?[] slots)
    {
        if (slots.Length != Definitions.Count) throw new ArgumentException("Invalid equipmentImpl size.", nameof(slots));
        var identities = new HashSet<ItemEntityId>();
        for (var index = 0; index < slots.Length; index++)
        {
            if (slots[index] is not { } entity) continue;
            if (entity.Id == default || !identities.Add(entity.Id) || entity.Count != 1 ||
                !CanPlace(Definitions[index].Key, entity.DefinitionId))
                throw new ArgumentException("Invalid equipmentImpl placement or duplicate identity.", nameof(slots));
        }
        return new EquipmentSnapshot(checked(Revision + 1), slots);
    }

    internal void Install(EquipmentSnapshot snapshot) => Snapshot = snapshot;
    internal void SetPublicationInProgress(bool publishing) => _publishing = publishing;

    internal void NotifyObservers()
    {
        if (Changed is not { } handlers) return;
        foreach (Action<EquipmentSnapshot> observer in handlers.GetInvocationList())
        {
            try { observer(Snapshot); }
            catch (Exception exception)
            {
                try { _reportObserverError?.Invoke(exception); }
                catch (Exception) { }
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Changed = null;
    }
}
