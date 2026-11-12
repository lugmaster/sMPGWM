using sMPGWM.Scripts.AbilitySystem; // TEST-SUPPORT: HEALTH-RESOURCE-001 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
using sMPGWM.Scripts.ShipSystem; // TEST-SUPPORT: HEALTH-RESOURCE-002 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
using sMPGWM.Scripts.StatSystem; // TEST-SUPPORT: HEALTH-RESOURCE-003 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically

namespace sMPGWM.Tests; // TEST-SUPPORT: HEALTH-RESOURCE-004 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically

public sealed class HealthResourceTests // TEST-SUPPORT: HEALTH-RESOURCE-005 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
{ // TEST-SUPPORT: HEALTH-RESOURCE-006 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
    [Test] // TEST-SUPPORT: HEALTH-RESOURCE-007 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
    public void InitializesAndPublishesValidatedHealthChanges() // TEST-SUPPORT: HEALTH-RESOURCE-008 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
    { // TEST-SUPPORT: HEALTH-RESOURCE-009 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        using var ship = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-RESOURCE-010 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health, Is.EqualTo(new ResourceState(150, 150, 0))); // TEST-SUPPORT: HEALTH-RESOURCE-011 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        ship.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-012 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health, Is.EqualTo(new ResourceState(150, 150, 150))); // TEST-SUPPORT: HEALTH-RESOURCE-013 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Resources.Health, Is.EqualTo(ship.Health)); // TEST-SUPPORT: HEALTH-RESOURCE-014 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        var notifications = 0; // TEST-SUPPORT: HEALTH-RESOURCE-015 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        ship.Changed += () => notifications++; // TEST-SUPPORT: HEALTH-RESOURCE-016 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        var revision = ship.Revision; // TEST-SUPPORT: HEALTH-RESOURCE-017 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.SetHealthCurrent(87.5), Is.True); // TEST-SUPPORT: HEALTH-RESOURCE-018 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health.Current, Is.EqualTo(87.5)); // TEST-SUPPORT: HEALTH-RESOURCE-019 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Revision, Is.EqualTo(revision + 1)); // TEST-SUPPORT: HEALTH-RESOURCE-020 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(notifications, Is.EqualTo(1)); // TEST-SUPPORT: HEALTH-RESOURCE-021 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.SetHealthCurrent(87.5), Is.False); // TEST-SUPPORT: HEALTH-RESOURCE-022 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        ship.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-023 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health.Current, Is.EqualTo(87.5)); // TEST-SUPPORT: HEALTH-RESOURCE-024 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Revision, Is.EqualTo(revision + 1)); // TEST-SUPPORT: HEALTH-RESOURCE-025 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(notifications, Is.EqualTo(1)); // TEST-SUPPORT: HEALTH-RESOURCE-026 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.SetHealthCurrent(0), Is.True); // TEST-SUPPORT: HEALTH-RESOURCE-027 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health.Current, Is.Zero); // TEST-SUPPORT: HEALTH-RESOURCE-028 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.SetHealthCurrent(150), Is.True); // TEST-SUPPORT: HEALTH-RESOURCE-029 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health.Current, Is.EqualTo(150)); // TEST-SUPPORT: HEALTH-RESOURCE-030 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        ship.Dispose(); // TEST-SUPPORT: HEALTH-RESOURCE-031 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        var disposedRevision = ship.Revision; // TEST-SUPPORT: HEALTH-RESOURCE-032 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.SetHealthCurrent(20), Is.False); // TEST-SUPPORT: HEALTH-RESOURCE-033 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Health.Capacity, Is.Zero); // TEST-SUPPORT: HEALTH-RESOURCE-034 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
        Assert.That(ship.Revision, Is.EqualTo(disposedRevision)); // TEST-SUPPORT: HEALTH-RESOURCE-035 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges
    } // TEST-SUPPORT: HEALTH-RESOURCE-036 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges

