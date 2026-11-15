using System;
using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

// Reliable positional rows: entity, configuration epoch, effects epoch, configuration, effects, resources.
internal sealed class GameplayDeltaState : IDisposable
{
    internal static readonly int[] ConfigurationFields = [2, 5, 7, 8, 16, 17];
    internal static readonly int[] EffectFields = [4, 6];
    internal static readonly int[] ResourceFields = [1, 3, 9, 10, 11, 12, 13, 14, 15, 18];
    private readonly BehaviourInstance[] _behaviours = new BehaviourInstance[ShipRuntime.MaxBehaviours];
    private readonly string?[] _conditions = new string?[ShipRuntime.MaxConditions];
    private int _behaviourCount = -1, _conditionCount = -1;
    private ResolvedShipStats _stats;
    private ShipComponentKind _components;
    private int _faction;
    private double _healthCapacity;
    private long _revision, _configurationEpoch, _effectEpoch;
    private readonly Array _row = new(), _configuration = new(), _effects = new(), _resources = new(), _empty = new();

    public GameplayDeltaState()
    {
        _row.Resize(6);
        _resources.Resize(ResourceFields.Length);
    }

    public Array? Encode(int entity, ShipRuntime state)
    {
        if (_revision == state.Revision) return null;
        var configurationChanged = _revision == 0 || _stats != state.Stats || _components != state.Components.Presence ||
            _faction != state.FactionId || _healthCapacity != state.Health.Capacity;
        var effectsChanged = _behaviourCount != state.ActiveBehaviours.Count || _conditionCount != state.Conditions.Count;
        if (!effectsChanged)
        {
            for (var i = 0; i < _behaviourCount; i++) if (_behaviours[i] != state.ActiveBehaviours[i]) { effectsChanged = true; break; }
            for (var i = 0; i < _conditionCount; i++) if (_conditions[i] != state.Conditions[i]) { effectsChanged = true; break; }
        }
        if (configurationChanged || effectsChanged)
        {
            using var full = GameplaySnapshotCodec.Encode("", state);
            if (configurationChanged)
            {
                _configuration.Clear();
                foreach (var field in ConfigurationFields) _configuration.Add(full[field]);
                _configurationEpoch = state.Revision;
                _stats = state.Stats; _components = state.Components.Presence;
                _faction = state.FactionId; _healthCapacity = state.Health.Capacity;
            }
            if (effectsChanged)
            {
                _effects.Clear();
                foreach (var field in EffectFields) _effects.Add(full[field]);
                _effectEpoch = state.Revision;
                System.Array.Clear(_conditions);
                _behaviourCount = state.ActiveBehaviours.Count; _conditionCount = state.Conditions.Count;
                for (var i = 0; i < _behaviourCount; i++) _behaviours[i] = state.ActiveBehaviours[i];
                for (var i = 0; i < _conditionCount; i++) _conditions[i] = state.Conditions[i];
            }
        }
        _revision = state.Revision;
        _resources[0] = state.Revision; _resources[1] = state.Shield.Current; _resources[2] = state.Energy.Current;
        _resources[3] = state.ConfigurationRevision; _resources[4] = state.AuthoritativeTick;
        _resources[5] = state.ShieldDowntimeDeadline; _resources[6] = state.EnergyDepleted;
        _resources[7] = state.ShieldProtectionAvailable; _resources[8] = state.AfterburnerActive; _resources[9] = state.Health.Current;
        _row[0] = entity; _row[1] = _configurationEpoch; _row[2] = _effectEpoch;
        _row[3] = configurationChanged ? _configuration : _empty;
        _row[4] = effectsChanged ? _effects : _empty; _row[5] = _resources;
        return _row;
    }

    internal static bool TryReadResources(Array values, out ShipResourceUpdate update)
    {
        update = default;
        if (values.Count != 10) return false;
        for (var i = 0; i < 10; i++)
        {
            var type = i is 1 or 2 or 9 ? Variant.Type.Float : i is 6 or 7 or 8 ? Variant.Type.Bool : Variant.Type.Int;
            if (values[i].VariantType != type) return false;
        }
        update = new(values[0].AsInt64(), values[1].AsDouble(), values[2].AsDouble(), values[3].AsInt64(),
            values[4].AsInt64(), values[5].AsInt64(), values[6].AsBool(), values[7].AsBool(), values[8].AsBool(), values[9].AsDouble());
        return true;
    }

    public void Dispose()
    {
        _row.Dispose(); _configuration.Dispose(); _effects.Dispose(); _resources.Dispose(); _empty.Dispose();
    }
}
