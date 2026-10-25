using sMPGWM.Scripts.InputSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class HoldInputModeTests
{
    [Test]
    public void Reset_ClearsHeldActionAndRequiresReleaseBeforeReactivation()
    {
        var mode = new HoldInputMode("Action");
        mode.Update(true);
        Assert.That(mode.IsActive(), Is.True);

        mode.ResetState();
        Assert.That(mode.IsActive(), Is.False);
        mode.Update(true);
        Assert.That(mode.IsActive(), Is.False);
        mode.Update(false);
        mode.Update(true);
        Assert.That(mode.IsActive(), Is.True);
    }

    [Test]
    public void RepeatedResets_RequireFreshPressOnEveryCycle()
    {
        var mode = new HoldInputMode("Action");
        for (var cycle = 0; cycle < 3; cycle++)
        {
            mode.Update(true);
            Assert.That(mode.IsActive(), Is.True);
            mode.ResetState();
            mode.Update(true);
            Assert.That(mode.IsActive(), Is.False);
            mode.Update(false);
        }
    }
}
