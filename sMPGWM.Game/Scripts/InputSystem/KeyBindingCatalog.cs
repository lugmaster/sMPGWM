using System;

namespace sMPGWM.Scripts.InputSystem;

public static class KeyBindingCatalog
{
    private static KeyBindingDefinition[]? _actions;

    public static KeyBindingDefinition[] Actions =>
        _actions ?? throw new InvalidOperationException("Keybinding configuration has not been loaded.");

    internal static void Initialize(KeyBindingDefinition[] actions) => _actions = actions;
}
