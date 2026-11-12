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
        JObject yaml, Func<string, IconDefinition> resolveIcon) =>
        GameplayCatalogueLoader.LoadItemCategoryDefinitions(yaml, resolveIcon);

    internal static Dictionary<string, ItemDefinition> LoadItemDefinitions(JObject yaml,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = GameplayCatalogueLoader.LoadItemDefinitions(yaml, categories, resolveIcon);
        foreach (var definition in definitions.Values) EquipmentStatCalculator.Precompile(definition);
        return definitions;
    }
}
