using sMPGWM.Scripts.ItemSystem;
using ItemLocation = sMPGWM.Scripts.EquipmentSystem.ItemLocation;

namespace sMPGWM.Scripts.UiSystem.HudSystem.HotbarSystem;


public sealed record HotbarAvailability(string? DefinitionId, long? Quantity, string Label, bool Available);

public sealed record ItemHotbarActivationRequest(int Position, HotbarBinding Binding, string DefinitionId, long? Quantity);

public readonly record struct HotbarAssignmentRequest(ulong Session, long HotbarRevision, int Position,
    ItemLocation Source, ItemEntityId Entity, long InventoryRevision, long EquipmentRevision);
