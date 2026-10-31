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
    IItemTransfers transfers,
    Action<Exception>? reportError = null)
{
    public IReadOnlyList<HotbarBinding?> Entries => state.Entries;
    public long Revision => state.Revision;
    private Action? _changed;
    private Delegate[] _observers = Array.Empty<Delegate>();
    private readonly Dictionary<HotbarBinding, HotbarAvailability> _availability = new();
    private long _cachedHotbarRevision = -1;
    private long _cachedInventoryRevision = -1;
    private long _cachedEquipmentRevision = -1;
    private static readonly HotbarAvailability InvalidPosition = new(null, null, "", false);
    private static readonly HotbarAvailability EmptyPosition = new(null, null, "Empty hotbar position", false);
    public event Action Changed
    {
        add
        {
            if (value == null) return;
            if (_changed == null)
            {
                state.Changed += NotifyChanged;
                inventory.Changed += OnInventoryChanged;
                equipment.Changed += OnEquipmentChanged;
            }
            _changed += value;
            _observers = _changed.GetInvocationList();
        }
        remove
        {
            _changed -= value;
            _observers = _changed?.GetInvocationList() ?? Array.Empty<Delegate>();
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
        // Keep this publication's snapshot even if a handler changes subscriptions.
        foreach (Action handler in _observers)
        {
            try { handler(); }
            catch (Exception exception)
            {
                try { reportError?.Invoke(exception); }
                catch (Exception) { }
            }
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
        var current = state.Entries[position];
        if (ItemHotbarPolicy.IsConsumable(definition.CategoryName))
        {
            if (current is ConsumableHotbarBinding consumable && consumable.DefinitionId == definition.Id)
                return HotbarResult.NoChange;
            return commit ? state.Assign(session, hotbarRevision, position, new ConsumableHotbarBinding(definition.Id)) :
                HotbarResult.Success;
        }
        if (source.EquipmentSlotKey is { } slotKey && ItemHotbarPolicy.IsWeapon(definition.CategoryName))
        {
            if (current is WeaponHotbarBinding weapon && weapon.SlotKey == slotKey)
                return HotbarResult.NoChange;
            return commit ? state.Assign(session, hotbarRevision, position, new WeaponHotbarBinding(slotKey)) :
                HotbarResult.Success;
        }
        return HotbarResult.IneligibleSource;
    }

    private int FindEquipmentSlot(string key)
    {
        for (var index = 0; index < equipment.Definitions.Count; index++)
            if (equipment.Definitions[index].Key == key) return index;
        return -1;
    }

    public HotbarAvailability Resolve(int position)
    {
        if (position is < 0 or >= ItemHotbarState.PositionCount) return InvalidPosition;
        var inventorySnapshot = inventory.Snapshot;
        var equipmentSnapshot = equipment.Snapshot;
        // Resolve can be called without subscribers, so events alone cannot invalidate this cache.
        if (_cachedHotbarRevision != state.Revision || _cachedInventoryRevision != inventorySnapshot.Revision ||
            _cachedEquipmentRevision != equipmentSnapshot.Revision)
        {
            _availability.Clear();
            _cachedHotbarRevision = state.Revision;
            _cachedInventoryRevision = inventorySnapshot.Revision;
            _cachedEquipmentRevision = equipmentSnapshot.Revision;
        }
        var binding = state.Entries[position];
        if (binding == null) return EmptyPosition;
        if (_availability.TryGetValue(binding, out var cached)) return cached;
        var availability = ResolveBinding(binding, inventorySnapshot, equipmentSnapshot);
        _availability.Add(binding, availability);
        return availability;
    }

    private HotbarAvailability ResolveBinding(HotbarBinding binding, InventorySnapshot inventorySnapshot,
        EquipmentSnapshot equipmentSnapshot)
    {
        switch (binding)
        {
            case ConsumableHotbarBinding consumable:
                long quantity = 0;
                foreach (var entity in inventorySnapshot.Slots)
                    if (entity?.DefinitionId == consumable.DefinitionId) quantity = checked(quantity + entity.Count);
                return new(consumable.DefinitionId, quantity, ItemRegistry.GetById(consumable.DefinitionId).DisplayName, quantity > 0);
            case WeaponHotbarBinding weapon:
                var index = FindEquipmentSlot(weapon.SlotKey);
                if (index < 0) return new(null, null, weapon.SlotKey, false);
                var equipped = equipmentSnapshot.Slots[index];
                return new(equipped?.DefinitionId, null, equipment.Definitions[index].DisplayName, equipped != null);
            case ActionHotbarBinding action:
                return new(null, null, action.ActionKey, true);
            default:
                return EmptyPosition;
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

