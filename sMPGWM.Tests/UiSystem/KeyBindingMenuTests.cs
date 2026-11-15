using Godot;
using Dictionary = Godot.Collections.Dictionary;
using FileAccess = System.IO.FileAccess;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.SettingsMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class KeyBindingMenuTests
{
    [Test]
    public void BindingUniquenessIsEnforcedWithinEachContextIncludingSecondarySlots()
    {
        var bindings = KeyBindingSettings.CreateDefaultBindings();
        Assert.That(KeyBindingSettings.IsValid(bindings), Is.True);
        var fire = FindAction("PrimaryFire");
        var action = FindAction("Action");
        Assert.That(KeyBindingCatalog.Actions[fire].Context, Is.EqualTo(InputContext.Flight));
        Assert.That(KeyBindingCatalog.Actions[action].Context, Is.EqualTo(InputContext.AbilityTargeting));
        Assert.That(KeyBindingSettings.SameBinding(bindings[fire, 0], bindings[action, 0]), Is.True);

        var forward = FindAction("Forwards");
        bindings[forward, 1] = bindings[FindAction("InventoryMenu"), 0];
        Assert.That(KeyBindingSettings.IsValid(bindings), Is.True, "Different contexts may share a binding.");
        bindings[forward, 1] = bindings[fire, 0];
        Assert.That(KeyBindingSettings.IsValid(bindings), Is.False);
        Assert.That(GameConfig.SaveKeyBindings(bindings), Is.EqualTo(Error.InvalidData));
        bindings[forward, 1] = bindings[forward, 0];
        Assert.That(KeyBindingSettings.IsValid(bindings), Is.False, "Slots of one action must also be unique.");
    }

    [Test]
    public void NormalizationPreservesModifiersAndMouseButtons()
    {
        using var plain = new InputEventKey { PhysicalKeycode = Key.T, Pressed = true };
        using var shifted = new InputEventKey { PhysicalKeycode = Key.T, ShiftPressed = true, Pressed = true };
        using var mouse = new InputEventMouseButton { ButtonIndex = MouseButton.Right, CtrlPressed = true, Pressed = true };
        using var normalizedPlain = KeyBindingSettings.Normalize(plain);
        using var normalizedShifted = KeyBindingSettings.Normalize(shifted);
        using var normalizedMouse = KeyBindingSettings.Normalize(mouse);
        Assert.That(normalizedPlain, Is.Not.Null);
        Assert.That(normalizedShifted, Is.TypeOf<InputEventKey>());
        Assert.That(((InputEventKey)normalizedShifted!).ShiftPressed, Is.True);
        Assert.That(KeyBindingSettings.SameBinding(normalizedPlain, normalizedShifted), Is.False);
        Assert.That(normalizedMouse, Is.TypeOf<InputEventMouseButton>());
        Assert.That(((InputEventMouseButton)normalizedMouse!).ButtonIndex, Is.EqualTo(MouseButton.Right));
        Assert.That(((InputEventMouseButton)normalizedMouse).CtrlPressed, Is.True);
        plain.Echo = true;
        Assert.That(KeyBindingSettings.Normalize(plain), Is.Null);
        plain.Echo = false;
        plain.Pressed = false;
        Assert.That(KeyBindingSettings.Normalize(plain), Is.Null);
        using var modifier = new InputEventKey { PhysicalKeycode = Key.Shift, Pressed = true, ShiftPressed = true };
        Assert.That(KeyBindingSettings.Normalize(modifier), Is.Null);
        Assert.That(KeyBindingSettings.Format(null), Is.Not.Empty);
    }

    [Test]
    public void SaveReloadAndRestorePreserveDefaultsAndCompatibility()
    {
        var configPath = ProjectSettings.GlobalizePath(FilePaths.SystemConfigPath);
        var originalConfig = File.ReadAllText(configPath);
        var originalBindings = KeyBindingSettings.CreateCurrentBindings();
        var defaults = KeyBindingSettings.CreateDefaultBindings();
        try
        {
            using var config = new ConfigFile();
            Assert.That(config.Load(FilePaths.SystemConfigPath), Is.EqualTo(Error.Ok));
            config.SetValue("config", "reload_config_on_start", false);
            Assert.That(config.Save(FilePaths.SystemConfigPath), Is.EqualTo(Error.Ok));
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok));
            GameConfig.Load();
            var checksum = GameConfig.ConfigDataChecksum;
            var draft = KeyBindingSettings.CreateCurrentBindings();
            var forward = FindAction("Forwards");
            var message = FindAction("MessageMenu");
            draft[forward, 0] = new InputEventKey { PhysicalKeycode = Key.K, CtrlPressed = true };
            draft[forward, 1] = new InputEventMouseButton { ButtonIndex = MouseButton.Xbutton1 };
            draft[message, 0] = null;
            draft[message, 1] = new InputEventKey { PhysicalKeycode = Key.F9 };
            Assert.That(GameConfig.SaveKeyBindings(draft), Is.EqualTo(Error.Ok));
            InputMap.ActionEraseEvents("Forwards");
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok));
            GameConfig.Load();
            var reloaded = KeyBindingSettings.CreateCurrentBindings();
            AssertBindingsEqual(draft, reloaded);
            Assert.That(InputMap.ActionGetEvents("Forwards").Count, Is.EqualTo(2));
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, CtrlPressed = true, Pressed = true }, "Forwards", true), Is.True);
            Assert.That(GameConfig.ConfigDataChecksum, Is.EqualTo(checksum));
            AssertBindingsEqual(defaults, KeyBindingSettings.CreateDefaultBindings());
            Assert.That(GameConfig.SaveKeyBindings(defaults), Is.EqualTo(Error.Ok));
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok));
            GameConfig.Load();
            AssertBindingsEqual(defaults, KeyBindingSettings.CreateCurrentBindings());
        }
        finally
        {
            File.WriteAllText(configPath, originalConfig);
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok));
            GameConfig.Load();
            Assert.That(GameConfig.SaveKeyBindings(originalBindings), Is.EqualTo(Error.Ok));
        }
    }

    [Test]
    public void RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch()
    {
        var originalBindings = KeyBindingSettings.CreateCurrentBindings();
        var scene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/settings/settings_menu_container.tscn");
        var settings = scene.Instantiate<SettingsMenuController>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(settings);
        try
        {
            Assert.That(settings.GetTabCount(), Is.EqualTo(4));
            Assert.That(settings.GetTabTitle(0), Is.EqualTo("Keybindings"));
            Assert.That(settings.GetTabTitle(1), Is.EqualTo("Audio"));
            Assert.That(settings.GetTabTitle(2), Is.EqualTo("Graphics"));
            Assert.That(settings.GetTabTitle(3), Is.EqualTo("Game"));
            var menu = settings.GetNode<KeyBindingMenuController>("Keybindings");
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer");
            Assert.That(sections.GetChildCount(), Is.EqualTo(3));
            Assert.That(sections.GetChild(0).GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Flight"));
            Assert.That(sections.GetChild(1).GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Ability Targeting"));
            Assert.That(sections.GetChild(2).GetNode<Label>("SectionHeader/SectionLabel").Text, Is.EqualTo("Menu Shortcuts"));
            var rowCount = 0;
            foreach (var section in sections.GetChildren())
                rowCount += section.GetNode<VBoxContainer>("RowsContainer").GetChildCount();
            Assert.That(rowCount, Is.EqualTo(KeyBindingCatalog.Actions.Length));
            Assert.That(rowCount, Is.EqualTo(17));
            var firstRows = sections.GetChild(0).GetNode<VBoxContainer>("RowsContainer");
            var row = firstRows.GetChild(0);
            var primary = row.GetNode<Button>("PrimaryBindingButton");
            var save = menu.GetNode<Button>("MenuContainer/ActionsContainer/ApplyButton");
            var restore = menu.GetNode<Button>("MenuContainer/ActionsContainer/ResetDefaultsButton");
            var before = KeyBindingSettings.CreateCurrentBindings();
            primary.EmitSignal(BaseButton.SignalName.Pressed);
            menu._Input(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true });
            Assert.That(primary.Text, Does.Contain("K"));
            AssertBindingsEqual(before, KeyBindingSettings.CreateCurrentBindings());
            settings.CurrentTab = 1;
            settings.CurrentTab = 0;
            Assert.That(primary.Text, Does.Contain("K"));
            save.EmitSignal(BaseButton.SignalName.Pressed);
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true }, KeyBindingCatalog.Actions[0].Action, true), Is.True);
            restore.EmitSignal(BaseButton.SignalName.Pressed);
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true }, KeyBindingCatalog.Actions[0].Action, true), Is.True);
            save.EmitSignal(BaseButton.SignalName.Pressed);
            AssertBindingsEqual(KeyBindingSettings.CreateDefaultBindings(), KeyBindingSettings.CreateCurrentBindings());
        }
        finally
        {
            settings.Free();
            Assert.That(GameConfig.SaveKeyBindings(originalBindings), Is.EqualTo(Error.Ok));
        }
    }


    [Test]
    public void ConflictsCancelClearAndUnsavedClosePreserveSavedBindings()
    {
        var original = KeyBindingSettings.CreateCurrentBindings();
        var scene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn");
        var menu = scene.Instantiate<KeyBindingMenuController>();
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        root.AddChild(menu);
        try
        {
            var row = menu.GetNode("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer/Section0/RowsContainer/Forwards");
            var primary = row.GetNode<Button>("PrimaryBindingButton");
            var secondary = row.GetNode<Button>("SecondaryBindingButton");
            var save = menu.GetNode<Button>("MenuContainer/ActionsContainer/ApplyButton");
            var cancel = menu.GetNode<Button>("MenuContainer/CaptureControls/CancelCaptureButton");
            var clear = menu.GetNode<Button>("MenuContainer/CaptureControls/ClearBindingButton");
            primary.EmitSignal(BaseButton.SignalName.Pressed);
            menu._Input(new InputEventKey { PhysicalKeycode = Key.S, Pressed = true });
            menu._Input(new InputEventKey { PhysicalKeycode = Key.S, Pressed = false });
            Assert.That(save.Disabled, Is.True);
            Assert.That(menu.GetNode<Label>("MenuContainer/BindingStatus").Text, Does.Contain("assigned to both"));
            save.EmitSignal(BaseButton.SignalName.Pressed);
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings());
            primary.EmitSignal(BaseButton.SignalName.Pressed);
            cancel.EmitSignal(BaseButton.SignalName.Pressed);
            Assert.That(primary.Text, Is.EqualTo("S"));
            primary.EmitSignal(BaseButton.SignalName.Pressed);
            clear.EmitSignal(BaseButton.SignalName.Pressed);
            Assert.That(primary.Text, Is.EqualTo(KeyBindingSettings.Format(null)));
            Assert.That(save.Disabled, Is.False);
            secondary.EmitSignal(BaseButton.SignalName.Pressed);
            menu._Input(new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = true });
            menu._Input(new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = false });
            Assert.That(secondary.Text, Does.Contain("Esc"));
            Assert.That(save.Disabled, Is.False, "Flight can share a binding with Menu Shortcuts.");
            secondary.EmitSignal(BaseButton.SignalName.Pressed);
            menu._Input(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = true });
            menu._Input(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = false });
            save.EmitSignal(BaseButton.SignalName.Pressed);
            var saved = KeyBindingSettings.CreateCurrentBindings();
            Assert.That(saved[FindAction("Forwards"), 0], Is.Null);
            Assert.That(((InputEventKey)saved[FindAction("Forwards"), 1]!).PhysicalKeycode, Is.EqualTo(Key.F10));
            secondary.EmitSignal(BaseButton.SignalName.Pressed);
            clear.EmitSignal(BaseButton.SignalName.Pressed);
            menu.Free();
            menu = scene.Instantiate<KeyBindingMenuController>();
            root.AddChild(menu);
            Assert.That(menu.GetNode<Button>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer/Section0/RowsContainer/Forwards/SecondaryBindingButton").Text, Is.EqualTo("F10"));
            AssertBindingsEqual(saved, KeyBindingSettings.CreateCurrentBindings());
        }
        finally
        {
            menu.Free();
            Assert.That(GameConfig.SaveKeyBindings(original), Is.EqualTo(Error.Ok));
        }
    }


    [Test]
    public void FailedSaveRollsBackAndMalformedBindingsAreRejected()
    {
        var original = KeyBindingSettings.CreateCurrentBindings();
        var stored = GD.VarToStr(ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey));
        var draft = KeyBindingSettings.CreateCurrentBindings();
        draft[FindAction("Forwards"), 0] = new InputEventKey { PhysicalKeycode = Key.F12 };
        var path = ProjectSettings.GlobalizePath(FilePaths.UserConfigPath);
        try
        {
            using (var lockedConfig = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.That(GameConfig.SaveKeyBindings(draft), Is.Not.EqualTo(Error.Ok));
            Assert.That(GD.VarToStr(ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey)), Is.EqualTo(stored));
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings());
            var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey);
            Assert.Throws<InvalidOperationException>(() => KeyBindingSettings.LoadConfiguration(shipped, new Dictionary { ["Forwards"] = "invalid stored binding" }, FilePaths.UserConfigPath));
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings());
        }
        finally
        {
            Assert.That(GameConfig.SaveKeyBindings(original), Is.EqualTo(Error.Ok));
        }
    }

    private static int FindAction(string action)
    {
        for (var index = 0; index < KeyBindingCatalog.Actions.Length; index++)
            if (KeyBindingCatalog.Actions[index].Action == action) return index;
        throw new AssertionException("Unknown action: " + action);
    }

    private static void AssertBindingsEqual(InputEvent?[,] expected, InputEvent?[,] actual)
    {
        Assert.That(actual.GetLength(0), Is.EqualTo(expected.GetLength(0)));
        Assert.That(actual.GetLength(1), Is.EqualTo(expected.GetLength(1)));
        for (var action = 0; action < expected.GetLength(0); action++)
            for (var slot = 0; slot < expected.GetLength(1); slot++)
                Assert.That(KeyBindingSettings.SameBinding(expected[action, slot], actual[action, slot]), Is.True, $"Action {action}, slot {slot}");
    }
}

