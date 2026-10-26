#nullable enable

using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.EquipmentSystem;

public sealed record TransferRequest(Guid EditSession, ItemLocation Source, ItemEntityId SourceEntity,
    ItemLocation Destination, ItemEntityId? DestinationEntity, long InventoryRevision, long EquipmentRevision);
