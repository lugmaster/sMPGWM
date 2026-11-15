using sMPGWM.Scripts.InputSystem;
using sMPGWM.Scripts.PlayerControllerSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class PlayerInputPolicyTests
{
    [Test]
    public void TargetingKeepsFlightMovementAndMenuShortcutsButCapturesPointerUntilRelease()
    {
        var policy = new PlayerInputPolicy();
        policy.SetSessionActive(true);
        var availabilityChanges = 0;
        policy.CanControlPlayerChanged += _ => availabilityChanges++;
        policy.SetAbilityTargeting(true, false);

        Assert.That(policy.IsContextAvailable(InputContext.Flight), Is.True);
        Assert.That(policy.IsContextAvailable(InputContext.AbilityTargeting), Is.True);
        Assert.That(policy.IsContextAvailable(InputContext.MenuShortcuts), Is.True);
        Assert.That(policy.CanUseFlightPointerActions(false), Is.False);
        policy.SetAbilityTargeting(false, true);
        Assert.That(policy.IsContextAvailable(InputContext.AbilityTargeting), Is.False);
        Assert.That(policy.CanUseFlightPointerActions(false), Is.False);
        policy.SetAbilityTargeting(false, false);
        Assert.That(policy.CanUseFlightPointerActions(false), Is.True);
        Assert.That(policy.CanUseFlightPointerActions(true), Is.False);
        Assert.That(availabilityChanges, Is.Zero);
    }

    [Test]
    public void InventoryEditingAndRequiredMessagesHaveDistinctPermissions()
    {
        var policy = new PlayerInputPolicy();
        policy.SetSessionActive(true);
        policy.SetHasBlockingWindow(true);
        policy.SetInventoryInteractionAvailable(true);
        Assert.That(policy.IsContextAvailable(InputContext.InventoryInteraction), Is.True);
        Assert.That(policy.IsContextAvailable(InputContext.MenuInteraction), Is.True);
        Assert.That(policy.IsContextAvailable(InputContext.MenuShortcuts), Is.True);
        Assert.That(policy.IsContextAvailable(InputContext.Flight), Is.False);

        policy.SetHasPendingAcknowledgement(true);
        Assert.That(policy.IsContextAvailable(InputContext.InventoryInteraction), Is.False);
        Assert.That(policy.IsContextAvailable(InputContext.MenuShortcuts), Is.False);
        Assert.That(policy.IsContextAvailable(InputContext.MenuInteraction), Is.True);
        policy.SetSessionActive(false);
        foreach (var context in Enum.GetValues<InputContext>())
            Assert.That(policy.IsContextAvailable(context), Is.False);
    }

    [TestCase(InputRestriction.None, false, true)]
    [TestCase(InputRestriction.TextEntry, false, false)]
    [TestCase(InputRestriction.TextEntry, true, true)]
    [TestCase(InputRestriction.BindingCapture, true, false)]
    [TestCase(InputRestriction.BindingCapture | InputRestriction.TextEntry, true, false)]
    [TestCase(InputRestriction.RequiredAcknowledgement, true, false)]
    [TestCase(InputRestriction.RequiredAcknowledgement, false, false)]
    public void MenuRestrictionsPreserveEscapeForTextEntryOnly(InputRestriction restrictions, bool mainMenu, bool allowed)
    {
        Assert.That(PlayerInputPolicy.AllowsMenuShortcut(restrictions, mainMenu), Is.EqualTo(allowed));
    }

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
