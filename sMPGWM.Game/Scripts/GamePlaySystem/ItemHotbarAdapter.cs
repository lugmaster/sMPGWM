#nullable enable

using System;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public sealed record ActionHotbarBinding(string ActionKey) : HotbarBinding;
public sealed record HotbarAvailability(ItemDefinitionId? DefinitionId, long? Quantity, string Label, bool Available);
public sealed record ItemHotbarActivationRequest(int Position, HotbarBinding Binding, ItemDefinitionId DefinitionId, long? Quantity);

public sealed class ItemHotbarAdapter(ItemHotbarState state, InventoryImpl inventory, EquipmentImpl equipment,
    InventoryEquipmentTransferService transfers)
{
    public HotbarResult Assign(Guid session, long hotbarRevision, int position, ItemLocation source,
        ItemEntityId entity, long inventoryRevision, long equipmentRevision, bool commit = true)
    {
        var result = state.ValidateEdit(session, hotbarRevision, position);
        if (result != HotbarResult.Success) return result;
        var validation = transfers.Validate(new TransferRequest(session, source, entity, source, entity,
            inventoryRevision, equipmentRevision));
        if (validation != TransferResult.NoChange) return validation switch
        {
            TransferResult.InvalidSession => HotbarResult.InvalidSession,
            TransferResult.StaleRevision or TransferResult.StaleEntity => HotbarResult.StaleRevision,
            TransferResult.InvalidLocation => HotbarResult.InvalidLocation,
            TransferResult.Disposed => HotbarResult.Disposed,
            TransferResult.PublicationInProgress => HotbarResult.PublicationInProgress,
            // A validated transfer to the source itself can only be NoChange or a validation error.
            _ => throw new InvalidOperationException($"Unexpected source validation result: {validation}")
        };
        var sourceEntity = source.InventoryAddress.HasValue ? inventory.Snapshot.Slots[source.InventoryAddress.Value] :
            equipment.Snapshot.Slots[equipment.FindSlot(source.EquipmentSlotKey!)];
        var definition = inventory.Catalog.Get(sourceEntity!.DefinitionId);
        HotbarBinding? binding = ItemHotbarPolicy.IsConsumable(definition.CategoryId)
            ? new ConsumableHotbarBinding(definition.Id)
            : source.EquipmentSlotKey != null && ItemHotbarPolicy.IsWeapon(definition.CategoryId)
                ? new WeaponHotbarBinding(source.EquipmentSlotKey) : null;
        if (binding == null) return HotbarResult.IneligibleSource;
        return commit ? state.Assign(session, hotbarRevision, position, binding) :
            state.ValidateAssignment(session, hotbarRevision, position, binding);
    }

    public HotbarAvailability Resolve(int position)
    {
        if (position is < 0 or >= ItemHotbarState.PositionCount) return new(null, null, "", false);
        switch (state.Entries[position])
        {
            case ConsumableHotbarBinding consumable:
                long quantity = 0;
                foreach (var entity in inventory.Snapshot.Slots)
                    if (entity?.DefinitionId == consumable.DefinitionId) quantity = checked(quantity + entity.Count);
                return new(consumable.DefinitionId, quantity, inventory.Catalog.Get(consumable.DefinitionId).DisplayName, quantity > 0);
            case WeaponHotbarBinding weapon:
                var index = equipment.FindSlot(weapon.SlotKey);
                if (index < 0) return new(null, null, weapon.SlotKey, false);
                var equipped = equipment.Snapshot.Slots[index];
                return new(equipped?.DefinitionId, null, equipment.Definitions[index].DisplayName, equipped != null);
            case ActionHotbarBinding action:
                return new(null, null, action.ActionKey, true);
            default:
                return new(null, null, "Empty hotbar position", false);
        }
    }

    public HotbarResult Activate(int position, Action<string> activateAction, Action<ItemHotbarActivationRequest> activateItem)
    {
        var availability = Resolve(position);
        if (!availability.Available) return HotbarResult.Unavailable;
        var binding = state.Entries[position]!;
        if (binding is ActionHotbarBinding action) activateAction(action.ActionKey);
        else activateItem(new ItemHotbarActivationRequest(position, binding, availability.DefinitionId!.Value, availability.Quantity));
        return HotbarResult.Success;
    }
}
