using System;
using System.Collections.Generic;
using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.GameplayDataSystem;

namespace sMPGWM.Scripts.NetworkSystem;

internal static class GameplaySnapshotCodec
{
    internal static Godot.Collections.Array Encode(string entity, ShipGameplayState state)
    {
        var behaviours = new Godot.Collections.Array();
        foreach (var active in state.ActiveBehaviours)
            behaviours.Add(new Godot.Collections.Array { active.DefinitionId, active.RuntimeId, active.Deadline });
        var conditions = new Godot.Collections.Array();
        foreach (var condition in state.Conditions) conditions.Add(condition);
        return new() { entity, state.Revision, state.Shield.Capacity, state.Shield.Current, behaviours, state.FactionId, conditions };
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
            if (row.Count != 7 || row[0].VariantType != Variant.Type.String || row[1].VariantType != Variant.Type.Int ||
                row[2].VariantType != Variant.Type.Float || row[3].VariantType != Variant.Type.Float ||
                row[4].VariantType != Variant.Type.Array || row[5].VariantType != Variant.Type.Int ||
                row[6].VariantType != Variant.Type.Array) return false;
            var entity = row[0].AsString();
            var revision = row[1].AsInt64();
            var capacity = row[2].AsDouble(); var current = row[3].AsDouble();
            var faction = row[5].AsInt64();
            if (faction < 0 || faction > int.MaxValue) return false;
            var conditionRows = row[6].AsGodotArray();
            if (conditionRows.Count > ShipGameplayState.MaxConditions) return false;
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
            if (!Guid.TryParseExact(entity, "N", out _) || !entities.Add(entity) || revision < 1 ||
                !double.IsFinite(capacity) || !double.IsFinite(current) || current < 0 || capacity < current) return false;
            var values = row[4].AsGodotArray();
            if (values.Count > ShipGameplayState.MaxBehaviours) return false;
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
            decoded[i] = new(entity, revision, capacity, current, behaviours, (int)faction, conditions);
        }
        entries = decoded;
        return true;
    }
}
