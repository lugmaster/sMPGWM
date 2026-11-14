using Godot;
using sMPGWM.Scripts.AbilitySystem;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.GameplayDataSystem;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.InventorySystem;
using sMPGWM.Scripts.ItemSystem.Hotbar;
using sMPGWM.Scripts.ShipSystem;
using sMPGWM.Scripts.WorldSystem;

namespace sMPGWM.Tests.GamePlaySystem;

[Category("Godot")]
public sealed class InitialPlayerStateTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void HostPresetAppliesOnceToEachJoiningPlayer(bool development)
    {
        var world = new GameWorld { AuthoritativeItems = true,
            InitializeLoadout = development ? DevelopmentLoadout.Equip : StartLoadout.Equip };
        try
        {
            for (var peer = 1; peer <= 3; peer++)
            {
                world.ApproveSpawn(peer, PlayerStartupData.Create());
                var owner = world.Players[peer];
                var snapshot = owner.Inventory.Snapshot;
                if (development) AssertDevelopmentInventory(snapshot);
                else Assert.That(snapshot.Occupied, Is.Empty);
                var ship = owner.ControlledShip!;
                var equipment = ship.Equipment.Snapshot;
                Assert.That(equipment.UsedWeaponSlots, Is.EqualTo(development ? 6 : 1));
                Assert.That(equipment.Slots[ship.Equipment.FindSlot("weapon_1")]!.DefinitionId, Is.EqualTo("vulcan_mk1"));
                Assert.That(equipment.Slots[ship.Equipment.FindSlot("weapon_2")]?.DefinitionId,
                    Is.EqualTo(development ? "mock_standard_medium_weapon" : null));
                Assert.That(equipment.Slots[ship.Equipment.FindSlot("weapon_3")]?.DefinitionId,
                    Is.EqualTo(development ? "mock_standard_heavy_weapon" : null));
                ship.Runtime.SetShieldCurrent(37);
                world.InitializePlayerLoadout(peer);
                Assert.That(owner.Inventory.Snapshot, Is.SameAs(snapshot));
                Assert.That(ship.Equipment.Snapshot, Is.SameAs(equipment));
                var spare = world.SpawnEntity(new EntitySpawnRecord(Guid.NewGuid().ToString("N"), "fighter", default, 0, default));
                owner.AssignShip(spare);
                world.InitializePlayerLoadout(peer);
                Assert.That(owner.Inventory.Snapshot, Is.SameAs(snapshot));
                foreach (var item in spare.Equipment.Snapshot.Slots) Assert.That(item, Is.Null);
                owner.AssignShip(ship);
                Assert.That(ship.Equipment.Snapshot, Is.SameAs(equipment));
                Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(37));
            }
        }
        finally { world.Clear(); world.Free(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RestorationPreservesLoadoutWhenPresetChanges(bool development)
    {
        var source = new GameWorld { AuthoritativeItems = true,
            InitializeLoadout = development ? DevelopmentLoadout.Equip : StartLoadout.Equip };
        var restored = new GameWorld { AuthoritativeItems = true,
            InitializeLoadout = development ? StartLoadout.Equip : DevelopmentLoadout.Equip };
        try
        {
            var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
            host.AddChild(source);
            host.AddChild(restored);
            source.ApproveSpawn(1, PlayerStartupData.Create());
            source.Players[1].ControlledShip!.Runtime.SetShieldCurrent(37);
            var saved = source.CaptureState(0, 0);
            restored.RestoreState(saved);
            restored.InitializePlayerLoadout(1);
            var owner = restored.Players[1];
            Assert.That(owner.Inventory.Snapshot.Slots, Is.EqualTo(saved.Players[0].Inventory.Slots));
            Assert.That(owner.ControlledShip!.Equipment.Snapshot.Slots, Is.EqualTo(saved.Ships[0].Equipment.Slots));
            Assert.That(owner.ControlledShip.Runtime.Shield.Current, Is.EqualTo(37));
        }
        finally { source.Clear(); source.Free(); restored.Clear(); restored.Free(); }
    }

    private static void AssertDevelopmentInventory(InventorySnapshot snapshot)
    {
        Assert.That(snapshot.Occupied.Count, Is.EqualTo(32));
        AssertStacks(snapshot, "mock_standard_engine", 1);
        AssertStacks(snapshot, "mock_advanced_engine", 1, 1);
        AssertStacks(snapshot, "mock_standard_hull", 1);
        AssertStacks(snapshot, "mock_advanced_hull", 1, 1);
        AssertStacks(snapshot, "mock_standard_shield_generator", 1);
        AssertStacks(snapshot, "mock_advanced_shield_generator", 1, 1);
        AssertStacks(snapshot, "mock_standard_reactor", 1);
        AssertStacks(snapshot, "mock_advanced_reactor", 1, 1);
        AssertStacks(snapshot, "mock_standard_weapon_fixture", 1);
        AssertStacks(snapshot, "vulcan_mk1", 1);
        AssertStacks(snapshot, "laser_mk1", 1, 1);
        AssertStacks(snapshot, "mock_standard_medium_weapon", 1);
        AssertStacks(snapshot, "mock_advanced_medium_weapon", 1, 1);
        AssertStacks(snapshot, "mock_standard_heavy_weapon", 1);
        AssertStacks(snapshot, "mock_advanced_heavy_weapon", 1, 1);
        AssertStacks(snapshot, "mock_standard_consumable", 10, 10, 7);
        AssertStacks(snapshot, "mock_advanced_consumable", 1000, 250);
        AssertStacks(snapshot, "mock_standard_resource", 10, 10, 7);
        AssertStacks(snapshot, "mock_advanced_resource", 1000, 250);
    }

    private static void AssertStacks(InventorySnapshot inventory, string definition, params int[] expected)
    {
        var counts = new List<int>();
        foreach (var item in inventory.Slots)
            if (item?.DefinitionId == definition) counts.Add(item.Count);
        Assert.That(counts, Is.EquivalentTo(expected), definition);
    }

    [Test]
    public void ProductionStartupCanMoveAndActivateEquippedAbility()
    {
        StartLoadout.ValidateDefinitions(GameplayDefinitions.Current.Items);
        using var initial = new InventoryController();
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = StartLoadout.Equip };
        try
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
            var record = world.ApproveSpawn(1, PlayerStartupData.Create());
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            Assert.That(ship.Runtime.MovementAvailable, Is.True);
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(ship.Runtime.Shield.Capacity));
            Assert.That(ship.Runtime.Energy.Current, Is.Zero);
            Assert.That(owner.Inventory.Snapshot.Occupied, Is.Empty);
            Assert.That(ship.Equipment.Snapshot.MaxWeaponSlots, Is.EqualTo(9));
            Assert.That(ship.Equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(1));
            Assert.That(ship.Equipment.Snapshot.FreeWeaponSlots, Is.EqualTo(8));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_fixture")]!.DefinitionId, Is.EqualTo("mock_standard_weapon_fixture"));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!.Definition.CategoryId, Is.EqualTo("light_weapon"));
            for (var weapon = 2; weapon <= 9; weapon++)
                Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_" + weapon)], Is.Null);
            for (var index = 0; index < ship.Equipment.Definitions.Count; index++)
                if (!ship.Equipment.Definitions[index].IsWeaponSlot) Assert.That(ship.Equipment.Snapshot.Slots[index], Is.Not.Null);
            using var transfers = new InventoryEquipmentTransferService(owner.Inventory, ship.Equipment);
            using var abilities = new AbilityRuntime(GameplayDefinitions.Current);
            using var hotbar = new ItemHotbarController(owner.Inventory, ship.Equipment, transfers)
            { SupportsAbility = id => abilities.Definitions.Abilities.IsSupported(id, out _) };
            hotbar.InitializeEquipmentAbilities();
            Assert.That(hotbar.Entries[0], Is.TypeOf<EquipmentAbilityHotbarBinding>());
            Assert.That(hotbar.Resolve(0).Available, Is.True);
            var activated = false;
            Assert.That(hotbar.Activate(0, _ => Assert.Fail("Unexpected action binding."), request =>
            {
                var binding = (EquipmentAbilityHotbarBinding)request.Binding;
                Assert.That(abilities.Activate(binding.AbilityId, new(ship.Runtime, ship.Runtime, 0), 0), Is.EqualTo(AbilityActivationResult.Accepted));
                activated = true;
            }), Is.EqualTo(HotbarResult.Success));
            Assert.That(activated, Is.True);
            ship.SetInput(new ShipInputState(Vector2.Right, 0, false, false, false, true));
            ship.Simulate(1d / 60);
            Assert.That(ship.Velocity.Length(), Is.GreaterThan(0));
            Assert.That(world.ApproveSpawn(1, PlayerStartupData.Create()), Is.EqualTo(record));
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void RepeatedInitializationPreservesItemsResourcesAndUserHotbar()
    {
        using var initial = new InventoryController();
        var world = new GameWorld { AuthoritativeItems = true, InitializeLoadout = StartLoadout.Equip };
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var owner = world.Players[1];
            var ship = owner.ControlledShip!;
            var equipment = ship.Equipment.Snapshot;
            var inventory = owner.Inventory.Snapshot;
            ship.Runtime.SetShieldCurrent(37);
            using var transfers = new InventoryEquipmentTransferService(owner.Inventory, ship.Equipment);
            using var hotbar = new ItemHotbarController(owner.Inventory, ship.Equipment, transfers);
            hotbar.InitializeEquipmentAbilities();
            hotbar.SetEditSession(123);
            Assert.That(hotbar.Clear(123, hotbar.Revision, 0), Is.EqualTo(HotbarResult.Success));
            var hotbarRevision = hotbar.Revision;
            world.InitializePlayerLoadout(1);
            StartLoadout.Equip(ship.Loadout, owner.Inventory);
            ship.Loadout.InitializeResources();
            hotbar.InitializeEquipmentAbilities();
            Assert.That(ship.Runtime.Shield.Current, Is.EqualTo(37));
            Assert.That(ship.Equipment.Snapshot, Is.SameAs(equipment));
            Assert.That(owner.Inventory.Snapshot, Is.SameAs(inventory));
            Assert.That(hotbar.Entries[0], Is.Null);
            Assert.That(hotbar.Revision, Is.EqualTo(hotbarRevision));
        }
        finally { world.Clear(); world.Free(); }
    }

    [Test]
    public void ExplicitEmptyWorldDoesNotGainStarterEquipment()
    {
        var world = new GameWorld { AuthoritativeItems = true };
        try
        {
            world.ApproveSpawn(1, PlayerStartupData.Create());
            var owner = world.Players[1];
            Assert.That(owner.Inventory.Snapshot.Occupied, Is.Empty);
            foreach (var item in owner.ControlledShip!.Equipment.Snapshot.Slots) Assert.That(item, Is.Null);
            Assert.That(owner.ControlledShip.Runtime.MovementAvailable, Is.False);
        }
        finally { world.Clear(); world.Free(); }
    }
}

