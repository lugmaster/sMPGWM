namespace sMPGWM.Scripts.Items.Equipment;

public sealed record TransferRequest(ulong EditSession, ItemCellAddress Source, ItemEntityId SourceEntity,
    ItemCellAddress Destination, ItemEntityId? DestinationEntity, long InventoryRevision, long EquipmentRevision);
