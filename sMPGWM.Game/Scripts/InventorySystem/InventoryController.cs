

using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;
public sealed class InventoryController : IDisposable, IInventoryView
{
    public const int InitialCapacity = 128;
    public const int GrowthChunk = 64;

    private bool _publishing;
    private bool _disposed;

    public ItemController Items { get; }
    public ItemContainer Container { get; }
    private InventorySnapshot? _snapshot;
    public InventorySnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision
        ? snapshot : _snapshot = new InventorySnapshot(Container.Revision, Container.Entities);
    public int Capacity => Snapshot.Capacity;
    public long Revision => Snapshot.Revision;
    public event Action<InventorySnapshot>? Changed;
    internal InventoryResult Availability => _disposed ? InventoryResult.Disposed :
        (_publishing || Items.Publishing) ? InventoryResult.PublicationInProgress : InventoryResult.Success;

    public InventoryController()
        : this(new ItemController()) { }

    public InventoryController(ItemController items)
    {
        Items = items;
        Container = items.CreateContainer(InitialCapacity, GrowthChunk);
        Container.Subscribe(NotifyObservers);
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
