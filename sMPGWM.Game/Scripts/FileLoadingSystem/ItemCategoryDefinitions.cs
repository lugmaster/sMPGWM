#nullable enable

using System;
using System.Linq;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.FileLoadingSystem;

public sealed record ItemCategoryDefinition(
    string Id,
    string DisplayName,
    int DisplayOrder,
    bool CanStack,
    string? IconAssetPath = null);

public sealed record ItemCategoryDefinitions(ItemCategoryDefinition[] Categories)
{
    public LoadResult<ItemCategories> ToCategories(string sourcePath, string schemaPath)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Categories);
            return LoadResult<ItemCategories>.Success(new ItemCategories(Categories.Select(definition =>
                new ItemCategory(new ItemCategoryId(definition.Id), definition.DisplayName,
                    definition.DisplayOrder, definition.CanStack, definition.IconAssetPath))));
        }
        catch (ArgumentException exception)
        {
            return LoadResult<ItemCategories>.Failure(new DataLoadError(
                "Invalid item categories", exception.Message, sourcePath, schemaPath, exception));
        }
    }
}
