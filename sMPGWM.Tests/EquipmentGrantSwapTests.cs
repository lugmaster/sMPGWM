using Godot; // TEST-SUPPORT: GRANT-SWAP-001 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
using sMPGWM.Scripts.Items; // TEST-SUPPORT: GRANT-SWAP-002 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
using sMPGWM.Scripts.Items.Equipment; // TEST-SUPPORT: GRANT-SWAP-003 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
using sMPGWM.Scripts.World; // TEST-SUPPORT: GRANT-SWAP-004 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources

namespace sMPGWM.Tests; // TEST-SUPPORT: GRANT-SWAP-005 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources

[Category("Godot")] // TEST-SUPPORT: GRANT-SWAP-006 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
public sealed class EquipmentGrantSwapTests // TEST-SUPPORT: GRANT-SWAP-007 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
{
    [Test] // TEST-SUPPORT: GRANT-SWAP-008 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
    public void SwappingGrantBearingSlotsRetainsBothSources() // TEST-SUPPORT: GRANT-SWAP-009 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
    {
        var world = new GameWorld(); // TEST-SUPPORT: GRANT-SWAP-010 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests").AddChild(world); // TEST-SUPPORT: GRANT-SWAP-011 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        using var items = new PlayerItemState(); // TEST-SUPPORT: GRANT-SWAP-012 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        try // TEST-SUPPORT: GRANT-SWAP-013 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        {
            world.ApproveSpawn(1, new("fighter")); // TEST-SUPPORT: GRANT-SWAP-014 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            items.Bind(world.Players[1]); // TEST-SUPPORT: GRANT-SWAP-015 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var grants = world.Players[1].ControlledShip!.Gameplay.Grants; // TEST-SUPPORT: GRANT-SWAP-016 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var weapon = new ItemDefinition("mock_standard_light_weapon", "Fixture weapon", ItemRegistry.GetCategoryById("light_weapon").DisplayName, "", false, 1, null!) // TEST-SUPPORT: GRANT-SWAP-017 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                { AbilityIds = Array.AsReadOnly(new[] { "shieldCapacityBoost" }) }; // TEST-SUPPORT: GRANT-SWAP-018 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var first = new ItemEntity(ItemEntityId.Create(), weapon, 1); // TEST-SUPPORT: GRANT-SWAP-019 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var second = new ItemEntity(ItemEntityId.Create(), weapon, 1); // TEST-SUPPORT: GRANT-SWAP-020 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            var equipment = new ItemEntity?[items.Equipment.Definitions.Count]; // TEST-SUPPORT: GRANT-SWAP-021 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            equipment[items.Equipment.FindSlot("light_weapon_1")] = first; // TEST-SUPPORT: GRANT-SWAP-022 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            equipment[items.Equipment.FindSlot("light_weapon_2")] = second; // TEST-SUPPORT: GRANT-SWAP-023 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(items.Store.Confirm(items.Inventory.Container, new ItemEntity?[items.Inventory.Capacity], 1, // TEST-SUPPORT: GRANT-SWAP-024 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                items.Equipment.Container, equipment, 1), Is.True); // TEST-SUPPORT: GRANT-SWAP-025 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-026 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-027 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(items.Transfer(new(0, ItemCellAddress.Equipment("light_weapon_1"), first.Id, // TEST-SUPPORT: GRANT-SWAP-028 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                ItemCellAddress.Equipment("light_weapon_2"), second.Id, items.Inventory.Revision, items.Equipment.Revision)), // TEST-SUPPORT: GRANT-SWAP-029 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
                Is.EqualTo(TransferResult.Success)); // TEST-SUPPORT: GRANT-SWAP-030 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + first.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-031 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            Assert.That(grants.ContainsSource("item:" + second.Id, "shieldCapacityBoost"), Is.True); // TEST-SUPPORT: GRANT-SWAP-032 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        }
        finally // TEST-SUPPORT: GRANT-SWAP-033 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        {
            items.Dispose(); // TEST-SUPPORT: GRANT-SWAP-034 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
            world.Free(); // TEST-SUPPORT: GRANT-SWAP-035 | Tests: sMPGWM.Tests.EquipmentGrantSwapTests.SwappingGrantBearingSlotsRetainsBothSources
        }
    }
}

