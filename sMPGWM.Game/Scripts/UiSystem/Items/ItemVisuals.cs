

using Godot;
using sMPGWM.Scripts.Items;
using sMPGWM.Scripts.Util.UiComponents;

namespace sMPGWM.Scripts.UiSystem.Items;

internal sealed class ItemVisuals
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

        var definition = entity.Definition;
        icon.SetDisplay(ItemIconResolver.Item(definition), ItemDisplayFormatter.Create(entity));
    }

    internal void ApplyDefinition(ClickableIcon icon, string? definitionId, long? quantity, string label,
        bool available, bool showPlaceholder = false)
    {
        if (definitionId == null)
            icon.SetDisplay(showPlaceholder ? ItemIconResolver.Placeholder() : null, new IconDisplayData(label));
        else
        {
            var definition = ItemRegistry.GetById(definitionId);
            icon.SetDisplay(ItemIconResolver.Item(definition),
                ItemDisplayFormatter.Create(definition.Id, quantity));
        }
        icon.DisplayModulate = available ? Colors.White : new Color(0.6f, 0.6f, 0.6f);
        icon.Modulate = icon.DisplayModulate;
    }
}
