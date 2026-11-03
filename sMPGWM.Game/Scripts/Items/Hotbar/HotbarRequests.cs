using sMPGWM.Scripts.Items.Equipment;

namespace sMPGWM.Scripts.Items.Hotbar;


public sealed record HotbarAvailability(string? DefinitionId, long? Quantity, string Label, bool Available);

public sealed record ItemHotbarActivationRequest(int Position, HotbarBinding Binding, string DefinitionId, long? Quantity);

public readonly record struct HotbarAssignmentRequest(ulong Session, long HotbarRevision, int Position,
    ItemCellAddress Source, ItemEntityId Entity, long InventoryRevision, long EquipmentRevision);
