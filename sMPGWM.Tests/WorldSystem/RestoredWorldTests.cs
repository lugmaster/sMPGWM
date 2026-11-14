using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.WorldSystem;

[Category("Godot")]
public sealed class RestoredWorldTests
{
    [Test]
    public void CapturedWorldRestoresOwnersItemsResourcesEffectsAndContinuityWithoutDefaults()
    {
        var source = CreateSource(out var abilities);
        var target = new GameWorld { AuthoritativeItems = true, InitializeLoadout = (_, _) => Assert.Fail("Restore applied starter defaults") };
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(target);
            var saved = source.CaptureState(120, abilities.CaptureSequence());
            var published = 0;
            target.EntitySpawned += _ =>
            {
                Assert.That(target.Players.Count, Is.EqualTo(2));
                Assert.That(target.Entities.Count, Is.EqualTo(3));
                Assert.That(target.Entities[saved.Ships[1].Entity.EntityId].Runtime.Shield.Current, Is.EqualTo(saved.Ships[1].Runtime.ShieldCurrent));
                published++;
            };
            target.RestoreState(saved);
            Assert.That(target.InitialCompletedTick, Is.EqualTo(120));
            Assert.That(target.Players.Count, Is.EqualTo(2));
            Assert.That(target.Entities.Count, Is.EqualTo(3));
            foreach (var player in saved.Players)
            {
                var restored = target.Players[player.Binding.Peer];
                Assert.That(restored.ControlGeneration, Is.EqualTo(player.Binding.Generation));
                Assert.That(restored.ControlledShip?.EntityId ?? "", Is.EqualTo(player.Binding.Entity));
                Assert.That(restored.InventoryInitialized, Is.True);
                AssertContainer(target.Items, restored.Inventory.Container, player.Inventory);
            }
            foreach (var ship in saved.Ships)
            {
                var restored = target.Entities[ship.Entity.EntityId];
                Assert.That(restored.GlobalPosition, Is.EqualTo(ship.Entity.Position));
                Assert.That(restored.GlobalRotation, Is.EqualTo(ship.Entity.Rotation));
                Assert.That(restored.Velocity, Is.EqualTo(ship.Entity.Velocity));
                Assert.That(restored.CaptureMovementState(), Is.EqualTo(ship.Movement));
                AssertContainer(target.Items, restored.Equipment.Container, ship.Equipment);
                var runtime = restored.Runtime.CaptureState();
                Assert.That(runtime.EquipmentInputs, Is.EqualTo(ship.Runtime.EquipmentInputs));
                Assert.That(runtime.ShieldCurrent, Is.EqualTo(ship.Runtime.ShieldCurrent));
                Assert.That(runtime.EnergyCurrent, Is.EqualTo(ship.Runtime.EnergyCurrent));
                Assert.That(runtime.BatteryIdentity, Is.EqualTo(ship.Runtime.BatteryIdentity));
                Assert.That(runtime.AuthoritativeTick, Is.EqualTo(ship.Runtime.AuthoritativeTick));
                Assert.That(runtime.LastResourceTick, Is.EqualTo(ship.Runtime.LastResourceTick));
                Assert.That(runtime.ShieldDowntimeDeadline, Is.EqualTo(ship.Runtime.ShieldDowntimeDeadline));
                Assert.That(runtime.Revision, Is.EqualTo(ship.Runtime.Revision));
                Assert.That(runtime.ConfigurationRevision, Is.EqualTo(ship.Runtime.ConfigurationRevision));
                Assert.That(runtime.FactionId, Is.EqualTo(ship.Runtime.FactionId));
                Assert.That(runtime.Behaviours, Is.EqualTo(ship.Runtime.Behaviours));
                Assert.That(runtime.Conditions, Is.EqualTo(ship.Runtime.Conditions));
                Assert.That(runtime.Grants.Count, Is.EqualTo(ship.Runtime.Grants.Count));
                restored.Loadout.InitializeResources();
                Assert.That(restored.Runtime.Shield.Current, Is.EqualTo(ship.Runtime.ShieldCurrent));
                Assert.That(restored.Runtime.Energy.Current, Is.EqualTo(ship.Runtime.EnergyCurrent));
            }
            var activeShip = target.Players[2].ControlledShip!;
            Assert.That(activeShip.Runtime.ActiveBehaviours.Count, Is.EqualTo(1));
            Assert.That(activeShip.Runtime.Shield.Current, Is.EqualTo(37));
            Assert.That(activeShip.Runtime.Energy.Current, Is.GreaterThan(0));
            var inventorySnapshot = target.Players[2].Inventory.Snapshot;
            var equipmentSnapshot = activeShip.Equipment.Snapshot;
            target.RestoreState(saved);
            Assert.That(published, Is.EqualTo(3));
            Assert.That(target.Players[2].Inventory.Snapshot, Is.SameAs(inventorySnapshot));
            Assert.That(activeShip.Equipment.Snapshot, Is.SameAs(equipmentSnapshot));
            Assert.That(activeShip.Runtime.ActiveBehaviours.Count, Is.EqualTo(1));
            var recaptured = target.CaptureState(120, saved.AbilitySequence);
            Assert.That(recaptured.NextSpawnIndex, Is.EqualTo(saved.NextSpawnIndex));
            Assert.That(recaptured.AbilitySequence, Is.EqualTo(saved.AbilitySequence));
            Assert.That(source.Entities.Count, Is.EqualTo(3));
            Assert.That(source.Players[2].ControlledShip!.Runtime.Shield.Current, Is.EqualTo(37));
        }
        finally
        {
            abilities.Dispose();
            target.Clear(); target.Free();
            source.Clear(); source.Free();
        }
    }

    [TestCase("runtime")]
    [TestCase("items")]
    [TestCase("movement")]
    [TestCase("topology")]
    [TestCase("expired")]
    [TestCase("sequence")]
    public void MalformedWorldDoesNotPublishOrChangeSourceAndCanRetry(string invalid)
    {
        var source = CreateSource(out var abilities);
        var target = new GameWorld { AuthoritativeItems = true, InitializeLoadout = (_, _) => Assert.Fail("Restore applied defaults") };
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(target);
            var valid = source.CaptureState(120, abilities.CaptureSequence());
            var ships = new RestoredShipState[valid.Ships.Count];
            var players = new RestoredPlayerState[valid.Players.Count];
            for (var i = 0; i < ships.Length; i++) ships[i] = valid.Ships[i];
            for (var i = 0; i < players.Length; i++) players[i] = valid.Players[i];
            if (invalid == "runtime") ships[0] = ships[0] with { Runtime = ships[0].Runtime with { ShieldCurrent = double.NaN } };
            if (invalid == "movement") ships[0] = ships[0] with { Movement = ships[0].Movement with { ConfigurationRevision = -1 } };
            if (invalid == "items") players[0] = players[0] with
            {
                Inventory = new ItemContainerState(ships[0].Equipment.Id, players[0].Inventory.Revision,
                    players[0].Inventory.GrowthChunk, players[0].Inventory.Slots)
            };
            if (invalid == "topology") players[1] = players[1] with { Binding = players[1].Binding with { Entity = players[0].Binding.Entity } };
            if (invalid == "expired") ships[1] = ships[1] with
            {
                Runtime = ships[1].Runtime with
                {
                    Behaviours = new[] { ships[1].Runtime.Behaviours[0] with { Deadline = valid.CompletedTick } }
                }
            };
            var malformed = new RestoredWorldState(valid.CompletedTick, invalid == "sequence" ? 0 : valid.AbilitySequence, valid.NextSpawnIndex, ships, players);
            var publications = 0;
            target.EntitySpawned += _ => publications++;
            var sourceInventory = source.Players[1].Inventory.Snapshot;
            Assert.Throws<ArgumentException>(() => target.RestoreState(malformed));
            Assert.That(target.Entities, Is.Empty);
            Assert.That(target.Players, Is.Empty);
            Assert.That(target.Items.IsEmpty, Is.True);
            Assert.That(target.GetChildCount(), Is.Zero);
            Assert.That(publications, Is.Zero);
            Assert.That(source.Entities.Count, Is.EqualTo(3));
            Assert.That(source.Players[1].Inventory.Snapshot, Is.SameAs(sourceInventory));
            Assert.That(source.Players[2].ControlledShip!.Runtime.Shield.Current, Is.EqualTo(37));
            target.RestoreState(valid);
            Assert.That(target.Entities.Count, Is.EqualTo(3));
            Assert.That(target.Players.Count, Is.EqualTo(2));
            Assert.That(publications, Is.EqualTo(3));
            Assert.That(target.Players[2].ControlledShip!.Runtime.Shield.Current, Is.EqualTo(37));
        }
        finally
        {
            abilities.Dispose();
            target.Clear(); target.Free();
            source.Clear(); source.Free();
        }
    }

    private static GameWorld CreateSource(out AbilityRuntime abilities)
    {
        using var initial = new InventoryController();
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = StartLoadout.Equip };
        abilities = new AbilityRuntime(GameplayDefinitions.Current);
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
            world.ApproveSpawn(1, PlayerStartupData.Create());
            world.ApproveSpawn(2, PlayerStartupData.Create());
            var spare = world.SpawnEntity(new EntitySpawnRecord(Guid.NewGuid().ToString("N"), "fighter", new Vector2(900, 800), .25f, new Vector2(7, 3)));
            world.Players[1].AssignShip(spare);
            var second = world.Players[2].ControlledShip!;
            world.Players[2].AssignShip(null);
            world.Players[2].AssignShip(second);
            Assert.That(world.Items.CreateItems(world.Players[1].Inventory.Container, "mock_standard_engine", 2), Is.EqualTo(InventoryResult.Success));
            Assert.That(world.Items.CreateItems(world.Players[2].Inventory.Container, "mock_advanced_shield_generator", 1), Is.EqualTo(InventoryResult.Success));
            second.Runtime.SetFaction(3);
            Assert.That(abilities.Activate("shieldCapacityBoost", new(second.Runtime, second.Runtime, 0), 100), Is.EqualTo(AbilityActivationResult.Accepted));
            second.Runtime.AdvanceResources(120);
            second.Runtime.SetShieldCurrent(37);
            second.Teleport(new Vector2(120, 240), .5f, new Vector2(3, 4));
            second.SetInput(new ShipInputState(Vector2.Right, .5f, true, false, false, true));
            foreach (var ship in world.Entities.Values) ship.RefreshMovementConfiguration();
            return world;
        }
        catch
        {
            abilities.Dispose();
            world.Clear(); world.Free();
            throw;
        }
    }

    private static void AssertContainer(ItemStore store, ItemContainer actual, ItemContainerState saved)
    {
        Assert.That(actual.Id, Is.EqualTo(saved.Id));
        Assert.That(actual.Revision, Is.EqualTo(saved.Revision));
        Assert.That(actual.Capacity, Is.EqualTo(saved.Slots.Count));
        for (var i = 0; i < saved.Slots.Count; i++)
        {
            Assert.That(actual.Slots[i], Is.EqualTo(saved.Slots[i]?.Id));
            if (saved.Slots[i] is not { } expected) continue;
            var item = store.GetItem(expected.Id);
            Assert.That(item.DefinitionId, Is.EqualTo(expected.DefinitionId));
            Assert.That(item.Count, Is.EqualTo(expected.Count));
            Assert.That(store.GetLocation(item.Id), Is.EqualTo(new ItemLocation(saved.Id, i)));
        }
    }
}
