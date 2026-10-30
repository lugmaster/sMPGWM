using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

public static class ItemRegistry
{
    private sealed record RegistryData(
        Dictionary<string, ItemDefinition> Items,
        Dictionary<string, ItemCategoryDefinition> Categories)
    {
        internal IReadOnlyList<ItemDefinition> AllItems { get; } = Array.AsReadOnly(Items.Values.ToArray());

        internal IReadOnlyList<ItemCategoryDefinition> AllCategories { get; } =
            Array.AsReadOnly(Categories.Values.ToArray());
    }

    private static readonly Lazy<RegistryData> Data = new(Load);

    public static ItemDefinition GetById(string id) => Data.Value.Items[id];

    public static bool TryGetById(string id, out ItemDefinition definition) =>
        Data.Value.Items.TryGetValue(id, out definition!);

    public static IReadOnlyList<ItemDefinition> GetAll() => Data.Value.AllItems;
    public static ItemCategoryDefinition GetCategoryById(string id) => Data.Value.Categories[id];
    public static IReadOnlyList<ItemCategoryDefinition> GetAllCategories() => Data.Value.AllCategories;


    private static RegistryData Load()
    {
        var categoryYaml = FileLoader.LoadYaml(FilePaths.UserItemCategoryDataYaml, FilePaths.ItemCategoryDataYaml,
            FilePaths.ItemCategoryDataYamlSchema);
        var categories = LoadItemCategoryDefinitions(categoryYaml, Icons.Get);
        var itemYaml = FileLoader.LoadYaml(FilePaths.UserItemDataYaml, FilePaths.ItemDataYaml,
            FilePaths.ItemDataYamlSchema);
        return new RegistryData(LoadItemDefinitions(itemYaml, categories, Icons.Get), categories);
    }

    internal static Dictionary<string, ItemCategoryDefinition> LoadItemCategoryDefinitions(
        JObject yaml, Func<string, IconDefinition> resolveIcon) =>
        yaml["Categories"]!.ToDictionary(row => (string)row["Id"]!, row =>
        {
            var (canStack, limit) = ResolveStacking(row, false, 1);
            var definition = new ItemCategoryDefinition((string)row["Id"]!, (string)row["DisplayName"]!,
                resolveIcon((string?)row["Icon"] ?? "Software.WarningSignCircleQuestionMarkHelp"))
            {
                Description = (string?)row["Description"] ?? "",
                CanStack = canStack,
                StackLimit = limit
            };
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        }, StringComparer.Ordinal);

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
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon) =>
        yaml["Items"]!.ToDictionary(row => (string)row["Id"]!, row =>
        {
            var category = categories[(string)row["CategoryId"]!];
            var (canStack, limit) = ResolveStacking(row, category.CanStack, category.StackLimit);
            var definition = new ItemDefinition((string)row["Id"]!, (string)row["DisplayName"]!, category.DisplayName,
                (string?)row["Description"] ?? "", canStack, limit,
                row["Icon"]?.Type == JTokenType.String ? resolveIcon((string)row["Icon"]!) : category.Icon);
            ValidateStacking(definition.CanStack, definition.StackLimit);
            return definition;
        }, StringComparer.Ordinal);
}

