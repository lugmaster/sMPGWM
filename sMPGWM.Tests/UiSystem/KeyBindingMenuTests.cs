using Godot;
using sMPGWM.Scripts.FileLoadingSystem;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.KeyBindingSystem;
using sMPGWM.Scripts.UiSystem.SharedUiSystems.SettingsMenuSystem;

namespace sMPGWM.Tests.UiSystem; // TEST-SUPPORT: KEYBIND-MENU-001 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected

[Category("Godot")] // TEST-SUPPORT: KEYBIND-MENU-002 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
public sealed class KeyBindingMenuTests // TEST-SUPPORT: KEYBIND-MENU-003 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
{
    [Test] // TEST-SUPPORT: KEYBIND-MENU-004 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
    public void NormalizationPreservesModifiersAndMouseButtons() // TEST-SUPPORT: KEYBIND-MENU-005 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
    {
        using var plain = new InputEventKey { PhysicalKeycode = Key.T, Pressed = true }; // TEST-SUPPORT: KEYBIND-MENU-006 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var shifted = new InputEventKey { PhysicalKeycode = Key.T, ShiftPressed = true, Pressed = true }; // TEST-SUPPORT: KEYBIND-MENU-007 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var mouse = new InputEventMouseButton { ButtonIndex = MouseButton.Right, CtrlPressed = true, Pressed = true }; // TEST-SUPPORT: KEYBIND-MENU-008 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var normalizedPlain = KeyBindingSettings.Normalize(plain); // TEST-SUPPORT: KEYBIND-MENU-009 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var normalizedShifted = KeyBindingSettings.Normalize(shifted); // TEST-SUPPORT: KEYBIND-MENU-010 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var normalizedMouse = KeyBindingSettings.Normalize(mouse); // TEST-SUPPORT: KEYBIND-MENU-011 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(normalizedPlain, Is.Not.Null); // TEST-SUPPORT: KEYBIND-MENU-012 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(normalizedShifted, Is.TypeOf<InputEventKey>()); // TEST-SUPPORT: KEYBIND-MENU-013 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(((InputEventKey)normalizedShifted!).ShiftPressed, Is.True); // TEST-SUPPORT: KEYBIND-MENU-014 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(KeyBindingSettings.SameBinding(normalizedPlain, normalizedShifted), Is.False); // TEST-SUPPORT: KEYBIND-MENU-015 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(normalizedMouse, Is.TypeOf<InputEventMouseButton>()); // TEST-SUPPORT: KEYBIND-MENU-016 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(((InputEventMouseButton)normalizedMouse!).ButtonIndex, Is.EqualTo(MouseButton.Right)); // TEST-SUPPORT: KEYBIND-MENU-017 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(((InputEventMouseButton)normalizedMouse).CtrlPressed, Is.True); // TEST-SUPPORT: KEYBIND-MENU-018 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        plain.Echo = true; // TEST-SUPPORT: KEYBIND-MENU-019 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(KeyBindingSettings.Normalize(plain), Is.Null); // TEST-SUPPORT: KEYBIND-MENU-020 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        plain.Echo = false; // TEST-SUPPORT: KEYBIND-MENU-021 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        plain.Pressed = false; // TEST-SUPPORT: KEYBIND-MENU-022 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(KeyBindingSettings.Normalize(plain), Is.Null); // TEST-SUPPORT: KEYBIND-MENU-023 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        using var modifier = new InputEventKey { PhysicalKeycode = Key.Shift, Pressed = true, ShiftPressed = true }; // TEST-SUPPORT: KEYBIND-MENU-024 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(KeyBindingSettings.Normalize(modifier), Is.Null); // TEST-SUPPORT: KEYBIND-MENU-025 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
        Assert.That(KeyBindingSettings.Format(null), Is.Not.Empty); // TEST-SUPPORT: KEYBIND-MENU-026 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons
    }

