using sMPGWM.Scripts.EquipmentSystem;

namespace sMPGWM.Scripts.ItemSystem.Hotbar;


public sealed record HotbarAvailability(string? DefinitionId, long? Quantity, string Label, bool Available);

public sealed record ItemHotbarActivationRequest(int Position, HotbarBinding Binding, string DefinitionId, long? Quantity);

public readonly record struct HotbarAssignmentRequest(ulong Session, long HotbarRevision, int Position,
    ItemCellAddress Source, ItemEntityId Entity, long InventoryRevision, long EquipmentRevision);
