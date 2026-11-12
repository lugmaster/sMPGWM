using System;

namespace sMPGWM.Scripts.UtilitySystem.UiComponents;

internal sealed partial class IconDefinitions : AbstractSingleton<IconDefinitions>
{
    private IconAtlasProvider _atlasProvider = null!;
    private IconDefinition[] _icons = [];

    protected override void OnEnterTree()
    {
        _icons = Icons.LoadDefinitions(out _atlasProvider);
        Cursors.Initialize();
    }

    protected override void OnExitTree()
    {
        _icons = [];
        Cursors.Release();
        _atlasProvider = null!;
    }

    internal static IconDefinition GetIcon(int index)
    {
        if (Instance == null || Instance._icons.Length == 0)
            throw new InvalidOperationException("IconDefinitions must enter the scene tree before accessing icons.");

        return Instance._icons[index];
    }
}
