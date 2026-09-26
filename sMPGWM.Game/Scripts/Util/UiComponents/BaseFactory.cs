using System;
using sMPGWM.Scripts.UiSystem.Hud;

namespace sMPGWM.Scripts.Util.UiComponents;

public static class BaseFactory
{
    public static ClickableIcon CreateClickableIcon(IconDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(definition.IconTexture);

        var icon = HudProvider.CreateClickableIcon();

        icon.IconTexture = definition.IconTexture;
        icon.IconSize = definition.IconSize;

        return icon;
    }
}