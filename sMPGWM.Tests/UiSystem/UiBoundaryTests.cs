using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class UiBoundaryTests
{
    [Test] // TEST-SUPPORT: KEYBIND-BOUNDARY-001 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    [Category("Godot")] // TEST-SUPPORT: KEYBIND-BOUNDARY-002 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    public void EveryWindowId_HasAnIdenticallyNamedInputMapAction() // TEST-SUPPORT: KEYBIND-BOUNDARY-003 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    {
        foreach (var action in Enum.GetNames<UiWindowId>()) // TEST-SUPPORT: KEYBIND-BOUNDARY-004 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
            Assert.That(InputMap.HasAction(action), Is.True, action); // TEST-SUPPORT: KEYBIND-BOUNDARY-005 | Tests: sMPGWM.Tests.UiSystem.UiBoundaryTests.EveryWindowId_HasAnIdenticallyNamedInputMapAction
    }

}
