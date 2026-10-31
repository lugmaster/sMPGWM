using ItemLocation = sMPGWM.Scripts.EquipmentSystem.ItemLocation;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.Items;


public sealed record HotbarAvailability(string? DefinitionId, long? Quantity, string Label, bool Available);

public sealed record ItemHotbarActivationRequest(int Position, HotbarBinding Binding, string DefinitionId, long? Quantity);

public sealed record HotbarAssignmentRequest(ulong Session, long HotbarRevision, int Position,
    ItemLocation Source, ItemEntityId Entity, long InventoryRevision, long EquipmentRevision);