    [TestCase(0d)] // TEST-SUPPORT: HEALTH-RESOURCE-037 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    [TestCase(-1d)] // TEST-SUPPORT: HEALTH-RESOURCE-038 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    [TestCase(double.NaN)] // TEST-SUPPORT: HEALTH-RESOURCE-039 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    [TestCase(double.PositiveInfinity)] // TEST-SUPPORT: HEALTH-RESOURCE-040 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    [TestCase(double.NegativeInfinity)] // TEST-SUPPORT: HEALTH-RESOURCE-041 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    public void RejectsInvalidShipHealthCapacity(double capacity) // TEST-SUPPORT: HEALTH-RESOURCE-042 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    { // TEST-SUPPORT: HEALTH-RESOURCE-043 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
        Assert.Throws<ArgumentOutOfRangeException>(() => new ShipRuntime(capacity)); // TEST-SUPPORT: HEALTH-RESOURCE-044 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity
    } // TEST-SUPPORT: HEALTH-RESOURCE-045 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity

    [TestCase(-1d)] // TEST-SUPPORT: HEALTH-RESOURCE-046 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    [TestCase(150.1d)] // TEST-SUPPORT: HEALTH-RESOURCE-047 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    [TestCase(double.NaN)] // TEST-SUPPORT: HEALTH-RESOURCE-048 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    [TestCase(double.PositiveInfinity)] // TEST-SUPPORT: HEALTH-RESOURCE-049 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    [TestCase(double.NegativeInfinity)] // TEST-SUPPORT: HEALTH-RESOURCE-050 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    public void RejectsInvalidCurrentWithoutPublishing(double current) // TEST-SUPPORT: HEALTH-RESOURCE-051 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    { // TEST-SUPPORT: HEALTH-RESOURCE-052 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        using var ship = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-RESOURCE-053 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        ship.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-054 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        var before = ship.Health; // TEST-SUPPORT: HEALTH-RESOURCE-055 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        var revision = ship.Revision; // TEST-SUPPORT: HEALTH-RESOURCE-056 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        var notifications = 0; // TEST-SUPPORT: HEALTH-RESOURCE-057 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        ship.Changed += () => notifications++; // TEST-SUPPORT: HEALTH-RESOURCE-058 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.SetHealthCurrent(current)); // TEST-SUPPORT: HEALTH-RESOURCE-059 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        Assert.That(ship.Health, Is.EqualTo(before)); // TEST-SUPPORT: HEALTH-RESOURCE-060 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        Assert.That(ship.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: HEALTH-RESOURCE-061 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
        Assert.That(notifications, Is.Zero); // TEST-SUPPORT: HEALTH-RESOURCE-062 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing
    } // TEST-SUPPORT: HEALTH-RESOURCE-063 | Tests: sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing

    [Test] // TEST-SUPPORT: HEALTH-RESOURCE-064 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
    public void EquipmentAndEffectsCannotReplaceShipHealth() // TEST-SUPPORT: HEALTH-RESOURCE-065 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
    { // TEST-SUPPORT: HEALTH-RESOURCE-066 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: HEALTH-RESOURCE-067 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        using var abilities = new AbilityRuntime(definitions); // TEST-SUPPORT: HEALTH-RESOURCE-068 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        using var ship = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-RESOURCE-069 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        var inputs = new ResolvedShipStats(200, 300, 2, 100, 150, 10, 20, 100, 1000, 100, 120, 60, 100, true, true, true, true, true); // TEST-SUPPORT: HEALTH-RESOURCE-070 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.ApplyEquipment(inputs, 1); // TEST-SUPPORT: HEALTH-RESOURCE-071 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-072 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.SetHealthCurrent(74); // TEST-SUPPORT: HEALTH-RESOURCE-073 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        var health = ship.Health; // TEST-SUPPORT: HEALTH-RESOURCE-074 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.Grants.SetSource("player", new[] { "shieldCapacityBoost" }); // TEST-SUPPORT: HEALTH-RESOURCE-075 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        Assert.That(abilities.Activate("shieldCapacityBoost", new(ship, ship, 0), 1), Is.EqualTo(AbilityActivationResult.Accepted)); // TEST-SUPPORT: HEALTH-RESOURCE-076 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        Assert.That(ship.Health, Is.EqualTo(health)); // TEST-SUPPORT: HEALTH-RESOURCE-077 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.ApplyEquipment(inputs with { Armor = 80, ShieldCapacity = 200, EnergyStorage = 300 }, 2); // TEST-SUPPORT: HEALTH-RESOURCE-078 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        Assert.That(ship.Health, Is.EqualTo(health)); // TEST-SUPPORT: HEALTH-RESOURCE-079 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.StageComponentChange(ShipComponentKind.All, false); // TEST-SUPPORT: HEALTH-RESOURCE-080 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.CommitConfiguration(2); // TEST-SUPPORT: HEALTH-RESOURCE-081 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.AdvanceResources(3); // TEST-SUPPORT: HEALTH-RESOURCE-082 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        Assert.That(ship.Health, Is.EqualTo(health)); // TEST-SUPPORT: HEALTH-RESOURCE-083 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.StageComponentChange(ShipComponentKind.All, true); // TEST-SUPPORT: HEALTH-RESOURCE-084 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.CommitConfiguration(4); // TEST-SUPPORT: HEALTH-RESOURCE-085 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        ship.ApplyEquipment(default, 0); // TEST-SUPPORT: HEALTH-RESOURCE-086 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
        Assert.That(ship.Health, Is.EqualTo(health)); // TEST-SUPPORT: HEALTH-RESOURCE-087 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth
    } // TEST-SUPPORT: HEALTH-RESOURCE-088 | Tests: sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth

    [Test] // TEST-SUPPORT: HEALTH-RESOURCE-089 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
    public void RestorationPreservesHealthAndRejectsInvalidStateAtomically() // TEST-SUPPORT: HEALTH-RESOURCE-090 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
    { // TEST-SUPPORT: HEALTH-RESOURCE-091 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        var definitions = GameplayTestData.Load(); // TEST-SUPPORT: HEALTH-RESOURCE-092 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        using var source = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-RESOURCE-093 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        source.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-094 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        source.SetHealthCurrent(61); // TEST-SUPPORT: HEALTH-RESOURCE-095 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        var saved = source.CaptureState(); // TEST-SUPPORT: HEALTH-RESOURCE-096 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.That(saved.HealthCapacity, Is.EqualTo(150)); // TEST-SUPPORT: HEALTH-RESOURCE-097 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.That(saved.HealthCurrent, Is.EqualTo(61)); // TEST-SUPPORT: HEALTH-RESOURCE-098 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        using var restored = new ShipRuntime(150); // TEST-SUPPORT: HEALTH-RESOURCE-099 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        var revision = restored.Revision; // TEST-SUPPORT: HEALTH-RESOURCE-100 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        foreach (var invalid in new[] { saved with { HealthCapacity = 200 }, saved with { HealthCapacity = double.NaN }, saved with { HealthCurrent = -1 }, saved with { HealthCurrent = 151 }, saved with { HealthCurrent = double.NaN }, saved with { HealthCurrent = double.PositiveInfinity } }) // TEST-SUPPORT: HEALTH-RESOURCE-101 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        { // TEST-SUPPORT: HEALTH-RESOURCE-102 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
            Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid, definitions)); // TEST-SUPPORT: HEALTH-RESOURCE-103 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
            Assert.That(restored.Health, Is.EqualTo(new ResourceState(150, 150, 0))); // TEST-SUPPORT: HEALTH-RESOURCE-104 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
            Assert.That(restored.Revision, Is.EqualTo(revision)); // TEST-SUPPORT: HEALTH-RESOURCE-105 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        } // TEST-SUPPORT: HEALTH-RESOURCE-106 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        restored.RestoreState(saved, definitions); // TEST-SUPPORT: HEALTH-RESOURCE-107 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.That(restored.Health, Is.EqualTo(source.Health)); // TEST-SUPPORT: HEALTH-RESOURCE-108 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.That(restored.Revision, Is.EqualTo(source.Revision)); // TEST-SUPPORT: HEALTH-RESOURCE-109 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        restored.InitializeResources(); // TEST-SUPPORT: HEALTH-RESOURCE-110 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.That(restored.Health.Current, Is.EqualTo(61)); // TEST-SUPPORT: HEALTH-RESOURCE-111 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        using var differentShip = new ShipRuntime(200); // TEST-SUPPORT: HEALTH-RESOURCE-112 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
        Assert.Throws<ArgumentException>(() => differentShip.RestoreState(saved, definitions)); // TEST-SUPPORT: HEALTH-RESOURCE-113 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
    } // TEST-SUPPORT: HEALTH-RESOURCE-114 | Tests: sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically
} // TEST-SUPPORT: HEALTH-RESOURCE-115 | Tests: sMPGWM.Tests.HealthResourceTests.InitializesAndPublishesValidatedHealthChanges, sMPGWM.Tests.HealthResourceTests.RejectsInvalidShipHealthCapacity, sMPGWM.Tests.HealthResourceTests.RejectsInvalidCurrentWithoutPublishing, sMPGWM.Tests.HealthResourceTests.EquipmentAndEffectsCannotReplaceShipHealth, sMPGWM.Tests.HealthResourceTests.RestorationPreservesHealthAndRejectsInvalidStateAtomically

