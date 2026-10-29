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

    public static ItemEntity CreateEntity(string id, int count = 1)
    {
        var definition = GetById(id);
        if (count <= 0 || count > definition.StackLimit)
            throw new ArgumentOutOfRangeException(nameof(count));
        return new ItemEntity(ItemEntityId.Create(), definition, count);
    }

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
            new ItemCategoryDefinition((string)row["Id"]!, (string)row["DisplayName"]!,
                resolveIcon((string)row["Icon"]!)), StringComparer.Ordinal);

    internal static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon) =>
        yaml["Items"]!.ToDictionary(row => (string)row["Id"]!, row =>
        {
            var category = categories[(string)row["CategoryId"]!];
            var canStack = (bool)row["CanStack"]!;
            var limit = (int)row["StackLimit"]!;
            if (!canStack && limit != 1)
                throw new ArgumentException($"Nonstackable item {row["Id"]} must have a stack limit of one.");
            return new ItemDefinition((string)row["Id"]!, (string)row["DisplayName"]!, category.DisplayName,
                (string?)row["Description"] ?? "", canStack, limit,
                row["Icon"]?.Type == JTokenType.String ? resolveIcon((string)row["Icon"]!) : category.Icon);
        }, StringComparer.Ordinal);
}