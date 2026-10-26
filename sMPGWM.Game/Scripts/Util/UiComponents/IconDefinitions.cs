using System;

namespace sMPGWM.Scripts.Util.UiComponents;

internal sealed partial class IconDefinitions : AbstractSingleton<IconDefinitions>
{
    private IconAtlasProvider _atlasProvider = null!;
    private IconDefinition[] _icons = [];

    protected override void OnEnterTree()
    {
        _icons = Icons.LoadDefinitions(out _atlasProvider);
    }

    protected override void OnExitTree()
    {
        _icons = [];
        _atlasProvider = null!;
    }

    internal static IconDefinition GetIcon(int index)
    {
        if (Instance == null || Instance._icons.Length == 0)
            throw new InvalidOperationException("IconDefinitions must enter the scene tree before accessing icons.");

        return Instance._icons[index];
    }
}
