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
    [Test]
    public void ProductionStartupCanMoveAndActivateEquippedAbility()
    {
        StarterLoadout.ValidateDefinitions(GameplayDefinitions.Current.Items);
        using var initial = new InventoryController();
        var world = new GameWorld { AuthoritativeItems = true, InitialInventory = initial.Snapshot, InitializeLoadout = StarterLoadout.Equip };
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
            Assert.That(ship.Equipment.Snapshot.UsedWeaponSlots, Is.EqualTo(6));
            Assert.That(ship.Equipment.Snapshot.FreeWeaponSlots, Is.EqualTo(3));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_fixture")]!.DefinitionId, Is.EqualTo("mock_standard_weapon_fixture"));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_1")]!.Definition.CategoryId, Is.EqualTo("light_weapon"));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_2")]!.Definition.CategoryId, Is.EqualTo("medium_weapon"));
            Assert.That(ship.Equipment.Snapshot.Slots[ship.Equipment.FindSlot("weapon_3")]!.Definition.CategoryId, Is.EqualTo("heavy_weapon"));
            for (var weapon = 4; weapon <= 9; weapon++)
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
        var world = new GameWorld { AuthoritativeItems = true, InitialInventory = initial.Snapshot, InitializeLoadout = StarterLoadout.Equip };
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
            world.InitializePlayerLoadout(1, initial.Snapshot);
            StarterLoadout.Equip(ship.Loadout, owner.Inventory);
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

