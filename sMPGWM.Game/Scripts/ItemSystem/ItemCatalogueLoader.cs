using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.UtilitySystem.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

internal static class ItemCatalogueLoader
{
    internal static readonly CatalogueSource CategorySource =
        new("ItemCategoryDefinitions.yml", "ItemCategorySchema.yml", "Categories", UserDirectory: "items/", DefaultDirectory: "items/");

    internal static IReadOnlyList<CatalogueSource> ItemSources { get; } = Array.AsReadOnly(new CatalogueSource[]
    {
        Source("engine"), Source("hull"), Source("shield_generator"), Source("reactor"),
        Source("light_weapon"), Source("medium_weapon"), Source("heavy_weapon"), Source("consumable"),
        Source("resource"), Source("battery"), Source("weapon_fixture")
    });

    private static CatalogueSource Source(string category) =>
        new(category + ".yml", category + ".schema.yml", "Items", UserDirectory: "items/", DefaultDirectory: "items/");

    internal static ItemCategoryDefinitions LoadCategories(Func<CatalogueSource, JObject> read,
        Func<string, IconDefinition> resolveIcon)
    {
        var catalogue = new Catalogue<ItemCategoryDefinition>(CategorySource,
            row => ItemDefinitionParser.ParseCategory(row, resolveIcon));
        return new ItemCategoryDefinitions(CatalogueParser.Load(catalogue, source =>
        {
            var document = read(source);
            RequireVersion(document);
            return document;
        }));
    }

    internal static ItemDefinitions LoadItems(Func<CatalogueSource, JObject> read,
        ItemCategoryDefinitions categories, Func<string, IconDefinition> resolveIcon)
    {
        var definitions = new List<ItemDefinition>();
        var sources = new Dictionary<string, CatalogueSource>(StringComparer.Ordinal);
        string? version = null;
        foreach (var source in ItemSources)
        {
            var catalogue = new Catalogue<ItemDefinition>(source,
                row => ItemDefinitionParser.ParseItem(row, categories.GetById, resolveIcon));
            var rows = CatalogueParser.Load(catalogue, current =>
            {
                var document = read(current);
                var currentVersion = (string?)document["Version"];
                if (version != null && !string.Equals(version, currentVersion, StringComparison.Ordinal))
                    throw new ArgumentException($"Version: item definition version mismatch; expected '{version}', got '{currentVersion}'.");
                RequireVersion(document);
                version = currentVersion;
                return document;
            });
            CatalogueParser.WithSource(source, () =>
            {
                foreach (var definition in rows)
                {
                    if (!sources.TryAdd(definition.Id, source))
                        throw new ArgumentException($"Id: duplicate definition '{definition.Id}' also declared in {sources[definition.Id].UserPath}.");
                    definitions.Add(definition);
                }
                return true;
            });
        }
        return new ItemDefinitions(definitions.ToArray(), sources);
    }

    private static void RequireVersion(JObject document)
    {
        if (document["Version"]?.Type != JTokenType.String || (string?)document["Version"] != "3.0.0")
            throw new ArgumentException("Version must be 3.0.0; migrate legacy item data to flat properties and typed Supply.");
    }
}
