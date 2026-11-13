using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;

namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: KEYBIND-CONFIG-001 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs,sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime

[Category("Godot")] // TEST-SUPPORT: KEYBIND-CONFIG-002 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs,sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
public sealed class KeyBindingConfigurationTests // TEST-SUPPORT: KEYBIND-CONFIG-003 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs,sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
{
    [Test] // TEST-SUPPORT: KEYBIND-CONFIG-004 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
    public void ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs() // TEST-SUPPORT: KEYBIND-CONFIG-005 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true); // TEST-SUPPORT: KEYBIND-CONFIG-006 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        var original = ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true); // TEST-SUPPORT: KEYBIND-CONFIG-007 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        var edited = shipped.Duplicate(true); // TEST-SUPPORT: KEYBIND-CONFIG-008 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        var forward = edited["Forwards"].AsGodotDictionary(); // TEST-SUPPORT: KEYBIND-CONFIG-009 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        forward["label"] = "Custom forward"; // TEST-SUPPORT: KEYBIND-CONFIG-010 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        forward["section"] = "Flight"; // TEST-SUPPORT: KEYBIND-CONFIG-011 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        forward["order"] = 100; // TEST-SUPPORT: KEYBIND-CONFIG-012 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        forward["deadzone"] = 0.15; // TEST-SUPPORT: KEYBIND-CONFIG-013 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        forward["primary"] = new Godot.Collections.Dictionary { ["type"] = "key", ["physical_key"] = "F10" }; // TEST-SUPPORT: KEYBIND-CONFIG-014 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        var uiEvents = InputMap.ActionGetEvents("ui_accept").Count; // TEST-SUPPORT: KEYBIND-CONFIG-015 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        var oldProjectSetting = ProjectSettings.HasSetting("input/Forwards") ? ProjectSettings.GetSetting("input/Forwards") : default; // TEST-SUPPORT: KEYBIND-CONFIG-016 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        KeyBindingMenuController? menu = null; // TEST-SUPPORT: KEYBIND-CONFIG-017 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        try // TEST-SUPPORT: KEYBIND-CONFIG-018 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        {
            ProjectSettings.SetSetting("input/Forwards", new Godot.Collections.Dictionary { ["deadzone"] = 0.99, ["events"] = new Godot.Collections.Array { new InputEventKey { PhysicalKeycode = Key.F12 } } }); // TEST-SUPPORT: KEYBIND-CONFIG-019 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            InputMap.EraseAction("Forwards"); // TEST-SUPPORT: KEYBIND-CONFIG-020 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            InputMap.ActionAddEvent("Action", new InputEventJoypadButton { ButtonIndex = JoyButton.A }); // TEST-SUPPORT: KEYBIND-CONFIG-021 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            KeyBindingSettings.LoadConfiguration(shipped, edited, FilePaths.UserConfigPath); // TEST-SUPPORT: KEYBIND-CONFIG-022 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(KeyBindingCatalog.Actions[^1].Action, Is.EqualTo("Forwards")); // TEST-SUPPORT: KEYBIND-CONFIG-023 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(KeyBindingCatalog.Actions[^1].Label, Is.EqualTo("Custom forward")); // TEST-SUPPORT: KEYBIND-CONFIG-024 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(KeyBindingCatalog.Actions[^1].Section, Is.EqualTo("Flight")); // TEST-SUPPORT: KEYBIND-CONFIG-025 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(InputMap.ActionGetDeadzone("Forwards"), Is.EqualTo(0.15f).Within(0.0001f)); // TEST-SUPPORT: KEYBIND-CONFIG-026 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = true }, "Forwards", true), Is.True); // TEST-SUPPORT: KEYBIND-CONFIG-027 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(InputMap.ActionGetEvents("Action").Count, Is.EqualTo(1)); // TEST-SUPPORT: KEYBIND-CONFIG-028 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(InputMap.ActionGetEvents("ui_accept").Count, Is.EqualTo(uiEvents)); // TEST-SUPPORT: KEYBIND-CONFIG-029 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            var defaults = KeyBindingSettings.CreateDefaultBindings(); // TEST-SUPPORT: KEYBIND-CONFIG-030 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(((InputEventKey)defaults[KeyBindingCatalog.Actions.Length - 1, 0]!).PhysicalKeycode, Is.EqualTo(Key.W)); // TEST-SUPPORT: KEYBIND-CONFIG-031 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            var saved = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-CONFIG-032 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            var savedForward = saved["Forwards"].AsGodotDictionary(); // TEST-SUPPORT: KEYBIND-CONFIG-033 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(savedForward["label"].AsString(), Is.EqualTo("Custom forward")); // TEST-SUPPORT: KEYBIND-CONFIG-034 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(savedForward["section"].AsString(), Is.EqualTo("Flight")); // TEST-SUPPORT: KEYBIND-CONFIG-035 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(savedForward["order"].AsInt32(), Is.EqualTo(100)); // TEST-SUPPORT: KEYBIND-CONFIG-036 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(savedForward["deadzone"].AsDouble(), Is.EqualTo(0.15).Within(0.0001)); // TEST-SUPPORT: KEYBIND-CONFIG-037 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            menu = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn").Instantiate<KeyBindingMenuController>(); // TEST-SUPPORT: KEYBIND-CONFIG-038 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(menu); // TEST-SUPPORT: KEYBIND-CONFIG-039 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer"); // TEST-SUPPORT: KEYBIND-CONFIG-040 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            var flight = sections.GetChild(sections.GetChildCount() - 1); // TEST-SUPPORT: KEYBIND-CONFIG-041 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(flight.GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Flight")); // TEST-SUPPORT: KEYBIND-CONFIG-042 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            Assert.That(flight.GetNode<Label>("RowsContainer/Forwards/ActionLabel").Text, Is.EqualTo("Custom forward")); // TEST-SUPPORT: KEYBIND-CONFIG-043 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        }
        finally // TEST-SUPPORT: KEYBIND-CONFIG-044 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        {
            menu?.Free(); // TEST-SUPPORT: KEYBIND-CONFIG-045 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            ProjectSettings.SetSetting("input/Forwards", oldProjectSetting); // TEST-SUPPORT: KEYBIND-CONFIG-046 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
            KeyBindingSettings.LoadConfiguration(shipped, original, FilePaths.UserConfigPath); // TEST-SUPPORT: KEYBIND-CONFIG-047 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.ConfigurationControlsMetadataOrderDefaultsAndRuntimeInputs
        }
    }

    [TestCase("missing-effective-action", "Forwards", false)] // TEST-SUPPORT: KEYBIND-CONFIG-048 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("missing-shipped-action", "Forwards", true)] // TEST-SUPPORT: KEYBIND-CONFIG-049 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("empty-effective", "bindings", false)] // TEST-SUPPORT: KEYBIND-CONFIG-050 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("empty-shipped", "bindings", true)] // TEST-SUPPORT: KEYBIND-CONFIG-051 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("missing-label", "label", false)] // TEST-SUPPORT: KEYBIND-CONFIG-052 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("invalid-deadzone", "deadzone", false)] // TEST-SUPPORT: KEYBIND-CONFIG-053 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("missing-primary", "primary", false)] // TEST-SUPPORT: KEYBIND-CONFIG-054 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("malformed-primary", "primary", false)] // TEST-SUPPORT: KEYBIND-CONFIG-055 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    [TestCase("unknown-key", "primary", false)] // TEST-SUPPORT: KEYBIND-CONFIG-056 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    public void InvalidConfigurationIsRejectedBeforeChangingRuntime(string scenario, string field, bool shippedError) // TEST-SUPPORT: KEYBIND-CONFIG-057 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true); // TEST-SUPPORT: KEYBIND-CONFIG-058 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        var effective = ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey).AsGodotDictionary().Duplicate(true); // TEST-SUPPORT: KEYBIND-CONFIG-059 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        var previousActions = KeyBindingCatalog.Actions; // TEST-SUPPORT: KEYBIND-CONFIG-060 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        var previousBindings = GD.VarToStr(KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings())); // TEST-SUPPORT: KEYBIND-CONFIG-061 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        var previousForwardEvents = InputMap.ActionGetEvents("Forwards"); // TEST-SUPPORT: KEYBIND-CONFIG-062 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        var forward = effective["Forwards"].AsGodotDictionary(); // TEST-SUPPORT: KEYBIND-CONFIG-063 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        switch (scenario) // TEST-SUPPORT: KEYBIND-CONFIG-064 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        {
            case "missing-effective-action": effective.Remove("Forwards"); break; // TEST-SUPPORT: KEYBIND-CONFIG-065 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "missing-shipped-action": shipped.Remove("Forwards"); break; // TEST-SUPPORT: KEYBIND-CONFIG-066 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "empty-effective": effective.Clear(); break; // TEST-SUPPORT: KEYBIND-CONFIG-067 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "empty-shipped": shipped.Clear(); break; // TEST-SUPPORT: KEYBIND-CONFIG-068 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "missing-label": forward.Remove("label"); break; // TEST-SUPPORT: KEYBIND-CONFIG-069 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "invalid-deadzone": forward["deadzone"] = 2.0; break; // TEST-SUPPORT: KEYBIND-CONFIG-070 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "missing-primary": forward.Remove("primary"); break; // TEST-SUPPORT: KEYBIND-CONFIG-071 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "malformed-primary": forward["primary"] = "invalid"; break; // TEST-SUPPORT: KEYBIND-CONFIG-072 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            case "unknown-key": forward["primary"] = new Godot.Collections.Dictionary { ["type"] = "key", ["physical_key"] = "NotARealKey" }; break; // TEST-SUPPORT: KEYBIND-CONFIG-073 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        }
        var error = Assert.Throws<InvalidOperationException>(() => KeyBindingSettings.LoadConfiguration(shipped, effective, FilePaths.UserConfigPath)); // TEST-SUPPORT: KEYBIND-CONFIG-074 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        Assert.That(error!.Message, Does.Contain(shippedError ? FilePaths.SystemConfigPath : FilePaths.UserConfigPath)); // TEST-SUPPORT: KEYBIND-CONFIG-075 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        Assert.That(error.Message, Does.Contain(field)); // TEST-SUPPORT: KEYBIND-CONFIG-076 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        Assert.That(KeyBindingCatalog.Actions, Is.SameAs(previousActions)); // TEST-SUPPORT: KEYBIND-CONFIG-077 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        Assert.That(GD.VarToStr(KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings())), Is.EqualTo(previousBindings)); // TEST-SUPPORT: KEYBIND-CONFIG-078 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        Assert.That(InputMap.ActionGetEvents("Forwards").Count, Is.EqualTo(previousForwardEvents.Count)); // TEST-SUPPORT: KEYBIND-CONFIG-079 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
        for (var index = 0; index < previousForwardEvents.Count; index++) // TEST-SUPPORT: KEYBIND-CONFIG-080 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
            Assert.That(KeyBindingSettings.SameBinding(InputMap.ActionGetEvents("Forwards")[index], previousForwardEvents[index]), Is.True); // TEST-SUPPORT: KEYBIND-CONFIG-081 | Tests: sMPGWM.Tests.UiSystem.KeyBindingConfigurationTests.InvalidConfigurationIsRejectedBeforeChangingRuntime
    }
}

