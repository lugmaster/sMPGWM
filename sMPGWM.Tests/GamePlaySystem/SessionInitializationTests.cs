using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.GamePlaySystem;

public class SessionInitializationTests
{
    [Test]
    public void ActivationRequiresDefinitionsConstructionAndLocalBindings()
    {
        var activations = 0;
        var coordinator = new SessionCoordinator(() => activations++, Assert.Fail, error => Assert.Fail(error.Message));
        coordinator.CompleteLoading();
        coordinator.Initialization.Synchronize(() => Assert.Fail("Early network report must be ignored"));
        coordinator.Initialization.PrepareDefinitions(() => { });
        coordinator.Start(_ => coordinator.CompleteLoading());
        coordinator.CompleteLoading();
        Assert.That(activations, Is.Zero);
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Starting));
        var bound = false;
        coordinator.Initialization.Synchronize(() => bound = true);
        coordinator.Initialization.Synchronize(() => Assert.Fail("Duplicate readiness must be ignored"));
        Assert.That(bound, Is.True);
        Assert.That(activations, Is.EqualTo(1));
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Active));
        coordinator.Stop();
    }

    [Test]
    public void FailedPreparationStopsAndNewAttemptCanSucceed()
    {
        var failures = new List<string>();
        var failed = new SessionCoordinator(() => Assert.Fail("Failed preparation activated"), failures.Add, _ => { });
        failed.Initialization.PrepareDefinitions(() => throw new ArgumentException("Invalid item reference"));
        failed.Start(_ => Assert.Fail("Failed attempt constructed a world"));
        failed.Initialization.Synchronize(() => Assert.Fail("Stale readiness ran"));
        Assert.That(failures, Has.Count.EqualTo(1));
        Assert.That(failures[0], Does.Contain("Invalid item reference"));
        Assert.That(failed.State, Is.EqualTo(SessionState.Stopped));
        var fresh = new SessionCoordinator(() => { }, Assert.Fail, _ => { });
        fresh.Initialization.PrepareDefinitions(() => { });
        fresh.Start(_ => { });
        fresh.Initialization.Synchronize(() => { });
        Assert.That(fresh.State, Is.EqualTo(SessionState.Active));
        fresh.Stop();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReadinessFailureOrCancellationReleasesScopeWithoutActivating(bool cancel)
    {
        var failures = new List<string>();
        var disposed = 0;
        var coordinator = new SessionCoordinator(() => Assert.Fail("Incomplete bindings activated"), failures.Add, _ => { });
        coordinator.Initialization.PrepareDefinitions(() => { });
        coordinator.Start(scope => scope.Defer(() => disposed++));
        coordinator.Initialization.Synchronize(() =>
        {
            if (cancel) coordinator.Stop();
            else throw new InvalidOperationException("Missing bindings");
        });
        coordinator.Initialization.Synchronize(() => Assert.Fail("Stale completion ran"));
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped));
        Assert.That(disposed, Is.EqualTo(1));
        Assert.That(failures.Count, Is.EqualTo(cancel ? 0 : 1));
    }
}
