using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;
public sealed class InventoryController : IDisposable, IInventoryView
{
    // TODO move capacity and growth decisions to the container. 
    public const int InitialCapacity = 128;
    public const int GrowthChunk = 64;

    private bool _disposed;

    public ItemStore Items { get; }
    public ItemContainer Container { get; }
    private InventorySnapshot? _snapshot;
    private ItemEntity?[]? _snapshotEntities;
    public InventorySnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision &&
        (_disposed || ReferenceEquals(_snapshotEntities, Container.Entities))
        ? snapshot : _snapshot = new InventorySnapshot(Container.Revision, _snapshotEntities = Container.Entities);
    public int Capacity => _disposed ? Snapshot.Capacity : Container.Capacity;
    public long Revision => _disposed ? Snapshot.Revision : Container.Revision;
    public event Action<InventorySnapshot>? Changed;
    internal InventoryResult Availability => _disposed ? InventoryResult.Disposed :
        Items.Publishing ? InventoryResult.PublicationInProgress : InventoryResult.Success;

    public InventoryController()
        : this(new ItemStore()) { }

    public InventoryController(ItemStore itemRequestService)
    {
        Items = itemRequestService;
        Container = itemRequestService.CreateContainer(InitialCapacity, GrowthChunk);
        Container.Subscribe(NotifyObservers);
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
