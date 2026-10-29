#nullable enable

using System;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.ItemSystem;

// Newtonsoft.Json instantiates these records when deserializing category YAML through DefinitionDeserializer.
// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ItemCategoryDefinitions(ItemCategoryDefinition[] Categories)
{
    public LoadResult<ItemCategories> ToCategories(string sourcePath, string schemaPath)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(Categories);
            var categories = new ItemCategory[Categories.Length];
            for (var index = 0; index < Categories.Length; index++)
            {
                var definition = Categories[index];
                ArgumentNullException.ThrowIfNull(definition);
                if (definition.Icon != null && !Icons.IsDefined(definition.Icon))
                    throw new ArgumentException($"Category {definition.Id}: unknown icon {definition.Icon}.");
                categories[index] = new ItemCategory(new ItemCategoryId(definition.Id), definition.DisplayName,
                    definition.DisplayOrder, definition.CanStack, definition.IconAssetPath)
                {
                    IconKey = definition.Icon
                };
            }
            var result = new ItemCategories(categories);
            foreach (var slot in EquipmentSlotDefinition.Defaults)
                result.Get(slot.AcceptedCategory);
            return LoadResult<ItemCategories>.Success(result);
        }
        catch (ArgumentException exception)
        {
            return LoadResult<ItemCategories>.Failure(new DataLoadError(
                "Invalid item categories", exception.Message, sourcePath, schemaPath, exception));
        }
    }
}
