

using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;
public sealed class EquipmentController : IDisposable, IEquipmentView
{
    private bool _disposed;
    private bool _publishing;

    public IReadOnlyList<EquipmentSlotDefinition> Definitions { get; }
    public ItemController Items { get; }
    public ItemContainer Container { get; }
    private EquipmentSnapshot? _snapshot;
    public EquipmentSnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision
        ? snapshot : _snapshot = new EquipmentSnapshot(Container.Revision, Container.Entities);
    public long Revision => Snapshot.Revision;
    public event Action<EquipmentSnapshot>? Changed;
    internal TransferResult Availability => _disposed ? TransferResult.Disposed :
        (_publishing || Items.Publishing) ? TransferResult.PublicationInProgress : TransferResult.Success;

    public EquipmentController()
        : this(new ItemController()) { }

    public EquipmentController(ItemController items)
    {
        Items = items;
        Definitions = EquipmentSlotDefinition.Defaults;
        foreach (var definition in Definitions)
            ItemRegistry.GetCategoryById(definition.AcceptedCategory);
        Container = items.CreateContainer(Definitions.Count, accepts: (index, definition, count) =>
            count == 1 && CanPlace(Definitions[index].Key, definition.Id));
        Container.Subscribe(NotifyObservers);
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
        return index >= 0 && ItemRegistry.GetById(definitionId).CategoryName == ItemRegistry.GetCategoryById(Definitions[index].AcceptedCategory).DisplayName;
    }

    internal void SetPublicationInProgress(bool publishing) => _publishing = publishing;

    // Subscriber failures are programming errors: propagate them immediately.
    internal void NotifyObservers() => Changed?.Invoke(Snapshot);

    public void Dispose()
    {
        Container.Unsubscribe(NotifyObservers);
        Container.Disposed = true;
        _disposed = true;
        Changed = null;
    }
}
