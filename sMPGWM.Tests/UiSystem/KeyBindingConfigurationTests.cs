using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class KeyBindingConfigurationTests
{
    [Test]
    public void ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs()
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true);
        var original = ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true);
        var edited = shipped.Duplicate(true);
        var forward = edited["Forwards"].AsGodotDictionary();
        forward["label"] = "Custom forward";
        forward["section"] = "Flight";
        forward["order"] = 100;
        forward["deadzone"] = 0.15;
        forward["primary"] = new Godot.Collections.Dictionary { ["type"] = "key", ["physical_key"] = "F10" };
        var uiEvents = InputMap.ActionGetEvents("ui_accept").Count;
        var oldProjectSetting = ProjectSettings.HasSetting("input/Forwards") ? ProjectSettings.GetSetting("input/Forwards") : default;
        KeyBindingMenuController? menu = null;
        try
        {
            ProjectSettings.SetSetting("input/Forwards", new Godot.Collections.Dictionary { ["deadzone"] = 0.99, ["events"] = new Godot.Collections.Array { new InputEventKey { PhysicalKeycode = Key.F12 } } });
            InputMap.EraseAction("Forwards");
            InputMap.ActionAddEvent("Action", new InputEventJoypadButton { ButtonIndex = JoyButton.A });
            KeyBindingSettings.LoadConfiguration(shipped, edited, FilePaths.UserConfigPath);
            Assert.That(KeyBindingCatalog.Actions[^1].Action, Is.EqualTo("Forwards"));
            Assert.That(KeyBindingCatalog.Actions[^1].Label, Is.EqualTo("Custom forward"));
            Assert.That(KeyBindingCatalog.Actions[^1].Section, Is.EqualTo("Flight"));
            Assert.That(InputMap.ActionGetDeadzone("Forwards"), Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = true }, "Forwards", true), Is.True);
            Assert.That(InputMap.ActionGetEvents("Action").Count, Is.EqualTo(1));
            Assert.That(InputMap.ActionGetEvents("ui_accept").Count, Is.EqualTo(uiEvents));
            var defaults = KeyBindingSettings.CreateDefaultBindings();
            Assert.That(((InputEventKey)defaults[KeyBindingCatalog.Actions.Length - 1, 0]!).PhysicalKeycode, Is.EqualTo(Key.W));
            var saved = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings());
            var savedForward = saved["Forwards"].AsGodotDictionary();
            Assert.That(savedForward["label"].AsString(), Is.EqualTo("Custom forward"));
            Assert.That(savedForward["section"].AsString(), Is.EqualTo("Flight"));
            Assert.That(savedForward["order"].AsInt32(), Is.EqualTo(100));
            Assert.That(savedForward["deadzone"].AsDouble(), Is.EqualTo(0.15).Within(0.0001));
            menu = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn").Instantiate<KeyBindingMenuController>();
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(menu);
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer");
            var flight = sections.GetChild(sections.GetChildCount() - 1);
            Assert.That(flight.GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Flight"));
            Assert.That(flight.GetNode<Label>("RowsContainer/Forwards/ActionLabel").Text, Is.EqualTo("Custom forward"));
        }
        finally
        {
            menu?.Free();
            ProjectSettings.SetSetting("input/Forwards", oldProjectSetting);
            KeyBindingSettings.LoadConfiguration(shipped, original, FilePaths.UserConfigPath);
        }
    }

    [TestCase("missing-effective-action", "Forwards", false)]
    [TestCase("missing-shipped-action", "Forwards", true)]
    [TestCase("empty-effective", "bindings", false)]
    [TestCase("empty-shipped", "bindings", true)]
    [TestCase("missing-label", "label", false)]
    [TestCase("invalid-deadzone", "deadzone", false)]
    [TestCase("missing-primary", "primary", false)]
    [TestCase("malformed-primary", "primary", false)]
    [TestCase("unknown-key", "primary", false)]
    public void InvalidConfigurationIsRejectedBeforeChangingRuntime(string scenario, string field, bool shippedError)
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true);
        var effective = ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true);
        var previousActions = KeyBindingCatalog.Actions;
        var previousBindings = GD.VarToStr(KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings()));
        var previousForwardEvents = InputMap.ActionGetEvents("Forwards");
        var forward = effective["Forwards"].AsGodotDictionary();
        switch (scenario)
        {
            case "missing-effective-action": effective.Remove("Forwards"); break;
            case "missing-shipped-action": shipped.Remove("Forwards"); break;
            case "empty-effective": effective.Clear(); break;
            case "empty-shipped": shipped.Clear(); break;
            case "missing-label": forward.Remove("label"); break;
            case "invalid-deadzone": forward["deadzone"] = 2.0; break;
            case "missing-primary": forward.Remove("primary"); break;
            case "malformed-primary": forward["primary"] = "invalid"; break;
            case "unknown-key": forward["primary"] = new Godot.Collections.Dictionary { ["type"] = "key", ["physical_key"] = "NotARealKey" }; break;
        }
        var error = Assert.Throws<InvalidOperationException>(() => KeyBindingSettings.LoadConfiguration(shipped, effective, FilePaths.UserConfigPath));
        Assert.That(error!.Message, Does.Contain(shippedError ? FilePaths.SystemConfigPath : FilePaths.UserConfigPath));
        Assert.That(error.Message, Does.Contain(field));
        Assert.That(KeyBindingCatalog.Actions, Is.SameAs(previousActions));
        Assert.That(GD.VarToStr(KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings())), Is.EqualTo(previousBindings));
        Assert.That(InputMap.ActionGetEvents("Forwards").Count, Is.EqualTo(previousForwardEvents.Count));
        for (var index = 0; index < previousForwardEvents.Count; index++)
            Assert.That(KeyBindingSettings.SameBinding(InputMap.ActionGetEvents("Forwards")[index], previousForwardEvents[index]), Is.True);
    }
}

