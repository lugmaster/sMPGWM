using System;
using System.Collections.Generic;
using System.Linq;

namespace sMPGWM.Scripts.ItemSystem;

public sealed class ItemCatalog
{
    private readonly Dictionary<ItemDefinitionId, ItemDefinition> _byId;

    public ItemCategories Categories { get; }
    public IReadOnlyList<ItemDefinition> Definitions { get; }

    public ItemCatalog(ItemCategories categories, IEnumerable<ItemDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(definitions);
        Categories = categories;
        _byId = new Dictionary<ItemDefinitionId, ItemDefinition>();
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            var category = categories.Get(definition.CategoryId);
            if (!category.CanStack && definition.MaximumStackSize is > 1)
                throw new ArgumentException($"Nonstackable item {definition.Id} cannot have a stack limit above one.",
                    nameof(definitions));
            if (!_byId.TryAdd(definition.Id, definition))
                throw new ArgumentException($"Duplicate item definition ID: {definition.Id}.", nameof(definitions));
        }

        Definitions = Array.AsReadOnly(_byId.Values.ToArray());
    }

    public ItemDefinition Get(ItemDefinitionId id) => _byId.TryGetValue(id, out var definition)
        ? definition
        : throw new ArgumentException($"Unknown item definition ID: {id}.", nameof(id));

    public int GetStackLimit(ItemDefinitionId id)
    {
        var definition = Get(id);
        return Categories.Get(definition.CategoryId).CanStack
            ? definition.MaximumStackSize ?? int.MaxValue
            : 1;
    }

    public ItemEntity CreateEntity(ItemDefinitionId id, int count = 1)
    {
        var limit = GetStackLimit(id);
        if (count <= 0 || count > limit)
            throw new ArgumentOutOfRangeException(nameof(count), $"Count must be between one and {limit}.");
        return new ItemEntity(ItemEntityId.Create(), id, count);
    }

    public bool CanStackTogether(ItemDefinitionId source, ItemDefinitionId destination)
    {
        var sourceDefinition = Get(source);
        Get(destination);
        return source == destination && Categories.Get(sourceDefinition.CategoryId).CanStack;
    }

    public ItemTooltipData GetTooltip(ItemDefinitionId id)
    {
        var definition = Get(id);
        var category = Categories.Get(definition.CategoryId);
        return new ItemTooltipData(definition.DisplayName, category.DisplayName, definition.Description,
            category.CanStack, GetStackLimit(id), definition.MaximumStackSize);
    }
}
