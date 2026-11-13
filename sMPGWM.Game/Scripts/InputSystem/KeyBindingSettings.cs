using System;
using Godot;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Scripts.InputSystem;

public static class KeyBindingSettings
{
    internal static event Action? BindingsChanged;
    public const string ConfigSection = "keybindings";
    public const string ConfigKey = "bindings";
    public const int SlotCount = 2;

    private static InputEvent?[,]? _current;
    private static InputEvent?[,]? _defaults;
    private static Godot.Collections.Dictionary? _configuration;

    public static InputEvent?[,] CreateCurrentBindings() =>
        Clone(_current ?? throw new InvalidOperationException("Keybinding configuration has not been loaded."));

    public static InputEvent?[,] CreateDefaultBindings() =>
        Clone(_defaults ?? throw new InvalidOperationException("Default keybinding configuration has not been loaded."));

    public static void LoadConfiguration(Variant defaults, Variant effective, string effectiveSource)
    {
        var shipped = KeyBindingConfiguration.Parse(defaults, FilePaths.SystemConfigPath);
        var configured = KeyBindingConfiguration.Parse(KeyBindingConfiguration.MergeNewActions(defaults, effective), effectiveSource);
        KeyBindingConfiguration.RequireMatchingActions(shipped, configured, effectiveSource);
        var defaultBindings = new InputEvent?[configured.Actions.Length, SlotCount];
        for (var row = 0; row < configured.Actions.Length; row++)
        {
            for (var defaultRow = 0; defaultRow < shipped.Actions.Length; defaultRow++)
            {
                if (shipped.Actions[defaultRow].Action != configured.Actions[row].Action) continue;
                for (var slot = 0; slot < SlotCount; slot++) defaultBindings[row, slot] = shipped.Bindings[defaultRow, slot];
                break;
            }
        }
        KeyBindingCatalog.Initialize(configured.Actions);
        _configuration = configured.Values;
        _defaults = defaultBindings;
        Apply(configured.Bindings);
    }

