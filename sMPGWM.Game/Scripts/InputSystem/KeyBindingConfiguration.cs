using System;
using System.Collections.Generic;
using Godot;
using Dictionary = Godot.Collections.Dictionary;

namespace sMPGWM.Scripts.InputSystem;

internal sealed class KeyBindingConfiguration
{
    // These identifiers are consumed by gameplay code; all presentation and input defaults live in config.
    private static readonly string[] RequiredActions =
    [
        "Forwards", "Backwards", "StrafeLeft", "StrafeRight", "AfterBurner", "Break", "EnableAutoBreaking",
        "Action", "AlternateAction", "TargetSwitch", "TargetNearestSwitch", "MainMenu", "InventoryMenu",
        "MapMenu", "MessageMenu", "FireGroup2", "ReloadWeapons"
    ];

    public KeyBindingDefinition[] Actions { get; }
    public InputEvent?[,] Bindings { get; }
    public Dictionary Values { get; }

    private KeyBindingConfiguration(KeyBindingDefinition[] actions, InputEvent?[,] bindings, Dictionary values)
    {
        Actions = actions;
        Bindings = bindings;
        Values = values;
    }

    public static KeyBindingConfiguration Parse(Variant value, string source)
    {
        var path = source + " [keybindings] bindings";
        var values = RequireDictionary(value, path);
        if (values.Count == 0) throw Invalid(path, "must contain the required game actions");
        foreach (var action in RequiredActions)
            if (!values.ContainsKey(action)) throw Invalid(path + "." + action, "required game action is missing");

        var actions = new KeyBindingDefinition[values.Count];
        var orders = new HashSet<int>();
        var index = 0;
        foreach (var entry in values)
        {
            var action = RequireString(entry.Key, path + ".action");
            if (!IsActionName(action)) throw Invalid(path + "." + action, "must be an identifier and must not use the reserved ui_ prefix");
            var actionPath = path + "." + action;
            var definition = RequireDictionary(entry.Value, actionPath);
            var label = RequireString(Require(definition, "label", actionPath), actionPath + ".label");
            var section = RequireString(Require(definition, "section", actionPath), actionPath + ".section");
            var orderValue = Require(definition, "order", actionPath);
            if (orderValue.VariantType != Variant.Type.Int || orderValue.AsInt64() < 0 || orderValue.AsInt64() > int.MaxValue)
                throw Invalid(actionPath + ".order", "must be a nonnegative integer");
            var order = orderValue.AsInt32();
            if (!orders.Add(order)) throw Invalid(actionPath + ".order", "must be unique across actions");
            var deadzoneValue = Require(definition, "deadzone", actionPath);
            if (deadzoneValue.VariantType is not (Variant.Type.Float or Variant.Type.Int))
                throw Invalid(actionPath + ".deadzone", "must be a number between 0 and 1");
            var deadzone = deadzoneValue.AsDouble();
            if (!double.IsFinite(deadzone) || deadzone < 0 || deadzone > 1)
                throw Invalid(actionPath + ".deadzone", "must be a number between 0 and 1");
            actions[index++] = new KeyBindingDefinition(action, label, section) { Order = order, Deadzone = (float)deadzone };
        }

        Array.Sort(actions, (left, right) => left.Order.CompareTo(right.Order));
        var bindings = new InputEvent?[actions.Length, KeyBindingSettings.SlotCount];
        for (var row = 0; row < actions.Length; row++)
        {
            var actionPath = path + "." + actions[row].Action;
            var definition = values[actions[row].Action].AsGodotDictionary();
            bindings[row, 0] = Decode(Require(definition, "primary", actionPath), actionPath + ".primary");
            bindings[row, 1] = Decode(Require(definition, "secondary", actionPath), actionPath + ".secondary");
        }
        return new KeyBindingConfiguration(actions, bindings, values.Duplicate(true));
    }

    public static Variant MergeNewActions(Variant defaults, Variant effective)
    {
        if (defaults.VariantType != Variant.Type.Dictionary || effective.VariantType != Variant.Type.Dictionary) return effective;
        var merged = effective.AsGodotDictionary().Duplicate(true);
        var shipped = defaults.AsGodotDictionary();
        foreach (var action in new[] { "FireGroup2", "ReloadWeapons" })
        {
            if (merged.ContainsKey(action) || !shipped.ContainsKey(action)) continue;
            var definition = shipped[action].AsGodotDictionary().Duplicate(true);
            var order = definition["order"].AsInt64();
            foreach (var entry in merged)
                if (entry.Value.VariantType == Variant.Type.Dictionary && entry.Value.AsGodotDictionary().TryGetValue("order", out var existing) &&
                    existing.VariantType == Variant.Type.Int && existing.AsInt64() >= order)
                    order = checked(existing.AsInt64() + 1);
            definition["order"] = order;
            merged[action] = definition;
        }
        if (merged.TryGetValue("Action", out var primary) && primary.VariantType == Variant.Type.Dictionary)
            primary.AsGodotDictionary()["label"] = "Fire Group 1";
        return merged;
    }

