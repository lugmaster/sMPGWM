using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.StatSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Tests;

internal static class ItemTestData
{
    internal static Dictionary<string, ItemCategoryDefinition> ParseCategories(JObject document,
        Func<string, IconDefinition> resolveIcon)
    {
        var source = ItemCatalogueLoader.CategorySource;
        ValidateFixture(document, source);
        var result = new Dictionary<string, ItemCategoryDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(document, source,
                     row => ItemDefinitionParser.ParseCategory(row, resolveIcon)))
            result.Add(definition.Id, definition);
        return result;
    }

    internal static Dictionary<string, ItemDefinition> ParseItems(JObject document,
        Dictionary<string, ItemCategoryDefinition> categories, Func<string, IconDefinition> resolveIcon)
    {
        var source = new CatalogueSource("ItemFixture.yml", "items/ItemSchema.yml", "Items");
        ValidateFixture(document, source);
        var result = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        foreach (var definition in CatalogueParser.Parse(document, source,
                     row => ItemDefinitionParser.ParseItem(row, id => categories[id], resolveIcon)))
        {
            EquipmentStatCalculator.Precompile(definition);
            result.Add(definition.Id, definition);
        }
        return result;
    }

    private static void ValidateFixture(JObject document, CatalogueSource source)
    {
        foreach (var property in document.Properties())
            if (property.Name != "Version" && property.Name != source.RootKey)
                throw new ArgumentException($"{source.FileName}: unexpected field '{property.Name}'.");
        if (document["Version"]?.Type != JTokenType.String || (string?)document["Version"] != "3.0.0")
            throw new ArgumentException("Version must be 3.0.0; migrate legacy item data.");
        if (source.RootKey == "Categories" && document[source.RootKey] is JArray { Count: 0 })
            throw new ArgumentException("Categories: expected at least one category.");
    }
}
