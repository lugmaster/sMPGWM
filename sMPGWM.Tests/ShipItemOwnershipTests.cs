using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.NetworkSystem;
using sMPGWM.Scripts.AbilitySystem;
using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;
namespace sMPGWM.Tests;
[Category("Godot")]
public sealed class ShipItemOwnershipTests
{
    [Test]
    public void InvalidCombinedLoadoutLeavesBothContainersAndGameplayUnchanged()
    {
        var world = new GameWorld { AuthoritativeItems = true };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var player = world.Players[1];
            var ship = player.ControlledShip!;
            var definition = (EngineItemDefinition)GameplayDefinitions.Current.Items.GetById("mock_standard_engine") with
                { Mass = float.MaxValue };
            var item = new ItemEntity(ItemEntityId.Create(), definition, 1);
            var slots = new ItemEntity?[player.Inventory.Capacity];
            slots[0] = item;
            Assert.That(world.Items.Confirm(new[] { new ItemContainerConfirmation(player.Inventory.Container, slots, 1) }), Is.True);
            var inventory = player.Inventory.Snapshot;
            var equipment = ship.Equipment.Snapshot;
            var stats = ship.Stats;
            var resources = ship.Runtime.Shield;
            var revision = ship.Runtime.Revision;
            var request = new TransferRequest(0, ItemCellAddress.Inventory(0), item.Id, ItemCellAddress.Equipment("engine"), null,
                player.Inventory.Revision, ship.Equipment.Revision);
            Assert.That(player.Transfer(request), Is.EqualTo(TransferResult.IncompatiblePlacement));
            Assert.That(player.Inventory.Snapshot, Is.SameAs(inventory));
            Assert.That(ship.Equipment.Snapshot, Is.SameAs(equipment));
            Assert.That(ship.Stats, Is.EqualTo(stats));
            Assert.That(ship.Runtime.Shield, Is.EqualTo(resources));
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            Assert.That(world.Items.GetLocation(item.Id).ContainerId, Is.EqualTo(player.Inventory.Container.Id));
        }
        finally { world.Free(); }
    }
    [Test]
    public void ConfirmationRejectsRetainedDuplicatesAndEqualRevisionConflictsAtomically()
    {
        var store = new ItemStore();
        using var first = new InventoryController(store);
        using var retained = new InventoryController(store);
        store.CreateItems(first.Container, "mock_standard_engine", 1);
        store.CreateItems(retained.Container, "mock_standard_engine", 1);
        var before = first.Snapshot;
        var conflicting = new ItemEntity?[first.Capacity];
        Assert.That(store.Confirm(new[] { new ItemContainerConfirmation(first.Container, conflicting, first.Revision) }), Is.False);
        conflicting[0] = retained.Snapshot.Slots[0];
        Assert.That(store.Confirm(new[] { new ItemContainerConfirmation(first.Container, conflicting, first.Revision + 1) }), Is.False);
        Assert.That(first.Snapshot, Is.SameAs(before));
        Assert.That(store.GetLocation(retained.Snapshot.Slots[0]!.Id).ContainerId, Is.EqualTo(retained.Container.Id));
        var candidate = new ItemEntity?[first.Capacity];
        candidate[0] = before.Slots[0];
        Assert.That(store.Confirm(new[] { new ItemContainerConfirmation(first.Container, candidate, first.Revision) }), Is.True);
    }
    [Test]
    public void FailedSpawnDestroysPrivateInventoryAndEquipmentBeforeRestart()
    {
        var store = new ItemStore();
        using var initial = new InventoryController(store);
        store.CreateItems(initial.Container, "mock_standard_engine", 1);
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = sMPGWM.Scripts.GamePlaySystem.StartLoadout.Equip };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        ShipController? failedShip = null;
        ItemEntityId failedItem = default;
        void Reject(ShipController ship)
        {
            failedShip = ship;
            failedItem = ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("engine")]!.Id;
            throw new InvalidOperationException("Reject completed composition.");
        }
        world.EntitySpawned += Reject;
        try
        {
            Assert.Throws<InvalidOperationException>(() => world.ApproveSpawn(1, new("fighter")));
            Assert.That(world.Players, Is.Empty);
            Assert.That(world.Entities, Is.Empty);
            Assert.That(GodotObject.IsInstanceValid(failedShip), Is.False);
            Assert.Throws<KeyNotFoundException>(() => world.Items.GetItem(failedItem));
            world.EntitySpawned -= Reject;
            world.ApproveSpawn(1, new("fighter"));
            Assert.That(world.Players[1].ControlledShip!.Runtime.Shield.Current, Is.GreaterThan(0));
            Assert.That(world.Players[1].Inventory.Items, Is.SameAs(world.Items));
        }
        finally { world.Free(); }
    }

    [TestCase(5, false)]
    [TestCase(5, true)]
    [TestCase(10, false)]
    [TestCase(10, true)]
    public void TransferCostAtSessionScale(int players, bool shared)
    {
        var common = new ItemStore();
        var inventories = new InventoryController[players];
        var equipment = new EquipmentController[players];
        try
        {
            for (var i = 0; i < players; i++)
            {
                var store = shared ? common : new ItemStore();
                inventories[i] = new(store);
                equipment[i] = new(store);
                Assert.That(store.CreateItems(inventories[i].Container, "mock_standard_engine", 128), Is.EqualTo(InventoryResult.Success));
            }
            var inventory = inventories[0];
            var installed = equipment[0];
            var engine = installed.FindSlot("engine");
            var id = inventory.Snapshot.Slots[0]!.Id;
            var from = new ItemLocation(inventory.Container.Id, 0);
            var to = new ItemLocation(installed.Container.Id, engine);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var gen0 = GC.CollectionCount(0);
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < 200; i++)
            {
                if (inventory.Items.TransferItem(id, from, to, inventory.Revision, installed.Revision) != TransferResult.Success ||
                    inventory.Items.TransferItem(id, to, from, installed.Revision, inventory.Revision) != TransferResult.Success)
                    Assert.Fail("Transfer benchmark command failed.");
            }
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
            var collections = GC.CollectionCount(0) - gen0;
            TestContext.Out.WriteLine($"Transfer cost: players={players} shared={shared} transfers=400 ms={elapsed:F3} bytes={bytes} gen0={collections}");
            Assert.That(inventory.Items.GetLocation(id), Is.EqualTo(from));
        }
        finally
        {
            for (var i = 0; i < players; i++) { equipment[i]?.Dispose(); inventories[i]?.Dispose(); }
        }
    }
    [Test]
    public void PrivateBaselinesRejectConflictingScopesAndOldControlAndSupportNoShip()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        host.Store.CreateItems(host.Inventory.Container, "mock_standard_engine", 1);
        var baseline = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(baseline, client.Player), Is.True);
        var snapshot = client.Inventory.Snapshot;
        var wrongInventory = baseline.Duplicate(true);
        wrongInventory[3] = baseline[3].AsInt64() + 100000;
        Assert.That(ItemSnapshotCodec.Apply(wrongInventory, client.Player), Is.False);
        var wrongEquipment = baseline.Duplicate(true);
        wrongEquipment[4] = baseline[4].AsInt64() + 100000;
        Assert.That(ItemSnapshotCodec.Apply(wrongEquipment, client.Player), Is.False);
        Assert.That(client.Inventory.Snapshot, Is.SameAs(snapshot));
        client.Player.ControlledShip!.Loadout.EvictReplica();
        host.Player.AssignShip(null);
        client.Player.AssignShip(null);
        Assert.That(ItemSnapshotCodec.IsOldBinding(baseline, client.Player), Is.True);
        Assert.That(ItemSnapshotCodec.Apply(baseline, client.Player), Is.False);
        host.Store.CreateItems(host.Inventory.Container, "mock_standard_consumable", 1);
        var inventoryOnly = ItemSnapshotCodec.Encode(host.Player);
        Assert.That(ItemSnapshotCodec.Apply(inventoryOnly, client.Player), Is.True);
        Assert.That(client.Inventory.Snapshot.Slots[1]!.DefinitionId, Is.EqualTo("mock_standard_consumable"));
        Assert.That(client.Player.ControlledShip, Is.Null);
    }
    [Test]
    public void ReplacementOverflowWithLiveEffectPreservesItemsGrantsStatsAndResources()
    {
        var world = new GameWorld { AuthoritativeItems = true };
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        using var abilities = new sMPGWM.Scripts.AbilitySystem.AbilityRuntime(GameplayTestData.Load());
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var player = world.Players[1];
            var ship = player.ControlledShip!;
            world.Items.CreateItems(player.Inventory.Container, "mock_standard_shield_generator", 1);
            var original = player.Inventory.Snapshot.Slots[0]!;
            var address = ItemCellAddress.Equipment("shield_generator");
            Assert.That(player.Transfer(new(0, ItemCellAddress.Inventory(0), original.Id, address, null,
                player.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
            ship.Runtime.SetShieldCurrent(20);
            Assert.That(ship.Runtime.ApplyBehaviour(new SupportedBehaviour(abilities.Definitions.Behaviours.GetById("shieldCapacityBoost"), 1800, 0, .5), 1, 1, out _),
                Is.True);
            var definition = (ShieldGeneratorItemDefinition)GameplayDefinitions.Current.Items.GetById("mock_standard_shield_generator") with
                { ShieldCapacity = double.MaxValue / 1.2, RechargeTime = 5000, DownTime = 3000, AbilityIds = new[] { "shieldCapacityBoost" } };
            var candidate = new ItemEntity(ItemEntityId.Create(), definition, 1);
            var slots = new ItemEntity?[player.Inventory.Capacity];
            slots[0] = candidate;
            Assert.That(world.Items.Confirm(new[] { new ItemContainerConfirmation(player.Inventory.Container, slots, player.Inventory.Revision + 1) }), Is.True);
            var inventory = player.Inventory.Snapshot;
            var equipment = ship.Equipment.Snapshot;
            var stats = ship.Stats;
            var shield = ship.Runtime.Shield;
            var energy = ship.Runtime.Energy;
            var behaviour = ship.Runtime.ActiveBehaviours[0];
            var revision = ship.Runtime.Revision;
            var notifications = 0;
            ship.Runtime.Changed += () => notifications++;
            Assert.That(player.Transfer(new(0, ItemCellAddress.Inventory(0), candidate.Id, address, original.Id,
                player.Inventory.Revision, ship.Equipment.Revision)), Is.EqualTo(TransferResult.IncompatiblePlacement));
            Assert.That(player.Inventory.Snapshot, Is.SameAs(inventory));
            Assert.That(ship.Equipment.Snapshot, Is.SameAs(equipment));
            Assert.That(ship.Stats, Is.EqualTo(stats));
            Assert.That(ship.Runtime.Shield, Is.EqualTo(shield));
            Assert.That(ship.Runtime.Energy, Is.EqualTo(energy));
            Assert.That(ship.Runtime.ActiveBehaviours[0], Is.EqualTo(behaviour));
            Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
            Assert.That(ship.Loadout.Provides("shield_generator", (long)original.Id.Value, "shieldCapacityBoost"), Is.True);
            Assert.That(ship.Loadout.Provides("shield_generator", (long)candidate.Id.Value, "shieldCapacityBoost"), Is.False);
            Assert.That(notifications, Is.Zero);
        }
        finally { world.Free(); }
    }
    [Test]
    public void EvictingPrivateEquipmentPreservesConfirmedPublicShipState()
    {
        using var host = new ShipItemTestContext();
        using var client = new ShipItemTestContext();
        host.Store.CreateItems(host.Inventory.Container, "mock_standard_shield_generator", 1);
        var generator = host.Inventory.Snapshot.Slots[0]!;
        Assert.That(host.Transfer(new(0, ItemCellAddress.Inventory(0), generator.Id, ItemCellAddress.Equipment("shield_generator"), null,
            host.Inventory.Revision, host.Equipment.Revision)), Is.EqualTo(TransferResult.Success));
        Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host.Player), client.Player), Is.True);
        var ship = client.Player.ControlledShip!;
        ship.Runtime.ApplyEquipment(new(0, 0, 0, 0, 0, 0, 0, 100, 5000, 3000, 0, 0, 0, false, false, true, false, false), 0);
        ship.Runtime.SetShieldCurrent(37);
        var stats = ship.Stats;
        var shield = ship.Runtime.Shield;
        var revision = ship.Runtime.Revision;
        ship.Loadout.EvictReplica();
        Assert.That(ship.Equipment.Revision, Is.Zero);
        Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("shield_generator")], Is.Null);
        Assert.That(ship.Runtime.Grants.ContainsSource("item:" + generator.Id, "shieldCapacityBoost"), Is.False);
        Assert.Throws<KeyNotFoundException>(() => client.Store.GetItem(generator.Id));
        Assert.That(ship.Stats, Is.EqualTo(stats));
        Assert.That(ship.Runtime.Shield, Is.EqualTo(shield));
        Assert.That(ship.Runtime.Revision, Is.EqualTo(revision));
        Assert.That(ItemSnapshotCodec.Apply(ItemSnapshotCodec.Encode(host.Player), client.Player), Is.True);
        Assert.That(ship.Loadout.Provides("shield_generator", (long)generator.Id.Value, "shieldCapacityBoost"), Is.True);
        Assert.That(ship.Runtime.Shield, Is.EqualTo(shield));
    }
}
