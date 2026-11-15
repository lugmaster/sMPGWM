using Godot;
using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.PlayerControllerSystem;

namespace sMPGWM.Tests.UiSystem;

[Category("Godot")]
public sealed class WeaponFireInputTests
{
    [TestCase(false, 0, false)]
    [TestCase(false, 0, true)]
    [TestCase(true, 0, false)]
    [TestCase(true, 0, true)]
    [TestCase(true, 1, false)]
    [TestCase(true, 1, true)]
    public void PointerCaptureSuppressesBothBindingsPreservesMovementAndRequiresRelease(bool keyboard, int slot, bool interactiveUi)
    {
        var original = KeyBindingSettings.CreateCurrentBindings();
        var previousFocus = InputController.IsWindowFocused;
        var input = new InputController();
        var policy = new PlayerInputPolicy();
        var pointerOverUi = false;
        try
        {
            ConfigureBindings(keyboard, slot);
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(input);
            Focus(true);
            policy.SetSessionActive(true);
            input.SetPointerInputFilter(() => !policy.CanUseFlightPointerActions(pointerOverUi));
            input.SetGameplayInputEnabled(true);
            FireKeys(keyboard, false);
            input.SampleInput();
            Input.ActionPress("Forwards");
            FireKeys(keyboard, true);
            input.SampleInput();
            AssertFire(input, true);
            Assert.That(input.Direction.X, Is.GreaterThan(0));

            if (interactiveUi) pointerOverUi = true;
            else policy.SetAbilityTargeting(true, false);
            input.SampleInput();
            AssertFire(input, false);
            Assert.That(input.Direction.X, Is.GreaterThan(0), "Targeting captures firing but leaves movement available.");
            if (!interactiveUi)
            {
                policy.SetAbilityTargeting(false, true);
                input.SampleInput();
                AssertFire(input, false);
                Assert.That(input.Direction.X, Is.GreaterThan(0));
            }
            pointerOverUi = false;
            policy.SetAbilityTargeting(false, false);
            input.SampleInput();
            AssertFire(input, false);
            FireKeys(keyboard, false);
            input.SampleInput();
            AssertFire(input, false);
            FireKeys(keyboard, true);
            input.SampleInput();
            AssertFire(input, true);
        }
        finally
        {
            FireKeys(keyboard, false);
            Input.ActionRelease("Forwards");
            input.Free();
            Focus(previousFocus);
            KeyBindingSettings.Apply(original);
        }
    }

    [TestCase("blocking menu")]
    [TestCase("required acknowledgement")]
    [TestCase("focus")]
    [TestCase("control")]
    public void ControlRestrictionClearsHeldKeyboardFireUntilFreshRelease(string reason)
    {
        var original = KeyBindingSettings.CreateCurrentBindings();
        var previousFocus = InputController.IsWindowFocused;
        var input = new InputController();
        var policy = new PlayerInputPolicy();
        try
        {
            ConfigureBindings(true, 1);
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(input);
            Focus(true);
            policy.CanControlPlayerChanged += input.SetGameplayInputEnabled;
            policy.SetSessionActive(true);
            input.SetPointerInputFilter(() => !policy.CanUseFlightPointerActions(false));
            FireKeys(true, false);
            input.SampleInput();
            FireKeys(true, true);
            input.SampleInput();
            AssertFire(input, true);
            SetRestriction(reason, policy, true);
            input.SampleInput();
            AssertFire(input, false);
            SetRestriction(reason, policy, false);
            input.SampleInput();
            AssertFire(input, false);
            FireKeys(true, false);
            input.SampleInput();
            FireKeys(true, true);
            input.SampleInput();
            AssertFire(input, true);
        }
        finally
        {
            policy.CanControlPlayerChanged -= input.SetGameplayInputEnabled;
            FireKeys(true, false);
            input.Free();
            Focus(previousFocus);
            KeyBindingSettings.Apply(original);
        }
    }

    private static void SetRestriction(string reason, PlayerInputPolicy policy, bool active)
    {
        switch (reason)
        {
            case "blocking menu": policy.SetHasBlockingWindow(active); break;
            case "required acknowledgement": policy.SetHasPendingAcknowledgement(active); break;
            case "focus": Focus(!active); break;
            case "control": policy.SetSessionActive(!active); break;
        }
    }

    private static void ConfigureBindings(bool keyboard, int slot)
    {
        var bindings = KeyBindingSettings.CreateDefaultBindings();
        if (keyboard)
            for (var i = 0; i < KeyBindingCatalog.Actions.Length; i++)
            {
                if (KeyBindingCatalog.Actions[i].Action == "PrimaryFire") bindings[i, slot] = new InputEventKey { PhysicalKeycode = Key.F8 };
                if (KeyBindingCatalog.Actions[i].Action == "SecondaryFire") bindings[i, slot] = new InputEventKey { PhysicalKeycode = Key.F9 };
            }
        KeyBindingSettings.Apply(bindings);
    }

    private static void FireKeys(bool keyboard, bool pressed)
    {
        if (keyboard)
        {
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.F8, Keycode = Key.F8, Pressed = pressed });
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.F9, Keycode = Key.F9, Pressed = pressed });
        }
        else
        {
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed });
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.Q, Keycode = Key.Q, Pressed = pressed });
        }
        Input.FlushBufferedEvents();
    }

    private static void Focus(bool value) => typeof(InputController).GetProperty(nameof(InputController.IsWindowFocused))!.SetValue(null, value);

    private static void AssertFire(InputController input, bool expected)
    {
        Assert.That(input.PrimaryFirePressed, Is.EqualTo(expected));
        Assert.That(input.SecondaryFirePressed, Is.EqualTo(expected));
    }
}
