#nullable enable

using Godot;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.Items;

internal sealed class ItemVisuals(ItemCatalog catalog)
{
    internal void Apply(ClickableIcon icon, ItemEntity? entity, string emptyTooltip = "")
    {
        icon.DisplayModulate = Colors.White;
        icon.Modulate = Colors.White;
        if (entity == null)
        {
            icon.SetDisplay(null, new IconDisplayData(emptyTooltip));
            return;
        }

        var definition = catalog.Get(entity.DefinitionId);
        icon.SetDisplay(ItemIconResolver.Item(catalog, definition), ItemDisplayFormatter.Create(catalog, entity));
    }

    internal void ApplyDefinition(ClickableIcon icon, ItemDefinitionId? definitionId, long? quantity, string label,
        bool available, bool showPlaceholder = false)
    {
        if (definitionId == null)
            icon.SetDisplay(showPlaceholder ? ItemIconResolver.Placeholder() : null, new IconDisplayData(label));
        else
        {
            var definition = catalog.Get(definitionId.Value);
            icon.SetDisplay(ItemIconResolver.Item(catalog, definition),
                ItemDisplayFormatter.Create(catalog, definition.Id, quantity));
        }
        icon.DisplayModulate = available ? Colors.White : new Color(0.6f, 0.6f, 0.6f);
        icon.Modulate = icon.DisplayModulate;
    }
}
