#nullable enable

using System;

namespace sMPGWM.Scripts.ItemSystem;

public sealed record ItemCategory
{
    public ItemCategoryId Id { get; }
    public string DisplayName { get; }
    public int DisplayOrder { get; }
    public string? IconAssetPath { get; }
    public string? IconKey { get; init; }
    public bool CanStack { get; }

    public ItemCategory(ItemCategoryId id, string displayName, int displayOrder, bool canStack,
        string? iconAssetPath = null)
    {
        if (string.IsNullOrWhiteSpace(id.Value))
            throw new ArgumentException("A category ID is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A category display name is required.", nameof(displayName));

        Id = id;
        DisplayName = displayName;
        DisplayOrder = displayOrder;
        CanStack = canStack;
        IconAssetPath = iconAssetPath;
    }
}
