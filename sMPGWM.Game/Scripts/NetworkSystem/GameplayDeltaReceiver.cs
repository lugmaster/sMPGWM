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
    private readonly GameplaySnapshotEntry?[] _full = new GameplaySnapshotEntry?[WorldTopology.MaxEntities];
    private readonly Cache?[] _pending = new Cache?[WorldTopology.MaxEntities];
    private readonly Array _validation = new();
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
        try
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].VariantType != Variant.Type.Array) return false;
                var row = rows[i].AsGodotArray();
                if (row.Count != 6 || row[0].VariantType != Variant.Type.Int || row[1].VariantType != Variant.Type.Int ||
                    row[2].VariantType != Variant.Type.Int || row[3].VariantType != Variant.Type.Array ||
                    row[4].VariantType != Variant.Type.Array || row[5].VariantType != Variant.Type.Array ||
                    row[0].AsInt64() is <= 0 or > int.MaxValue) return false;
                var entity = resolve(row[0].AsInt32());
                if (entity == null || !world.Entities.TryGetValue(entity, out var ship)) return false;
                for (var j = 0; j < i; j++) if (_ships[j] == ship) return false;
                _ships[i] = ship;
                var configurationEpoch = row[1].AsInt64(); var effectEpoch = row[2].AsInt64();
                var config = row[3].AsGodotArray(); var effects = row[4].AsGodotArray(); var resources = row[5].AsGodotArray();
                if (!GameplayDeltaState.TryReadResources(resources, out var update) || configurationEpoch <= 0 || effectEpoch <= 0 ||
                    configurationEpoch > update.Revision || effectEpoch > update.Revision ||
                    config.Count != 0 && config.Count != GameplayDeltaState.ConfigurationFields.Length ||
                    effects.Count != 0 && effects.Count != GameplayDeltaState.EffectFields.Length) return false;
                _resources[i] = update;
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
                var full = cache?.Row.Duplicate() ?? new Array();
                full.Resize(19); full[0] = entity;
                for (var j = 0; j < config.Count; j++) full[GameplayDeltaState.ConfigurationFields[j]] = config[j];
                for (var j = 0; j < effects.Count; j++) full[GameplayDeltaState.EffectFields[j]] = effects[j];
                for (var j = 0; j < resources.Count; j++) full[GameplayDeltaState.ResourceFields[j]] = resources[j];
                _pending[i] = new(full, configurationEpoch, effectEpoch);
                _validation.Clear(); _validation.Add(full);
                if (!GameplaySnapshotCodec.TryDecode(_validation, definitions, out var decoded)) return false;
                _full[i] = decoded[0];
            }
            for (var i = 0; i < rows.Count; i++)
            {
                var ship = _ships[i]!;
                if (_full[i] is { } entry)
                {
                    ship.Runtime.Confirm(entry.Revision, entry.Capacity, entry.Current, entry.Behaviours, entry.FactionId, entry.Conditions,
                        entry.Stats, entry.EnergyCurrent, entry.EnergyDepleted, entry.ShieldProtectionAvailable,
                        entry.ShieldDowntimeDeadline, entry.AuthoritativeTick, entry.ConfigurationRevision, entry.AfterburnerActive,
                        entry.HealthCapacity, entry.HealthCurrent, entry.Components);
                    if (_states.Remove(ship.EntityId, out var previous)) previous.Row.Dispose();
                    _states.Add(ship.EntityId, _pending[i]!); _pending[i] = null;
                }
                else if (ship.Runtime.ConfirmResources(_resources[i]) && _states.TryGetValue(ship.EntityId, out var cache))
                {
                    var row = rows[i].AsGodotArray();
                    cache.ConfigurationEpoch = row[1].AsInt64();
                    cache.EffectEpoch = row[2].AsInt64();
                }
            }
            return true;
        }
        finally
        {
            for (var i = 0; i < rows.Count; i++) { _pending[i]?.Row.Dispose(); _pending[i] = null; _full[i] = null; _ships[i] = null; }
            _validation.Clear();
        }
    }

    public void Dispose()
    {
        foreach (var state in _states.Values) state.Row.Dispose();
        _states.Clear(); _validation.Dispose();
    }

    private sealed class Cache(Array row, long configurationEpoch, long effectEpoch)
    {
        public readonly Array Row = row;
        public long ConfigurationEpoch = configurationEpoch;
        public long EffectEpoch = effectEpoch;
    }
}
