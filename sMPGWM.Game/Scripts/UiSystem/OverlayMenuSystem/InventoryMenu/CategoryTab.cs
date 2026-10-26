#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using sMPGWM.Scripts.ItemSystem;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal sealed record CategoryTab(ItemCategoryId? CategoryId, string Title)
{
    internal static IReadOnlyList<CategoryTab> Create(ItemCategories categories) =>
        Array.AsReadOnly(new[] { new CategoryTab(null, "All") }
            .Concat(categories.All.Select(category => new CategoryTab(category.Id, category.DisplayName))).ToArray());
}
