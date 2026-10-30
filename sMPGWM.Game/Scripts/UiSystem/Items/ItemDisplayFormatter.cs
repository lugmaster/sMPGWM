

using System.Globalization;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.Items;

internal static class ItemDisplayFormatter
{
    internal static IconDisplayData Create(ItemEntity? entity)
    {
        if (entity == null) return new IconDisplayData();
        return Create(entity.Definition, entity.Count);
    }

    internal static IconDisplayData Create(string definitionId, long? quantity)
    {
        return Create(ItemRegistry.GetById(definitionId), quantity);
    }

    private static IconDisplayData Create(ItemDefinition data, long? quantity)
    {
        var tooltip = $"{data.DisplayName}\n{data.CategoryName}";
        if (data.Description.Length > 0) tooltip += $"\n{data.Description}";
        if (data.CanStack)
        {
            var limit = data.StackLimit == int.MaxValue ? "Unlimited" : data.StackLimit.ToString(CultureInfo.InvariantCulture);
            tooltip += $"\nQuantity: {quantity.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)}\nStack limit: {limit}";
        }
        return new IconDisplayData(tooltip, data.CanStack ? quantity : null);
    }
}
