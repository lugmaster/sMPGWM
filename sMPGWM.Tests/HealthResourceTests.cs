using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class HealthResourceTests
{
    [Test]
    public void InitializesAndPublishesValidatedHealthChanges()
    {
        using var ship = new ShipRuntime(150);
        Assert.That(ship.Health, Is.EqualTo(new ResourceState(150, 150, 0)));
        ship.InitializeResources();
        Assert.That(ship.Health, Is.EqualTo(new ResourceState(150, 150, 150)));
        Assert.That(ship.Resources.Health, Is.EqualTo(ship.Health));
        var notifications = 0;
        ship.Changed += () => notifications++;
        var revision = ship.Revision;
        Assert.That(ship.SetHealthCurrent(87.5), Is.True);
        Assert.That(ship.Health.Current, Is.EqualTo(87.5));
        Assert.That(ship.Revision, Is.EqualTo(revision + 1));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(ship.SetHealthCurrent(87.5), Is.False);
        ship.InitializeResources();
        Assert.That(ship.Health.Current, Is.EqualTo(87.5));
        Assert.That(ship.Revision, Is.EqualTo(revision + 1));
        Assert.That(notifications, Is.EqualTo(1));
        Assert.That(ship.SetHealthCurrent(0), Is.True);
        Assert.That(ship.Health.Current, Is.Zero);
        Assert.That(ship.SetHealthCurrent(150), Is.True);
        Assert.That(ship.Health.Current, Is.EqualTo(150));
        ship.Dispose();
        var disposedRevision = ship.Revision;
        Assert.That(ship.SetHealthCurrent(20), Is.False);
        Assert.That(ship.Health.Capacity, Is.Zero);
        Assert.That(ship.Revision, Is.EqualTo(disposedRevision));
    }

    [TestCase(0d)]
    [TestCase(-1d)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void RejectsInvalidShipHealthCapacity(double capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShipRuntime(capacity));
    }

    [TestCase(-1d)]
    [TestCase(150.1d)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void RejectsInvalidCurrentWithoutPublishing(double current)
    {
        using var ship = new ShipRuntime(150);
        ship.InitializeResources();
        var before = ship.Health;
        var revision = ship.Revision;
        var notifications = 0;
        ship.Changed += () => notifications++;
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.SetHealthCurrent(current));
        Assert.That(ship.Health, Is.EqualTo(before));
        Assert.That(ship.Revision, Is.EqualTo(revision));
        Assert.That(notifications, Is.Zero);
    }

    [Test]
    public void EquipmentAndEffectsCannotReplaceShipHealth()
    {
        var definitions = GameplayTestData.Load();
        using var abilities = new AbilityRuntime(definitions);
        using var ship = new ShipRuntime(150);
        var inputs = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 20, 100, 1000, 100, 120, 60, 100, true, true, true, true, true);
        ship.ApplyEquipment(inputs, 1);
        ship.InitializeResources();
        ship.SetHealthCurrent(74);
        var health = ship.Health;
        ship.Grants.SetSource("player", new[] { "shieldCapacityBoost" });
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 1), Is.EqualTo(AbilityActivationResult.Accepted));
        Assert.That(ship.Health, Is.EqualTo(health));
        ship.ApplyEquipment(inputs with { Armor = 80, ShieldCapacity = 200, EnergyStorage = 300 }, 2);
        Assert.That(ship.Health, Is.EqualTo(health));
        ship.StageComponentChange(ShipComponentKind.All, false);
        ship.CommitConfiguration(2);
        ship.AdvanceResources(3);
        Assert.That(ship.Health, Is.EqualTo(health));
        ship.StageComponentChange(ShipComponentKind.All, true);
        ship.CommitConfiguration(4);
        ship.ApplyEquipment(default, 0);
        Assert.That(ship.Health, Is.EqualTo(health));
    }

    [Test]
    public void RestorationPreservesHealthAndRejectsInvalidStateAtomically()
    {
        var definitions = GameplayTestData.Load();
        using var source = new ShipRuntime(150);
        source.InitializeResources();
        source.SetHealthCurrent(61);
        var saved = source.CaptureState();
        Assert.That(saved.HealthCapacity, Is.EqualTo(150));
        Assert.That(saved.HealthCurrent, Is.EqualTo(61));
        using var restored = new ShipRuntime(150);
        var revision = restored.Revision;
        foreach (var invalid in new[] { saved with { HealthCapacity = 200 }, saved with { HealthCapacity = double.NaN }, saved with { HealthCurrent = -1 }, saved with { HealthCurrent = 151 }, saved with { HealthCurrent = double.NaN }, saved with { HealthCurrent = double.PositiveInfinity } })
        {
            Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid, definitions));
            Assert.That(restored.Health, Is.EqualTo(new ResourceState(150, 150, 0)));
            Assert.That(restored.Revision, Is.EqualTo(revision));
        }
        restored.RestoreState(saved, definitions);
        Assert.That(restored.Health, Is.EqualTo(source.Health));
        Assert.That(restored.Revision, Is.EqualTo(source.Revision));
        restored.InitializeResources();
        Assert.That(restored.Health.Current, Is.EqualTo(61));
        using var differentShip = new ShipRuntime(200);
        Assert.Throws<ArgumentException>(() => differentShip.RestoreState(saved, definitions));
    }
}

