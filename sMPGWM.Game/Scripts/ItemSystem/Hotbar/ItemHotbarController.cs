using System;
using System.Collections.Generic;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.InventorySystem;

namespace sMPGWM.Scripts.ItemSystem.Hotbar;
public sealed class ItemHotbarController : IDisposable
{
    private readonly ItemHotbarState _state;
    private IInventoryView _inventory;
    private IEquipmentView _equipment;
    private readonly IItemTransfers _transfers;
    private readonly Action<Exception>? _reportError;
    private bool _disposed;
    private bool _restored;
    public Func<bool>? EquipmentAbilitiesAvailable { get; set; }
    private bool _cachedEquipmentAvailability;
    public Predicate<string>? SupportsAbility { private get; init; }

    public ItemHotbarController(IInventoryView inventory, IEquipmentView equipment, IItemTransfers transfers,
        Action<Exception>? reportError = null)
        : this(new ItemHotbarState(), inventory, equipment, transfers, reportError) { }

    // Internal injection supports seeded fixture state; the controller owns its lifetime.
    internal ItemHotbarController(ItemHotbarState state, IInventoryView inventory, IEquipmentView equipment,
        IItemTransfers transfers, Action<Exception>? reportError = null)
    {
        this._state = state;
        this._inventory = inventory;
        this._equipment = equipment;
        this._transfers = transfers;
        this._reportError = reportError;
    }

    internal void InvalidateAvailability()
    {
        _cachedHotbarRevision = -1;
        _availability.Clear();
        NotifyChanged();
    }

    public void Rebind(IInventoryView inventory, IEquipmentView equipment)
    {
        _state.SetEditSession(0);
        _inventory.Changed -= OnInventoryChanged;
        _equipment.Changed -= OnEquipmentChanged;
        _inventory = inventory;
        _equipment = equipment;
        if (_changed != null)
        {
            _inventory.Changed += OnInventoryChanged;
            _equipment.Changed += OnEquipmentChanged;
        }
        _cachedInventoryRevision = -1;
        _cachedEquipmentRevision = -1;
        _availability.Clear();
        NotifyChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state.Changed -= NotifyChanged;
        _inventory.Changed -= OnInventoryChanged;
        _equipment.Changed -= OnEquipmentChanged;
        _changed = null;
        _observers = Array.Empty<Delegate>();
        _state.Dispose();
    }

    private readonly Dictionary<string, string> _actionTitles = new();
    public void SetActionTitle(string key, string title)
    {
        _actionTitles[key] = title;
        _availability.Clear();
        _cachedHotbarRevision = -1;
        NotifyChanged();
    }
    public void BindAction(int position, string key)
    {
        _state.SetEditSession(ulong.MaxValue);
        _state.Assign(ulong.MaxValue, _state.Revision, position, new ActionHotbarBinding(key));
        _state.SetEditSession(0);
    }

