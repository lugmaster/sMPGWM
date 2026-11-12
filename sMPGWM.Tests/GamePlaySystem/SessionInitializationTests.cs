using sMPGWM.Scripts.GamePlaySystem;

namespace sMPGWM.Tests.GamePlaySystem;

public class SessionInitializationTests // TEST-SUPPORT: INIT-GATE-5 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings,sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed,sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
{
    [Test] // TEST-SUPPORT: INIT-GATE-7 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings,sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed,sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
    public void ActivationRequiresDefinitionsConstructionAndLocalBindings() // TEST-SUPPORT: INIT-GATE-8 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
    {
        var activations = 0; // TEST-SUPPORT: INIT-GATE-10 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        var coordinator = new SessionCoordinator(() => activations++, Assert.Fail, error => Assert.Fail(error.Message)); // TEST-SUPPORT: INIT-GATE-11 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.CompleteLoading(); // TEST-SUPPORT: INIT-GATE-12 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Initialization.Synchronize(() => Assert.Fail("Early network report must be ignored")); // TEST-SUPPORT: INIT-GATE-13 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-GATE-14 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Start(_ => coordinator.CompleteLoading()); // TEST-SUPPORT: INIT-GATE-15 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.CompleteLoading(); // TEST-SUPPORT: INIT-GATE-16 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        Assert.That(activations, Is.Zero); // TEST-SUPPORT: INIT-GATE-17 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Starting)); // TEST-SUPPORT: INIT-GATE-18 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        var bound = false; // TEST-SUPPORT: INIT-GATE-19 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Initialization.Synchronize(() => bound = true); // TEST-SUPPORT: INIT-GATE-20 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Initialization.Synchronize(() => Assert.Fail("Duplicate readiness must be ignored")); // TEST-SUPPORT: INIT-GATE-21 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        Assert.That(bound, Is.True); // TEST-SUPPORT: INIT-GATE-22 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        Assert.That(activations, Is.EqualTo(1)); // TEST-SUPPORT: INIT-GATE-23 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Active)); // TEST-SUPPORT: INIT-GATE-24 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
        coordinator.Stop(); // TEST-SUPPORT: INIT-GATE-25 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
    }

    [Test] // TEST-SUPPORT: INIT-GATE-28 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ActivationRequiresDefinitionsConstructionAndLocalBindings
    public void FailedPreparationStopsAndNewAttemptCanSucceed() // TEST-SUPPORT: INIT-GATE-29 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
    {
        var failures = new List<string>(); // TEST-SUPPORT: INIT-GATE-31 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        var failed = new SessionCoordinator(() => Assert.Fail("Failed preparation activated"), failures.Add, _ => { }); // TEST-SUPPORT: INIT-GATE-32 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        failed.Initialization.PrepareDefinitions(() => throw new ArgumentException("Invalid item reference")); // TEST-SUPPORT: INIT-GATE-33 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        failed.Start(_ => Assert.Fail("Failed attempt constructed a world")); // TEST-SUPPORT: INIT-GATE-34 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        failed.Initialization.Synchronize(() => Assert.Fail("Stale readiness ran")); // TEST-SUPPORT: INIT-GATE-35 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        Assert.That(failures, Has.Count.EqualTo(1)); // TEST-SUPPORT: INIT-GATE-36 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        Assert.That(failures[0], Does.Contain("Invalid item reference")); // TEST-SUPPORT: INIT-GATE-37 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        Assert.That(failed.State, Is.EqualTo(SessionState.Stopped)); // TEST-SUPPORT: INIT-GATE-38 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        var fresh = new SessionCoordinator(() => { }, Assert.Fail, _ => { }); // TEST-SUPPORT: INIT-GATE-39 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        fresh.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-GATE-40 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        fresh.Start(_ => { }); // TEST-SUPPORT: INIT-GATE-41 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        fresh.Initialization.Synchronize(() => { }); // TEST-SUPPORT: INIT-GATE-42 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        Assert.That(fresh.State, Is.EqualTo(SessionState.Active)); // TEST-SUPPORT: INIT-GATE-43 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
        fresh.Stop(); // TEST-SUPPORT: INIT-GATE-44 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
    }

    [TestCase(false)] // TEST-SUPPORT: INIT-GATE-47 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
    [TestCase(true)] // TEST-SUPPORT: INIT-GATE-48 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.FailedPreparationStopsAndNewAttemptCanSucceed
    public void ReadinessFailureOrCancellationReleasesScopeWithoutActivating(bool cancel) // TEST-SUPPORT: INIT-GATE-49 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
    {
        var failures = new List<string>(); // TEST-SUPPORT: INIT-GATE-51 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        var disposed = 0; // TEST-SUPPORT: INIT-GATE-52 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        var coordinator = new SessionCoordinator(() => Assert.Fail("Incomplete bindings activated"), failures.Add, _ => { }); // TEST-SUPPORT: INIT-GATE-53 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        coordinator.Initialization.PrepareDefinitions(() => { }); // TEST-SUPPORT: INIT-GATE-54 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        coordinator.Start(scope => scope.Defer(() => disposed++)); // TEST-SUPPORT: INIT-GATE-55 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        coordinator.Initialization.Synchronize(() => // TEST-SUPPORT: INIT-GATE-56 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        {
            if (cancel) coordinator.Stop(); // TEST-SUPPORT: INIT-GATE-58 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
            else throw new InvalidOperationException("Missing bindings"); // TEST-SUPPORT: INIT-GATE-59 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        });
        coordinator.Initialization.Synchronize(() => Assert.Fail("Stale completion ran")); // TEST-SUPPORT: INIT-GATE-61 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        Assert.That(coordinator.State, Is.EqualTo(SessionState.Stopped)); // TEST-SUPPORT: INIT-GATE-62 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        Assert.That(disposed, Is.EqualTo(1)); // TEST-SUPPORT: INIT-GATE-63 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
        Assert.That(failures.Count, Is.EqualTo(cancel ? 0 : 1)); // TEST-SUPPORT: INIT-GATE-64 | Tests: sMPGWM.Tests.GamePlaySystem.SessionInitializationTests.ReadinessFailureOrCancellationReleasesScopeWithoutActivating
    }
}
