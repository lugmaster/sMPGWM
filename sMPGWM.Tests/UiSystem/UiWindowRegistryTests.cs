using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class UiWindowRegistryTests
{
    [TestCase(UiWindowId.InventoryMenu)]
    [TestCase(UiWindowId.MapMenu)]
    public void ClosingOneOfTwoWindows_KeepsPlayerBlocked(UiWindowId firstClosed)
    {
        var registry = new UiWindowRegistry();
        var policy = new PlayerInputPolicy();
        registry.BlockingChanged += policy.SetHasBlockingWindow;
        policy.SetSessionActive(true);

        registry.Open(UiWindowId.InventoryMenu);
        registry.Open(UiWindowId.MapMenu);
        registry.Close(firstClosed);

        Assert.That(policy.CanControlPlayer, Is.False);
        registry.CloseAll();
        Assert.That(policy.CanControlPlayer, Is.True);
    }

    [Test]
    public void ReopeningWindow_FocusesExistingInstanceWithoutChangingBlocking()
    {
        var registry = new UiWindowRegistry();
        var blockingChanges = new List<bool>();
        registry.BlockingChanged += blockingChanges.Add;
        registry.Open(UiWindowId.InventoryMenu);
        registry.Open(UiWindowId.MapMenu);

        Assert.That(registry.Open(UiWindowId.InventoryMenu), Is.False);
        Assert.That(registry.FocusOrder, Is.EqualTo(new[] { UiWindowId.MapMenu, UiWindowId.InventoryMenu }));
        Assert.That(blockingChanges, Is.EqualTo(new[] { true }));
    }

    [Test]
    public void FocusAndTopmostClose_LeaveOtherWindowsOpen()
    {
        var registry = new UiWindowRegistry();
        registry.Open(UiWindowId.InventoryMenu);
        registry.Open(UiWindowId.MapMenu);
        registry.Open(UiWindowId.MainMenu);
        registry.Focus(UiWindowId.InventoryMenu);
        registry.Close(registry.Topmost!.Value);

        Assert.That(registry.Topmost, Is.EqualTo(UiWindowId.MainMenu));
        Assert.That(registry.IsOpen(UiWindowId.MapMenu), Is.True);
        Assert.That(registry.IsOpen(UiWindowId.InventoryMenu), Is.False);
    }

    [Test]
    public void ClosingUnknownWindow_DoesNotChangeFocusOrBlocking()
    {
        var registry = new UiWindowRegistry();
        registry.Open(UiWindowId.InventoryMenu);

        Assert.That(registry.Close(UiWindowId.MapMenu), Is.False);
        registry.Focus(UiWindowId.MapMenu);
        Assert.That(registry.Topmost, Is.EqualTo(UiWindowId.InventoryMenu));
        Assert.That(registry.HasBlockingWindow, Is.True);
    }

    [Test]
    public void CloseAll_ResetsFocusAndPublishesOneBlockingChange()
    {
        var registry = new UiWindowRegistry();
        var changes = new List<bool>();
        registry.BlockingChanged += changes.Add;
        registry.Open(UiWindowId.InventoryMenu);
        registry.Open(UiWindowId.MapMenu);
        registry.CloseAll();
        registry.CloseAll();

        Assert.That(registry.FocusOrder, Is.Empty);
        Assert.That(registry.Topmost, Is.Null);
        Assert.That(changes, Is.EqualTo(new[] { true, false }));
    }

    [Test]
    public void NonBlockingWindow_DoesNotKeepPlayerBlocked()
    {
        var registry = new UiWindowRegistry();
        registry.Open(UiWindowId.MapMenu, blocksPlayerControls: false);
        registry.Open(UiWindowId.InventoryMenu);
        registry.Close(UiWindowId.InventoryMenu);

        Assert.That(registry.IsOpen(UiWindowId.MapMenu), Is.True);
        Assert.That(registry.HasBlockingWindow, Is.False);
    }
}
