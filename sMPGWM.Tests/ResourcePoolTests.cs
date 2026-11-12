using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: STAT-MIGRATE-0 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: STAT-MIGRATE-1 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool

namespace sMPGWM.Tests; // TEST-SUPPORT: STAT-MIGRATE-3 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool

public sealed class ResourcePoolTests // TEST-SUPPORT: STAT-MIGRATE-5 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
{ // TEST-SUPPORT: STAT-MIGRATE-6 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
    [Test] // TEST-SUPPORT: STAT-MIGRATE-7 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
    public void InvalidContributionCannotCorruptThePool() // TEST-SUPPORT: STAT-MIGRATE-8 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
    { // TEST-SUPPORT: STAT-MIGRATE-9 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        var pool = new ShipStatState(); // TEST-SUPPORT: STAT-MIGRATE-10 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false)); // TEST-SUPPORT: STAT-MIGRATE-11 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        pool.SetContribution(new(StatContributionKind.Effect, 1), 50, 0); // TEST-SUPPORT: STAT-MIGRATE-12 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.SetContribution(new(StatContributionKind.Effect, 1), double.NaN, 0)); // TEST-SUPPORT: STAT-MIGRATE-13 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        Assert.Throws<InvalidOperationException>(() => pool.SetContribution(new(StatContributionKind.Effect, 2), 0, double.MaxValue)); // TEST-SUPPORT: STAT-MIGRATE-14 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(150)); // TEST-SUPPORT: STAT-MIGRATE-15 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool

        Assert.That(pool.RemoveSource(new(StatContributionKind.Effect, 2)), Is.False); // TEST-SUPPORT: STAT-MIGRATE-17 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
    } // TEST-SUPPORT: STAT-MIGRATE-18 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool

    [Test] // TEST-SUPPORT: STAT-MIGRATE-20 | Tests: sMPGWM.Tests.ResourcePoolTests.InvalidContributionCannotCorruptThePool
    public void ModifierInsertionOrderDoesNotChangeBaselineArithmetic() // TEST-SUPPORT: STAT-MIGRATE-21 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
    { // TEST-SUPPORT: STAT-MIGRATE-22 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        var first = new ShipStatState(); // TEST-SUPPORT: STAT-MIGRATE-23 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        first.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false)); // TEST-SUPPORT: STAT-MIGRATE-24 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        var second = new ShipStatState(); // TEST-SUPPORT: STAT-MIGRATE-25 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        second.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false)); // TEST-SUPPORT: STAT-MIGRATE-26 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        first.SetContribution(new(StatContributionKind.Effect, 1), 20, .1); // TEST-SUPPORT: STAT-MIGRATE-27 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        first.SetContribution(new(StatContributionKind.Effect, 2), 50, .2); // TEST-SUPPORT: STAT-MIGRATE-28 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        second.SetContribution(new(StatContributionKind.Effect, 2), 50, .2); // TEST-SUPPORT: STAT-MIGRATE-29 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        second.SetContribution(new(StatContributionKind.Effect, 1), 20, .1); // TEST-SUPPORT: STAT-MIGRATE-30 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        Assert.That(first.Effective.ShieldCapacity, Is.EqualTo(221)); // TEST-SUPPORT: EQUIP-RESOURCE-UPDATE-1 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
        Assert.That(second.Effective.ShieldCapacity, Is.EqualTo(first.Effective.ShieldCapacity)); // TEST-SUPPORT: STAT-MIGRATE-32 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
    } // TEST-SUPPORT: STAT-MIGRATE-33 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic

    [Test] // TEST-SUPPORT: STAT-MIGRATE-35 | Tests: sMPGWM.Tests.ResourcePoolTests.ModifierInsertionOrderDoesNotChangeBaselineArithmetic
    public void ContributionsPreserveCurrentAndRemoveOnlyTheirOwner() // TEST-SUPPORT: STAT-MIGRATE-36 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
    { // TEST-SUPPORT: STAT-MIGRATE-37 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        var pool = new ShipStatState(); // TEST-SUPPORT: STAT-MIGRATE-38 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false)); // TEST-SUPPORT: STAT-MIGRATE-39 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        pool.SetContribution(new(StatContributionKind.Effect, 1), 20, 0); // TEST-SUPPORT: STAT-MIGRATE-40 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        pool.SetContribution(new(StatContributionKind.Effect, 2), 50, 0); // TEST-SUPPORT: STAT-MIGRATE-41 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(170)); // TEST-SUPPORT: STAT-MIGRATE-42 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        pool.SetContribution(new(StatContributionKind.Effect, 2), 50, 0); // TEST-SUPPORT: STAT-MIGRATE-43 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(170)); // TEST-SUPPORT: STAT-MIGRATE-44 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        pool.RemoveSource(new(StatContributionKind.Effect, 2)); // TEST-SUPPORT: STAT-MIGRATE-45 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(120)); // TEST-SUPPORT: STAT-MIGRATE-46 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner

    } // TEST-SUPPORT: STAT-MIGRATE-48 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner

    [Test] // TEST-SUPPORT: STAT-MIGRATE-50 | Tests: sMPGWM.Tests.ResourcePoolTests.ContributionsPreserveCurrentAndRemoveOnlyTheirOwner
    public void FractionsUseBaseAndCapacityReductionClampsCurrent() // TEST-SUPPORT: STAT-MIGRATE-51 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
    { // TEST-SUPPORT: STAT-MIGRATE-52 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        var resource = new ResourcePool(100, 100); // TEST-SUPPORT: STAT-MIGRATE-53 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        var pool = new ShipStatState(); // TEST-SUPPORT: STAT-MIGRATE-54 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        pool.SetEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 0, 0, 0, 0, false, false, true, false, false)); // TEST-SUPPORT: STAT-MIGRATE-55 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        pool.SetContribution(new(StatContributionKind.Effect, 1), 50, .2); // TEST-SUPPORT: STAT-MIGRATE-56 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        Assert.That(pool.Effective.ShieldCapacity, Is.EqualTo(180)); // TEST-SUPPORT: EQUIP-RESOURCE-UPDATE-2 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        pool.SetContribution(new(StatContributionKind.Effect, 2), -100, 0); // TEST-SUPPORT: STAT-MIGRATE-58 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        resource.SetCapacity(pool.Effective.ShieldCapacity); // TEST-SUPPORT: STAT-MIGRATE-59 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        Assert.That(resource.Current, Is.EqualTo(60)); // TEST-SUPPORT: EQUIP-RESOURCE-UPDATE-3 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        pool.RemoveSource(new(StatContributionKind.Effect, 2)); // TEST-SUPPORT: STAT-MIGRATE-61 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        resource.SetCapacity(pool.Effective.ShieldCapacity); // TEST-SUPPORT: STAT-MIGRATE-62 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
        Assert.That(resource.Current, Is.EqualTo(60)); // TEST-SUPPORT: EQUIP-RESOURCE-UPDATE-4 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
    } // TEST-SUPPORT: STAT-MIGRATE-64 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
} // TEST-SUPPORT: STAT-MIGRATE-65 | Tests: sMPGWM.Tests.ResourcePoolTests.FractionsUseBaseAndCapacityReductionClampsCurrent
