using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class GameplaySnapshotCodec
{
    internal static Godot.Collections.Array Encode(string entity, ShipRuntime state)
    {
        var behaviours = new Godot.Collections.Array();
        foreach (var active in state.ActiveBehaviours)
            behaviours.Add(new Godot.Collections.Array { active.DefinitionId, active.RuntimeId, active.Deadline });
        var conditions = new Godot.Collections.Array();
        foreach (var condition in state.Conditions) conditions.Add(condition);
        var s = state.Stats;
        var values = new double[] { s.Speed, s.AfterBurnerSpeed, s.RotationSpeed, s.Acceleration,
            s.AfterBurnerAcceleration, s.Mass, s.Armor, s.ShieldCapacity, s.RechargeTime, s.DownTime,
            s.EnergyProduction, s.EnergyConsumption, s.EnergyStorage, s.EngineEnergyConsumption };
        var systems = (s.HasEngine ? 1 : 0) | (s.HasHull ? 2 : 0) | (s.HasShieldGenerator ? 4 : 0) |
            (s.HasReactor ? 8 : 0) | (s.HasBattery ? 16 : 0);
        return new() { entity, state.Revision, state.Shield.Capacity, state.Shield.Current, behaviours,
            state.FactionId, conditions, values, systems, state.Energy.Current, state.ConfigurationRevision,
            state.AuthoritativeTick, state.ShieldDowntimeDeadline, state.EnergyDepleted, state.ShieldProtectionAvailable, state.AfterburnerActive, (int)state.Components.Presence };
    }

    internal static bool TryDecode(Godot.Collections.Array rows, GameplayCatalogues definitions, out GameplaySnapshotEntry[] entries)
    {
        entries = Array.Empty<GameplaySnapshotEntry>();
        if (rows.Count > 64) return false;
        var decoded = new GameplaySnapshotEntry[rows.Count];
        var entities = new HashSet<string>(StringComparer.Ordinal);
        var instances = new HashSet<long>();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].VariantType != Variant.Type.Array) return false;
            var row = rows[i].AsGodotArray();
            if (row.Count != 17 || row[0].VariantType != Variant.Type.String || row[1].VariantType != Variant.Type.Int ||
                row[2].VariantType != Variant.Type.Float || row[3].VariantType != Variant.Type.Float ||
                row[4].VariantType != Variant.Type.Array || row[5].VariantType != Variant.Type.Int ||
                row[6].VariantType != Variant.Type.Array || row[7].VariantType != Variant.Type.PackedFloat64Array ||
                row[8].VariantType != Variant.Type.Int || row[9].VariantType != Variant.Type.Float ||
                row[10].VariantType != Variant.Type.Int || row[11].VariantType != Variant.Type.Int ||
                row[12].VariantType != Variant.Type.Int || row[13].VariantType != Variant.Type.Bool ||
                row[14].VariantType != Variant.Type.Bool || row[15].VariantType != Variant.Type.Bool || row[16].VariantType != Variant.Type.Int) return false;
            var entity = row[0].AsString();
            var revision = row[1].AsInt64();
            var capacity = row[2].AsDouble(); var current = row[3].AsDouble();
            var faction = row[5].AsInt64();
            var statsValues = row[7].AsFloat64Array();
            var systems = row[8].AsInt64();
            if (statsValues.Length != 14 || systems < 0 || systems > 31) return false;
            for (var j = 0; j < statsValues.Length; j++)
                if (!double.IsFinite(statsValues[j]) || statsValues[j] < 0) return false;
            var stats = new ResolvedShipStats((float)statsValues[0], (float)statsValues[1], (float)statsValues[2],
                (float)statsValues[3], (float)statsValues[4], statsValues[5], statsValues[6], statsValues[7],
                statsValues[8], statsValues[9], statsValues[10], statsValues[11], statsValues[12],
                (systems & 1) != 0, (systems & 2) != 0, (systems & 4) != 0, (systems & 8) != 0, (systems & 16) != 0)
                { EngineEnergyConsumption = statsValues[13] };
            var energy = row[9].AsDouble();
            var configurationRevision = row[10].AsInt64();
            var tick = row[11].AsInt64();
            var shieldDeadline = row[12].AsInt64();
            var depleted = row[13].AsBool();
            var protection = row[14].AsBool();
            var afterburnerActive = row[15].AsBool();
            var componentBits = row[16].AsInt64();
            if (componentBits < 0 || componentBits > (long)ShipComponentKind.All) return false;
            var components = (ShipComponentKind)componentBits;
            if (!stats.IsValid || capacity != stats.ShieldCapacity || !double.IsFinite(energy) || energy < 0 || energy > stats.EnergyStorage ||
                (!stats.HasShieldGenerator && (capacity != 0 || current != 0)) ||
                (stats.HasShieldGenerator && !double.IsFinite(capacity * (1000d / 60 / stats.RechargeTime))) ||
                configurationRevision < 1 || tick < 0 || shieldDeadline < 0 ||
                afterburnerActive && (!stats.HasEngine || (components & ShipComponentKind.Movement) == 0) ||
                depleted != (stats.EnergyProduction < stats.GetEnergyConsumption(afterburnerActive) && energy == 0) ||
                protection != ((components & (ShipComponentKind.Power | ShipComponentKind.Shield)) == (ShipComponentKind.Power | ShipComponentKind.Shield) && stats.HasShieldGenerator && capacity > 0 && current > 0 && !depleted && shieldDeadline <= tick)) return false;
            if (faction < 0 || faction > int.MaxValue) return false;
            var conditionRows = row[6].AsGodotArray();
            if (conditionRows.Count > ShipRuntime.MaxConditions) return false;
            var conditions = new string[conditionRows.Count];
            for (var j = 0; j < conditions.Length; j++)
            {
                if (conditionRows[j].VariantType != Variant.Type.String) return false;
                var id = conditionRows[j].AsString();
                if (id.Length > 64 || !definitions.Conditions.TryGetById(id, out _)) return false;
                for (var k = 0; k < j; k++)
                    if (conditions[k] == id) return false;
                conditions[j] = id;
            }
            var hasDepletedCondition = false;
            foreach (var condition in conditions)
                if (condition == "energyDepleted") hasDepletedCondition = true;
            if (hasDepletedCondition != depleted) return false;
            if (!Guid.TryParseExact(entity, "N", out _) || !entities.Add(entity) || revision < 1 ||
                !double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current) return false;
            var values = row[4].AsGodotArray();
            if (values.Count > ShipRuntime.MaxBehaviours) return false;
            var behaviours = new BehaviourInstance[values.Count];
            var definitionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var j = 0; j < values.Count; j++)
            {
                if (values[j].VariantType != Variant.Type.Array) return false;
                var value = values[j].AsGodotArray();
                if (value.Count != 3 || value[0].VariantType != Variant.Type.String ||
                    value[1].VariantType != Variant.Type.Int || value[2].VariantType != Variant.Type.Int) return false;
                var id = value[0].AsString(); var instance = value[1].AsInt64(); var deadline = value[2].AsInt64();
                if (id.Length > 64 || !definitionIds.Add(id) || !definitions.Behaviours.TryGetById(id, out _) ||
                    instance <= 0 || deadline <= 0 || !instances.Add(instance)) return false;
                behaviours[j] = new(id, instance, deadline);
            }
            decoded[i] = new(entity, revision, capacity, current, behaviours, (int)faction, conditions,
                stats, energy, depleted, protection, shieldDeadline, tick, configurationRevision)
                { AfterburnerActive = afterburnerActive, Components = components };
        }
        entries = decoded;
        return true;
    }
}
