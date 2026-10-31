using ItemLocation = sMPGWM.Scripts.EquipmentSystem.ItemLocation;


using System;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.Items;
public sealed class ItemHotbarController(
    ItemHotbarState state,
    IInventoryView inventory,
    IEquipmentView equipment,
    IItemTransfers transfers)
{
    public IReadOnlyList<HotbarBinding?> Entries => state.Entries;
    public long Revision => state.Revision;
    private Action? _changed;
    public event Action Changed
    {
        add
        {
            if (_changed == null)
            {
                state.Changed += NotifyChanged;
                inventory.Changed += OnInventoryChanged;
                equipment.Changed += OnEquipmentChanged;
            }
            _changed += value;
        }
        remove
        {
            _changed -= value;
            if (_changed == null)
            {
                state.Changed -= NotifyChanged;
                inventory.Changed -= OnInventoryChanged;
                equipment.Changed -= OnEquipmentChanged;
            }
        }
    }

    private void OnInventoryChanged(InventorySnapshot _) => NotifyChanged();
    private void OnEquipmentChanged(EquipmentSnapshot _) => NotifyChanged();
    private void NotifyChanged()
    {
        if (_changed is not { } handlers) return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try { handler(); }
            catch (Exception exception) { sMPGWM.Scripts.LoggingSystem.Logger.Error($"Hotbar observer failed: {exception}"); }
        }
    }

    public void SetEditSession(ulong session) => state.SetEditSession(session);
    public HotbarResult ValidateEdit(ulong session, long revision, int position) => state.ValidateEdit(session, revision, position);
    public HotbarResult ValidateMove(ulong session, long revision, int source, int destination) => state.ValidateMove(session, revision, source, destination);
    public HotbarResult Move(ulong session, long revision, int source, int destination) => state.Move(session, revision, source, destination);
    public HotbarResult Clear(ulong session, long revision, int position) => state.Clear(session, revision, position);

    public HotbarResult ValidateAssignment(HotbarAssignmentRequest request) => EvaluateAssignment(request, false);
    public HotbarResult Assign(HotbarAssignmentRequest request) => EvaluateAssignment(request, true);

    private HotbarResult EvaluateAssignment(HotbarAssignmentRequest request, bool commit)
    {
        var (session, hotbarRevision, position, source, entity, inventoryRevision, equipmentRevision) = request;
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
            equipment.Snapshot.Slots[FindEquipmentSlot(source.EquipmentSlotKey!)];
        var definition = sourceEntity!.Definition;
        HotbarBinding? binding = ItemHotbarPolicy.IsConsumable(definition.CategoryName)
            ? new ConsumableHotbarBinding(definition.Id)
            : source.EquipmentSlotKey != null && ItemHotbarPolicy.IsWeapon(definition.CategoryName)
                ? new WeaponHotbarBinding(source.EquipmentSlotKey) : null;
        if (binding == null) return HotbarResult.IneligibleSource;
        return commit ? state.Assign(session, hotbarRevision, position, binding) :
            state.ValidateAssignment(session, hotbarRevision, position, binding);
    }

    private int FindEquipmentSlot(string key)
    {
        for (var index = 0; index < equipment.Definitions.Count; index++)
            if (equipment.Definitions[index].Key == key) return index;
        return -1;
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
                return new(consumable.DefinitionId, quantity, ItemRegistry.GetById(consumable.DefinitionId).DisplayName, quantity > 0);
            case WeaponHotbarBinding weapon:
                var index = FindEquipmentSlot(weapon.SlotKey);
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
        else activateItem(new ItemHotbarActivationRequest(position, binding, availability.DefinitionId!, availability.Quantity));
        return HotbarResult.Success;
    }
}

