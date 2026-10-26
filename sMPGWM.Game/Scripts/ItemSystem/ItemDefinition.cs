#nullable enable

using System;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemDefinition
{
    public ItemDefinitionId Id { get; }
    public string DisplayName { get; }
    public ItemCategoryId CategoryId { get; }
    public string? IconAssetPath { get; }
    public string Description { get; }
    public int? MaximumStackSize { get; }

    public ItemDefinition(ItemDefinitionId id, string displayName, ItemCategoryId categoryId,
        string description = "", string? iconAssetPath = null, int? maximumStackSize = null)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
            throw new ArgumentException("An item definition ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("An item display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(categoryId.Value))
            throw new ArgumentException("A category ID is required.", nameof(categoryId));
        ArgumentNullException.ThrowIfNull(description);
        if (maximumStackSize.HasValue && maximumStackSize.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumStackSize));

        Id = id;
        DisplayName = displayName;
        CategoryId = categoryId;
        Description = description;
        IconAssetPath = iconAssetPath;
        MaximumStackSize = maximumStackSize;
    }
}

public sealed record ItemTooltipData(
    string DisplayName,
    string CategoryName,
    string Description,
    bool CanStack,
    int EffectiveStackLimit,
    int? ConfiguredStackLimit);
