using System;
using System.Collections.Generic;

namespace sMPGWM.Scripts.Items.Equipment;
public sealed class EquipmentController : IDisposable, IEquipmentView
{
    private bool _disposed;

    public IReadOnlyList<EquipmentSlotDefinition> Definitions { get; }
    public ItemStore Items { get; }
    public ItemContainer Container { get; }
    private EquipmentSnapshot? _snapshot;
    public EquipmentSnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision
        ? snapshot : _snapshot = new EquipmentSnapshot(Container.Revision, Container.Entities);
    public long Revision => Snapshot.Revision;
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
        var copy = new EquipmentSlotDefinition[definitions.Count];
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var singletonCategories = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < copy.Length; i++)
        {
            var definition = definitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.Key) || definition.Key.Length > 64 || !keys.Add(definition.Key))
                throw new ArgumentException("Invalid or duplicate equipment slot key.", nameof(definitions));
            ItemRegistry.GetCategoryById(definition.AcceptedCategory);
            if (definition.AcceptedCategory is "engine" or "hull" or "shield_generator" or "reactor" or "battery" &&
                !singletonCategories.Add(definition.AcceptedCategory))
                throw new ArgumentException("Duplicate singleton component category.", nameof(definitions));
            copy[i] = definition;
        }
        Definitions = Array.AsReadOnly(copy);
        Container = itemRequestService.CreateContainer(Definitions.Count, accepts: (index, definition, count) =>
            index >= 0 && index < Definitions.Count && count == 1 && CanPlace(Definitions[index].Key, definition.Id));
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

    // Subscriber failures are programming errors: propagate them immediately.
    internal void NotifyObservers() => Changed?.Invoke(Snapshot);

    public void Dispose()
    {
        if (_disposed) return;
        _ = Snapshot;
        Items.DestroyContainer(Container);
        Container.Unsubscribe(NotifyObservers);
        _disposed = true;
        Changed = null;
    }
}