    public static InputEvent? Normalize(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } or InputEventMouseButton { Pressed: true })
            return CopyBinding(input);
        return null;
    }

    public static bool SameBinding(InputEvent? left, InputEvent? right)
    {
        if (left == null || right == null) return left == right;
        if (left is not InputEventWithModifiers a || right is not InputEventWithModifiers b ||
            a.CtrlPressed != b.CtrlPressed || a.ShiftPressed != b.ShiftPressed ||
            a.AltPressed != b.AltPressed || a.MetaPressed != b.MetaPressed) return false;
        if (a is InputEventKey ak && b is InputEventKey bk)
            return ak.PhysicalKeycode == bk.PhysicalKeycode && ak.Keycode == bk.Keycode;
        return a is InputEventMouseButton am && b is InputEventMouseButton bm && am.ButtonIndex == bm.ButtonIndex;
    }

    public static string Format(InputEvent? input)
    {
        if (input is not InputEventWithModifiers binding) return "Unbound";
        var prefix = (binding.CtrlPressed ? "Ctrl+" : "") + (binding.ShiftPressed ? "Shift+" : "") +
                     (binding.AltPressed ? "Alt+" : "") + (binding.MetaPressed ? "Meta+" : "");
        if (binding is InputEventKey key)
        {
            var code = key.PhysicalKeycode != Key.None
                ? DisplayServer.KeyboardGetKeycodeFromPhysical(key.PhysicalKeycode)
                : key.Keycode;
            if (code == Key.None) code = key.PhysicalKeycode;
            return prefix + OS.GetKeycodeString(code);
        }
        if (binding is InputEventMouseButton mouse)
            return prefix + (mouse.ButtonIndex switch
            {
                MouseButton.Left => "Left Mouse",
                MouseButton.Right => "Right Mouse",
                MouseButton.Middle => "Middle Mouse",
                MouseButton.WheelUp => "Wheel Up",
                MouseButton.WheelDown => "Wheel Down",
                MouseButton.WheelLeft => "Wheel Left",
                MouseButton.WheelRight => "Wheel Right",
                MouseButton.Xbutton1 => "Mouse 4",
                MouseButton.Xbutton2 => "Mouse 5",
                _ => "Mouse " + (int)mouse.ButtonIndex
            });
        return "Unbound";
    }

    public static bool IsValid(InputEvent?[,] bindings)
    {
        if (bindings.GetLength(0) != KeyBindingCatalog.Actions.Length || bindings.GetLength(1) != SlotCount) return false;
        foreach (var binding in bindings)
        {
            if (binding == null) continue;
            if (binding is InputEventKey key && IsSupportedKey(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode)) continue;
            if (binding is InputEventMouseButton mouse && mouse.ButtonIndex is >= MouseButton.Left and <= MouseButton.Xbutton2) continue;
            return false;
        }
        return true;
    }

    public static Godot.Collections.Dictionary Serialize(InputEvent?[,] bindings)
    {
        if (!IsValid(bindings)) throw new ArgumentException("Invalid keybindings.", nameof(bindings));
        var saved = (_configuration ?? throw new InvalidOperationException("Keybinding configuration has not been loaded.")).Duplicate(true);
        for (var row = 0; row < KeyBindingCatalog.Actions.Length; row++)
        {
            var definition = saved[KeyBindingCatalog.Actions[row].Action].AsGodotDictionary();
            definition["primary"] = Encode(bindings[row, 0]);
            definition["secondary"] = Encode(bindings[row, 1]);
        }
        return saved;
    }

    public static void Apply(InputEvent?[,] bindings)
    {
        if (!IsValid(bindings)) throw new ArgumentException("Invalid keybindings.", nameof(bindings));
        _current = Clone(bindings);
        for (var row = 0; row < KeyBindingCatalog.Actions.Length; row++)
        {
            var action = KeyBindingCatalog.Actions[row].Action;
            if (!InputMap.HasAction(action)) InputMap.AddAction(action);
            InputMap.ActionEraseEvents(action);
            InputMap.ActionSetDeadzone(action, KeyBindingCatalog.Actions[row].Deadzone);
            for (var slot = 0; slot < SlotCount; slot++)
                if (_current[row, slot] is { } input) InputMap.ActionAddEvent(action, input);
        }
        BindingsChanged?.Invoke();
    }

    private static InputEvent?[,] Clone(InputEvent?[,] source)
    {
        var copy = new InputEvent?[source.GetLength(0), SlotCount];
        for (var row = 0; row < source.GetLength(0); row++)
            for (var slot = 0; slot < SlotCount; slot++)
                copy[row, slot] = source[row, slot] is { } input ? CopyBinding(input) : null;
        return copy;
    }

    private static InputEvent? CopyBinding(InputEvent input)
    {
        InputEventWithModifiers copy;
        if (input is InputEventKey key)
        {
            var code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
            if (!IsSupportedKey(code)) return null;
            copy = new InputEventKey
            {
                PhysicalKeycode = key.PhysicalKeycode,
                Keycode = key.PhysicalKeycode != Key.None ? Key.None : key.Keycode
            };
        }
        else if (input is InputEventMouseButton mouse && mouse.ButtonIndex is >= MouseButton.Left and <= MouseButton.Xbutton2)
            copy = new InputEventMouseButton { ButtonIndex = mouse.ButtonIndex };
        else return null;
        var original = (InputEventWithModifiers)input;
        copy.Device = -1;
        copy.CtrlPressed = original.CtrlPressed;
        copy.ShiftPressed = original.ShiftPressed;
        copy.AltPressed = original.AltPressed;
        copy.MetaPressed = original.MetaPressed;
        return copy;
    }

    private static Variant Encode(InputEvent? input)
    {
        if (input is not InputEventWithModifiers binding) return default;
        var encoded = new Godot.Collections.Dictionary();
        if (input is InputEventKey key)
        {
            encoded["type"] = "key";
            var physical = key.PhysicalKeycode != Key.None;
            encoded[physical ? "physical_key" : "key"] = OS.GetKeycodeString(physical ? key.PhysicalKeycode : key.Keycode);
        }
        else
        {
            encoded["type"] = "mouse";
            encoded["button"] = ((InputEventMouseButton)input).ButtonIndex.ToString();
        }
        if (binding.CtrlPressed) encoded["ctrl"] = true;
        if (binding.ShiftPressed) encoded["shift"] = true;
        if (binding.AltPressed) encoded["alt"] = true;
        if (binding.MetaPressed) encoded["meta"] = true;
        return encoded;
    }

    internal static bool IsSupportedKey(Key key)
    {
        if (key is Key.None or Key.Unknown or Key.Shift or Key.Ctrl or Key.Alt or Key.Meta) return false;
        return (ulong)key <= 0x10ffff || Enum.IsDefined(key);
    }

}
