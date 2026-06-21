using System;
using sMPGWM.Scripts.Provider;

namespace sMPGWM.Scripts.Ui.Base;

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