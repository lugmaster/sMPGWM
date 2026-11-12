using sMPGWM.Scripts.UtilitySystem.UiComponents;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.CombatSystem;

namespace sMPGWM.Scripts.GameplayDataSystem;

internal static class GameplayDefinitionParser
{
    public static string Text(JToken row, string field) => (string?)row[field] ??
        throw new ArgumentException($"{row.Path}.{field}: required string.");
    public static double Number(JToken row, string field)
    {
        var value = (double)row[field]!;
        if (!double.IsFinite(value)) throw new ArgumentException($"{row.Path}.{field}: expected finite number.");
        return value;
    }
    public static double? OptionalNumber(JToken row, string field) => row[field] == null ? null : Number(row, field);
    public static T EnumValue<T>(JToken row, string field) where T : struct, Enum =>
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

    public static AbilityDefinition Ability(JToken r) => new(
        Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), r["Icon"] is { Type: JTokenType.String } icon ? Icons.GetReference((string)icon!) : null,
        Text(r, "TargetingId"), Text(r, "EffectId"), EnumValue<ActivationKind>(r, "Activation"),
        Number(r, "CooldownMs"), List(r["Costs"], c => new AbilityCost(Text(c, "StatId"), Number(c, "Amount"))),
        EnumValue<AbilityAvailability>(r, "Availability"), Strings(r, "BlockedByConditions"));

    private static ModifierDefinition Modifier(JToken r) => new(Text(r, "StatId"),
        EnumValue<ModifierValueKind>(r, "ValueKind"), EnumValue<ModifierOperation>(r, "Operation"), Number(r, "Value"));

    public static BehaviourDefinition Behaviour(JToken r)
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

    public static EffectDefinition Effect(JToken r)
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
    public static bool IsResource(string id) => id is "health" or "shield" or "energy_storage" or "oxygen" or "food" or "water";
}