    public static void RequireMatchingActions(KeyBindingConfiguration defaults, KeyBindingConfiguration effective, string source)
    {
        foreach (var action in defaults.Actions)
            if (!effective.Values.ContainsKey(action.Action))
                throw Invalid(source + " [keybindings] bindings." + action.Action, "action defined in shipped configuration is missing");
        foreach (var action in effective.Actions)
            if (!defaults.Values.ContainsKey(action.Action))
                throw Invalid(source + " [keybindings] bindings." + action.Action, "action has no shipped default");
    }

    private static InputEvent? Decode(Variant value, string path)
    {
        if (value.VariantType == Variant.Type.Nil) return null;
        var fields = RequireDictionary(value, path);
        var type = RequireString(Require(fields, "type", path), path + ".type");
        foreach (var entry in fields)
        {
            var field = RequireString(entry.Key, path);
            if (field is "type" or "ctrl" or "shift" or "alt" or "meta") continue;
            if (type == "key" && field is "key" or "physical_key") continue;
            if (type == "mouse" && field == "button") continue;
            throw Invalid(path + "." + field, "unknown binding field");
        }
        var ctrl = Modifier(fields, "ctrl", path);
        var shift = Modifier(fields, "shift", path);
        var alt = Modifier(fields, "alt", path);
        var meta = Modifier(fields, "meta", path);
        InputEventWithModifiers input;
        if (type == "key")
        {
            var physical = fields.ContainsKey("physical_key");
            if (physical == fields.ContainsKey("key")) throw Invalid(path, "provide exactly one of physical_key or key");
            var field = physical ? "physical_key" : "key";
            var name = RequireString(fields[field], path + "." + field);
            var code = OS.FindKeycodeFromString(name);
            if (!KeyBindingSettings.IsSupportedKey(code)) throw Invalid(path + "." + field, "unknown or unsupported key name: " + name);
            input = physical ? new InputEventKey { PhysicalKeycode = code } : new InputEventKey { Keycode = code };
        }
        else if (type == "mouse")
        {
            var name = RequireString(Require(fields, "button", path), path + ".button");
            if (!Enum.TryParse<MouseButton>(name, true, out var button) || button is < MouseButton.Left or > MouseButton.Xbutton2)
                throw Invalid(path + ".button", "unknown mouse button: " + name);
            input = new InputEventMouseButton { ButtonIndex = button };
        }
        else throw Invalid(path + ".type", "must be key or mouse");
        input.Device = -1;
        input.CtrlPressed = ctrl;
        input.ShiftPressed = shift;
        input.AltPressed = alt;
        input.MetaPressed = meta;
        return input;
    }

    private static bool Modifier(Dictionary fields, string name, string path)
    {
        if (!fields.TryGetValue(name, out var value)) return false;
        if (value.VariantType != Variant.Type.Bool) throw Invalid(path + "." + name, "must be true or false");
        return value.AsBool();
    }

    private static bool IsActionName(string action)
    {
        if (action.StartsWith("ui_", StringComparison.Ordinal)) return false;
        foreach (var character in action)
            if (!char.IsAsciiLetterOrDigit(character) && character != '_') return false;
        return true;
    }

    private static Variant Require(Dictionary fields, string name, string path)
    {
        if (!fields.TryGetValue(name, out var value)) throw Invalid(path + "." + name, "required value is missing");
        return value;
    }

    private static Dictionary RequireDictionary(Variant value, string path)
    {
        if (value.VariantType != Variant.Type.Dictionary) throw Invalid(path, "must be a dictionary");
        return value.AsGodotDictionary();
    }

    private static string RequireString(Variant value, string path)
    {
        if (value.VariantType != Variant.Type.String || string.IsNullOrWhiteSpace(value.AsString()))
            throw Invalid(path, "must be a nonempty string");
        return value.AsString();
    }

    private static InvalidOperationException Invalid(string path, string reason) => new(path + ": " + reason + ".");
}
