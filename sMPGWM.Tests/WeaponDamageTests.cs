using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.StatSystem;

namespace sMPGWM.Tests;

public sealed class WeaponDamageTests
{
    private static ShipRuntime Create(double shield, double armor = 0)
    {
        var ship = new ShipRuntime(100);
        ship.ApplyEquipment(new ResolvedShipStats(10, 20, 1, 1, 1, 10, armor, shield,
            1000, 1000, 120, 0, 100, true, true, true, true, true), 1);
        ship.InitializeResources();
        return ship;
    }

    [TestCase(25, 20, 5, 100)]
    [TestCase(20, 20, 0, 100)]
    [TestCase(5, 20, 0, 85)]
    public void ShieldAbsorbsBeforeHealth(double shield, double damage, double expectedShield, double expectedHealth)
    {
        using var ship = Create(shield);
        Assert.That(ship.ApplyDamage(damage), Is.True);
        Assert.That(ship.Shield.Current, Is.EqualTo(expectedShield));
        Assert.That(ship.Health.Current, Is.EqualTo(expectedHealth));
        if (expectedShield == 0) Assert.That(ship.ShieldDowntimeDeadline, Is.EqualTo(60));
    }

    [TestCase(0, 15)]
    [TestCase(50, 10)]
    [TestCase(100, 7.5)]
    [TestCase(200, 5)]
    public void ArmorPreservesFractionalOverflow(double armor, double healthDamage)
    {
        using var ship = Create(5, armor);
        Assert.That(ship.ApplyDamage(20), Is.True);
        Assert.That(ship.Health.Current, Is.EqualTo(100 - healthDamage).Within(1e-10));
    }

    [Test]
    public void UnavailableChargedShieldDoesNotAbsorbAndLaterHitsDoNotReportShield()
    {
        using var ship = Create(5);
        ship.StageComponentChange(ShipComponentKind.Shield, false);
        ship.CommitConfiguration(0);
        Assert.That(ship.ApplyDamage(20), Is.False);
        Assert.That(ship.Shield.Current, Is.EqualTo(5));
        Assert.That(ship.Health.Current, Is.EqualTo(80));
        ship.StageComponentChange(ShipComponentKind.Shield, true);
        ship.CommitConfiguration(1);
        Assert.That(ship.ApplyDamage(5), Is.True);
        Assert.That(ship.ApplyDamage(1), Is.False);
        Assert.That(ship.Health.Current, Is.EqualTo(79));
    }

    [Test]
    public void DeathOccursOnceAndStopsResourceAndActionAdmission()
    {
        using var ship = Create(5);
        ship.SetAfterburner(true);
        var revision = ship.Revision;
        Assert.That(ship.ApplyDamage(1000), Is.True);
        Assert.That(ship.IsAlive, Is.False);
        Assert.That(ship.MovementAvailable, Is.False);
        Assert.That(ship.WeaponsAvailable, Is.False);
        Assert.That(ship.AfterburnerActive, Is.False);
        Assert.That(ship.Revision, Is.EqualTo(revision + 1));
        revision = ship.Revision;
        Assert.That(ship.ApplyDamage(1000), Is.False);
        Assert.That(ship.Revision, Is.EqualTo(revision));
        Assert.That(ship.AdvanceResources(100), Is.False);
        Assert.That(ship.Shield.Current, Is.Zero);
        Assert.That(ship.Energy.Current, Is.Zero);
        ship.SetAfterburner(true);
        Assert.That(ship.AfterburnerActive, Is.False);
        Assert.Throws<InvalidOperationException>(() => ship.SetHealthCurrent(1));
        Assert.Throws<InvalidOperationException>(() => ship.SetCondition(GameplayTestData.Load().Conditions.GetById("dead"), false));
        Assert.That(ship.Revision, Is.EqualTo(revision));
    }

    [Test]
    public void RestoredDeadStateRequiresZeroHealthAndNoImplicitRevival()
    {
        var definitions = GameplayTestData.Load();
        using var ship = new ShipRuntime(100);
        ship.InitializeResources();
        ship.ApplyDamage(100);
        var saved = ship.CaptureState();
        using var restored = new ShipRuntime(100);
        Assert.Throws<ArgumentException>(() => restored.RestoreState(saved with { HealthCurrent = 1 }, definitions));
        Assert.Throws<ArgumentException>(() => restored.RestoreState(saved with { Conditions = Array.Empty<string>() }, definitions));
        restored.RestoreState(saved, definitions);
        restored.InitializeResources();
        Assert.That(restored.IsAlive, Is.False);
        Assert.That(restored.Health.Current, Is.Zero);
    }

    [Test]
    public void ZeroDamageStillReportsShieldAndInvalidDamageCannotMutate()
    {
        using var ship = Create(5);
        var revision = ship.Revision;
        Assert.That(ship.ApplyDamage(0), Is.True);
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.ApplyDamage(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.ApplyDamage(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.ApplyDamage(double.PositiveInfinity));
        Assert.That(ship.Revision, Is.EqualTo(revision));
    }
}
