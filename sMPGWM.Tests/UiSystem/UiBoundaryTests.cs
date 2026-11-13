using System.Reflection;
using Godot;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class UiBoundaryTests
{
    [Test]
    public void Canvas_ExposesOnlyFourMenuOperationsBeyondGodotOverrides()
    {
        var methods = GetPublicOperations(typeof(UiCanvas));

        Assert.That(methods, Is.EquivalentTo(new[]
        {
            nameof(UiCanvas.OpenMenu),
            nameof(UiCanvas.CloseMenu),
            nameof(UiCanvas.ToggleMenu),
            nameof(UiCanvas.CloseAllMenus)
        }));
    }

    [Test]
    public void WindowManagerAndStateHelpers_AreHiddenBehindCanvas()
    {
        Assert.That(GetPublicOperations(typeof(UiWindowManager)), Is.Empty);
        Assert.That(typeof(UiWindowManager).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(UiWindowManager).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(UiWindowRegistry).IsNotPublic, Is.True);
        Assert.That(typeof(GameOverlayProvider).IsNotPublic, Is.True);
    }

    [Test] // TEST-SUPPORT: KEYBIND-BOUNDARY-001 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    [Category("Godot")] // TEST-SUPPORT: KEYBIND-BOUNDARY-002 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    public void EveryWindowId_HasAnIdenticallyNamedInputMapAction() // TEST-SUPPORT: KEYBIND-BOUNDARY-003 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    {
        foreach (var action in Enum.GetNames<UiWindowId>()) // TEST-SUPPORT: KEYBIND-BOUNDARY-004 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
            Assert.That(InputMap.HasAction(action), Is.True, action); // TEST-SUPPORT: KEYBIND-BOUNDARY-005 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    }

    private static IEnumerable<string> GetPublicOperations(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsVirtual && !method.IsSpecialName)
            .Select(method => method.Name);
}
