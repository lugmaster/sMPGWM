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
    private readonly Array _activeBehaviours = new(), _activeConditions = new();
    private readonly Array[] _behaviourRows = new Array[ShipRuntime.MaxBehaviours];
    private readonly double[] _statValues = new double[14];

    public GameplayDeltaState()
    {
        _row.Resize(6);
        _configuration.Resize(ConfigurationFields.Length);
        _effects.Resize(EffectFields.Length);
        _resources.Resize(ResourceFields.Length);
        for (var i = 0; i < _behaviourRows.Length; i++)
        {
            _behaviourRows[i] = new();
            _behaviourRows[i].Resize(3);
        }
        _effects[0] = _activeBehaviours;
        _effects[1] = _activeConditions;
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
        if (configurationChanged)
        {
            var s = state.Stats;
            _statValues[0] = s.Speed; _statValues[1] = s.AfterBurnerSpeed; _statValues[2] = s.RotationSpeed;
            _statValues[3] = s.Acceleration; _statValues[4] = s.AfterBurnerAcceleration; _statValues[5] = s.Mass;
            _statValues[6] = s.Armor; _statValues[7] = s.ShieldCapacity; _statValues[8] = s.RechargeTime;
            _statValues[9] = s.DownTime; _statValues[10] = s.EnergyProduction; _statValues[11] = s.EnergyConsumption;
            _statValues[12] = s.EnergyStorage; _statValues[13] = s.EngineEnergyConsumption;
            _configuration[0] = state.Shield.Capacity; _configuration[1] = state.FactionId;
            _configuration[2] = _statValues;
            _configuration[3] = (s.HasEngine ? 1 : 0) | (s.HasHull ? 2 : 0) | (s.HasShieldGenerator ? 4 : 0) |
                (s.HasReactor ? 8 : 0) | (s.HasBattery ? 16 : 0);
            _configuration[4] = (int)state.Components.Presence; _configuration[5] = state.Health.Capacity;
            _configurationEpoch = state.Revision;
            _stats = state.Stats; _components = state.Components.Presence;
            _faction = state.FactionId; _healthCapacity = state.Health.Capacity;
        }
        if (effectsChanged)
        {
            _activeBehaviours.Clear(); _activeConditions.Clear();
            System.Array.Clear(_conditions);
            _behaviourCount = state.ActiveBehaviours.Count; _conditionCount = state.Conditions.Count;
            for (var i = 0; i < _behaviourCount; i++)
            {
                var active = _behaviours[i] = state.ActiveBehaviours[i];
                var row = _behaviourRows[i];
                row[0] = active.DefinitionId; row[1] = active.RuntimeId; row[2] = active.Deadline;
                _activeBehaviours.Add(row);
            }
            for (var i = 0; i < _conditionCount; i++)
                _activeConditions.Add(_conditions[i] = state.Conditions[i]);
            _effectEpoch = state.Revision;
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
        _activeBehaviours.Dispose(); _activeConditions.Dispose();
        foreach (var row in _behaviourRows) row.Dispose();
    }
}