    public void InitializeEquipmentAbilities()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_restored) return;
        if (_state.Revision != 0) return;
        foreach (var binding in _state.Entries)
            if (binding != null) return;
        var position = 0;
        _state.SetEditSession(ulong.MaxValue);
        try
        {
            for (var slot = 0; slot < _equipment.Definitions.Count; slot++)
            {
                if (_equipment.Snapshot.Slots[slot] is not { } item) continue;
                foreach (var ability in item.Definition.AbilityIds)
                {
                    if (SupportsAbility?.Invoke(ability) == false) continue;
                    if (position == ItemHotbarState.PositionCount) return;
                    var result = _state.Assign(ulong.MaxValue, _state.Revision, position++,
                        new EquipmentAbilityHotbarBinding(_equipment.Definitions[slot].Key, ability));
                    if (result != HotbarResult.Success)
                        throw new InvalidOperationException("Could not initialize equipment hotbar: " + result);
                }
            }
        }
        finally { _state.SetEditSession(0); }
    }

    public IReadOnlyList<HotbarBinding?> Entries => _state.Entries;
    public ItemHotbarSnapshot CaptureState() => new(_state.Revision, _state.Entries);

    public void RestoreState(ItemHotbarSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_restored) throw new InvalidOperationException("Hotbar state is already restored.");
        foreach (var binding in snapshot.Entries)
        {
            if (binding is ConsumableHotbarBinding consumable && !ItemRegistry.TryGetById(consumable.DefinitionId, out _))
                throw new ArgumentException("Unknown restored hotbar item: " + consumable.DefinitionId);
            var slotKey = binding switch
            {
                EquipmentAbilityHotbarBinding ability => ability.SlotKey,
                WeaponHotbarBinding weapon => weapon.SlotKey,
                _ => null
            };
            if (slotKey != null)
            {
                var found = false;
                foreach (var slot in _equipment.Definitions)
                    if (slot.Key == slotKey) found = true;
                if (!found) throw new ArgumentException("Unknown restored hotbar equipment slot: " + slotKey);
            }
            if (binding is EquipmentAbilityHotbarBinding equipmentAbility &&
                !GameplayDefinitions.Current.Abilities.TryGetById(equipmentAbility.AbilityId, out _))
                throw new ArgumentException("Unknown restored hotbar ability: " + equipmentAbility.AbilityId);
        }
        _state.RestoreState(snapshot);
        _restored = true;
        InvalidateAvailability();
    }
    public long Revision => _state.Revision;
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
            if (value == null || _disposed) return;
            if (_changed == null)
            {
                _state.Changed += NotifyChanged;
                _inventory.Changed += OnInventoryChanged;
                _equipment.Changed += OnEquipmentChanged;
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
                _state.Changed -= NotifyChanged;
                _inventory.Changed -= OnInventoryChanged;
                _equipment.Changed -= OnEquipmentChanged;
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
                try { _reportError?.Invoke(exception); }
                catch (Exception) { }
            }
        }
    }

    public void SetEditSession(ulong session) => _state.SetEditSession(session);
    public HotbarResult ValidateEdit(ulong session, long revision, int position) => _state.ValidateEdit(session, revision, position);
    public HotbarResult ValidateMove(ulong session, long revision, int source, int destination) => _state.ValidateMove(session, revision, source, destination);
    public HotbarResult Move(ulong session, long revision, int source, int destination) => _state.Move(session, revision, source, destination);
    public HotbarResult Clear(ulong session, long revision, int position) => _state.Clear(session, revision, position);

    public HotbarResult ValidateAssignment(HotbarAssignmentRequest request) => EvaluateAssignment(request, false);
    public HotbarResult Assign(HotbarAssignmentRequest request) => EvaluateAssignment(request, true);

    private HotbarResult EvaluateAssignment(HotbarAssignmentRequest request, bool commit)
    {
        var (session, hotbarRevision, position, source, entity, inventoryRevision, equipmentRevision) = request;
        var result = _state.ValidateEdit(session, hotbarRevision, position);
        if (result != HotbarResult.Success) return result;
        var validation = _transfers.Validate(new TransferRequest(session, source, entity, source, entity,
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
        var sourceEntity = source.InventoryAddress.HasValue ? _inventory.Snapshot.Slots[source.InventoryAddress.Value] :
            _equipment.Snapshot.Slots[FindEquipmentSlot(source.EquipmentSlotKey!)];
        var definition = sourceEntity!.Definition;
        var current = _state.Entries[position];
        if (ItemHotbarPolicy.IsConsumable(definition.CategoryName))
        {
            if (current is ConsumableHotbarBinding consumable && consumable.DefinitionId == definition.Id)
                return HotbarResult.NoChange;
            return commit ? _state.Assign(session, hotbarRevision, position, new ConsumableHotbarBinding(definition.Id)) :
                HotbarResult.Success;
        }
        if (source.EquipmentSlotKey is { } slotKey && ItemHotbarPolicy.IsWeapon(definition.CategoryName))
        {
            if (current is WeaponHotbarBinding weapon && weapon.SlotKey == slotKey)
                return HotbarResult.NoChange;
            return commit ? _state.Assign(session, hotbarRevision, position, new WeaponHotbarBinding(slotKey)) :
                HotbarResult.Success;
        }
        if (source.EquipmentSlotKey is { } abilitySlot && definition.AbilityIds.Count == 1)
        {
            if (SupportsAbility?.Invoke(definition.AbilityIds[0]) == false) return HotbarResult.IneligibleSource;
            var binding = new EquipmentAbilityHotbarBinding(abilitySlot, definition.AbilityIds[0]);
            if (current == binding) return HotbarResult.NoChange;
            return commit ? _state.Assign(session, hotbarRevision, position, binding) : HotbarResult.Success;
        }
        return HotbarResult.IneligibleSource;
    }

    private int FindEquipmentSlot(string key)
    {
        for (var index = 0; index < _equipment.Definitions.Count; index++)
            if (_equipment.Definitions[index].Key == key) return index;
        return -1;
    }

    public HotbarAvailability Resolve(int position)
    {
        if (position is < 0 or >= ItemHotbarState.PositionCount) return InvalidPosition;
        var inventorySnapshot = _inventory.Snapshot;
        var equipmentSnapshot = _equipment.Snapshot;
        var equipmentAvailable = EquipmentAbilitiesAvailable?.Invoke() ?? true;
        // Resolve can be called without subscribers, so events alone cannot invalidate this cache.
        if (_cachedEquipmentAvailability != equipmentAvailable || _cachedHotbarRevision != _state.Revision || _cachedInventoryRevision != inventorySnapshot.Revision ||
            _cachedEquipmentRevision != equipmentSnapshot.Revision)
        {
            _availability.Clear();
            _cachedEquipmentAvailability = equipmentAvailable;
            _cachedHotbarRevision = _state.Revision;
            _cachedInventoryRevision = inventorySnapshot.Revision;
            _cachedEquipmentRevision = equipmentSnapshot.Revision;
        }
        var binding = _state.Entries[position];
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
            case EquipmentAbilityHotbarBinding ability:
                var slot = FindEquipmentSlot(ability.SlotKey);
                var source = slot < 0 ? null : equipmentSnapshot.Slots[slot];
                var available = false;
                if (source != null)
                    foreach (var id in source.Definition.AbilityIds)
                        if (id == ability.AbilityId) { available = true; break; }
                var definition = GameplayDefinitions.Current.Abilities.GetById(ability.AbilityId);
                return new(source?.DefinitionId, null, definition.DisplayName, available && _cachedEquipmentAvailability && SupportsAbility?.Invoke(ability.AbilityId) != false);
            case ConsumableHotbarBinding consumable:
                long quantity = 0;
                foreach (var entity in inventorySnapshot.Slots)
                    if (entity?.DefinitionId == consumable.DefinitionId) quantity = checked(quantity + entity.Count);
                return new(consumable.DefinitionId, quantity, ItemRegistry.GetById(consumable.DefinitionId).DisplayName, quantity > 0);
            case WeaponHotbarBinding weapon:
                var index = FindEquipmentSlot(weapon.SlotKey);
                if (index < 0) return new(null, null, weapon.SlotKey, false);
                var equipped = equipmentSnapshot.Slots[index];
                return new(equipped?.DefinitionId, null, _equipment.Definitions[index].DisplayName, equipped != null);
            case ActionHotbarBinding action:
                return new(null, null, _actionTitles.TryGetValue(action.ActionKey, out var title) ? title : action.ActionKey, true);
            default:
                return EmptyPosition;
        }
    }

    public HotbarResult Activate(int position, Action<string> activateAction, Action<ItemHotbarActivationRequest> activateItem)
    {
        if (_disposed) return HotbarResult.Disposed;
        var availability = Resolve(position);
        if (!availability.Available) return HotbarResult.Unavailable;
        var binding = _state.Entries[position]!;
        if (binding is ActionHotbarBinding action) activateAction(action.ActionKey);
        else activateItem(new ItemHotbarActivationRequest(position, binding, availability.DefinitionId!, availability.Quantity));
        return HotbarResult.Success;
    }
}

