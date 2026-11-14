using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem;
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld;

namespace sMPGWM.Tests;

[Category("Godot")]
public sealed class EquipmentGrantSwapTests
{
    [Test]
    public void SwappingGrantBearingSlotsRetainsBothSources()
    {
        var world = new GameWorld();
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world);
        try
        {
            world.ApproveSpawn(1, new("fighter"));
            var items = world.Players[1];
            var grants = world.Players[1].ControlledShip!.Runtime.Grants;
            var weapon = (WeaponItemDefinition)ItemRegistry.GetById("mock_standard_light_weapon") with
                { AbilityIds = Array.AsReadOnly(new[] { "shieldCapacityBoost" }) };
            var first = new ItemEntity(ItemEntityId.Create(), weapon, 1);
            var second = new ItemEntity(ItemEntityId.Create(), weapon, 1);
            var equipment = new ItemEntity?[items.ControlledShip!.Equipment.Definitions.Count];
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_fixture")] = new(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_weapon_fixture"), 1);
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_1")] = first;
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_2")] = second;
            Assert.That(items.Inventory.Items.Confirm(items.Inventory.Container, new ItemEntity?[items.Inventory.Capacity], 1,
                items.ControlledShip!.Equipment.Container, equipment, 1), Is.True);
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True);
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True);
            Assert.That(items.Transfer(new(0, ItemCellAddress.Equipment("weapon_1"), first.Id,
                ItemCellAddress.Equipment("weapon_2"), second.Id, items.Inventory.Revision, items.ControlledShip!.Equipment.Revision)),
                Is.EqualTo(TransferResult.Success));
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True);
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True);
        }
        finally
        {
            world.Free();
        }
    }
}

