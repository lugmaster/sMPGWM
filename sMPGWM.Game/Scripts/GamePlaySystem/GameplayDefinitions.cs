using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.GamePlaySystem;

public static class GameplayDefinitions
{
    private static readonly Lazy<ShieldBoostDefinition> Definition = new(Load);
    public static ShieldBoostDefinition Boost => Definition.Value;
    private static Dictionary<string, JToken> Catalogue(string file, string schema, string key)
    {
        var document = FileLoader.LoadYaml("user://config/gameplay/" + file,
            "res://assets/game_data/" + file, "res://assets/game_data/" + schema);
        var result = new Dictionary<string, JToken>(StringComparer.Ordinal);
        foreach (var row in document[key]!)
            if (!result.TryAdd((string)row["Id"]!, row)) throw new ArgumentException($"Duplicate ID in {file}: {row["Id"]}");
        return result;
    }
    private static ShieldBoostDefinition Load()
    {
        var abilities = Catalogue("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions");
        var effects = Catalogue("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions");
        var behaviours = Catalogue("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions");
        var targeting = Catalogue("TargetingDefinitions.yml", "TargetingSchema.yml", "TargetingDefinitions");
        var stats = Catalogue("Attributes.yml", "AttributeSchema.yml", "Stats");
        var conditions = Catalogue("Conditions.yml", "ConditionSchema.yml", "Conditions");
        if (!stats.ContainsKey("shield")) throw new ArgumentException("Missing shield resource definition.");
        foreach (var item in Items.ItemRegistry.GetAll())
            foreach (var ability in item.AbilityIds)
                if (!abilities.ContainsKey(ability)) throw new ArgumentException($"Item '{item.Id}' references missing ability '{ability}'.");
        var a = abilities["shieldCapacityBoost"];
        foreach (var condition in a["BlockedByConditions"]!)
            if (!conditions.ContainsKey((string)condition!)) throw new ArgumentException("Missing blocking condition.");
        var e = effects[(string)a["EffectId"]!];
        var b = behaviours[(string)e["BehaviourId"]!];
        var t = targeting[(string)a["TargetingId"]!];
        if ((string?)a["Availability"] != "ExampleLoadout" || (string?)a["Activation"] != "Instant" || (int)a["CooldownMs"]! != 0 || a["Costs"]!.HasValues ||
            (string?)e["Kind"] != "ApplyBehaviour" || (string?)b["Lifetime"]!["Kind"] != "Timed" ||
            (string?)b["Stacking"]!["Policy"] != "RefreshDuration" || (string?)b["Stacking"]!["Scope"] != "PerTargetDefinition" ||
            (bool)t["MechanicalOnly"]! || t["Radius"] != null || (string?)t["Relation"] != "SelfOrAlly" || (string?)t["LifeState"] != "Alive" ||
            b["OnApplyEffectId"] != null || b["OnExpireEffectId"] != null || b["OnRemove"] != null ||
            b["PeriodicAccumulator"] != null || b["GrantedConditions"] != null ||
            b["GrantedConditionsAfterFirstTick"] != null || b["BlockedByConditions"] != null ||
            b["Interrupts"] != null || b["DisabledCapabilities"] != null || b["LethalProtection"] != null)
            throw new ArgumentException("Unsupported Shield Capacity Boost dependency combination.");
        var flat = 0d; var fraction = 0d;
        foreach (var m in b["Modifiers"]!)
        {
            var value = (double)m["Value"]!;
            if (!double.IsFinite(value) || (string?)m["StatId"] != "shield" || (string?)m["ValueKind"] != "Maximum")
                throw new ArgumentException("Invalid shield capacity modifier.");
            switch ((string?)m["Operation"])
            {
                case "AddFlat": flat += value; break;
                case "AddBaselineFraction": fraction += value; break;
                default: throw new ArgumentException("Unsupported modifier operation.");
            }
        }
        if (!double.IsFinite(flat) || !double.IsFinite(fraction)) throw new ArgumentException("Modifier sum overflow.");
        foreach (var ship in ShipSystem.ShipRegistry.GetAll())
        {
            var pool = new ResourcePool(ship.ShieldCapacity, ship.ShieldCurrent);
            pool.SetContribution(1, flat, fraction);
        }
        var duration = (double)b["Lifetime"]!["DurationMs"]!;
        var range = (float)t["MaxRange"]!;
        if (!double.IsFinite(duration) || duration <= 0 || duration > 3600000 || !float.IsFinite(range) || range < 0)
            throw new ArgumentException("Invalid boost duration or range.");
        return new((string)a["Id"]!, (string)a["DisplayName"]!, (string)a["Icon"]!, (string)b["Id"]!,
            (long)Math.Ceiling(duration * 60 / 1000), flat, fraction, range);
    }
}
