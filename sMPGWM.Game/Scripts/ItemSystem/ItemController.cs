using System;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.EquipmentSystem;

namespace sMPGWM.Scripts.ItemSystem;

/// <summary>World-facing client of the shared item authority.</summary>
public sealed class ItemController
{
    // TODO ItemController should hold a ItemContainer called worldContainer (for now. later for each chunk)
    public ItemRequestService ItemRequestService { get; }

    public ItemController(ItemRequestService items) =>
        ItemRequestService = items ?? throw new ArgumentNullException(nameof(items));

    public ItemEntity GetItem(ItemEntityId id) => ItemRequestService.GetItem(id);
    public ItemLocation GetLocation(ItemEntityId id) => ItemRequestService.GetLocation(id);

    public InventoryResult CreateItems(ItemContainer destination, string definitionId, long quantity) =>
        ItemRequestService.CreateItems(destination, definitionId, quantity);

    public TransferResult TransferItem(ItemEntityId itemId, ItemLocation expectedSource, ItemLocation destination,
        long sourceRevision, long destinationRevision, ItemEntityId? expectedDestination = null, bool commit = true) =>
        ItemRequestService.TransferItem(itemId, expectedSource, destination, sourceRevision, destinationRevision, expectedDestination, commit);
}
