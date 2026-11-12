using System;

namespace sMPGWM.Scripts.Items.Inventory;
public sealed class InventoryController : IDisposable, IInventoryView
{
    // TODO move capacity and growth decisions to the container. 
    public const int InitialCapacity = 128;
    public const int GrowthChunk = 64;

    private bool _disposed;

    public ItemStore Items { get; }
    public ItemContainer Container { get; }
    private InventorySnapshot? _snapshot;
    public InventorySnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision
        ? snapshot : _snapshot = new InventorySnapshot(Container.Revision, Container.Entities);
    public int Capacity => Snapshot.Capacity;
    public long Revision => Snapshot.Revision;
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
