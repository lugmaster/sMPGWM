using sMPGWM.Scripts.GameplayDataSystem;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemRegistry
{
    private sealed class ItemDefinitions(IEnumerable<ItemDefinition> definitions)
        : DefinitionRegistry<ItemDefinition>(definitions, "items");

    private sealed class CategoryDefinitions(IEnumerable<ItemCategoryDefinition> definitions)
        : DefinitionRegistry<ItemCategoryDefinition>(definitions, "item categories");

    private sealed record RegistryData(ItemDefinitions Items, CategoryDefinitions Categories);

    private static readonly Lazy<RegistryData> Data = new(Load);

    public static ItemDefinition GetById(string id) => Data.Value.Items.GetById(id);

    public static bool TryGetById(string id, out ItemDefinition definition) =>
        Data.Value.Items.TryGetById(id, out definition!);

    public static IReadOnlyList<ItemDefinition> GetAll() => Data.Value.Items.GetAll();
    public static ItemCategoryDefinition GetCategoryById(string id) => Data.Value.Categories.GetById(id);
    public static IReadOnlyList<ItemCategoryDefinition> GetAllCategories() => Data.Value.Categories.GetAll();


    private static RegistryData Load()
    {
        var categoryYaml = FileLoader.LoadYaml(FilePaths.UserItemCategoryDataYaml, FilePaths.ItemCategoryDataYaml,
            FilePaths.ItemCategoryDataYamlSchema);
        var categories = LoadItemCategoryDefinitions(categoryYaml, Icons.Get);
        var itemYaml = FileLoader.LoadYaml(FilePaths.UserItemDataYaml, FilePaths.ItemDataYaml,
            FilePaths.ItemDataYamlSchema);
        return new RegistryData(new ItemDefinitions(LoadItemDefinitions(itemYaml, categories, Icons.Get).Values), new CategoryDefinitions(categories.Values));
    }

    internal static Dictionary<string, ItemCategoryDefinition> LoadItemCategoryDefinitions(
        JObject yaml, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemCategoryDefinition>(StringComparer.Ordinal);
        foreach (var row in yaml["Categories"]!)
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
            definitions.Add(definition.Id, definition);
        }
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

    internal static void ValidateStacking(bool canStack, int limit)
    {
        if (canStack ? limit < 2 : limit != 1)
            throw new ArgumentException("Resolved stacking configuration is inconsistent.");
    }

    internal static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var row in yaml["Items"]!)
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
                Values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(values),
                AbilityIds = abilities.AsReadOnly(),
                BehaviourGrants = grants.AsReadOnly()
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            EquipmentStatCalculator.Precompile(definition);
            definitions.Add(definition.Id, definition);
        }
        return definitions;
    }
}

