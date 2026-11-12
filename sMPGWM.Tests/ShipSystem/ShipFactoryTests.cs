using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;

namespace sMPGWM.Tests.ShipSystem; // TEST-SUPPORT: CLEANUP-SHIP-051 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry

[Category("Godot")] // TEST-SUPPORT: CLEANUP-SHIP-001 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
public class ShipFactoryTests // TEST-SUPPORT: CLEANUP-SHIP-002 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode, sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
{
    [Test] // TEST-SUPPORT: CLEANUP-SHIP-003 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
    public void LoadoutRequiresExplicitConfiguration() // TEST-SUPPORT: CLEANUP-SHIP-004 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
    {
        var ship = new ShipController(); // TEST-SUPPORT: CLEANUP-SHIP-005 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(ship); // TEST-SUPPORT: CLEANUP-SHIP-006 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
        var store = new ItemStore(); // TEST-SUPPORT: CLEANUP-SHIP-007 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
        try
        {
            Assert.Throws<InvalidOperationException>(() => _ = ship.Loadout); // TEST-SUPPORT: CLEANUP-SHIP-008 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            Assert.Throws<InvalidOperationException>(() => _ = ship.Equipment); // TEST-SUPPORT: CLEANUP-SHIP-009 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            ship.ConfigureLoadout(store, false); // TEST-SUPPORT: CLEANUP-SHIP-010 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            var loadout = ship.Loadout; // TEST-SUPPORT: CLEANUP-SHIP-011 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            Assert.That(loadout.Store, Is.SameAs(store)); // TEST-SUPPORT: CLEANUP-SHIP-012 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            ship.ConfigureLoadout(store, false); // TEST-SUPPORT: CLEANUP-SHIP-013 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            Assert.That(ship.Loadout, Is.SameAs(loadout)); // TEST-SUPPORT: CLEANUP-SHIP-014 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
            Assert.Throws<InvalidOperationException>(() => ship.ConfigureLoadout(new ItemStore(), false)); // TEST-SUPPORT: CLEANUP-SHIP-015 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
        }
        finally { ship.Free(); } // TEST-SUPPORT: CLEANUP-SHIP-016 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
        Assert.That(store.IsEmpty, Is.True); // TEST-SUPPORT: CLEANUP-SHIP-017 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.LoadoutRequiresExplicitConfiguration
    }

    [Test] // TEST-SUPPORT: CLEANUP-SHIP-018 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
    public void FailureBeforeLoadoutPreservesCauseAndReleasesNode() // TEST-SUPPORT: CLEANUP-SHIP-019 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
    {
        GamePlayProvider.CreateShip().Free(); // TEST-SUPPORT: CLEANUP-SHIP-020 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        var data = ShipRegistry.GetById("fighter"); // TEST-SUPPORT: CLEANUP-SHIP-021 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        var store = new ItemStore(); // TEST-SUPPORT: CLEANUP-SHIP-022 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        using var inventory = new InventoryController(store); // TEST-SUPPORT: CLEANUP-SHIP-023 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        InvalidOperationException? failure = null; // TEST-SUPPORT: CLEANUP-SHIP-024 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        inventory.Changed += _ => failure = Assert.Throws<InvalidOperationException>(() => // TEST-SUPPORT: CLEANUP-SHIP-025 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
            ShipFactory.Create(data, "failed-before-loadout", Vector2.Zero, 0, Vector2.Zero, store, false)); // TEST-SUPPORT: CLEANUP-SHIP-026 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        var orphanCount = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount); // TEST-SUPPORT: CLEANUP-SHIP-027 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(store.CreateItems(inventory.Container, "mock_standard_engine", 1), Is.EqualTo(InventoryResult.Success)); // TEST-SUPPORT: CLEANUP-SHIP-028 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(failure, Is.Not.Null); // TEST-SUPPORT: CLEANUP-SHIP-029 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(failure!.InnerException, Is.TypeOf<InvalidOperationException>()); // TEST-SUPPORT: CLEANUP-SHIP-030 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(failure.InnerException!.Message, Is.EqualTo("Cannot create storage during publication.")); // TEST-SUPPORT: CLEANUP-SHIP-031 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount), Is.EqualTo(orphanCount)); // TEST-SUPPORT: CLEANUP-SHIP-032 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        inventory.Dispose(); // TEST-SUPPORT: CLEANUP-SHIP-033 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
        Assert.That(store.IsEmpty, Is.True); // TEST-SUPPORT: CLEANUP-SHIP-034 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureBeforeLoadoutPreservesCauseAndReleasesNode
    }

    [Test] // TEST-SUPPORT: CLEANUP-SHIP-035 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
    public void FailureAfterLoadoutReleasesStoreAndAllowsRetry() // TEST-SUPPORT: CLEANUP-SHIP-036 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
    {
        GamePlayProvider.CreateShip().Free(); // TEST-SUPPORT: CLEANUP-SHIP-037 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var data = ShipRegistry.GetById("fighter"); // TEST-SUPPORT: CLEANUP-SHIP-038 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var invalid = new ShipData(new ShipDefinition { Name = "invalid-texture", TexturePath = "res://scenes/game/game_objects/ship.tscn" }, Array.Empty<Vector2>(), data.Stats); // TEST-SUPPORT: CLEANUP-SHIP-039 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var store = new ItemStore(); // TEST-SUPPORT: CLEANUP-SHIP-040 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var orphanCount = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount); // TEST-SUPPORT: CLEANUP-SHIP-041 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var failure = Assert.Throws<InvalidOperationException>(() => ShipFactory.Create(invalid, "failed-after-loadout", Vector2.Zero, 0, Vector2.Zero, store, false)); // TEST-SUPPORT: CLEANUP-SHIP-042 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        Assert.That(failure!.InnerException, Is.TypeOf<InvalidCastException>()); // TEST-SUPPORT: CLEANUP-SHIP-043 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        Assert.That(store.IsEmpty, Is.True); // TEST-SUPPORT: CLEANUP-SHIP-044 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        Assert.That(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount), Is.EqualTo(orphanCount)); // TEST-SUPPORT: CLEANUP-SHIP-045 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        var ship = ShipFactory.Create(data, "retry", Vector2.Zero, 0, Vector2.Zero, store, false); // TEST-SUPPORT: CLEANUP-SHIP-046 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(ship); // TEST-SUPPORT: CLEANUP-SHIP-047 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        try { Assert.That(ship.Loadout.Store, Is.SameAs(store)); } // TEST-SUPPORT: CLEANUP-SHIP-048 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        finally { ship.Free(); } // TEST-SUPPORT: CLEANUP-SHIP-049 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
        Assert.That(store.IsEmpty, Is.True); // TEST-SUPPORT: CLEANUP-SHIP-050 | Tests: sMPGWM.Tests.ShipSystem.ShipFactoryTests.FailureAfterLoadoutReleasesStoreAndAllowsRetry
    }
}
