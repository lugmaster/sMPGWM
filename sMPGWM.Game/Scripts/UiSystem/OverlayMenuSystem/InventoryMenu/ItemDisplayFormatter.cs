#nullable enable

using System.Globalization;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.OverlayMenuSystem.InventoryMenu;

internal static class ItemDisplayFormatter
{
    internal static IconDisplayData Create(ItemCatalog catalog, ItemEntity? entity)
    {
        if (entity == null) return new IconDisplayData();
        var data = catalog.GetTooltip(entity.DefinitionId);
        var tooltip = $"{data.DisplayName}\n{data.CategoryName}";
        if (data.Description.Length > 0) tooltip += $"\n{data.Description}";
        if (data.CanStack)
        {
            var limit = data.ConfiguredStackLimit?.ToString(CultureInfo.InvariantCulture) ?? "No configured limit";
            tooltip += $"\nQuantity: {entity.Count.ToString(CultureInfo.InvariantCulture)}\nStack limit: {limit}";
        }
        return new IconDisplayData(tooltip, data.CanStack ? entity.Count : null);
    }
}
