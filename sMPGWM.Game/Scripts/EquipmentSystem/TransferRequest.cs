using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed record TransferRequest(ulong EditSession, ItemCellAddress Source, ItemEntityId SourceEntity,
    ItemCellAddress Destination, ItemEntityId? DestinationEntity, long InventoryRevision, long EquipmentRevision);
