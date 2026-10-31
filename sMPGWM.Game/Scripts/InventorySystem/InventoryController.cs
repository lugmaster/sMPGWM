

using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.InventorySystem;
public sealed class InventoryController : IDisposable, IInventoryView
{
    // TODO move capacity and growth decisions to the container. 
    public const int InitialCapacity = 128;
    public const int GrowthChunk = 64;

    private bool _disposed;

    public ItemRequestService ItemRequestService { get; }
    public ItemContainer Container { get; }
    private InventorySnapshot? _snapshot;
    public InventorySnapshot Snapshot => _snapshot is { } snapshot && snapshot.Revision == Container.Revision
        ? snapshot : _snapshot = new InventorySnapshot(Container.Revision, Container.Entities);
    public int Capacity => Snapshot.Capacity;
    public long Revision => Snapshot.Revision;
    public event Action<InventorySnapshot>? Changed;
    internal InventoryResult Availability => _disposed ? InventoryResult.Disposed :
        ItemRequestService.Publishing ? InventoryResult.PublicationInProgress : InventoryResult.Success;

    public InventoryController()
        : this(new ItemRequestService()) { }

    public InventoryController(ItemRequestService itemRequestService)
    {
        ItemRequestService = itemRequestService;
        Container = itemRequestService.CreateContainer(InitialCapacity, GrowthChunk);
        Container.Subscribe(NotifyObservers);
    }

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
