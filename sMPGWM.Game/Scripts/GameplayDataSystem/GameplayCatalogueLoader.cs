using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
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
    private readonly record struct Catalogue<T>(CatalogueSource Source, Func<JToken, T> ParseDefinition);

    private static T[] Load<T>(Catalogue<T> catalogue, Func<CatalogueSource, JObject> read) =>
        Parse(read(catalogue.Source), catalogue.Source, catalogue.ParseDefinition);

    private static readonly Catalogue<AbilityDefinition> Abilities = new(
        new("AbilityDefinitions.yml", "AbilitySchema.yml", "AbilityDefinitions"), Ability);
    private static readonly Catalogue<EffectDefinition> Effects = new(
        new("EffectDefinitions.yml", "EffectDefinitionSchema.yml", "EffectDefinitions"), Effect);
    private static readonly Catalogue<BehaviourDefinition> Behaviours = new(
        new("BehaviourDefinitions.yml", "BehaviourSchema.yml", "BehaviourDefinitions"), Behaviour);
    private static readonly Catalogue<TargetingDefinition> Targeting = new(
        new("TargetingDefinitions.yml", "TargetingSchema.yml", "TargetingDefinitions"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
            EnumValue<TargetRelation>(r, "Relation"), EnumValue<TargetLifeState>(r, "LifeState"),
            (bool)r["MechanicalOnly"]!, OptionalNumber(r, "MaxRange"), OptionalNumber(r, "Radius")));
    private static readonly Catalogue<BehaviourClassificationDefinition> Classifications = new(
        new("BehaviourClassificationDefinitions.yml", "BehaviourClassificationSchema.yml", "BehaviourClassifications"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));
    private static readonly Catalogue<StatDefinition> Stats = new(
        new("Attributes.yml", "AttributeSchema.yml", "Stats"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"), IsResource(Text(r, "Id"))));
    private static readonly Catalogue<ConditionDefinition> Conditions = new(
        new("Conditions.yml", "ConditionSchema.yml", "Conditions"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));
    private static readonly Catalogue<ProjectileDefinition> Projectiles = new(
        new("ProjectileDefinitions.yml", "ProjectileSchema.yml", "ProjectileDefinitions"), r => new(
            Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description"),
            Number(r, "Speed"), Number(r, "MaxLifetimeMs"),
            EnumValue<ProjectileCollision>(r, "Collision"), EnumValue<ProjectileExpiration>(r, "ExpireWithoutImpact")));
    private static readonly Catalogue<DamageSourceDefinition> DamageSources = new(
        new("DamageSources.yml", "DamageSourceSchema.yml", "DamageSources"),
        r => new(Text(r, "Id"), Text(r, "DisplayName"), Text(r, "Description")));

    public static IReadOnlyList<CatalogueSource> Sources { get; } = Array.AsReadOnly(new[]
    {
        Abilities.Source, Effects.Source, Behaviours.Source, Targeting.Source, Classifications.Source,
        Stats.Source, Conditions.Source, Projectiles.Source, DamageSources.Source
    });

    // The reader validates each document against its schema before returning it.
    public static GameplayCatalogues Load(Func<CatalogueSource, JObject> read)
    {
        return new GameplayCatalogues(
            new AbilityRegistry(Load(Abilities, read)),
            new EffectRegistry(Load(Effects, read)),
            new BehaviourRegistry(Load(Behaviours, read)),
            new TargetingRegistry(Load(Targeting, read)),
            new BehaviourClassificationRegistry(Load(Classifications, read)),
            new StatRegistry(Load(Stats, read)),
            new ConditionRegistry(Load(Conditions, read)),
            new ProjectileRegistry(Load(Projectiles, read)),
            new DamageSourceRegistry(Load(DamageSources, read)));
    }

    private static readonly CatalogueSource ItemCategorySource =
        new("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml", "Categories");
    private static readonly CatalogueSource ItemSource =
        new("ItemDefinitions.yml", "ItemSchema.yml", "Items");

    public static LoadResult<ShipDefinitionsDocument> LoadShipDefinitions(
        JObject yaml, string sourcePath, string schemaPath) =>
        DefinitionDeserializer.Deserialize<ShipDefinitionsDocument>(yaml, sourcePath, schemaPath);

    public static Dictionary<string, ItemCategoryDefinition> LoadItemCategoryDefinitions(
        JObject yaml, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemCategoryDefinition>(StringComparer.Ordinal);
        foreach (var definition in Parse(yaml, ItemCategorySource, row =>
        {
            var (canStack, limit) = ResolveStacking(row, false, 1);
            var definition = new ItemCategoryDefinition((string)row["Id"]!, (string)row["DisplayName"]!,
                resolveIcon((string?)row["Icon"] ?? "Software.WarningSignCircleQuestionMarkHelp"))
            {
                Values = Array.AsReadOnly(((string?)row["Values"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)),
                Description = (string?)row["Description"] ?? "",
                CanStack = canStack,
                StackLimit = limit
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        })) definitions.Add(definition.Id, definition);
        return definitions;
    }

    private static (bool CanStack, int StackLimit) ResolveStacking(JToken row, bool defaultCanStack, int defaultLimit)
    {
        if (row["CanStack"] is { } toggle)
            return ((bool)toggle, (bool)toggle ? int.MaxValue : 1);
        if (row["StackLimit"] is { } limit)
            return (true, (int)limit);
        return (defaultCanStack, defaultLimit);
    }

    private static void ValidateStacking(bool canStack, int limit)
    {
        if (canStack ? limit < 2 : limit != 1)
            throw new ArgumentException("Resolved stacking configuration is inconsistent.");
    }

    public static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var definition in Parse(yaml, ItemSource, row =>
        {
            var category = categories[(string)row["CategoryId"]!];
            var (canStack, limit) = ResolveStacking(row, category.CanStack, category.StackLimit);
            var values = new Dictionary<string, object>(StringComparer.Ordinal);
            var abilities = new List<string>();
            var grants = new List<ItemBehaviourGrant>();
            if (row["AbilityIds"] is JArray ids)
                foreach (var id in ids) abilities.Add((string)id!);
            if (row["BehaviourGrants"] is JArray suppliedGrants)
                foreach (var grant in suppliedGrants)
                    grants.Add(new((string)grant["BehaviourId"]!,
                        Enum.Parse<BehaviourGrantWhile>((string)grant["While"]!),
                        Enum.Parse<BehaviourGrantLifetime>((string)grant["Lifetime"]!)));
            if (row["Values"] is JObject legacyValues)
            {
                if (legacyValues.ContainsKey("Shield"))
                    throw new ArgumentException($"Item '{row["Id"]}': replace legacy Shield with ShieldCapacity (item data version 2).");
                if (legacyValues.ContainsKey("Energy"))
                    throw new ArgumentException($"Item '{row["Id"]}': remove legacy Energy; use EnergyProduction on reactors and EnergyStorage on batteries.");
            }
            foreach (var required in category.Values)
            {
                if (required == "AbilityIds")
                {
                    if (abilities.Count == 0) throw new ArgumentException($"Item '{row["Id"]}' requires AbilityIds.");
                    continue;
                }
                var value = row["Values"]?[required];
                if (value is not JValue scalar || scalar.Value == null ||
                    scalar.Type is not (JTokenType.Integer or JTokenType.Float or JTokenType.String))
                    throw new ArgumentException($"Item '{row["Id"]}' requires value '{required}'.");
                if ((required == "DamageSource") != (scalar.Type == JTokenType.String))
                    throw new ArgumentException($"Item '{row["Id"]}' has wrong type for '{required}'.");
                if (scalar.Type == JTokenType.String && (string.IsNullOrWhiteSpace((string?)scalar)))
                    throw new ArgumentException($"Item '{row["Id"]}' has invalid damage source.");
                if (scalar.Type != JTokenType.String && (!double.IsFinite((double)scalar) || (double)scalar < 0))
                    throw new ArgumentException($"Item '{row["Id"]}' has invalid value '{required}'.");
                if (required == "RechargeTime" && (double)scalar <= 0)
                    throw new ArgumentException($"Item '{row["Id"]}' requires positive RechargeTime in milliseconds.");
                values.Add(required, scalar.Value);
            }
            if (row["Values"] is JObject supplied && supplied.Count != values.Count)
                throw new ArgumentException($"Item '{row["Id"]}' contains unexpected values.");
            var definition = new ItemDefinition((string)row["Id"]!, (string)row["DisplayName"]!, category.DisplayName,
                (string?)row["Description"] ?? "", canStack, limit,
                row["Icon"]?.Type == JTokenType.String ? resolveIcon((string)row["Icon"]!) : category.Icon!)
            {
                CategoryId = category.Id,
                Values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(values),
                AbilityIds = abilities.AsReadOnly(),
                BehaviourGrants = grants.AsReadOnly()
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        })) definitions.Add(definition.Id, definition);
        return definitions;
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
