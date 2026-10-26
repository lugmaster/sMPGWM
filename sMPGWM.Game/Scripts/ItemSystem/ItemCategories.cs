#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

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

public sealed class ItemCategories
{
    public static readonly ItemCategoryId Engine = new("engine");
    public static readonly ItemCategoryId Hull = new("hull");
    public static readonly ItemCategoryId ShieldGenerator = new("shield_generator");
    public static readonly ItemCategoryId Reactor = new("reactor");
    public static readonly ItemCategoryId LightWeapon = new("light_weapon");
    public static readonly ItemCategoryId HeavyWeapon = new("heavy_weapon");
    public static readonly ItemCategoryId Consumable = new("consumable");
    public static readonly ItemCategoryId Resource = new("resource");

    private readonly Dictionary<ItemCategoryId, ItemCategory> _byId;

    public IReadOnlyList<ItemCategory> All { get; }

    public ItemCategories(IEnumerable<ItemCategory> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        _byId = new Dictionary<ItemCategoryId, ItemCategory>();
        foreach (var category in categories)
        {
            ArgumentNullException.ThrowIfNull(category);
            if (!_byId.TryAdd(category.Id, category))
                throw new ArgumentException($"Duplicate category ID: {category.Id.Value}.", nameof(categories));
        }

        All = Array.AsReadOnly(_byId.Values.OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Id.Value, StringComparer.Ordinal).ToArray());
    }

    public ItemCategory Get(ItemCategoryId id) => _byId.TryGetValue(id, out var category)
        ? category
        : throw new ArgumentException($"Unknown category ID: {id.Value}.", nameof(id));

    public static ItemCategories CreateDefault() => new(new[]
    {
        new ItemCategory(Engine, "Engine", 0, false),
        new ItemCategory(Hull, "Hull", 1, false),
        new ItemCategory(ShieldGenerator, "Shield Generator", 2, false),
        new ItemCategory(Reactor, "Reactor", 3, false),
        new ItemCategory(LightWeapon, "Light Weapon", 4, false),
        new ItemCategory(HeavyWeapon, "Heavy Weapon", 5, false),
        new ItemCategory(Consumable, "Consumable", 6, true),
        new ItemCategory(Resource, "Resource", 7, true)
    });
}
