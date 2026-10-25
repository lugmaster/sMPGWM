using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class PlayerInputPolicyTests
{
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Availability_RequiresActiveSessionAndNoBlockingWindows(bool active, bool blocking, bool expected)
    {
        var policy = new PlayerInputPolicy();
        policy.SetSessionActive(active);
        policy.SetHasBlockingWindow(blocking);

        Assert.That(policy.CanControlPlayer, Is.EqualTo(expected));
    }

    [Test]
    public void ClosingWindowsDuringInactiveSession_DoesNotEnableControls()
    {
        var policy = new PlayerInputPolicy();
        var changes = new List<bool>();
        policy.CanControlPlayerChanged += changes.Add;
        policy.SetSessionActive(true);
        policy.SetHasBlockingWindow(true);
        policy.SetSessionActive(false);
        policy.SetHasBlockingWindow(false);

        Assert.That(policy.CanControlPlayer, Is.False);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
        policy.SetSessionActive(true);
        Assert.That(policy.CanControlPlayer, Is.True);
    }

    [Test]
    public void RepeatedBlockingCycles_NotifyOnlyActualAvailabilityChanges()
    {
        var policy = new PlayerInputPolicy();
        var changes = new List<bool>();
        policy.CanControlPlayerChanged += changes.Add;
        policy.SetSessionActive(true);
        policy.SetSessionActive(true);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            policy.SetHasBlockingWindow(true);
            policy.SetHasBlockingWindow(true);
            policy.SetHasBlockingWindow(false);
        }

        Assert.That(changes, Is.EqualTo(new[] { true, false, true, false, true, false, true }));
    }
}