    [Test] // TEST-SUPPORT: KEYBIND-MENU-027 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
    public void SaveReloadAndRestorePreserveDefaultsAndCompatibility() // TEST-SUPPORT: KEYBIND-MENU-028 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
    {
        var configPath = ProjectSettings.GlobalizePath(FilePaths.SystemConfigPath); // TEST-SUPPORT: KEYBIND-MENU-029 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        var originalConfig = System.IO.File.ReadAllText(configPath); // TEST-SUPPORT: KEYBIND-MENU-030 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        var originalBindings = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-031 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        var defaults = KeyBindingSettings.CreateDefaultBindings(); // TEST-SUPPORT: KEYBIND-MENU-032 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        try // TEST-SUPPORT: KEYBIND-MENU-033 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        {
            using var config = new ConfigFile(); // TEST-SUPPORT: KEYBIND-MENU-034 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(config.Load(FilePaths.SystemConfigPath), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-035 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            config.SetValue("config", "reload_config_on_start", false); // TEST-SUPPORT: KEYBIND-MENU-036 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(config.Save(FilePaths.SystemConfigPath), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-037 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-038 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            GameConfig.Load(); // TEST-SUPPORT: KEYBIND-MENU-039 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            var checksum = GameConfig.ConfigDataChecksum; // TEST-SUPPORT: KEYBIND-MENU-040 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            var draft = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-041 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            var forward = FindAction("Forwards"); // TEST-SUPPORT: KEYBIND-MENU-042 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            var message = FindAction("MessageMenu"); // TEST-SUPPORT: KEYBIND-MENU-043 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            draft[forward, 0] = new InputEventKey { PhysicalKeycode = Key.K, CtrlPressed = true }; // TEST-SUPPORT: KEYBIND-MENU-044 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            draft[forward, 1] = new InputEventMouseButton { ButtonIndex = MouseButton.Xbutton1 }; // TEST-SUPPORT: KEYBIND-MENU-045 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            draft[message, 0] = null; // TEST-SUPPORT: KEYBIND-MENU-046 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            draft[message, 1] = new InputEventKey { PhysicalKeycode = Key.F9 }; // TEST-SUPPORT: KEYBIND-MENU-047 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(GameConfig.SaveKeyBindings(draft), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-048 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            InputMap.ActionEraseEvents("Forwards"); // TEST-SUPPORT: KEYBIND-MENU-049 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-050 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            GameConfig.Load(); // TEST-SUPPORT: KEYBIND-MENU-051 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            var reloaded = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-052 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            AssertBindingsEqual(draft, reloaded); // TEST-SUPPORT: KEYBIND-MENU-053 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(InputMap.ActionGetEvents("Forwards").Count, Is.EqualTo(2)); // TEST-SUPPORT: KEYBIND-MENU-054 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, CtrlPressed = true, Pressed = true }, "Forwards", true), Is.True); // TEST-SUPPORT: KEYBIND-MENU-055 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(GameConfig.ConfigDataChecksum, Is.EqualTo(checksum)); // TEST-SUPPORT: KEYBIND-MENU-056 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            AssertBindingsEqual(defaults, KeyBindingSettings.CreateDefaultBindings()); // TEST-SUPPORT: KEYBIND-MENU-057 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(GameConfig.SaveKeyBindings(defaults), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-058 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-059 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            GameConfig.Load(); // TEST-SUPPORT: KEYBIND-MENU-060 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            AssertBindingsEqual(defaults, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-061 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        }
        finally // TEST-SUPPORT: KEYBIND-MENU-062 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        {
            System.IO.File.WriteAllText(configPath, originalConfig); // TEST-SUPPORT: KEYBIND-MENU-063 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(ConfigLoader.Instance.Load().error, Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-064 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            GameConfig.Load(); // TEST-SUPPORT: KEYBIND-MENU-065 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
            Assert.That(GameConfig.SaveKeyBindings(originalBindings), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-066 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility
        }
    }

