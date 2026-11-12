using System;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Items.Inventory;
using sMPGWM.Scripts.Items.Equipment;

namespace sMPGWM.Scripts.World;

/// <summary>A connection's control binding. The world owns the ship's lifetime.</summary>
public sealed class PlayerSession : IDisposable
{
    public InventoryController Inventory { get; }
    internal bool InventoryInitialized { get; set; }
    private bool _disposed;
    internal long ReplicaInventoryIdentity { get; set; }
    internal long ReplicaEquipmentIdentity { get; set; }
    internal long ReplicaGeneration { get; set; } = -1;

    public PlayerSession() : this(new ItemStore()) { }
    public PlayerSession(ItemStore store) => Inventory = new(store);

    public TransferResult Transfer(TransferRequest request)
    {
        if (_disposed) return TransferResult.Disposed;
        var equipment = ControlledShip?.Equipment;
        if (equipment == null)
        {
            if (request.Source.InventoryAddress is not { } source || request.Destination.InventoryAddress is not { } target)
                return TransferResult.InvalidLocation;
            return Inventory.Items.TransferItem(request.SourceEntity, new(Inventory.Container.Id, source),
                new(Inventory.Container.Id, target), request.InventoryRevision, request.InventoryRevision, request.DestinationEntity);
        }
        if (!ReferenceEquals(Inventory.Items, equipment.Items) ||
            !ItemCellResolver.TryResolve(request.Source, Inventory, equipment, out var from) ||
            !ItemCellResolver.TryResolve(request.Destination, Inventory, equipment, out var to)) return TransferResult.InvalidLocation;
        return Inventory.Items.TransferItem(request.SourceEntity, from, to,
            request.Source.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision,
            request.Destination.InventoryAddress.HasValue ? request.InventoryRevision : request.EquipmentRevision, request.DestinationEntity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        AssignShip(null);
        Inventory.Dispose();
        ControlledShipChanged = null;
    }

    public int ControllingPeerId { get; init; }
    public long ControlGeneration { get; private set; }
    public ShipController? ControlledShip { get; private set; }
    public event Action<ShipController?, ShipController?>? ControlledShipChanged;

    public void AssignShip(ShipController? ship)
    {
        if (ship == ControlledShip) return;
        if (ship?.ControllingPlayer is { } owner && owner != this) owner.AssignShip(null);
        var previous = ControlledShip;
        if (previous != null)
        {
            previous.SetLocalControl(false);
            previous.ControllingPlayer = null;
        }
        if (ship != null) ship.ControllingPlayer = this;
        checked { ControlGeneration++; }
        ControlledShip = ship;
        ControlledShipChanged?.Invoke(previous, ship);
    }

    internal void ApplyGeneration(long generation) => ControlGeneration = generation;
}
