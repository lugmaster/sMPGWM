using Godot;
using sMPGWM.Scripts.GameConfigSystem;
using sMPGWM.Scripts.InputSystem;
using Dictionary = Godot.Collections.Dictionary;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class ReloadInputTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void MissingReloadUpgradePreservesBindingsAndAvoidsRConflict(bool conflict)
    {
        var shipped = ConfigLoader.Instance.GetDefaultValue(KeyBindingSettings.ConfigSection, KeyBindingSettings.ConfigKey);
        var original = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings());
        var previous = shipped.AsGodotDictionary().Duplicate(true);
        previous.Remove("ReloadWeapons");
        previous["Forwards"].AsGodotDictionary()["label"] = "Custom forward";
        previous["Forwards"].AsGodotDictionary()["primary"] = new Dictionary { ["type"] = "key", ["physical_key"] = conflict ? "R" : "F8" };
        try
        {
            KeyBindingSettings.LoadConfiguration(shipped, previous, "old-config");
            var saved = KeyBindingSettings.Serialize(KeyBindingSettings.CreateCurrentBindings());
            Assert.That(saved["Forwards"].AsGodotDictionary()["label"].AsString(), Is.EqualTo("Custom forward"));
            Assert.That(GD.VarToStr(saved["Forwards"]), Is.EqualTo(GD.VarToStr(previous["Forwards"])));
            var reload = saved["ReloadWeapons"].AsGodotDictionary()["primary"];
            if (conflict) Assert.That(reload.VariantType, Is.EqualTo(Variant.Type.Nil));
            else Assert.That(reload.AsGodotDictionary()["physical_key"].AsString(), Is.EqualTo("R"));
            Assert.That(previous.ContainsKey("ReloadWeapons"), Is.False);
        }
        finally { KeyBindingSettings.LoadConfiguration(shipped, original, "restored-config"); }
    }

    [TestCase(0)]
    [TestCase(1)]
    public void RemappedReloadIsOnePressAndBlockedHoldRequiresRelease(int slot)
    {
        var original = KeyBindingSettings.CreateCurrentBindings();
        var focus = InputController.IsWindowFocused;
        var input = new InputController();
        var blocked = false;
        try
        {
            var bindings = KeyBindingSettings.CreateDefaultBindings();
            for (var row = 0; row < KeyBindingCatalog.Actions.Length; row++)
                if (KeyBindingCatalog.Actions[row].Action == "ReloadWeapons")
                {
                    bindings[row, 0] = bindings[row, 1] = null;
                    bindings[row, slot] = new InputEventKey { PhysicalKeycode = Key.F8 };
                }
            KeyBindingSettings.Apply(bindings);
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(input);
            typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, true);
            input.SetPointerInputFilter(() => blocked);
            input.SetGameplayInputEnabled(true);
            Press(false); input.SampleInput();
            Press(true); input.SampleInput();
            Assert.That(input.ReloadWeaponsJustPressed, Is.True);
            input.SampleInput();
            Assert.That(input.ReloadWeaponsJustPressed, Is.False);
            blocked = true; input.SampleInput();
            blocked = false; input.SampleInput();
            Assert.That(input.ReloadWeaponsJustPressed, Is.False);
            Press(false); input.SampleInput();
            Press(true); input.SampleInput();
            Assert.That(input.ReloadWeaponsJustPressed, Is.True);
            input.SetGameplayInputEnabled(false);
            input.SetGameplayInputEnabled(true);
            input.SampleInput();
            Assert.That(input.ReloadWeaponsJustPressed, Is.False);
        }
        finally
        {
            Press(false); input.Free();
            typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, focus);
            KeyBindingSettings.Apply(original);
        }
    }

    private static void Press(bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.F8, Keycode = Key.F8, Pressed = pressed });
        Input.FlushBufferedEvents();
    }
}