    [Test] // TEST-SUPPORT: KEYBIND-MENU-067 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
    public void RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch() // TEST-SUPPORT: KEYBIND-MENU-068 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
    {
        var originalBindings = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-069 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        var scene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/settings/settings_menu_container.tscn"); // TEST-SUPPORT: KEYBIND-MENU-070 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        var settings = scene.Instantiate<SettingsMenuController>(); // TEST-SUPPORT: KEYBIND-MENU-071 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: KEYBIND-MENU-072 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        root.AddChild(settings); // TEST-SUPPORT: KEYBIND-MENU-073 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        try // TEST-SUPPORT: KEYBIND-MENU-074 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        {
            Assert.That(settings.GetTabCount(), Is.EqualTo(4)); // TEST-SUPPORT: KEYBIND-MENU-075 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(settings.GetTabTitle(0), Is.EqualTo("Keybindings")); // TEST-SUPPORT: KEYBIND-MENU-076 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(settings.GetTabTitle(1), Is.EqualTo("Audio")); // TEST-SUPPORT: KEYBIND-MENU-077 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(settings.GetTabTitle(2), Is.EqualTo("Graphics")); // TEST-SUPPORT: KEYBIND-MENU-078 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(settings.GetTabTitle(3), Is.EqualTo("Game")); // TEST-SUPPORT: KEYBIND-MENU-079 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var menu = settings.GetNode<KeyBindingMenuController>("Keybindings"); // TEST-SUPPORT: KEYBIND-MENU-080 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var sections = menu.GetNode<VBoxContainer>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer"); // TEST-SUPPORT: KEYBIND-MENU-081 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(sections.GetChildCount(), Is.EqualTo(3)); // TEST-SUPPORT: KEYBIND-MENU-082 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var rowCount = 0; // TEST-SUPPORT: KEYBIND-MENU-083 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            foreach (var section in sections.GetChildren()) // TEST-SUPPORT: KEYBIND-MENU-084 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
                rowCount += section.GetNode<VBoxContainer>("RowsContainer").GetChildCount(); // TEST-SUPPORT: KEYBIND-MENU-085 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(rowCount, Is.EqualTo(KeyBindingCatalog.Actions.Length)); // TEST-SUPPORT: KEYBIND-MENU-086 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(rowCount, Is.EqualTo(15)); // TEST-SUPPORT: KEYBIND-MENU-087 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var firstRows = sections.GetChild(0).GetNode<VBoxContainer>("RowsContainer"); // TEST-SUPPORT: KEYBIND-MENU-088 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var row = firstRows.GetChild(0); // TEST-SUPPORT: KEYBIND-MENU-089 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var primary = row.GetNode<Button>("PrimaryBindingButton"); // TEST-SUPPORT: KEYBIND-MENU-090 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var save = menu.GetNode<Button>("MenuContainer/ActionsContainer/ApplyButton"); // TEST-SUPPORT: KEYBIND-MENU-091 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var restore = menu.GetNode<Button>("MenuContainer/ActionsContainer/ResetDefaultsButton"); // TEST-SUPPORT: KEYBIND-MENU-092 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            var before = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-093 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            primary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-094 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            menu._Input(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true }); // TEST-SUPPORT: KEYBIND-MENU-095 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(primary.Text, Does.Contain("K")); // TEST-SUPPORT: KEYBIND-MENU-096 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            AssertBindingsEqual(before, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-097 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            settings.CurrentTab = 1; // TEST-SUPPORT: KEYBIND-MENU-098 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            settings.CurrentTab = 0; // TEST-SUPPORT: KEYBIND-MENU-099 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(primary.Text, Does.Contain("K")); // TEST-SUPPORT: KEYBIND-MENU-100 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            save.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-101 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true }, KeyBindingCatalog.Actions[0].Action, true), Is.True); // TEST-SUPPORT: KEYBIND-MENU-102 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            restore.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-103 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(InputMap.EventIsAction(new InputEventKey { PhysicalKeycode = Key.K, Pressed = true }, KeyBindingCatalog.Actions[0].Action, true), Is.True); // TEST-SUPPORT: KEYBIND-MENU-104 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            save.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-105 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            AssertBindingsEqual(KeyBindingSettings.CreateDefaultBindings(), KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-106 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        }
        finally // TEST-SUPPORT: KEYBIND-MENU-107 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        {
            settings.Free(); // TEST-SUPPORT: KEYBIND-MENU-108 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
            Assert.That(GameConfig.SaveKeyBindings(originalBindings), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-109 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch
        }
    }


