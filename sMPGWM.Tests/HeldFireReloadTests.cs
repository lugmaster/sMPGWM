using Godot; // TEST-SUPPORT: HELD-RELOAD-001 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: HELD-RELOAD-002 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
using sMPGWM.Scripts.InventorySystem; // TEST-SUPPORT: HELD-RELOAD-003 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
using sMPGWM.Scripts.WeaponSystem; // TEST-SUPPORT: HELD-RELOAD-004 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
using sMPGWM.Scripts.WorldSystem; // TEST-SUPPORT: HELD-RELOAD-005 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload

namespace sMPGWM.Tests; // TEST-SUPPORT: HELD-RELOAD-006 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload

public sealed class HeldFireReloadTests // TEST-SUPPORT: HELD-RELOAD-007 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
{ // TEST-SUPPORT: HELD-RELOAD-008 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
    [TestCase(false), TestCase(true), Category("Godot")] // TEST-SUPPORT: HELD-RELOAD-009 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
    public void PrimaryFireIdentifiesEveryShotAcrossReload(bool disableHeavy) // TEST-SUPPORT: HELD-RELOAD-010 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
    { // TEST-SUPPORT: HELD-RELOAD-011 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        using var inventory = new InventoryController(); // TEST-SUPPORT: HELD-RELOAD-012 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        var world = new GameWorld { AuthoritativeItems = true, InitialInventory = inventory.Snapshot, InitializeLoadout = StarterLoadout.Equip }; // TEST-SUPPORT: HELD-RELOAD-013 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: HELD-RELOAD-014 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        using var weapons = new WeaponSystem(); // TEST-SUPPORT: HELD-RELOAD-015 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        try // TEST-SUPPORT: HELD-RELOAD-016 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        { // TEST-SUPPORT: HELD-RELOAD-017 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            world.Spawn(new(1, Guid.NewGuid().ToString("N"), "fighter", new(30000, 30000), 0, Vector2.Zero)); // TEST-SUPPORT: HELD-RELOAD-018 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var ship = world.Players[1].ControlledShip!; // TEST-SUPPORT: HELD-RELOAD-019 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var controller = ship.Loadout.Weapons; // TEST-SUPPORT: HELD-RELOAD-020 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var vulcan = controller.Bindings[ship.Equipment.FindSlot("weapon_1")]!; // TEST-SUPPORT: HELD-RELOAD-021 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var heavy = controller.Bindings[ship.Equipment.FindSlot("weapon_3")]!; // TEST-SUPPORT: HELD-RELOAD-022 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(controller.GetGroup(vulcan.Slot), Is.EqualTo(1)); // TEST-SUPPORT: HELD-RELOAD-023 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(controller.GetGroup(heavy.Slot), Is.EqualTo(1)); // TEST-SUPPORT: HELD-RELOAD-024 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            if (disableHeavy) Assert.That(controller.AssignGroup(heavy.Slot, heavy.ItemId, controller.ConfigurationRevision, 0), Is.True); // TEST-SUPPORT: HELD-RELOAD-025 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var vulcanTicks = new List<long>(); // TEST-SUPPORT: HELD-RELOAD-026 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var reloadShots = new List<long>(); // TEST-SUPPORT: HELD-RELOAD-027 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            var trace = new List<string>(); // TEST-SUPPORT: HELD-RELOAD-028 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            weapons.ShotProduced += shot => // TEST-SUPPORT: HELD-RELOAD-029 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            { // TEST-SUPPORT: HELD-RELOAD-030 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                if (shot.ItemId == vulcan.ItemId) vulcanTicks.Add(shot.Tick); // TEST-SUPPORT: HELD-RELOAD-031 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                if (shot.Tick > 237 && shot.Tick < 327) // TEST-SUPPORT: HELD-RELOAD-032 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                { // TEST-SUPPORT: HELD-RELOAD-033 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                    Assert.That(shot.ItemId, Is.EqualTo(heavy.ItemId)); // TEST-SUPPORT: HELD-RELOAD-034 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                    Assert.That(vulcan.State.Rounds, Is.Zero); // TEST-SUPPORT: HELD-RELOAD-035 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                    Assert.That(vulcan.State.ReloadDeadline, Is.EqualTo(327)); // TEST-SUPPORT: HELD-RELOAD-036 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                    reloadShots.Add(shot.Tick); // TEST-SUPPORT: HELD-RELOAD-037 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                } // TEST-SUPPORT: HELD-RELOAD-038 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                if (shot.Tick >= 233 && shot.Tick <= 339) // TEST-SUPPORT: HELD-RELOAD-039 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                    trace.Add($"tick={shot.Tick}, weapon={world.Items.GetItem(shot.ItemId).DefinitionId}, sequence={shot.Sequence}, profile={shot.ProfileId}"); // TEST-SUPPORT: HELD-RELOAD-040 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            }; // TEST-SUPPORT: HELD-RELOAD-041 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            ship.SetInput(new(Vector2.Zero, 0, false, false, false, true, 0, controller.ConfigurationRevision)); // TEST-SUPPORT: HELD-RELOAD-042 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            for (var tick = 0; tick <= 700; tick++) // TEST-SUPPORT: HELD-RELOAD-043 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            { // TEST-SUPPORT: HELD-RELOAD-044 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                ship.Runtime.CommitConfiguration(tick); // TEST-SUPPORT: HELD-RELOAD-045 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                ship.Runtime.AdvanceResources(tick); // TEST-SUPPORT: HELD-RELOAD-046 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                if (tick == 1) ship.SetInput(ship.InputState with { FireMask = 1 }); // TEST-SUPPORT: HELD-RELOAD-047 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                weapons.Step(tick, world, _ => true); // TEST-SUPPORT: HELD-RELOAD-048 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            } // TEST-SUPPORT: HELD-RELOAD-049 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(vulcanTicks.Count, Is.GreaterThan(120)); // TEST-SUPPORT: HELD-RELOAD-050 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            for (var i = 1; i < vulcanTicks.Count; i++) // TEST-SUPPORT: HELD-RELOAD-051 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
                Assert.That(vulcanTicks[i] - vulcanTicks[i - 1], Is.EqualTo(i % 60 == 0 ? 90 : 4), $"Vulcan shot {i + 1}"); // TEST-SUPPORT: HELD-RELOAD-052 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(vulcanTicks[59], Is.EqualTo(237)); // TEST-SUPPORT: HELD-RELOAD-053 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(vulcanTicks[60], Is.EqualTo(327)); // TEST-SUPPORT: HELD-RELOAD-054 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(vulcanTicks[61], Is.EqualTo(331)); // TEST-SUPPORT: HELD-RELOAD-055 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            Assert.That(reloadShots, Is.EqualTo(disableHeavy ? Array.Empty<long>() : new long[] { 241, 301 })); // TEST-SUPPORT: HELD-RELOAD-056 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            TestContext.Out.WriteLine($"Heavy disabled: {disableHeavy}\n" + string.Join("\n", trace)); // TEST-SUPPORT: HELD-RELOAD-057 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        } // TEST-SUPPORT: HELD-RELOAD-058 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        finally // TEST-SUPPORT: HELD-RELOAD-059 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        { // TEST-SUPPORT: HELD-RELOAD-060 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            world.Clear(); // TEST-SUPPORT: HELD-RELOAD-061 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
            world.Free(); // TEST-SUPPORT: HELD-RELOAD-062 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
        } // TEST-SUPPORT: HELD-RELOAD-063 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
    } // TEST-SUPPORT: HELD-RELOAD-064 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
} // TEST-SUPPORT: HELD-RELOAD-065 | Tests: sMPGWM.Tests.HeldFireReloadTests.PrimaryFireIdentifiesEveryShotAcrossReload
