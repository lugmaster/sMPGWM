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
    public EquipmentSnapshot Snapshot { get; private set; } = new(0, new ItemEntity?[EquipmentSlotDefinition.Defaults.Count]);
    public long Revision => Snapshot.Revision;
    public event Action<EquipmentSnapshot>? Changed;
    internal TransferResult Availability => _disposed ? TransferResult.Disposed :
        _publishing ? TransferResult.PublicationInProgress : TransferResult.Success;

    public EquipmentImpl(ItemCatalog catalog, Action<Exception>? reportObserverError = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Catalog = catalog;
        _reportObserverError = reportObserverError;
        Definitions = EquipmentSlotDefinition.Defaults;
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
                // TODO: Add subscriber recovery through its owner (rebuild or disable a failed view).
                // Reporting alone does not recover it; preserve committed state and notify remaining subscribers.
                // State is already committed; a broken subscriber must not prevent other notifications.
                if (_reportObserverError is null)
                {
                    Console.Error.WriteLine($"Equipment observer failed: {exception}");
                    continue;
                }

                try { _reportObserverError(exception); }
                catch (Exception reportingException)
                {
                    // Bypass the failed reporter and preserve both exceptions for diagnosis.
                    Console.Error.WriteLine($"Equipment observer failed: {exception}{Environment.NewLine}" +
                        $"Equipment observer error reporter also failed: {reportingException}");
                }
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Changed = null;
    }
}