    [Test] // TEST-SUPPORT: KEYBIND-MENU-201 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
    public void ConflictsCancelClearAndUnsavedClosePreserveSavedBindings() // TEST-SUPPORT: KEYBIND-MENU-202 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
    {
        var original = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-203 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        var scene = GD.Load<PackedScene>("res://scenes/ui/menu/shared_menu/keybinding/keybinding_menu_container.tscn"); // TEST-SUPPORT: KEYBIND-MENU-204 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        var menu = scene.Instantiate<KeyBindingMenuController>(); // TEST-SUPPORT: KEYBIND-MENU-205 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        var root = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: KEYBIND-MENU-206 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        root.AddChild(menu); // TEST-SUPPORT: KEYBIND-MENU-207 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        try // TEST-SUPPORT: KEYBIND-MENU-208 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        {
            var row = menu.GetNode("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer/Section0/RowsContainer/Forwards"); // TEST-SUPPORT: KEYBIND-MENU-209 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var primary = row.GetNode<Button>("PrimaryBindingButton"); // TEST-SUPPORT: KEYBIND-MENU-210 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var secondary = row.GetNode<Button>("SecondaryBindingButton"); // TEST-SUPPORT: KEYBIND-MENU-211 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var save = menu.GetNode<Button>("MenuContainer/ActionsContainer/ApplyButton"); // TEST-SUPPORT: KEYBIND-MENU-212 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var cancel = menu.GetNode<Button>("MenuContainer/CaptureControls/CancelCaptureButton"); // TEST-SUPPORT: KEYBIND-MENU-213 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var clear = menu.GetNode<Button>("MenuContainer/CaptureControls/ClearBindingButton"); // TEST-SUPPORT: KEYBIND-MENU-214 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            primary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-215 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.S, Pressed = true }); // TEST-SUPPORT: KEYBIND-MENU-216 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.S, Pressed = false }); // TEST-SUPPORT: KEYBIND-MENU-217 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(save.Disabled, Is.True); // TEST-SUPPORT: KEYBIND-MENU-218 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(menu.GetNode<Label>("MenuContainer/BindingStatus").Text, Does.Contain("assigned to both")); // TEST-SUPPORT: KEYBIND-MENU-219 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            save.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-220 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-221 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            primary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-222 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            cancel.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-223 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(primary.Text, Is.EqualTo("S")); // TEST-SUPPORT: KEYBIND-MENU-224 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            primary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-225 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            clear.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-226 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(primary.Text, Is.EqualTo(KeyBindingSettings.Format(null))); // TEST-SUPPORT: KEYBIND-MENU-227 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(save.Disabled, Is.False); // TEST-SUPPORT: KEYBIND-MENU-228 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            secondary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-229 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = true }); // TEST-SUPPORT: KEYBIND-MENU-230 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.Escape, Pressed = false }); // TEST-SUPPORT: KEYBIND-MENU-231 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(secondary.Text, Does.Contain("Esc")); // TEST-SUPPORT: KEYBIND-MENU-232 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(save.Disabled, Is.True); // TEST-SUPPORT: KEYBIND-MENU-233 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            secondary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-234 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = true }); // TEST-SUPPORT: KEYBIND-MENU-235 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu._Input(new InputEventKey { PhysicalKeycode = Key.F10, Pressed = false }); // TEST-SUPPORT: KEYBIND-MENU-236 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            save.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-237 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            var saved = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-238 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(saved[FindAction("Forwards"), 0], Is.Null); // TEST-SUPPORT: KEYBIND-MENU-239 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(((InputEventKey)saved[FindAction("Forwards"), 1]!).PhysicalKeycode, Is.EqualTo(Key.F10)); // TEST-SUPPORT: KEYBIND-MENU-240 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            secondary.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-241 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            clear.EmitSignal(BaseButton.SignalName.Pressed); // TEST-SUPPORT: KEYBIND-MENU-242 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu.Free(); // TEST-SUPPORT: KEYBIND-MENU-243 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            menu = scene.Instantiate<KeyBindingMenuController>(); // TEST-SUPPORT: KEYBIND-MENU-244 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            root.AddChild(menu); // TEST-SUPPORT: KEYBIND-MENU-245 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(menu.GetNode<Button>("MenuContainer/ScrollContainer/MarginContainer/SectionsContainer/Section0/RowsContainer/Forwards/SecondaryBindingButton").Text, Is.EqualTo("F10")); // TEST-SUPPORT: KEYBIND-MENU-246 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            AssertBindingsEqual(saved, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-247 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        }
        finally // TEST-SUPPORT: KEYBIND-MENU-248 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        {
            menu.Free(); // TEST-SUPPORT: KEYBIND-MENU-249 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
            Assert.That(GameConfig.SaveKeyBindings(original), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-250 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings
        }
    }


