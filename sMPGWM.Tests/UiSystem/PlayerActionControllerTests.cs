using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class PlayerActionControllerTests
{
    [TestCase(false, false, true, false)]
    [TestCase(true, true, true, false)]
    [TestCase(true, false, false, false)]
    [TestCase(true, false, true, true)]
    public void HotbarActivation_RespectsSessionWindowsAndInputAvailability(
        bool active, bool blocking, bool inputAvailable, bool expected)
    {
        var policy = new PlayerInputPolicy();
        policy.SetSessionActive(active);
        policy.SetHasBlockingWindow(blocking);
        var activations = new List<int>();
        var controller = new PlayerActionController(policy, activations.Add, () => inputAvailable);

        controller.RequestHotbarActivation(3);

        Assert.That(activations, Is.EqualTo(expected ? new[] { 3 } : Array.Empty<int>()));
    }

    [Test]
    public void Requests_ReadCurrentPolicyAndFocusRatherThanCachedValues()
    {
        var policy = new PlayerInputPolicy();
        var focused = true;
        var activations = new List<int>();
        var controller = new PlayerActionController(policy, activations.Add, () => focused);
        policy.SetSessionActive(true);
        controller.RequestHotbarActivation(0);
        policy.SetHasBlockingWindow(true);
        controller.RequestHotbarActivation(1);
        policy.SetHasBlockingWindow(false);
        focused = false;
        controller.RequestHotbarActivation(2);
        focused = true;
        controller.RequestHotbarActivation(3);

        Assert.That(activations, Is.EqualTo(new[] { 0, 3 }));
    }
}
