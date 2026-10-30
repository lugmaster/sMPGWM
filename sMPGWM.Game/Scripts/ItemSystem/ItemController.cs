using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.EquipmentSystem;

namespace sMPGWM.Scripts.ItemSystem;

/// <summary>World-facing client of the shared item authority.</summary>
public sealed class ItemController
{
    public ItemRequestService Items { get; }

    public ItemController(ItemRequestService items) =>
        Items = items ?? throw new ArgumentNullException(nameof(items));

    public ItemEntity GetItem(ItemEntityId id) => Items.GetItem(id);
    public ItemLocation GetLocation(ItemEntityId id) => Items.GetLocation(id);

    public InventoryResult CreateItems(ItemContainer destination, string definitionId, long quantity) =>
        Items.CreateItems(destination, definitionId, quantity);

    public TransferResult TransferItem(ItemEntityId itemId, ItemLocation expectedSource, ItemLocation destination,
        long sourceRevision, long destinationRevision, ItemEntityId? expectedDestination = null, bool commit = true) =>
        Items.TransferItem(itemId, expectedSource, destination, sourceRevision, destinationRevision, expectedDestination, commit);
}
