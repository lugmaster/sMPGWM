using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class ShipWeaponController
{
    private readonly ShipController _ship;
    private readonly WeaponStateStore _states;
    private WeaponBinding?[] _bindings;
    private readonly byte[] _groups;
    private bool _armed;
    private long _generation = -1;
    public long ConfigurationRevision { get; private set; } = 1;
    public IReadOnlyList<WeaponBinding?> Bindings => _bindings;
    public byte HeldMask { get; private set; }
    public bool Available => _ship.Runtime.IsAlive && _ship.Runtime.PoweredSystemsAvailable && _ship.Runtime.Components.Has(ShipComponentKind.Weapons);

    public ShipWeaponController(ShipController ship, WeaponStateStore states, int slots)
    {
        _ship = ship; _states = states; _bindings = new WeaponBinding?[slots]; _groups = new byte[slots];
        Array.Fill(_groups, (byte)1);
    }
    public byte GetGroup(int slot) => _groups[slot];
    internal Action Prepare(ItemEntity?[] items, bool authoritative)
    {
        var bindings = new WeaponBinding?[items.Length];
        var changed = false;
        var count = 0;
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] is { Definition.Weapon: { } definition } item)
            {
                var stats = definition.Stats;
                if (++count > 9 || !stats.IsValid || item.Definition.CanStack || item.Count != 1 ||
                    definition.Delivery != WeaponDelivery.HitScan || definition.FireMode != WeaponFireMode.Automatic ||
                    (definition.SupplyMode == WeaponSupplyMode.Ammunition
                        ? stats.MagazineCapacity < 1 || stats.ReloadTicks < 1 || stats.EnergyPerShot != 0
                        : definition.SupplyMode != WeaponSupplyMode.Energy || stats.MagazineCapacity != 0 || stats.ReloadTicks != 0 || stats.EnergyPerShot <= 0))
                    throw new ArgumentException("Invalid resolved weapon binding: " + item.DefinitionId);
                bindings[i] = new(i, item.Id, definition, _states);
            }
            changed |= bindings[i]?.ItemId != _bindings[i]?.ItemId;
        }
        var revision = changed && authoritative ? checked(ConfigurationRevision + 1) : ConfigurationRevision;
        return () =>
        {
            for (var i = 0; i < _bindings.Length; i++)
                if (authoritative && _bindings[i] is { } previous && previous.ItemId != bindings[i]?.ItemId && _states.TryGet(previous.ItemId, out var state)) state.CancelReload();
            _bindings = bindings;
            ConfigurationRevision = revision;
            if (changed) HeldMask = 0;
        };
    }
    public void SetInput(byte held, long revision)
    {
        var generation = _ship.ControllingPlayer?.ControlGeneration ?? -1;
        if (_generation != generation) { ClearInput(); _generation = generation; }
        if (held == 0 && revision == ConfigurationRevision) _armed = true;
        HeldMask = _armed && Available && revision == ConfigurationRevision && held <= 3 ? held : (byte)0;
    }
    public void ClearInput() { HeldMask = 0; _armed = false; }
    public bool AssignGroup(int slot, ItemEntityId item, long expectedRevision, byte group)
    {
        if (group > 2 || slot < 0 || slot >= _bindings.Length || _bindings[slot]?.ItemId != item || expectedRevision != ConfigurationRevision) return false;
        if (_groups[slot] == group) return true;
        ConfigurationRevision = checked(ConfigurationRevision + 1);
        _groups[slot] = group; HeldMask = 0;
        return true;
    }
    public void ReloadAll(long tick)
    {
        if (!Available) return;
        foreach (var binding in _bindings) binding?.State.StartReload(tick, binding.Definition.Stats);
    }
    public void AdvanceReloads(long tick)
    {
        foreach (var binding in _bindings)
        {
            if (binding == null) continue;
            if (!Available) binding.State.CancelReload();
            else binding.State.CompleteReload(tick, binding.Definition.Stats);
        }
        if (!Available) HeldMask = 0;
    }
    public WeaponConfigurationSnapshot CaptureConfiguration()
    {
        var items = new ItemEntityId[_bindings.Length];
        for (var i = 0; i < items.Length; i++) items[i] = _bindings[i]?.ItemId ?? default;
        return new(ConfigurationRevision, (byte[])_groups.Clone(), items);
    }
    public bool ValidateConfiguration(WeaponConfigurationSnapshot state)
    {
        if (state.Revision < 1 || state.Revision == long.MaxValue || state.Groups.Length != _groups.Length || state.Items.Length != _bindings.Length) return false;
        for (var i = 0; i < _bindings.Length; i++)
            if (state.Groups[i] > 2 || state.Items[i] != (_bindings[i]?.ItemId ?? default) ||
                state.Revision == ConfigurationRevision && state.Groups[i] != _groups[i]) return false;
        return true;
    }
    public bool ConfirmConfiguration(WeaponConfigurationSnapshot state)
    {
        if (!ValidateConfiguration(state) || state.Revision < ConfigurationRevision) return false;
        ConfigurationRevision = state.Revision;
        Array.Copy(state.Groups, _groups, _groups.Length);
        ClearInput();
        return true;
    }
}
