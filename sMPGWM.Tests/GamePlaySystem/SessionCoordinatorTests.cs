using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.GamePlaySystem;

public class SessionCoordinatorTests
{
    [Test]
    public void ResourceAcquiredDuringCancellationIsImmediatelyReleased()
    {
        var released = false;
        var coordinator = new SessionCoordinator(() => Assert.Fail(), _ => Assert.Fail(), _ => Assert.Fail());
        coordinator.Start(scope =>
        {
            coordinator.Stop();
            scope.Defer(() => released = true);
        });
        Assert.That(released, Is.True);
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void InitializationFailureRollsBackAcquiredResourcesInReverseOrder(int acquired)
    {
        var disposed = new List<int>();
        var failures = new List<string>();
        var coordinator = new SessionCoordinator(() => Assert.Fail("Activated failed startup"), failures.Add,
            _ => Assert.Fail("Unexpected cleanup error"));
        coordinator.Start(scope =>
        {
            for (var i = 0; i < acquired; i++)
            {
                var resource = i;
                scope.Defer(() => disposed.Add(resource));
            }
            throw new InvalidOperationException("initialization failure");
        });
        coordinator.Stop();
        coordinator.Fail("late callback");
        coordinator.CompleteLoading();
        Assert.That(disposed, Is.EqualTo(Enumerable.Range(0, acquired).Reverse()));
        Assert.That(failures, Has.Count.EqualTo(1));
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped));
    }

    [Test]
    public void ReentrantFailureAndCleanupErrorsStillReleaseEverythingAndPresentOnce()
    {
        var events = new List<string>();
        SessionCoordinator coordinator = null!;
        coordinator = new SessionCoordinator(() => events.Add("ready"), reason => events.Add(reason),
            error => events.Add(error.Message));
        coordinator.Start(scope =>
        {
            scope.Defer(() => events.Add("disposed first"));
            scope.Defer(() => throw new InvalidOperationException("cleanup error"));
            scope.Defer(() => { events.Add("disposed last"); coordinator.Fail("reentrant"); });
        });
        coordinator.CompleteLoading();
        coordinator.CompleteLoading();
        coordinator.Fail("original failure");
        coordinator.Fail("duplicate");
        Assert.That(events, Is.EqualTo(new[] { "ready", "disposed last", "cleanup error", "disposed first", "original failure" }));
    }

    [Test]
    public void CancelledSessionIgnoresLateCompletionAndFreshSessionCanStart()
    {
        var activations = 0;
        var cleanups = 0;
        var old = new SessionCoordinator(() => activations++, _ => Assert.Fail("Cancellation is not failure"), _ => { });
        old.Start(scope => scope.Defer(() => cleanups++));
        old.Stop();
        old.CompleteLoading();
        old.Start(_ => Assert.Fail("Cannot restart a disposed scope"));
        var fresh = new SessionCoordinator(() => activations++, _ => Assert.Fail(), _ => { });
        fresh.Start(scope => scope.Defer(() => cleanups++));
        fresh.CompleteLoading();
        old.Fail("stale disconnect");
        Assert.That(fresh.State, Is.EqualTo(SessionState.Active));
        Assert.That(activations, Is.EqualTo(1));
        fresh.Stop();
        Assert.That(cleanups, Is.EqualTo(2));
    }
}
