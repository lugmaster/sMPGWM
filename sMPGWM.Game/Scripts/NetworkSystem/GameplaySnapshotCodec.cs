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
        using var behaviours = new Godot.Collections.Array();
        foreach (var active in state.ActiveBehaviours)
        {
            using var row = new Godot.Collections.Array { active.DefinitionId, active.RuntimeId, active.Deadline };
            behaviours.Add(row);
        }
        using var conditions = new Godot.Collections.Array();
        foreach (var condition in state.Conditions) conditions.Add(condition);
        var s = state.Stats;
        var values = new double[] { s.Speed, s.AfterBurnerSpeed, s.RotationSpeed, s.Acceleration,
            s.AfterBurnerAcceleration, s.Mass, s.Armor, s.ShieldCapacity, s.RechargeTime, s.DownTime,
            s.EnergyProduction, s.EnergyConsumption, s.EnergyStorage, s.EngineEnergyConsumption };
        var systems = (s.HasEngine ? 1 : 0) | (s.HasHull ? 2 : 0) | (s.HasShieldGenerator ? 4 : 0) |
            (s.HasReactor ? 8 : 0) | (s.HasBattery ? 16 : 0);
        return new() { entity, state.Revision, state.Shield.Capacity, state.Shield.Current, behaviours,
            state.FactionId, conditions, values, systems, state.Energy.Current, state.ConfigurationRevision,
            state.AuthoritativeTick, state.ShieldDowntimeDeadline, state.EnergyDepleted, state.ShieldProtectionAvailable, state.AfterburnerActive,
            (int)state.Components.Presence, state.Health.Capacity, state.Health.Current };
    }

    internal static bool TryDecode(Godot.Collections.Array rows, GameplayCatalogues definitions, out GameplaySnapshotEntry[] entries)
    {
        entries = Array.Empty<GameplaySnapshotEntry>();
        if (rows.Count > 64) return false;
        var decoded = new GameplaySnapshotEntry[rows.Count];
        var entities = new HashSet<string>(StringComparer.Ordinal);
        var instances = new HashSet<long>();
        var buffer = new GameplaySnapshotBuffer();
        for (var i = 0; i < rows.Count; i++)
        {
            using var rowValue = rows[i];
            if (rowValue.VariantType != Variant.Type.Array) return false;
            using var row = rowValue.AsGodotArray();
            if (!TryDecodeRow(row, definitions, buffer) || !entities.Add(buffer.Entity)) return false;
            for (var j = 0; j < buffer.Behaviours.Count; j++)
                if (!instances.Add(buffer.Behaviours[j].RuntimeId)) return false;
            decoded[i] = buffer.ToEntry();
        }
        entries = decoded;
        return true;
    }

    internal static bool TryDecodeRow(Godot.Collections.Array row, GameplayCatalogues definitions, GameplaySnapshotBuffer decoded)
    {
        if (row.Count != 19) return false;
        using var entityValue = row[0];
        using var behavioursValue = row[4];
        using var conditionsValue = row[6];
        using var statsValue = row[7];
        if (entityValue.VariantType != Variant.Type.String || row[1].VariantType != Variant.Type.Int ||
            row[2].VariantType != Variant.Type.Float || row[3].VariantType != Variant.Type.Float ||
            behavioursValue.VariantType != Variant.Type.Array || row[5].VariantType != Variant.Type.Int ||
            conditionsValue.VariantType != Variant.Type.Array || statsValue.VariantType != Variant.Type.PackedFloat64Array ||
            row[8].VariantType != Variant.Type.Int || row[9].VariantType != Variant.Type.Float ||
            row[10].VariantType != Variant.Type.Int || row[11].VariantType != Variant.Type.Int ||
            row[12].VariantType != Variant.Type.Int || row[13].VariantType != Variant.Type.Bool ||
            row[14].VariantType != Variant.Type.Bool || row[15].VariantType != Variant.Type.Bool || row[16].VariantType != Variant.Type.Int ||
            row[17].VariantType != Variant.Type.Float || row[18].VariantType != Variant.Type.Float) return false;
        var entity = entityValue.AsString();
        var revision = row[1].AsInt64();
        var capacity = row[2].AsDouble(); var current = row[3].AsDouble();
        var faction = row[5].AsInt64();
        var statsValues = statsValue.AsFloat64Array();
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
        var healthCapacity = row[17].AsDouble();
        var healthCurrent = row[18].AsDouble();
        if (!double.IsFinite(healthCapacity) || healthCapacity < 0 || !double.IsFinite(healthCurrent) ||
            healthCurrent < 0 || healthCurrent > healthCapacity) return false;
        if (componentBits < 0 || componentBits > (long)ShipComponentKind.All) return false;
        var components = (ShipComponentKind)componentBits;
        if (!stats.IsValid || capacity != stats.ShieldCapacity || !double.IsFinite(energy) || energy < 0 || energy > stats.EnergyStorage ||
            (!stats.HasShieldGenerator && (capacity != 0 || current != 0)) ||
            (stats.HasShieldGenerator && !double.IsFinite(capacity * (1000d / 60 / stats.RechargeTime))) ||
            configurationRevision < 1 || tick < 0 || shieldDeadline < 0 ||
            afterburnerActive && (!stats.HasEngine || (components & ShipComponentKind.Movement) == 0) ||
            depleted != (stats.EnergyProduction < stats.GetEnergyConsumption(afterburnerActive) && energy == 0)) return false;
        if (faction < 0 || faction > int.MaxValue) return false;
        using var conditionRows = conditionsValue.AsGodotArray();
        if (conditionRows.Count > ShipRuntime.MaxConditions) return false;
        var conditions = decoded.Conditions;
        conditions.Clear();
        for (var j = 0; j < conditionRows.Count; j++)
        {
            using var conditionValue = conditionRows[j];
            if (conditionValue.VariantType != Variant.Type.String) return false;
            var id = conditionValue.AsString();
            if (id.Length > 64 || !definitions.Conditions.TryGetById(id, out _)) return false;
            for (var k = 0; k < j; k++)
                if (conditions[k] == id) return false;
            conditions.Add(id);
        }
        var hasDepletedCondition = false;
        foreach (var condition in conditions)
            if (condition == "energyDepleted") hasDepletedCondition = true;
        if (hasDepletedCondition != depleted) return false;
        var dead = false;
        foreach (var condition in conditions) if (condition == "dead") dead = true;
        if (healthCapacity > 0 && (dead != (healthCurrent == 0) || dead && afterburnerActive)) return false;
        if (protection != (!dead && (components & (ShipComponentKind.Power | ShipComponentKind.Shield)) ==
            (ShipComponentKind.Power | ShipComponentKind.Shield) && stats.HasShieldGenerator && capacity > 0 &&
            current > 0 && !depleted && shieldDeadline <= tick)) return false;
        if (!Guid.TryParseExact(entity, "N", out _) || revision < 1 ||
            !double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current) return false;
        using var values = behavioursValue.AsGodotArray();
        if (values.Count > ShipRuntime.MaxBehaviours || healthCapacity > 0 && dead && values.Count != 0) return false;
        var behaviours = decoded.Behaviours;
        behaviours.Clear();
        for (var j = 0; j < values.Count; j++)
        {
            using var behaviourValue = values[j];
            if (behaviourValue.VariantType != Variant.Type.Array) return false;
            using var value = behaviourValue.AsGodotArray();
            if (value.Count != 3 || value[0].VariantType != Variant.Type.String ||
                value[1].VariantType != Variant.Type.Int || value[2].VariantType != Variant.Type.Int) return false;
            var id = value[0].AsString(); var instance = value[1].AsInt64(); var deadline = value[2].AsInt64();
            if (id.Length > 64 || !definitions.Behaviours.TryGetById(id, out _) ||
                instance <= 0 || deadline <= 0) return false;
            for (var k = 0; k < behaviours.Count; k++)
                if (behaviours[k].DefinitionId == id || behaviours[k].RuntimeId == instance) return false;
            behaviours.Add(new(id, instance, deadline));
        }
        decoded.Entity = entity; decoded.Revision = revision; decoded.Capacity = capacity; decoded.Current = current;
        decoded.FactionId = (int)faction; decoded.Stats = stats; decoded.EnergyCurrent = energy;
        decoded.EnergyDepleted = depleted; decoded.ShieldProtectionAvailable = protection;
        decoded.ShieldDowntimeDeadline = shieldDeadline; decoded.AuthoritativeTick = tick;
        decoded.ConfigurationRevision = configurationRevision; decoded.AfterburnerActive = afterburnerActive;
        decoded.Components = components; decoded.HealthCapacity = healthCapacity; decoded.HealthCurrent = healthCurrent;
        return true;
    }
}
