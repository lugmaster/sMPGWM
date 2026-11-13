using System;
using System.Collections.Generic;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.WeaponSystem;

public sealed class WeaponStateStore
{
    private readonly Dictionary<ItemEntityId, WeaponRuntimeState> _states = new();
    public bool TryGet(ItemEntityId id, out WeaponRuntimeState state) => _states.TryGetValue(id, out state!);
    internal WeaponRuntimeState GetOrCreate(ItemEntity item)
    {
        if (_states.TryGetValue(item.Id, out var state)) return state;
        state = new(item.Id, item.Definition.Weapon!.Stats.MagazineCapacity);
        _states.Add(item.Id, state);
        return state;
    }
    internal void Remove(ItemEntityId item) => _states.Remove(item);
    internal Action PrepareItems(IEnumerable<ItemEntity?[]> rows)
    {
        var additions = new List<WeaponRuntimeState>();
        foreach (var row in rows)
            foreach (var item in row)
                if (item?.Definition.Weapon is { } weapon && !_states.ContainsKey(item.Id))
                    additions.Add(new(item.Id, weapon.Stats.MagazineCapacity));
        _states.EnsureCapacity(_states.Count + additions.Count);
        return () => { foreach (var state in additions) _states.TryAdd(state.ItemId, state); };
    }
    public WeaponStateSnapshot[] CaptureState()
    {
        var result = new WeaponStateSnapshot[_states.Count];
        var i = 0;
        foreach (var state in _states.Values) result[i++] = state.CaptureState();
        return result;
    }
    public bool Validate(WeaponStateSnapshot state, ItemStore items)
    {
        if (state.ItemId == default || !items.TryGetItem(state.ItemId, out var item) || item.Definition.Weapon is not { } weapon) return false;
        return state.Revision > 0 && state.Revision < long.MaxValue && state.Rounds >= 0 && state.Rounds <= weapon.Stats.MagazineCapacity &&
            double.IsFinite(state.NextFireTick) && state.NextFireTick >= 0 && state.NextFireTick < long.MaxValue &&
            state.ShotSequence < ulong.MaxValue && ((state.ReloadStartTick == -1 && state.ReloadDeadline == -1) ||
            weapon.SupplyMode == WeaponSupplyMode.Ammunition && state.ReloadStartTick >= 0 &&
            state.ReloadDeadline > state.ReloadStartTick && state.ReloadDeadline - state.ReloadStartTick == weapon.Stats.ReloadTicks);
    }
    public bool Confirm(IReadOnlyList<WeaponStateSnapshot> states, ItemStore items)
    {
        var seen = new HashSet<ItemEntityId>();
        for (var i = 0; i < states.Count; i++)
        {
            var incoming = states[i];
            if (!Validate(incoming, items) || !seen.Add(incoming.ItemId) ||
                _states.TryGetValue(incoming.ItemId, out var current) && incoming.Revision == current.Revision && incoming != current.CaptureState()) return false;
        }
        for (var i = 0; i < states.Count; i++)
        {
            var incoming = states[i];
            var state = GetOrCreate(items.GetItem(incoming.ItemId));
            if (incoming.Revision > state.Revision) state.Install(incoming);
        }
        return true;
    }
    public void RestoreState(IReadOnlyList<WeaponStateSnapshot> states, ItemStore items)
    {
        if (states.Count != _states.Count || !Confirm(states, items)) throw new ArgumentException("Invalid restored weapon state.");
    }
}
