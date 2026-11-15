using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;
using Array = Godot.Collections.Array;

namespace sMPGWM.Scripts.NetworkSystem;

internal sealed class GameplayDeltaReceiver : IDisposable
{
    private readonly Dictionary<string, Cache> _states = new(StringComparer.Ordinal);
    private readonly ShipController?[] _ships = new ShipController?[WorldTopology.MaxEntities];
    private readonly ShipResourceUpdate[] _resources = new ShipResourceUpdate[WorldTopology.MaxEntities];
    private readonly GameplaySnapshotBuffer?[] _full = new GameplaySnapshotBuffer?[WorldTopology.MaxEntities];
    private readonly bool[] _changed = new bool[WorldTopology.MaxEntities];
    private readonly long[] _configurationEpochs = new long[WorldTopology.MaxEntities];
    private readonly long[] _effectEpochs = new long[WorldTopology.MaxEntities];
    private readonly Array?[] _pending = new Array?[WorldTopology.MaxEntities];
    private readonly string?[] _removed = new string?[WorldTopology.MaxEntities];

    public void Prune(GameWorld world)
    {
        var count = 0;
        foreach (var entity in _states.Keys) if (!world.Entities.ContainsKey(entity)) _removed[count++] = entity;
        for (var i = 0; i < count; i++)
        {
            var entity = _removed[i]!;
            _states[entity].Row.Dispose(); _states.Remove(entity); _removed[i] = null;
        }
    }

    public void Seed(string entity, ShipRuntime runtime)
    {
        if (_states.Remove(entity, out var previous)) previous.Row.Dispose();
        _states.Add(entity, new(GameplaySnapshotCodec.Encode(entity, runtime), 0, 0));
    }

    public bool Apply(Array rows, GameWorld world, GameplayCatalogues definitions, Func<int, string?> resolve)
    {
        if (rows.Count > _ships.Length) return false;
        var committed = false;
        try
        {
            for (var i = 0; i < rows.Count; i++)
            {
                using var rowValue = rows[i];
                if (rowValue.VariantType != Variant.Type.Array) return false;
                using var row = rowValue.AsGodotArray();
                if (row.Count != 6) return false;
                using var configurationValue = row[3];
                using var effectsValue = row[4];
                using var resourcesValue = row[5];
                if (row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.Int ||
                    row[2].VariantType != Variant.Type.Int || configurationValue.VariantType != Variant.Type.Array ||
                    effectsValue.VariantType != Variant.Type.Array || resourcesValue.VariantType != Variant.Type.Array ||
                    row[0].AsInt64() is <= 0 or > int.MaxValue) return false;
                var entity = resolve(row[0].AsInt32());
                if (entity == null || !world.Entities.TryGetValue(entity, out var ship)) return false;
                for (var j = 0; j < i; j++) if (_ships[j] == ship) return false;
                _ships[i] = ship;
                var configurationEpoch = row[1].AsInt64(); var effectEpoch = row[2].AsInt64();
                using var config = configurationValue.AsGodotArray();
                using var effects = effectsValue.AsGodotArray();
                using var resources = resourcesValue.AsGodotArray();
                if (!GameplayDeltaState.TryReadResources(resources, out var update) || configurationEpoch <= 0 || effectEpoch <= 0 ||
                    configurationEpoch > update.Revision || effectEpoch > update.Revision ||
                    config.Count != 0 && config.Count != GameplayDeltaState.ConfigurationFields.Length ||
                    effects.Count != 0 && effects.Count != GameplayDeltaState.EffectFields.Length) return false;
                _resources[i] = update;
                _configurationEpochs[i] = configurationEpoch; _effectEpochs[i] = effectEpoch;
                if (update.Revision <= ship.Runtime.Revision) continue;
                if (update.Tick < ship.Runtime.AuthoritativeTick || update.ConfigurationRevision < ship.Runtime.ConfigurationRevision ||
                    !ship.Runtime.IsAlive && ship.Runtime.Health.Capacity > 0 && update.Health > 0) return false;
                _states.TryGetValue(entity, out var cache);
                if (cache != null && (configurationEpoch < cache.ConfigurationEpoch || effectEpoch < cache.EffectEpoch)) return false;
                if (cache == null && (config.Count == 0 || effects.Count == 0)) return false;
                if (cache != null && (cache.ConfigurationEpoch != 0 && configurationEpoch != cache.ConfigurationEpoch && config.Count == 0 ||
                    cache.EffectEpoch != 0 && effectEpoch != cache.EffectEpoch && effects.Count == 0)) return false;
                if (config.Count == 0 && effects.Count == 0)
                {
                    if (!ship.Runtime.ValidResources(update)) return false;
                    continue;
                }
                var full = _pending[i] ??= new Array();
                full.Resize(19);
                full[0] = entity;
                for (var j = 0; j < GameplayDeltaState.ConfigurationFields.Length; j++)
                {
                    var field = GameplayDeltaState.ConfigurationFields[j];
                    full[field] = config.Count != 0 ? config[j] : cache!.Row[field];
                }
                for (var j = 0; j < GameplayDeltaState.EffectFields.Length; j++)
                {
                    var field = GameplayDeltaState.EffectFields[j];
                    full[field] = effects.Count != 0 ? effects[j] : cache!.Row[field];
                }
                for (var j = 0; j < resources.Count; j++) full[GameplayDeltaState.ResourceFields[j]] = resources[j];
                var decoded = _full[i] ??= new GameplaySnapshotBuffer();
                if (!GameplaySnapshotCodec.TryDecodeRow(full, definitions, decoded)) return false;
                _changed[i] = true;
            }
            for (var i = 0; i < rows.Count; i++)
            {
                var ship = _ships[i]!;
                if (_changed[i])
                {
                    var entry = _full[i]!;
                    ship.Runtime.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
                        entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable,
                        entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive,
                        entry.HealthCapacity, entry.HealthCurrent, entry.Components);
                    if (_states.TryGetValue(ship.EntityId, out var previous))
                    {
                        (previous.Row, _pending[i]) = (_pending[i]!, previous.Row);
                        previous.ConfigurationEpoch = _configurationEpochs[i]; previous.EffectEpoch = _effectEpochs[i];
                    }
                    else
                    {
                        _states.Add(ship.EntityId, new(_pending[i]!, _configurationEpochs[i], _effectEpochs[i]));
                        _pending[i] = null;
                    }
                }
                else if (ship.Runtime.ConfirmResources(_resources[i]) && _states.TryGetValue(ship.EntityId, out var cache))
                {
                    cache.ConfigurationEpoch = _configurationEpochs[i];
                    cache.EffectEpoch = _effectEpochs[i];
                }
            }
            committed = true;
            return true;
        }
        finally
        {
            for (var i = 0; i < rows.Count; i++)
            {
                _changed[i] = false; _ships[i] = null;
                if (!committed) { _pending[i]?.Dispose(); _pending[i] = null; }
                _full[i]?.Conditions.Clear(); _full[i]?.Behaviours.Clear();
            }
        }
    }

    public void Dispose()
    {
        foreach (var state in _states.Values) state.Row.Dispose();
        _states.Clear();
        for (var i = 0; i < _pending.Length; i++)
        {
            _pending[i]?.Dispose(); _pending[i] = null; _full[i] = null;
        }
    }

    private sealed class Cache(Array row, long configurationEpoch, long effectEpoch)
    {
        public Array Row = row;
        public long ConfigurationEpoch = configurationEpoch;
        public long EffectEpoch = effectEpoch;
    }
}
