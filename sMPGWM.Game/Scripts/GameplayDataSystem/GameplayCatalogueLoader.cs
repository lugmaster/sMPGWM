using sMPGWM.Scripts.UtilitySystem.UiComponents;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;
using sMPGWM.Scripts.ConditionSystem;
using sMPGWM.Scripts.ProjectileSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

public static class GameplayCatalogueLoader
{
    public static IReadOnlyList<CatalogueSource> Sources { get; } = Array.AsReadOnly(new[]
    {
        new CatalogueSource("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions"),
        new CatalogueSource("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions"),
        new CatalogueSource("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions"),
        new CatalogueSource("TargetingDefinitions.yml", "TargetingSchema.yml", "TargetingDefinitions"),
        new CatalogueSource("BehaviourClassificationDefinitions.yml", "BehaviourClassificationSchema.yml", "BehaviourClassifications"),
        new CatalogueSource("Attributes.yml", "AttributeSchema.yml", "Stats"),
        new CatalogueSource("Conditions.yml", "ConditionSchema.yml", "Conditions"),
        new CatalogueSource("ProjectileDefinitions.yml", "ProjectileSchema.yml", "ProjectileDefinitions"),
        new CatalogueSource("DamageSources.yml", "DamageSourceSchema.yml", "DamageSources"),
    });

