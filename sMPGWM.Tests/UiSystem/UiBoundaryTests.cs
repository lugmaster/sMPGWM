using Godot;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class UiBoundaryTests
{
    [Test]
    [Category("Godot")]
    public void EveryWindowId_HasAnIdenticallyNamedInputMapAction()
    {
        foreach (var action in Enum.GetNames<UiWindowId>())
            Assert.That(InputMap.HasAction(action), Is.True, action);
    }

}