    [Test] // TEST-SUPPORT: KEYBIND-MENU-301 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    public void FailedSaveRollsBackAndMalformedBindingsAreRejected() // TEST-SUPPORT: KEYBIND-MENU-302 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    {
        var original = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-303 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        var stored = GD.VarToStr(ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey)); // TEST-SUPPORT: KEYBIND-MENU-304 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        var draft = KeyBindingSettings.CreateCurrentBindings(); // TEST-SUPPORT: KEYBIND-MENU-305 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        draft[FindAction("Forwards"), 0] = new InputEventKey { PhysicalKeycode = Key.F12 }; // TEST-SUPPORT: KEYBIND-MENU-306 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        var path = ProjectSettings.GlobalizePath(FilePaths.UserConfigPath); // TEST-SUPPORT: KEYBIND-MENU-307 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        try // TEST-SUPPORT: KEYBIND-MENU-308 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        {
            using (var lockedConfig = new System.IO.FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.None)) // TEST-SUPPORT: KEYBIND-MENU-309 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
                Assert.That(GameConfig.SaveKeyBindings(draft), Is.Not.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-310 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            Assert.That(GD.VarToStr(ConfigLoader.Instance.GetValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey)), Is.EqualTo(stored)); // TEST-SUPPORT: KEYBIND-MENU-311 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-312 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey); // TEST-SUPPORT: KEYBIND-MENU-317 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            Assert.Throws<InvalidOperationException>(() => KeyBindingSettings.LoadConfiguration(shipped, new Godot.Collections.Dictionary { ["Forwards"] = "invalid stored binding" }, FilePaths.UserConfigPath)); // TEST-SUPPORT: KEYBIND-MENU-313 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            AssertBindingsEqual(original, KeyBindingSettings.CreateCurrentBindings()); // TEST-SUPPORT: KEYBIND-MENU-314 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        }
        finally // TEST-SUPPORT: KEYBIND-MENU-315 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        {
            Assert.That(GameConfig.SaveKeyBindings(original), Is.EqualTo(Error.Ok)); // TEST-SUPPORT: KEYBIND-MENU-316 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        }
    }

    private static int FindAction(string action) // TEST-SUPPORT: KEYBIND-MENU-110 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    {
        for (var index = 0; index < KeyBindingCatalog.Actions.Length; index++) // TEST-SUPPORT: KEYBIND-MENU-111 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            if (KeyBindingCatalog.Actions[index].Action == action) return index; // TEST-SUPPORT: KEYBIND-MENU-112 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        throw new AssertionException("Unknown action: " + action); // TEST-SUPPORT: KEYBIND-MENU-113 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    }

    private static void AssertBindingsEqual(InputEvent?[,] expected, InputEvent?[,] actual) // TEST-SUPPORT: KEYBIND-MENU-114 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    {
        Assert.That(actual.GetLength(0), Is.EqualTo(expected.GetLength(0))); // TEST-SUPPORT: KEYBIND-MENU-115 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        Assert.That(actual.GetLength(1), Is.EqualTo(expected.GetLength(1))); // TEST-SUPPORT: KEYBIND-MENU-116 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
        for (var action = 0; action < expected.GetLength(0); action++) // TEST-SUPPORT: KEYBIND-MENU-117 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
            for (var slot = 0; slot < expected.GetLength(1); slot++) // TEST-SUPPORT: KEYBIND-MENU-118 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
                Assert.That(KeyBindingSettings.SameBinding(expected[action, slot], actual[action, slot]), Is.True, $"Action {action}, slot {slot}"); // TEST-SUPPORT: KEYBIND-MENU-119 | Tests: sMPGWM.Tests.UiSystem.KeyBindingMenuTests.NormalizationPreservesModifiersAndMouseButtons,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.SaveReloadAndRestorePreserveDefaultsAndCompatibility,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.RuntimeMenuReplacesSamplesAndSavesDraftAfterTabSwitch,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.ConflictsCancelClearAndUnsavedClosePreserveSavedBindings,sMPGWM.Tests.UiSystem.KeyBindingMenuTests.FailedSaveRollsBackAndMalformedBindingsAreRejected
    }
}

