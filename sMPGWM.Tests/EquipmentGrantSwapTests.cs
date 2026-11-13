using Godot;
using sMPGWM.Scripts.EquipmentSystem;
using sMPGWM.Scripts.ItemSystem; // TEST-SUPPORT: GRANT-SWAP-001 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
// TEST-SUPPORT: GRANT-SWAP-002 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
// TEST-SUPPORT: GRANT-SWAP-003 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
using sMPGWM.Scripts.WorldSystem;
using GameWorld = sMPGWM.Scripts.WorldSystem.GameWorld; // TEST-SUPPORT: GRANT-SWAP-004 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources

namespace sMPGWM.Tests; // TEST-SUPPORT: GRANT-SWAP-005 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources

[Category("Godot")] // TEST-SUPPORT: GRANT-SWAP-006 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
public sealed class EquipmentGrantSwapTests // TEST-SUPPORT: GRANT-SWAP-007 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
{
    [Test] // TEST-SUPPORT: GRANT-SWAP-008 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
    public void SwappingGrantBearingSlotsRetainsBothSources() // TEST-SUPPORT: GRANT-SWAP-009 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
    {
        var world = new GameWorld(); // TEST-SUPPORT: GRANT-SWAP-010 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: GRANT-SWAP-011 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        try // TEST-SUPPORT: GRANT-SWAP-013 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: GRANT-SWAP-014 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var items = world.Players[1]; // TEST-SUPPORT: GRANT-SWAP-015 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var grants = world.Players[1].ControlledShip!.Runtime.Grants; // TEST-SUPPORT: GRANT-SWAP-016 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var weapon = new ItemDefinition("mock_standard_light_weapon", "Fixture weapon", ItemRegistry.GetCategoryById("light_weapon").DisplayName, "", false, 1, null!) // TEST-SUPPORT: GRANT-SWAP-017 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                { CategoryId = "light_weapon", AbilityIds = Array.AsReadOnly(new[] { "shieldCapacityBoost" }) }; // TEST-SUPPORT: GRANT-SWAP-018 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var first = new ItemEntity(ItemEntityId.Create(), weapon, 1); // TEST-SUPPORT: GRANT-SWAP-019 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var second = new ItemEntity(ItemEntityId.Create(), weapon, 1); // TEST-SUPPORT: GRANT-SWAP-020 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var equipment = new ItemEntity?[items.ControlledShip!.Equipment.Definitions.Count]; // TEST-SUPPORT: GRANT-SWAP-021 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_fixture")] = new(ItemEntityId.Create(), ItemRegistry.GetById("mock_standard_weapon_fixture"), 1); // TEST-SUPPORT: WEAPON-GRANT-FIXTURE-001 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_1")] = first; // TEST-SUPPORT: GRANT-SWAP-022 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            equipment[items.ControlledShip!.Equipment.FindSlot("weapon_2")] = second; // TEST-SUPPORT: GRANT-SWAP-023 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(items.Inventory.Items.Confirm(items.Inventory.Container, new ItemEntity?[items.Inventory.Capacity], 1, // TEST-SUPPORT: GRANT-SWAP-024 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                items.ControlledShip!.Equipment.Container, equipment, 1), Is.True); // TEST-SUPPORT: GRANT-SWAP-025 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-026 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-027 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(items.Transfer(new(0, ItemCellAddress.Equipment("weapon_1"), first.Id, // TEST-SUPPORT: GRANT-SWAP-028 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                ItemCellAddress.Equipment("weapon_2"), second.Id, items.Inventory.Revision, items.ControlledShip!.Equipment.Revision)), // TEST-SUPPORT: GRANT-SWAP-029 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: GRANT-SWAP-030 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-031 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-032 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        }
        finally // TEST-SUPPORT: GRANT-SWAP-033 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        {
            world.Free(); // TEST-SUPPORT: GRANT-SWAP-035 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        }
    }
}

