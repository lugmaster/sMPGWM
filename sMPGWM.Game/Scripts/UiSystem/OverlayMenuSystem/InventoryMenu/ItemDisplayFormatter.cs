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
        return Create(catalog, entity.DefinitionId, entity.Count);
    }

    internal static IconDisplayData Create(ItemCatalog catalog, ItemDefinitionId definitionId, long? quantity)
    {
        var data = catalog.GetTooltip(definitionId);
        var tooltip = $"{data.DisplayName}\n{data.CategoryName}";
        if (data.Description.Length > 0) tooltip += $"\n{data.Description}";
        if (data.CanStack)
        {
            var limit = data.ConfiguredStackLimit?.ToString(CultureInfo.InvariantCulture) ?? "No configured limit";
            tooltip += $"\nQuantity: {quantity.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)}\nStack limit: {limit}";
        }
        return new IconDisplayData(tooltip, data.CanStack ? quantity : null);
    }
}
