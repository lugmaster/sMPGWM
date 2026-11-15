using Godot;
using Array = Godot.Collections.Array;
using Dictionary = Godot.Collections.Dictionary;
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
        foreach (var entry in edited)
        {
            var definition = entry.Value.AsGodotDictionary();
            if (definition["sectionId"].AsString() == "flight") definition["sectionDisplayName"] = "Custom Flight";
        }
        forward["order"] = 100;
        forward["deadzone"] = 0.15;
        forward["primary"] = new Dictionary { ["type"] = "key", ["physical_key"] = "F10" };
        var uiEvents = InputMap.ActionGetEvents("ui_accept").Count;
        var oldProjectSetting = ProjectSettings.HasSetting("input/Forwards") ? ProjectSettings.GetSetting("input/Forwards") : default;
        KeyBindingMenuController? menu = null;
        try
        {
            ProjectSettings.SetSetting("input/Forwards", new Dictionary { ["deadzone"] = 0.99, ["events"] = new Array { new InputEventKey { PhysicalKeycode = Key.F12 } } });
            InputMap.EraseAction("Forwards");
            InputMap.ActionAddEvent("Action", new InputEventJoypadButton { ButtonIndex = JoyButton.A });
            KeyBindingSettings.LoadConfiguration(shipped, edited, FilePaths.UserConfigPath);
            Assert.That(KeyBindingCatalog.Actions[^1].Action, Is.EqualTo("Forwards"));
            Assert.That(KeyBindingCatalog.Actions[^1].Label, Is.EqualTo("Custom forward"));
            Assert.That(KeyBindingCatalog.Actions[^1].SectionId, Is.EqualTo("flight"));
            Assert.That(KeyBindingCatalog.Actions[^1].SectionDisplayName, Is.EqualTo("Custom Flight"));
            Assert.That(KeyBindingCatalog.Actions[^1].Context, Is.EqualTo(InputContext.Flight));
            Assert.That(InputMap.ActionGetDeadzone("Forwards"), Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = true }, "Forwards", true), Is.True);
            Assert.That(InputMap.ActionGetEvents("Action").Count, Is.EqualTo(1));
            Assert.That(InputMap.ActionGetEvents("ui_accept").Count, Is.EqualTo(uiEvents));
            var defaults = KeyBindingSettings.CreateDefaultBindings();
            Assert.That(((InputEventKey)defaults[KeyBindingCatalog.Actions.Length - 1, 0]!).PhysicalKeycode, Is.EqualTo(Key.W));
            var saved = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings());
            var savedForward = saved["Forwards"].AsGodotDictionary();
            Assert.That(savedForward["label"].AsString(), Is.EqualTo("Custom forward"));
            Assert.That(savedForward["sectionId"].AsString(), Is.EqualTo("flight"));
            Assert.That(savedForward["sectionDisplayName"].AsString(), Is.EqualTo("Custom Flight"));
            Assert.That(savedForward.ContainsKey("section"), Is.False);
            Assert.That(savedForward["order"].AsInt32(), Is.EqualTo(100));
            Assert.That(savedForward["deadzone"].AsDouble(), Is.EqualTo(0.15).Within(0.0001));
            KeyBindingSettings.LoadConfiguration(shipped, saved, FilePaths.UserConfigPath);
            menu = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn").Instantiate<KeyBindingMenuController>();
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(menu);
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer");
            var flight = sections.GetChild(0);
            Assert.That(flight.GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Custom Flight"));
            Assert.That(flight.GetNode<Label>("RowsContainer/Forwards/ActionLabel").Text, Is.EqualTo("Custom forward"));
        }
        finally
        {
            menu?.Free();
            ProjectSettings.SetSetting("input/Forwards", oldProjectSetting);
            KeyBindingSettings.LoadConfiguration(shipped, original, FilePaths.UserConfigPath);
        }
    }

    [Test]
    public void SectionsWithTheSameDisplayNameKeepSeparateIdentityAndBindingValidation()
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey);
        var original = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings());
        var edited = shipped.AsGodotDictionary().Duplicate(true);
        foreach (var entry in edited)
            entry.Value.AsGodotDictionary()["sectionDisplayName"] = "Controls";
        KeyBindingMenuController? menu = null;
        try
        {
            KeyBindingSettings.LoadConfiguration(shipped, edited, FilePaths.UserConfigPath);
            Assert.That(KeyBindingSettings.IsValid(KeyBindingSettings.CreateCurrentBindings()), Is.True);
            menu = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn").Instantiate<KeyBindingMenuController>();
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(menu);
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer");
            Assert.That(sections.GetChildCount(), Is.EqualTo(3));
            foreach (var section in sections.GetChildren())
                Assert.That(section.GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Controls"));
            Assert.That(sections.GetChild(0).HasNode("RowsContainer/PrimaryFire"), Is.True);
            Assert.That(sections.GetChild(1).HasNode("RowsContainer/Action"), Is.True);

            var bindings = KeyBindingSettings.CreateCurrentBindings();
            bindings[0, 1] = bindings[1, 0];
            Assert.That(KeyBindingSettings.IsValid(bindings), Is.False);
        }
        finally
        {
            menu?.Free();
            KeyBindingSettings.LoadConfiguration(shipped, original, FilePaths.UserConfigPath);
        }
    }

    [TestCase("missing-effective-action", "Forwards", false)]
    [TestCase("missing-shipped-action", "Forwards", true)]
    [TestCase("empty-effective", "bindings", false)]
    [TestCase("empty-shipped", "bindings", true)]
    [TestCase("missing-label", "label", false)]
    [TestCase("missing-section-id", "sectionId", false)]
    [TestCase("missing-section-display-name", "sectionDisplayName", false)]
    [TestCase("empty-section-display-name", "sectionDisplayName", false)]
    [TestCase("inconsistent-section-display-name", "sectionDisplayName", false)]
    [TestCase("unknown-context", "sectionId", false)]
    [TestCase("changed-context", "sectionId", false)]
    [TestCase("same-context-duplicate", "assigned to both", false)]
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
            case "missing-section-id": forward.Remove("sectionId"); break;
            case "missing-section-display-name": forward.Remove("sectionDisplayName"); break;
            case "empty-section-display-name": forward["sectionDisplayName"] = " "; break;
            case "inconsistent-section-display-name": forward["sectionDisplayName"] = "Different name"; break;
            case "unknown-context": forward["sectionId"] = "Flight"; break;
            case "changed-context": forward["sectionId"] = "inventory_interaction"; break;
            case "same-context-duplicate": forward["primary"] = effective["Backwards"].AsGodotDictionary()["primary"]; break;
            case "missing-label": forward.Remove("label"); break;
            case "invalid-deadzone": forward["deadzone"] = 2.0; break;
            case "missing-primary": forward.Remove("primary"); break;
            case "malformed-primary": forward["primary"] = "invalid"; break;
            case "unknown-key": forward["primary"] = new Dictionary { ["type"] = "key", ["physical_key"] = "NotARealKey" }; break;
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