    // The reader validates each document against its schema before returning it.
    public static GameplayCatalogues Load(Func<CatalogueSource, JObject> read)
    {
        var documents = new JObject[Sources.Count];
        for (var i = 0; i < documents.Length; i++) documents[i] = read(Sources[i]);
        return new GameplayCatalogues(
            new AbilityRegistry(Parse(documents[0], Sources[0], Ability)),
            new EffectRegistry(Parse(documents[1], Sources[1], Effect)),
            new BehaviourRegistry(Parse(documents[2], Sources[2], Behaviour)),
            new TargetingRegistry(Parse(documents[3], Sources[3], r => new TargetingDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
                EnumValue<TargetRelation>(r, "Relation"), EnumValue<TargetLifeState>(r, "LifeState"),
                (bool)r["MechanicalOnly"]!, OptionalNumber(r, "MaxRange"), OptionalNumber(r, "Radius")))),
            new BehaviourClassificationRegistry(Parse(documents[4], Sources[4], r => new BehaviourClassificationDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")))),
            new StatRegistry(Parse(documents[5], Sources[5], r => new StatDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), IsResource(Text(r, "Id"))))),
            new ConditionRegistry(Parse(documents[6], Sources[6], r => new ConditionDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")))),
            new ProjectileRegistry(Parse(documents[7], Sources[7], r => new ProjectileDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
                Number(r, "Speed"), Number(r, "MaxLifetimeMs"),
                EnumValue<ProjectileCollision>(r, "Collision"), EnumValue<ProjectileExpiration>(r, "ExpireWithoutImpact")))),
            new DamageSourceRegistry(Parse(documents[8], Sources[8], r => new DamageSourceDefinition(
                Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")))));
    }

    private static T[] Parse<T>(JObject document, CatalogueSource source, Func<JToken, T> parse)
    {
        var rows = (JArray)document[source.RootKey]!;
        var result = new T[rows.Count];
        for (var i = 0; i < result.Length; i++)
        {
            try { result[i] = parse(rows[i]); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
            { throw new ArgumentException($"{source.FileName}: definition '{rows[i]["Id"]}': {error.Message}", error); }
        }
        return result;
    }

    private static string Text(JToken row, string field) => (string?)row[field] ??
        throw new ArgumentException($"{row.Path}.{field}: required string.");
    private static double Number(JToken row, string field)
    {
        var value = (double)row[field]!;
        if (!double.IsFinite(value)) throw new ArgumentException($"{row.Path}.{field}: expected finite number.");
        return value;
    }
    private static double? OptionalNumber(JToken row, string field) => row[field] == null ? null : Number(row, field);
    private static T EnumValue<T>(JToken row, string field) where T : struct, Enum =>
        Enum.TryParse<T>(Text(row, field), false, out var value) && Enum.IsDefined(value) ? value :
            throw new ArgumentException($"{row.Path}.{field}: unsupported value '{row[field]}'.");
    private static T? OptionalEnum<T>(JToken row, string field) where T : struct, Enum =>
        row[field] == null ? null : EnumValue<T>(row, field);
    private static IReadOnlyList<T> List<T>(JToken? array, Func<JToken, T> parse)
    {
        if (array == null) return Array.Empty<T>();
        var items = (JArray)array;
        var result = new T[items.Count];
        for (var i = 0; i < result.Length; i++) result[i] = parse(items[i]);
        return Array.AsReadOnly(result);
    }
    private static IReadOnlyList<string> Strings(JToken row, string field) => List(row[field], value => (string)value!);
    private static IReadOnlyList<T> Enums<T>(JToken row, string field) where T : struct, Enum =>
        List(row[field], value => Enum.TryParse<T>((string)value!, false, out var parsed) && Enum.IsDefined(parsed)
            ? parsed : throw new ArgumentException($"{value.Path}: unsupported value '{value}'."));

    private static AbilityDefinition Ability(JToken r) => new(
        Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), r["Icon"] is { Type: JTokenType.String } icon ? Icons.GetReference((string)icon!) : null,
        Text(r, "TargetingId"), Text(r, "EffectId"), EnumValue<ActivationKind>(r, "Activation"),
        Number(r, "CooldownMs"), List(r["Costs"], c => new AbilityCost(Text(c, "StatId"), Number(c, "Amount"))),
        EnumValue<AbilityAvailability>(r, "Availability"), Strings(r, "BlockedByConditions"));

    private static ModifierDefinition Modifier(JToken r) => new(Text(r, "StatId"),
        EnumValue<ModifierValueKind>(r, "ValueKind"), EnumValue<ModifierOperation>(r, "Operation"), Number(r, "Value"));

    private static BehaviourDefinition Behaviour(JToken r)
    {
        var lifetime = r["Lifetime"]!;
        var stacking = r["Stacking"]!;
        var removal = r["OnRemove"];
        var lethal = r["LethalProtection"];
        var periodic = r["PeriodicAccumulator"];
        return new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), r["Icon"] is { Type: JTokenType.String } icon ? Icons.GetReference((string)icon!) : null,
            EnumValue<BehaviourDisposition>(r, "Disposition"),
            new(EnumValue<BehaviourLifetimeKind>(lifetime, "Kind"), OptionalNumber(lifetime, "DurationMs")),
            new(EnumValue<StackingScope>(stacking, "Scope"), EnumValue<StackingPolicy>(stacking, "Policy"),
                (int?)stacking["MaxStacks"], OptionalEnum<StackOverflowPolicy>(stacking, "Overflow")),
            (bool)r["OrdinaryRemoval"]!, Strings(r, "ClassificationIds"), List(r["Modifiers"], Modifier),
            Strings(r, "GrantedConditions"), Strings(r, "GrantedConditionsAfterFirstTick"), Strings(r, "BlockedByConditions"),
            OptionalEnum<BlockedBehaviourPolicy>(r, "BlockedPolicy"), Enums<ExecutionPhase>(r, "Interrupts"),
            Enums<DisabledCapability>(r, "DisabledCapabilities"), (string?)r["OnApplyEffectId"], (string?)r["OnExpireEffectId"],
            removal == null ? null : new(Enums<BehaviourRemovalReason>(removal, "ExceptReasons"), Text(removal, "EffectId")),
            lethal == null ? null : new(Number(lethal, "HealthFraction"), (bool)lethal["ConsumeOnSuccess"]!),
            periodic == null ? null : new(Number(periodic, "IntervalMs"), EnumValue<PeriodicFirstTick>(periodic, "FirstTick"),
                (bool)periodic["TickAtExpiration"]!, Modifier(periodic["Modifier"]!),
                Strings(periodic, "BlockedByConditions"), EnumValue<BlockedTickPolicy>(periodic, "BlockedTickPolicy")));
    }

    private static EffectDefinition Effect(JToken r)
    {
        var id = Text(r, "Id"); var name = Text(r, "DisplayName"); var description = Text(r, "Description");
        switch (Text(r, "Kind"))
        {
            case "ApplyBehaviour": return new ApplyBehaviourEffect(id, name, description, Text(r, "BehaviourId"));
            case "Revive": return new ReviveEffect(id, name, description, Number(r, "Health"));
            case "RestoreResource":
                var amount = r["Amount"]!;
                return new RestoreResourceEffect(id, name, description, Text(r, "StatId"),
                    new(EnumValue<ResourceAmountKind>(amount, "Kind"), Number(amount, "Value")), Strings(r, "BlockedByConditions"));
            case "RemoveBehaviours":
                var selector = r["Selector"]!;
                return new RemoveBehavioursEffect(id, name, description,
                    new(Strings(selector, "ClassificationIds"), OptionalEnum<BehaviourDisposition>(selector, "Disposition"),
                        Strings(selector, "ModifiedStatIds")), (int?)r["MaxCount"], EnumValue<BehaviourRemovalOrder>(r, "Order"));
            case "Sequence": return new SequenceEffect(id, name, description, Strings(r, "EffectIds"), (bool)r["ContinueOnFailure"]!);
            case "DealDamage": return new DealDamageEffect(id, name, description, Text(r, "DamageSourceId"), Number(r, "Amount"));
            case "SearchArea": return new SearchAreaEffect(id, name, description, Text(r, "TargetingId"), Text(r, "EffectId"));
            case "LaunchProjectile": return new LaunchProjectileEffect(id, name, description, Text(r, "ProjectileId"), Text(r, "ImpactEffectId"));
            default: throw new ArgumentException($"Kind: unknown effect '{r["Kind"]}'.");
        }
    }

    // Preserve the existing authoring contract until stat categories and legacy aliases are migrated.
    private static bool IsResource(string id) => id is "health" or "shield" or "energy_storage" or "oxygen" or "food" or "water";
}
