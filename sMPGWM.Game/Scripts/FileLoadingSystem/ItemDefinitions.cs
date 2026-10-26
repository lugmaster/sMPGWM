#nullable enable

using System;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.FileLoadingSystem;

// YAML transport records; the domain catalog owns cross-reference and stacking validation.
public sealed record ItemDefinitionData(
    string Id,
    string DisplayName,
    string CategoryId,
    string? IconAssetPath = null,
    int? MaximumStackSize = null)
{
    public string Description { get; init; } = "";
}

public sealed record ItemDefinitions(ItemDefinitionData[] Items)
{
    public LoadResult<ItemCatalog> ToCatalog(ItemCategories categories, string sourcePath, string schemaPath)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Items);
            var definitions = new ItemDefinition[Items.Length];
            for (var index = 0; index < Items.Length; index++)
            {
                var data = Items[index];
                ArgumentNullException.ThrowIfNull(data);
                definitions[index] = new ItemDefinition(new(data.Id), data.DisplayName, new(data.CategoryId),
                    data.Description, data.IconAssetPath, data.MaximumStackSize);
            }
            return LoadResult<ItemCatalog>.Success(new ItemCatalog(categories, definitions));
        }
        catch (ArgumentException exception)
        {
            return LoadResult<ItemCatalog>.Failure(new DataLoadError(
                "Invalid item definitions", exception.Message, sourcePath, schemaPath, exception));
        }
    }
}
