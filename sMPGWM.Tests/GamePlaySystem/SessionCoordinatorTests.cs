using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.GamePlaySystem;

public class SessionCoordinatorTests
{
    [Test]
    public void ResourceAcquiredDuringCancellationIsImmediatelyReleased()
    {
        var released = false;
        var coordinator = new SessionCoordinator(() => Assert.Fail(), _ => Assert.Fail(), _ => Assert.Fail());
        coordinator.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-COORD-12 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.ResourceAcquiredDuringCancellationIsImmediatelyReleased
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
        coordinator.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-COORD-32 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.InitializationFailureRollsBackAcquiredResourcesInReverseOrder
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
        coordinator.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-COORD-44 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.InitializationFailureRollsBackAcquiredResourcesInReverseOrder
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
        coordinator.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-COORD-57 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.ReentrantFailureAndCleanupErrorsStillReleaseEverythingAndPresentOnce
        coordinator.Start(scope =>
        {
            scope.Defer(() => events.Add("disposed first"));
            scope.Defer(() => throw new InvalidOperationException("cleanup error"));
            scope.Defer(() => { events.Add("disposed last"); coordinator.Fail("reentrant"); });
        });
        coordinator.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-COORD-64 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.ReentrantFailureAndCleanupErrorsStillReleaseEverythingAndPresentOnce
        coordinator.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-COORD-65 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.ReentrantFailureAndCleanupErrorsStillReleaseEverythingAndPresentOnce
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
        old.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-COORD-77 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.CancelledSessionIgnoresLateCompletionAndFreshSessionCanStart
        old.Start(scope => scope.Defer(() => cleanups++));
        old.Stop();
        old.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-COORD-80 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.CancelledSessionIgnoresLateCompletionAndFreshSessionCanStart
        old.Start(_ => Assert.Fail("Cannot restart a disposed scope"));
        var fresh = new SessionCoordinator(() => activations++, _ => Assert.Fail(), _ => { });
        fresh.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-COORD-83 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.CancelledSessionIgnoresLateCompletionAndFreshSessionCanStart
        fresh.Start(scope => scope.Defer(() => cleanups++));
        fresh.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-COORD-85 | Tests: sMPGWM.Tests.GamePlaySystem.SessionCoordinatorTests.CancelledSessionIgnoresLateCompletionAndFreshSessionCanStart
        old.Fail("stale disconnect");
        Assert.That(fresh.State, Is.EqualTo(SessionState.Active));
        Assert.That(activations, Is.EqualTo(1));
        fresh.Stop();
        Assert.That(cleanups, Is.EqualTo(2));
    }
}
